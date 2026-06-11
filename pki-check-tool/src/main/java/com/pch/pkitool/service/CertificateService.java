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

        // Upload CA mới do bị cảnh báo hết hạn, không có CA trong kho lưu trữ
        if (caFile != null && !caFile.isEmpty()) {
            byte[] caBytes = caFile.getBytes();
            finalCaCert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(caBytes));
        } else {
            // BẰNG LUỒNG QUÉT ĐỘNG THÔNG MINH:
            File trustStoreDir = new File("TrustStore");

            if (trustStoreDir.exists() && trustStoreDir.isDirectory()) {
                File[] files = trustStoreDir.listFiles((dir, name) -> name.toLowerCase().endsWith(".cer"));
                if (files != null) {
                    for (File file : files) {
                        try (FileInputStream fis = new FileInputStream(file)) {
                            X509Certificate candidateCa = (X509Certificate) cf.generateCertificate(fis);

                            // KIỂM TRA ĐIỀU KIỆN 1: Tên Subject của CA phải trùng với Issuer của User Cert
                            if (candidateCa.getSubjectX500Principal().equals(cert.getIssuerX500Principal())) {

                                // KIỂM TRA ĐIỀU KIỆN 2: Thử xác thực chữ ký (Toán học mã hóa)
                                try {
                                    cert.verify(candidateCa.getPublicKey());
                                    // Nếu verify thành công không báo lỗi -> Đây chính là CA chuẩn xác cần tìm!
                                    finalCaCert = candidateCa;
                                    break;
                                } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CertificateException e) {
                                    // Sai thuật toán hoặc sai cặp khóa, tiếp tục loop thử file CA khác
                                }
                            }
                        } catch (Exception e) {
                            // File lỗi hoặc lỗi đọc, bỏ qua thử file tiếp theo
                        }
                    }
                }
            }

            // Kiểm tra xem cuối cùng có tìm được CA nào không
            if (finalCaCert == null) {
                dto.setCaValidityStatus("NOT_FOUND_IN_TRUSTSTORE");
                return dto;
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

        dto.setCrlStatus(crlService.checkCRL(cert, finalCaCert, cf, dto));
        dto.setOcspStatus(ocspService.checkOCSP(cert, finalCaCert));

        return dto;
    }

    // Lưu CA mới vào kho lưu trữ
    public String saveCaToTrustStore(MultipartFile caFile) throws Exception {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        byte[] caBytes = caFile.getBytes();

        X509Certificate caCert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(caBytes));
        String subjectDN = caCert.getSubjectX500Principal().getName();

        // Gọi detectCAProvider từ Util
        String caProviderKey = CertificateUtil.detectCAProvider(subjectDN);

        if ("UNKNOWN_CA".equals(caProviderKey)) {
            // Không lấy được thì tạo file bằng đầu bằng NEW_số serial
            caProviderKey = "NEW_" + caCert.getSerialNumber().toString(16).toUpperCase();
        }

        // Trỏ tới đường dẫn local hiện tại
        Path trustStoreFolder = Paths.get("TrustStore");
        if (!Files.exists(trustStoreFolder)) {
            Files.createDirectories(trustStoreFolder);
        }

        // Lấy tên thuật toán ký (Vd: SHA256withRSA, SHA1withRSA)
        String sigAlgName = caCert.getSigAlgName().replace("with", "_").toUpperCase();

        // Lấy 4 số cuối của Serial Number để tránh trùng danh mục
        String serialStr = caCert.getSerialNumber().toString(16).toUpperCase();
        String serialSuffix = serialStr.length() > 4 ? serialStr.substring(serialStr.length() - 4) : serialStr;

        String finalFileName = String.format("%s_%s_%s.cer", caProviderKey.toUpperCase(), sigAlgName, serialSuffix);
        Path targetPath = trustStoreFolder.resolve(finalFileName);

        // Lưu file dưới dạng CaProvider.cer vào TrustStore
        // Lưu và ghi lên ổ cứng của máy
        Files.copy(new ByteArrayInputStream(caBytes), targetPath, StandardCopyOption.REPLACE_EXISTING);

        return caProviderKey; // Trả về chuỗi làm khóa
    }
}
