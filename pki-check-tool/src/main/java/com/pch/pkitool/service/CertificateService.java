package com.pch.pkitool.service;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.util.CertificateUtil;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
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

    // Hàm bóc tách, đối sánh toán học và kiểm tra trạng thái mạng của cặp file ca.cer và user.cer theo thư mục
    public CertificateInfoResponse processCaAndUserFromFolder(String caName) throws Exception {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");

        // Định vị đường dẫn vật lý chính xác tới 2 file nằm trong thư mục DataCrlOcsp/{caName}
        File userFileSource = new File("DataCrlOcsp/" + caName + "/user.cer");
        File caFileSource = new File("DataCrlOcsp/" + caName + "/ca.cer");

        // Kiểm tra xem sự tồn tại vật lý của cả 2 file
        if (!userFileSource.exists() || !caFileSource.exists()) {
            throw new FileNotFoundException("Thiếu file cấu trúc chứng chỉ chuẩn.");
        }

        // Nạp file user.cer từ ổ cứng lên RAM và phân dịch thành đối tượng cấu trúc X509
        X509Certificate cert;
        try (FileInputStream fisUser = new FileInputStream(userFileSource)) {
            cert = (X509Certificate) cf.generateCertificate(fisUser);
        }

        // Nạp file ca.cer từ ổ cứng lên RAM và phân dịch thành đối tượng cấu trúc X509
        X509Certificate finalCaCert;
        try (FileInputStream fisCa = new FileInputStream(caFileSource)) {
            finalCaCert = (X509Certificate) cf.generateCertificate(fisCa);
        }

        // Khởi tạo thùng chứa dữ liệu phản hồi đổ thông tin thô của CA ra hiển thị sức khỏe
        CertificateInfoResponse dto = new CertificateInfoResponse();
        dto.setSubject(finalCaCert.getSubjectX500Principal().toString());
        dto.setIssuer(finalCaCert.getIssuerX500Principal().toString());
        dto.setSerialNumber(finalCaCert.getSerialNumber().toString(16));
        dto.setValidFrom(finalCaCert.getNotBefore().toString());
        dto.setValidTo(finalCaCert.getNotAfter().toString()); // Lấy ngày hết hạn của chính CA đó
        dto.setCaProvider(CertificateUtil.detectCAProvider(finalCaCert.getSubjectX500Principal().toString()));

        // ==================== BỔ SUNG THÊM THÔNG TIN USER ====================
        dto.setUserSubject(cert.getSubjectX500Principal().toString());       // Lấy tên khách hàng
        dto.setUserSerialNumber(cert.getSerialNumber().toString(16));// Lấy Serial CTS khách hàng
        dto.setUserValidTo(cert.getNotAfter().toString());                  // Lấy ngày hết hạn CTS khách hàng
        // =====================================================================

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

        // Kiểm tra thời hạn vận hành của nhà mạng CA xem còn sống hay chết
        try {
            finalCaCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED"); // CA đã quá hạn sử dụng
        }

        try {
            cert.checkValidity(now);
            dto.setCertValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCertValidityStatus("EXPIRED"); //đã quá hạn sử dụng
        }

        // KÍCH HOẠT ĐƯỜNG TRUYỀN INTERNET: Gửi lệnh kiểm thử cổng thu hồi trạng thái của nhà mạng này
        dto.setCrlStatus(crlService.checkCRL(cert, finalCaCert, cf, dto)); // Kiểm tra qua file .crl tĩnh
        dto.setOcspStatus(ocspService.checkOCSP(cert, finalCaCert));       // Kiểm tra qua máy chủ trực tuyến OCSP

        return dto;
    }

    // Logic xử lý kiểm tra file user.cer tạm thời từ RAM
    public CertificateInfoResponse checkTemporaryUserCert(String caName, MultipartFile file) throws Exception {
        if (file.isEmpty()) {
            throw new IllegalArgumentException("File nạp lên bị rỗng.");
        }

        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        File caFileSource = new File("DataCrlOcsp/" + caName + "/ca.cer");

        if (!caFileSource.exists()) {
            throw new FileNotFoundException("Không tìm thấy cấu trúc CA hệ thống tương ứng.");
        }

        // Nạp file user từ bộ nhớ tạm
        X509Certificate cert;
        try (InputStream is = file.getInputStream()) {
            cert = (X509Certificate) cf.generateCertificate(is);
        }

        // Nạp file ca.cer từ ổ cứng
        X509Certificate finalCaCert;
        try (FileInputStream fisCa = new FileInputStream(caFileSource)) {
            finalCaCert = (X509Certificate) cf.generateCertificate(fisCa);
        }

        // Chọn file vào ô thêm mới user thực chất lại là file CA (BasicConstraints != -1)
        if (cert.getBasicConstraints() != -1) {
            throw new IllegalArgumentException("Tệp tải lên là một chứng chỉ CA, không phải là chứng chỉ User\nVui lòng chọn đúng tệp User!");
        }

        // KIỂM TRA TOÁN HỌC PHÂN CẤP (Xác thực chuỗi ký số - Chain Verification)
        // Dùng Public Key của file ca.cer hiện tại để giải mã chữ ký số trên file user.cer mới nạp
        try {
            cert.verify(finalCaCert.getPublicKey());
        } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CertificateException e) {
            throw new IllegalArgumentException("Chứng chỉ User này KHÔNG THUỘC VỀ hệ thống CA hiện tại\nVui lòng chọn đúng User trùng khớp với CA!");
        }

        // Điền dữ liệu vào DTO
        CertificateInfoResponse dto = new CertificateInfoResponse();
        dto.setSubject(finalCaCert.getSubjectX500Principal().toString());
        dto.setIssuer(finalCaCert.getIssuerX500Principal().toString());
        dto.setSerialNumber(finalCaCert.getSerialNumber().toString(16));
        dto.setValidFrom(finalCaCert.getNotBefore().toString());
        dto.setValidTo(finalCaCert.getNotAfter().toString());
        dto.setCaProvider(CertificateUtil.detectCAProvider(finalCaCert.getSubjectX500Principal().toString()));

        dto.setUserSubject(cert.getSubjectX500Principal().toString());
        dto.setUserSerialNumber(cert.getSerialNumber().toString(16));
        dto.setUserValidTo(cert.getNotAfter().toString());

        if (finalCaCert.getBasicConstraints() == -1) {
            dto.setCaValidityStatus("INVALID_CA_FILE_IS_USER");
            return dto;
        }
        if (cert.getBasicConstraints() != -1) {
            dto.setCertValidityStatus("INVALID_USER_FILE_IS_CA");
            return dto;
        }

        Date now = new Date();
        // Kiểm tra thời hạn vận hành của nhà mạng CA xem còn sống hay chết
        try {
            finalCaCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED"); // CA đã quá hạn sử dụng
        }

        try {
            cert.checkValidity(now);
            dto.setCertValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCertValidityStatus("EXPIRED"); //đã quá hạn sử dụng
        }

        // Kiểm tra trạng thái trực tuyến
        dto.setCrlStatus(crlService.checkCRL(cert, finalCaCert, cf, dto));
        dto.setOcspStatus(ocspService.checkOCSP(cert, finalCaCert));

        return dto;
    }

    // Logic lưu đè file vật lý vào ổ cứng
    public void saveUserCertificate(String caName, MultipartFile file) throws Exception {
        File targetFolder = new File("DataCrlOcsp/" + caName);
        if (!targetFolder.exists()) {
            throw new FileNotFoundException("Thư mục CA không tồn tại.");
        }

        File userFile = new File(targetFolder, "user.cer");
        // Ghi dữ liệu byte vào file
        try (FileOutputStream fos = new FileOutputStream(userFile)) {
            fos.write(file.getBytes());
        }
    }

    // Thêm mới CA chưa có vào kho lưu trữ 
    public String addNewCaCertificate(MultipartFile file, String caName) throws Exception {
        if (file.isEmpty()) {
            throw new IllegalArgumentException("File chứng chỉ CA nạp lên bị rỗng.");
        }
        if (caName == null || caName.trim().isEmpty()) {
            throw new IllegalArgumentException("Tên nhà cung cấp CA không được để trống.");
        }

        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        X509Certificate caCert;

        // Đọc file CA tạm thời từ bộ nhớ RAM
        try (InputStream is = file.getInputStream()) {
            caCert = (X509Certificate) cf.generateCertificate(is);
        }

        // 1. KIỂM TRA CHỐT CHẶN 1: Phải là chứng chỉ CA (BasicConstraints phải >= 0)
        // Nếu getBasicConstraints() == -1 nghĩa là đây là chứng chỉ của User (End-Entity)
        if (caCert.getBasicConstraints() == -1) {
            throw new IllegalArgumentException("Tệp tải lên là user certificate, không phải CA\nVui lòng chọn đúng file CA!");
        }

        // 2. Chuẩn hóa tên thư mục do người dùng nhập (bỏ khoảng trắng và thay thế bằng dấu gạch dưới)
        caName = caName.trim().replace(" ", "_");

        // 3. KIỂM TRA CHỐT CHẶN 2: Kiểm tra thư mục CA này đã tồn tại chưa
        File targetFolder = new File("DataCrlOcsp/" + caName);
        if (targetFolder.exists()) {
            throw new IllegalArgumentException("Hệ thống đã tồn tại nhà cung cấp CA mang tên: " + caName);
        }

        // 4. TIẾN HÀNH TẠO THƯ MỤC VÀ GHI FILE ca.cer
        if (!targetFolder.mkdirs()) {
            throw new IOException("Không thể tạo cấu trúc thư mục lưu trữ: " + targetFolder.getAbsolutePath());
        }

        File caFileDestination = new File(targetFolder, "ca.cer");
        try (FileOutputStream fos = new FileOutputStream(caFileDestination)) {
            fos.write(file.getBytes());
        }

        return caName; // Trả về tên CA vừa tạo thành công để Frontend cập nhật lên ComboBox
    }

    // Load lại list CA hiển thị sau khi thêm mới CA
    private X509Certificate loadCertificate(File file) throws Exception {
        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        try (FileInputStream fis = new FileInputStream(file)) {
            return (X509Certificate) cf.generateCertificate(fis);
        }
    }

    // Người dùng đưa user.cer tự động lấy CA.cer kiểm tra
    public CertificateInfoResponse checkAutoDiscover(MultipartFile file) throws Exception {
        if (file.isEmpty()) {
            throw new IllegalArgumentException("File nạp lên bị rỗng.");
        }

        CertificateFactory cf = CertificateFactory.getInstance("X.509");
        X509Certificate cert;
        try (InputStream is = file.getInputStream()) {
            cert = (X509Certificate) cf.generateCertificate(is);
        }

        if (cert.getBasicConstraints() != -1) {
            throw new IllegalArgumentException("Tệp tải lên là một chứng chỉ CA, không phải là chứng chỉ User\nVui lòng chọn đúng tệp User!");
        }

        String baseCaName = CertificateUtil.detectCAProvider(cert.getIssuerX500Principal().toString()).trim().replace(" ", "_");
        String finalCaName = baseCaName;

        X509Certificate caCert = null;
        File caFileSource = new File("DataCrlOcsp/" + finalCaName + "/ca.cer");
        
        boolean foundCa = false;
        if (caFileSource.exists()) {
            try {
                X509Certificate existingCa = loadCertificate(caFileSource);
                cert.verify(existingCa.getPublicKey());
                caCert = existingCa;
                foundCa = true;
            } catch (Exception e) {
                File rootFolder = new File("DataCrlOcsp");
                File[] matchingFolders = rootFolder.listFiles((dir, name) -> name.startsWith(baseCaName));
                if (matchingFolders != null) {
                    for (File folder : matchingFolders) {
                        File otherCaFile = new File(folder, "ca.cer");
                        if (otherCaFile.exists()) {
                            try {
                                X509Certificate otherCa = loadCertificate(otherCaFile);
                                cert.verify(otherCa.getPublicKey());
                                caCert = otherCa;
                                finalCaName = folder.getName();
                                foundCa = true;
                                break;
                            } catch (Exception ex) {
                                // continue
                            }
                        }
                    }
                }
            }
        }

        if (!foundCa) {
            String aiaUrl = CertificateUtil.getCaIssuerUrl(cert);
            if (aiaUrl == null || aiaUrl.trim().isEmpty()) {
                throw new IllegalArgumentException("Không tìm thấy CA tương thích trong hệ thống và chứng chỉ không khai báo liên kết tải CA (AIA)!");
            }

            byte[] caBytes;
            try {
                caBytes = CertificateUtil.downloadFile(aiaUrl);
            } catch (Exception e) {
                throw new IOException("Không thể tải chứng chỉ CA từ AIA URL: " + aiaUrl + " (" + e.getMessage() + ")");
            }

            X509Certificate downloadedCa;
            try (InputStream is = new java.io.ByteArrayInputStream(caBytes)) {
                downloadedCa = (X509Certificate) cf.generateCertificate(is);
            } catch (Exception e) {
                throw new IllegalArgumentException("Tệp tin tải về từ AIA không phải là chứng chỉ X.509 hợp lệ!");
            }

            if (downloadedCa.getBasicConstraints() == -1) {
                throw new IllegalArgumentException("Tệp tải về từ AIA là chứng chỉ User, không phải CA!");
            }

            try {
                cert.verify(downloadedCa.getPublicKey());
            } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CertificateException e) {
                throw new IllegalArgumentException("Chứng chỉ tải về từ AIA không ký cho chứng chỉ khách hàng!");
            }

            String downloadedCaSubject = downloadedCa.getSubjectX500Principal().toString();
            String detectedCaName = CertificateUtil.detectCAProvider(downloadedCaSubject).trim().replace(" ", "_");
            
            File targetFolder = new File("DataCrlOcsp/" + detectedCaName);
            int suffix = 1;
            while (targetFolder.exists()) {
                try {
                    X509Certificate existingCa = loadCertificate(new File(targetFolder, "ca.cer"));
                    if (existingCa.getSerialNumber().equals(downloadedCa.getSerialNumber())) {
                        break;
                    }
                } catch (Exception e) {
                    // ignore
                }
                targetFolder = new File("DataCrlOcsp/" + detectedCaName + "_" + suffix);
                suffix++;
            }

            if (!targetFolder.exists()) {
                if (!targetFolder.mkdirs()) {
                    throw new IOException("Không thể tạo thư mục lưu trữ CA mới: " + targetFolder.getAbsolutePath());
                }
                File destCaFile = new File(targetFolder, "ca.cer");
                try (FileOutputStream fos = new FileOutputStream(destCaFile)) {
                    fos.write(caBytes);
                }
            }
            
            caCert = downloadedCa;
            finalCaName = targetFolder.getName();
        }

        CertificateInfoResponse dto = new CertificateInfoResponse();
        dto.setSubject(caCert.getSubjectX500Principal().toString());
        dto.setIssuer(caCert.getIssuerX500Principal().toString());
        dto.setSerialNumber(caCert.getSerialNumber().toString(16));
        dto.setValidFrom(caCert.getNotBefore().toString());
        dto.setValidTo(caCert.getNotAfter().toString());
        dto.setCaProvider(finalCaName);

        dto.setUserSubject(cert.getSubjectX500Principal().toString());
        dto.setUserSerialNumber(cert.getSerialNumber().toString(16));
        dto.setUserValidTo(cert.getNotAfter().toString());

        Date now = new Date();
        try {
            caCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED");
        }

        try {
            cert.checkValidity(now);
            dto.setCertValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCertValidityStatus("EXPIRED");
        }

        dto.setCrlStatus(crlService.checkCRL(cert, caCert, cf, dto));
        dto.setOcspStatus(ocspService.checkOCSP(cert, caCert));

        return dto;
    }
}
