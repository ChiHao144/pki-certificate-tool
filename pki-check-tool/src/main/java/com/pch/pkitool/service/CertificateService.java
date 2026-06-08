package com.pch.pkitool.service;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.util.CertificateUtil;
import java.io.ByteArrayInputStream;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.security.Security;
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

    @Autowired // kết nối và inject xử lý CRL
    private CRLService crlService;

    @Autowired // kết nối và inject truy vấn trực tuyến OCSP
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

        // Chuyển đổi file caiFile thành 1 mảng dữ liệu byte thuần
        byte[] caBytes = caFile.getBytes();
        X509Certificate caCert = (X509Certificate) cf.generateCertificate(new ByteArrayInputStream(caBytes));

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

        // Gọi dịch vụ kiểm tra trạng thái qua CRL và OCSP
        Date now = new Date();

        // LAYER 4: Kiểm tra thời hạn hiệu lực chứng chỉ người dùng (Lỗi Log 2, 3, 4)
        try {
            cert.checkValidity(now);
            dto.setCertValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCertValidityStatus("EXPIRED");
        }

        // LAYER 5: Kiểm tra thời hạn hiệu lực chứng chỉ CA cấp phát (Lỗi Log 3, 4)
        try {
            caCert.checkValidity(now);
            dto.setCaValidityStatus("VALID");
        } catch (CertificateExpiredException | CertificateNotYetValidException e) {
            dto.setCaValidityStatus("EXPIRED");
        }

        // Run validation services
        dto.setCrlStatus(crlService.checkCRL(cert, caCert, cf, dto));
        dto.setOcspStatus(ocspService.checkOCSP(cert, caCert));

        return dto;
    }
}
