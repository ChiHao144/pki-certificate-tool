package com.pch.pkitool.service;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.util.CertificateUtil;
import java.io.ByteArrayInputStream;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.StandardCopyOption;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.NoSuchProviderException;
import java.security.Security;
import java.security.SignatureException;
import java.security.cert.CRLException;
import java.security.cert.CertificateException;
import java.security.cert.CertificateExpiredException;
import java.security.cert.CertificateFactory;
import java.security.cert.CertificateNotYetValidException;
import java.security.cert.X509Certificate;
import java.util.Date;
import org.bouncycastle.cert.ocsp.OCSPException;
import org.bouncycastle.jce.provider.BouncyCastleProvider;
import org.bouncycastle.operator.OperatorCreationException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import org.springframework.web.multipart.MultipartFile;

/**
 *
 * @author Chi Hao
 */
@Service
public class CertificateService {

    @Autowired
    private CRLService crlService;

    @Autowired
    private OCSPService ocspService;

    // Nạp BoucyCastle để xử lý các hàm mã hóa nâng cao trong OCSP
    public CertificateService() {
        // Đăng ký thư viện bảo mật BoucyCastle khi khởi chạy Service
        if (Security.getProvider(BouncyCastleProvider.PROVIDER_NAME) == null) {
            Security.addProvider(new BouncyCastleProvider());
        }
    }

    // Hàm xử lý 2 file nhị phân và trả về đối tượng kết quả
    public CertificateInfoResponse readCertificate(MultipartFile userFile, MultipartFile caFile) throws OperatorCreationException, OCSPException, CertificateException, IOException, FileNotFoundException, CRLException {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");

        // Chuyển đổi file userFile thành 1 mảng dữ liệu byte thuần
        byte[] userBytes = userFile.getBytes();
        X509Certificate cert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(userBytes));

        // Khởi tạo đối tượng response để làm thùng chứa dữ liệu phản hồi cuối cùng
        CertificateInfoResponse dto = new CertificateInfoResponse();
        dto.setSubject(cert.getSubjectX500Principal().toString());
        dto.setIssuer(cert.getIssuerX500Principal().toString());
        dto.setSerialNumber(cert.getSerialNumber().toString(16));
        dto.setValidFrom(cert.getNotBefore().toString());
        dto.setValidTo(cert.getNotAfter().toString());

        // Gọi hàm tĩnh từ Util để tự động bóc tách và định danh tên nhà mạng CA
        String provider = CertificateUtil.detectCAProvider(dto.getIssuer());
        dto.setCaProvider(provider);

        X509Certificate finalCaCert = null;

        // FALLBACK LOGIC: If manual CA is uploaded, use it immediately
        if (caFile != null && !caFile.isEmpty()) {
            byte[] caBytes = caFile.getBytes();
            finalCaCert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(caBytes));
        } else {
            // Automatically search inside internal TrustStore folder
            String caFileName = "TrustStore/" + provider.toUpperCase() + ".cer";
            File localCaFile = new File(caFileName);

            if (!localCaFile.exists()) {
                dto.setCaValidityStatus("NOT_FOUND_IN_TRUSTSTORE");
                return dto;
            }
            try (FileInputStream fis = new FileInputStream(localCaFile)) {
                finalCaCert = (X509Certificate) cf.generateCertificate(fis);
            }
        }

        // Gọi dịch vụ kiểm tra trạng thái qua CRL và OCSP
        Date now = new Date();

        // Kiểm tra toán học xem CA này có đúng là người ký ra User Cert không
        try {
            cert.verify(finalCaCert.getPublicKey());
        } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CertificateException e) {
            // Nếu sai chữ ký số 
            dto.setCaValidityStatus("MISMATCHED_CA_CHAIN");
            dto.setCrlStatus("SIGNATURE_VERIFICATION_FAILED");
            dto.setOcspStatus("SIGNATURE_VERIFICATION_FAILED");
            return dto; // Chặn đứng luồng xử lý tại đây
        }

        // Kiểm tra thời hạn hiệu lực chứng chỉ người dùng 
        try {
            cert.checkValidity(now);
            dto.setCertValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCertValidityStatus("EXPIRED");
        }

        // Kiểm tra thời hạn hiệu lực chứng chỉ CA cấp phát 
        try {
            finalCaCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED"); // Báo EXPIRED để WinForms kích hoạt luồng dự phòng
        }

        // Run validation services
        dto.setCrlStatus(crlService.checkCRL(cert, finalCaCert, cf, dto));
        dto.setOcspStatus(ocspService.checkOCSP(cert, finalCaCert));

        return dto;
    }

    public String saveCaToTrustStore(MultipartFile caFile) throws Exception {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        byte[] caBytes = caFile.getBytes();

        // Read the binary layout of the uploaded CA certificate
        X509Certificate caCert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(caBytes));

        // Extract the Subject DN instead of Issuer DN
        String subjectDN = caCert.getSubjectX500Principal().getName();

        // Reuse our keyword parser tool to extract short neat name (e.g. FPT, VNPT)
        String caProviderKey = CertificateUtil.detectCAProvider(subjectDN);

        if ("UNKNOWN_CA".equals(caProviderKey)) {
            // Fallback to a sanitized CN name if it is a completely new provider
            caProviderKey = "NEW_" + caCert.getSerialNumber().toString(16).toUpperCase();
        }

        // Ensure the internal TrustStore directory exists locally
        Path trustStoreFolder = Paths.get("TrustStore");
        if (!Files.exists(trustStoreFolder)) {
            Files.createDirectories(trustStoreFolder);
        }

        // Establish permanent destination path: TrustStore/PROVIDER.cer
        Path targetPath = trustStoreFolder.resolve(caProviderKey.toUpperCase() + ".cer");

        // Save or overwrite bytes onto local storage disk stream safely
        Files.copy(new ByteArrayInputStream(caBytes), targetPath, StandardCopyOption.REPLACE_EXISTING);

        return caProviderKey; // Return the string key to C# for visualization
    }
}
