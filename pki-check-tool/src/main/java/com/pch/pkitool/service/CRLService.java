package com.pch.pkitool.service;

import com.pch.pkitool.dto.CertificateInfoResponse;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.io.InputStream;
import java.net.URL;
import java.security.InvalidKeyException;
import java.security.NoSuchAlgorithmException;
import java.security.NoSuchProviderException;
import java.security.SignatureException;
import java.security.cert.CRLException;
import java.security.cert.Certificate;
import java.security.cert.CertificateFactory;
import java.security.cert.X509CRL;
import java.security.cert.X509Certificate;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;
import org.bouncycastle.asn1.ASN1InputStream;
import org.bouncycastle.asn1.DERIA5String;
import org.bouncycastle.asn1.x509.CRLDistPoint;
import org.bouncycastle.asn1.x509.DistributionPoint;
import org.bouncycastle.asn1.x509.DistributionPointName;
import org.bouncycastle.asn1.x509.GeneralName;
import org.bouncycastle.asn1.x509.GeneralNames;
import org.springframework.stereotype.Service;

/**
 *
 * @author Chi Hao
 */
@Service
public class CRLService {

    // Nhiệm vụ: trích xuất link CRL, tải tệp từ internet và đối chiếu số serial
    public String checkCRL(X509Certificate cert, Certificate caCert, CertificateFactory cf, CertificateInfoResponse dto) throws FileNotFoundException, IOException, CRLException {
        try {
            List<String> crlUrls = getCrlDistributionPoints(cert);
            if (crlUrls.isEmpty()) {
                dto.setCrlValidityStatus("UNKNOWN");
                return "CRL_LINK_NOT_FOUND_IN_CERT";
            }

            // Lấy đường link đầu tiên trong danh sách trả về
            String crlUrl = crlUrls.get(0);
            URL url = new URL(crlUrl);

            // Mở luồng kết nối internet để tải tệp về bộ nhớ tạm và biến dữ liệu thành X509CRL
            try (InputStream in = url.openStream()) {
                X509CRL crl = (X509CRL) cf.generateCRL(in);

                Date now = new Date();

                // LAYER 1: Kiểm tra thời hạn hiệu lực của chính tệp CRL (Lỗi Log 4)
                if (crl.getNextUpdate() != null && now.after(crl.getNextUpdate())) {
                    dto.setCrlValidityStatus("EXPIRED");
                    return "CRL_EXPIRED";
                } else {
                    dto.setCrlValidityStatus("VALID");
                }

                // LAYER 2: Xác thực chữ ký số của tệp CRL bằng khóa công khai của CA
                try {
                    crl.verify(caCert.getPublicKey());
                } catch (InvalidKeyException | NoSuchAlgorithmException | NoSuchProviderException | SignatureException | CRLException e) {
                    return "INVALID_CRL_SIGNATURE";
                }

                // LAYER 3: Đối chiếu số Serial xem có bị thu hồi không
                if (crl.isRevoked(cert)) {
                    return "REVOKED";
                }

                return "VALID";
            }
        } catch (Exception e) {
            dto.setCrlValidityStatus("ERROR");
            return "CRL_CHECK_FAILED: " + e.getMessage();
        }
    }

    // Phân tích ASN.1 để bóc tách OID mở rộng dùng URL phân phối file CRL
    private List<String> getCrlDistributionPoints(X509Certificate cert) throws Exception {
        List<String> crlUrls = new ArrayList<>();

        // Lấy chuỗi byte nhị phân của phần mở rộng danh sách phân phối CRL (mã OID 2.5.29.31)
        byte[] crlDPExtensionValue = cert.getExtensionValue(org.bouncycastle.asn1.x509.Extension.cRLDistributionPoints.getId());
        if (crlDPExtensionValue == null) {
            return crlUrls;
        }

        // ASN.1 là tiêu chuẩn ngôn ngữ và ký hiệu dùng để định nghĩa mã hóa và truyền dữ liệu độc lập với phần cứng và hệ điều hành
        // Giải mã cấu trúc phân cấp ASN.1 thông qua bộ lọc BouncyCastle để trích xuất chuỗi URI
        try (ASN1InputStream oAsnIn = new ASN1InputStream(crlDPExtensionValue)) {
            byte[] extOctets = ((org.bouncycastle.asn1.ASN1OctetString) oAsnIn.readObject()).getOctets();
            try (ASN1InputStream oAsnIn2 = new ASN1InputStream(extOctets)) {
                CRLDistPoint distPoint = CRLDistPoint.getInstance(oAsnIn2.readObject());

                for (DistributionPoint dp : distPoint.getDistributionPoints()) {
                    DistributionPointName dpn = dp.getDistributionPoint();

                    if (dpn != null && dpn.getType() == DistributionPointName.FULL_NAME) {
                        GeneralName[] genNames = GeneralNames.getInstance(dpn.getName()).getNames();
                        for (GeneralName genName : genNames) {
                            if (genName.getTagNo() == GeneralName.uniformResourceIdentifier) {
                                String url = DERIA5String.getInstance(genName.getName()).getString();
                                crlUrls.add(url);
                            }
                        }
                    }
                }
            }
        }
        return crlUrls;
    }
}
