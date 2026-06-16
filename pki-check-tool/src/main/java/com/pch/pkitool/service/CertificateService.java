package com.pch.pkitool.service;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.util.CertificateUtil;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.NoSuchProviderException;
import java.security.Security;
import java.security.SignatureException;
import java.security.cert.CertificateException;
import java.security.cert.CertificateExpiredException;
import java.security.cert.CertificateFactory;
import java.security.cert.CertificateNotYetValidException;
import java.security.cert.X509Certificate;
import java.util.Date;
import org.bouncycastle.jce.provider.BouncyCastleProvider;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

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
    
    // Hàm bóc tách, đối sánh toán học và kiểm tra trạng thái mạng của cặp file ca.cer và user.cer theo thư mục
    public CertificateInfoResponse processCaAndUserFromFolder(String caName) throws Exception {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        
        // 1. Định vị đường dẫn vật lý chính xác tới 2 file nằm trong thư mục DataCrlOcsp/{caName}
        File userFileSource = new File("DataCrlOcsp/" + caName + "/user.cer");
        File caFileSource = new File("DataCrlOcsp/" + caName + "/ca.cer");

        // Kiểm tra xem sự tồn tại vật lý của cả 2 file
        if (!userFileSource.exists() || !caFileSource.exists()) {
            throw new FileNotFoundException("Thiếu file cấu trúc chứng chỉ chuẩn.");
        }

        // 2. Nạp file user.cer từ ổ cứng lên RAM và phân dịch thành đối tượng cấu trúc X509
        X509Certificate cert;
        try (FileInputStream fisUser = new FileInputStream(userFileSource)) {
            cert = (X509Certificate) cf.generateCertificate(fisUser);
        }

        // 3. Nạp file ca.cer từ ổ cứng lên RAM và phân dịch thành đối tượng cấu trúc X509
        X509Certificate finalCaCert;
        try (FileInputStream fisCa = new FileInputStream(caFileSource)) {
            finalCaCert = (X509Certificate) cf.generateCertificate(fisCa);
        }

        // 4. Khởi tạo thùng chứa dữ liệu phản hồi đổ thông tin thô của CA ra hiển thị sức khỏe
        CertificateInfoResponse dto = new CertificateInfoResponse();
        dto.setSubject(finalCaCert.getSubjectX500Principal().toString());
        dto.setIssuer(finalCaCert.getIssuerX500Principal().toString());
        dto.setSerialNumber(finalCaCert.getSerialNumber().toString(16));
        dto.setValidFrom(finalCaCert.getNotBefore().toString());
        dto.setValidTo(finalCaCert.getNotAfter().toString()); // Lấy ngày hết hạn của chính CA đó
        dto.setCaProvider(CertificateUtil.detectCAProvider(finalCaCert.getSubjectX500Principal().toString()));

        // ==================== KHỐI LỆNH CHỐT CHẶN BẢO VỆ CẤU TRÚC ====================
        // Đề phòng trường hợp ông hoặc người dùng chép nhầm/đổi tên file lộn xộn trong thư mục
        
        // Chặn 1: Nếu file đặt tên là ca.cer thực chất bên trong lõi lại là file User thông thường
        if (finalCaCert.getBasicConstraints() == -1) {
            dto.setCaValidityStatus("INVALID_CA_FILE_IS_USER");
            dto.setCrlValidityStatus("ERROR");
            dto.setCrlStatus("CRL_CHECK_SKIPPED: Invalid ca.cer structure");
            dto.setOcspStatus("OCSP_CHECK_SKIPPED: Invalid ca.cer structure");
            return dto;
        }

        // Chặn 2: Nếu file đặt tên là user.cer thực chất bên trong lõi lại chứa quyền CA
        if (cert.getBasicConstraints() != -1) {
            dto.setCertValidityStatus("INVALID_USER_FILE_IS_CA");
            return dto;
        }
        // =============================================================================

        Date now = new Date();

        // 5. Kiểm tra thời hạn vận hành của nhà mạng CA xem còn sống hay chết
        try {
            finalCaCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED"); // CA đã quá hạn sử dụng
        }

        // 6. KIỂM TRA TOÁN HỌC PHÂN CẤP: Dùng Public Key của ca.cer giải mã chữ ký số trên user.cer
        try {
            cert.verify(finalCaCert.getPublicKey());
            dto.setCertValidityStatus("MATCHED_CHAIN"); // Chuỗi ký số trùng khớp hoàn toàn
        } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CertificateException e) {
            dto.setCertValidityStatus("MISMATCHED_CA_CHAIN"); // Lỗi: file ca.cer này không phải là thằng đã ký ra file user.cer này!
            dto.setCrlStatus("SIGNATURE_VERIFICATION_FAILED");
            dto.setOcspStatus("SIGNATURE_VERIFICATION_FAILED");
            return dto; // Chặn đứng tại đây, không cho truy vấn mạng bừa bãi
        }

        // 7. KÍCH HOẠT ĐƯỜNG TRUYỀN INTERNET: Gửi lệnh kiểm thử cổng thu hồi trạng thái của nhà mạng này
        dto.setCrlStatus(crlService.checkCRL(cert, finalCaCert, cf, dto)); // Kiểm tra qua file .crl tĩnh
        dto.setOcspStatus(ocspService.checkOCSP(cert, finalCaCert));       // Kiểm tra qua máy chủ trực tuyến OCSP

        return dto;
    }
}
