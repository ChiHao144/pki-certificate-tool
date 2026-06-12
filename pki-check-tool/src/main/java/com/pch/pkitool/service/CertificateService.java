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

    // Hàm xử lý file nhị phân và trả về đối tượng kết quả
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
        
        // Xác định chế độ kiểm tra
        boolean manualMode = caFile != null && !caFile.isEmpty();

        /* ==========================================================
           MANUAL MODE
           ========================================================== */
        if (manualMode) {

            byte[] caBytes = caFile.getBytes();
            finalCaCert = (X509Certificate) cf.generateCertificate(
                    new ByteArrayInputStream(caBytes));

            boolean userIsCA = cert.getBasicConstraints() >= 0;
            boolean caIsCA = finalCaCert.getBasicConstraints() >= 0;

            System.out.println("USER BasicConstraints = "
                    + cert.getBasicConstraints());

            System.out.println("CA BasicConstraints = "
                    + finalCaCert.getBasicConstraints());

            // Người dùng chọn cùng một file cho cả 2 vị trí
            if (cert.getSerialNumber().equals(finalCaCert.getSerialNumber())) {

                dto.setCaValidityStatus("INVALID_CA_FILE_IS_SAME_AS_USER");
                return dto;
            }

            // USER + USER
            if (!userIsCA && !caIsCA) {

                dto.setCaValidityStatus("INVALID_CA_FILE_IS_USER");
                return dto;
            }

            // CA + CA
            if (userIsCA && caIsCA) {

                dto.setCaValidityStatus("INVALID_BOTH_FILES_ARE_CA");
                return dto;
            }

            // CA + USER (đảo vị trí)
            if (userIsCA && !caIsCA) {

                dto.setCaValidityStatus("INVALID_FILES_REVERSED");
                return dto;
            }

            // USER + CA
            // Hợp lệ -> tiếp tục xử lý phía dưới
        } /* ==========================================================
             AUTO MODE
             ========================================================== */ else {

            // Chặn nếu người dùng nạp CA vào ô User
            if (cert.getBasicConstraints() >= 0) {

                dto.setCertValidityStatus("INVALID_USER_FILE_IS_CA");
                return dto;
            }

            // Truy cập TrustStore quét CA phù hợp
            File trustStoreDir = new File("TrustStore");

            if (trustStoreDir.exists() && trustStoreDir.isDirectory()) {
                File[] files = trustStoreDir.listFiles((dir, name) -> name.toLowerCase().endsWith(".cer"));
                if (files != null) {
                    for (File file : files) {
                        try (FileInputStream fis = new FileInputStream(file)) {
                            X509Certificate candidateCa = (X509Certificate) cf.generateCertificate(fis);

                            // Kiểm tra tên Subject của CA phải trùng với Issuer của User Cert
                            if (candidateCa.getSubjectX500Principal().equals(cert.getIssuerX500Principal())) {
                                // Thử xác thực chữ ký candidateCa.getPublicKey
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

        // Chặn không cho lưu file User vào kho CA
        if (caCert.getBasicConstraints() == -1) {
            throw new IllegalArgumentException("Định dạng sai: Tệp tin nạp lên là chứng chỉ cá nhân/doanh nghiệp (User Cert), không phải là chứng chỉ nhà cấp phát CA!");
        }

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

        return finalFileName; // Trả về tên  file cert
    }
}
