package com.pch.pkitool.service;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.net.HttpURLConnection;
import java.net.URL;
import java.security.cert.CertificateEncodingException;
import java.security.cert.X509Certificate;
import org.bouncycastle.asn1.ASN1InputStream;
import org.bouncycastle.asn1.ASN1ObjectIdentifier;
import org.bouncycastle.asn1.ASN1OctetString;
import org.bouncycastle.asn1.ASN1Sequence;
import org.bouncycastle.asn1.ASN1TaggedObject;
import org.bouncycastle.asn1.nist.NISTObjectIdentifiers;
import org.bouncycastle.asn1.x509.AlgorithmIdentifier;
import org.bouncycastle.asn1.x509.Extension;
import org.bouncycastle.cert.jcajce.JcaX509CertificateHolder;
import org.bouncycastle.cert.ocsp.BasicOCSPResp;
import org.bouncycastle.cert.ocsp.CertificateID;
import org.bouncycastle.cert.ocsp.CertificateStatus;
import org.bouncycastle.cert.ocsp.OCSPException;
import org.bouncycastle.cert.ocsp.OCSPReq;
import org.bouncycastle.cert.ocsp.OCSPReqBuilder;
import org.bouncycastle.cert.ocsp.OCSPResp;
import org.bouncycastle.cert.ocsp.RevokedStatus;
import org.bouncycastle.cert.ocsp.SingleResp;
import org.bouncycastle.operator.DigestCalculatorProvider;
import org.bouncycastle.operator.OperatorCreationException;
import org.bouncycastle.operator.jcajce.JcaDigestCalculatorProviderBuilder;
import org.springframework.stereotype.Service;

/**
 *
 * @author Chi Hao
 */
@Service
public class OCSPService {

    // Nhiệm vụ: đóng gói gói tin OCSP Request, bắn HTTP POST và phân tích mã kết quả trả về
    public String checkOCSP(X509Certificate cert, X509Certificate caCert) throws OperatorCreationException, OCSPException {
        try {
            String ocspUrl = getOcspUrlFromCertificate(cert);
            if (ocspUrl == null || ocspUrl.isEmpty()) {
                return "OCSP_URL_NOT_FOUND_IN_CERT";
            }

            // Bước 1: Khởi tạo danh sách các thuật toán băm cần thử nghiệm (Ưu tiên SHA-256 trước, SHA-1 sau)
            org.bouncycastle.asn1.ASN1ObjectIdentifier[] hashAlgorithms = {
                NISTObjectIdentifiers.id_sha256,
                org.bouncycastle.asn1.oiw.OIWObjectIdentifiers.idSHA1
            };
            String finalStatus = "UNKNOWN";

            // Bước 2: Vòng lặp tự động thử sai qua từng thuật toán
            for (org.bouncycastle.asn1.ASN1ObjectIdentifier hashAlgOid : hashAlgorithms) {
                try {
                    DigestCalculatorProvider digCalcProv = new JcaDigestCalculatorProviderBuilder().build();
                    AlgorithmIdentifier algId = new AlgorithmIdentifier(hashAlgOid);
                    CertificateID certId = new CertificateID(digCalcProv.get(algId), new JcaX509CertificateHolder(caCert), cert.getSerialNumber());

                    OCSPReqBuilder builder = new OCSPReqBuilder();
                    builder.addRequest(certId);
                    OCSPReq request = builder.build();

                    // Giao tiếp mạng HTTP POST lên máy chủ CA Responder
                    URL url = new URL(ocspUrl);
                    HttpURLConnection con = (HttpURLConnection) url.openConnection();
                    con.setRequestMethod("POST");
                    con.setRequestProperty("Content-Type", "application/ocsp-request");
                    con.setRequestProperty("Accept", "application/ocsp-response");
                    con.setConnectTimeout(5000); // Đặt timeout kết nối 5 giây tránh treo hệ thống
                    con.setReadTimeout(5000);
                    con.setDoOutput(true);

                    try (OutputStream out = con.getOutputStream()) {
                        out.write(request.getEncoded());
                        out.flush();
                    }

                    // Nhận phản hồi và phân tích mã trạng thái từ máy chủ CA
                    try (InputStream in = con.getInputStream()) {
                        OCSPResp ocspResponse = new OCSPResp(in);

                        // Nếu máy chủ báo lỗi cấu trúc (ví dụ SHA-256 không hợp lệ), bỏ qua để nhảy sang vòng lặp SHA-1
                        if (ocspResponse.getStatus() != OCSPResp.SUCCESSFUL) {
                            finalStatus = "FAILED_STATUS_" + ocspResponse.getStatus();
                            continue;
                        }

                        BasicOCSPResp basicResp = (BasicOCSPResp) ocspResponse.getResponseObject();
                        if (basicResp == null || basicResp.getResponses().length == 0) {
                            finalStatus = "NO_RESPONSE_DATA";
                            continue;
                        }

                        SingleResp responses = basicResp.getResponses()[0];
                        Object status = responses.getCertStatus();

                        // Nếu nhận được phản hồi nghiệp vụ hợp lệ, lập tức trả kết quả về và dừng vòng lặp
                        if (status == CertificateStatus.GOOD) {
                            return "GOOD";
                        }
                        if (status instanceof RevokedStatus) {
                            return "REVOKED";
                        }
                        return "UNKNOWN";
                    }
                } catch (IOException | CertificateEncodingException | OCSPException | OperatorCreationException e) {
                    // Nếu lỗi kết nối mạng ở thuật toán này, ghi nhận lỗi và tiếp tục thử thuật toán tiếp theo
                    finalStatus = "OCSP_ERROR: " + e.getMessage();
                }
            }

            // Nếu đã thử cả SHA-256 và SHA-1 mà vẫn thất bại, trả về mã lỗi cuối cùng ghi nhận được
            return finalStatus;

        } catch (Exception e) {
            return "OCSP_CRITICAL_ERROR: " + e.getMessage();
        }
    }

    // Phân tích ASN.1: Đi sâu cấu trúc trường mở rộng AIA để nhặt chuẩn xác OID máy chủ OCSP
    private String getOcspUrlFromCertificate(X509Certificate cert) {
        try {
            // AIA: là một tiện ích mở rộng trong chứng chỉ số X.509 (như SSL/TLS), chứa các URL giúp hệ thống tự động tải chứng chỉ trung gian (Intermediate CA) và kiểm tra trạng thái thu hồi chứng chỉ (OCSP)
            // Lấy chuỗi byte nhị phân thô của phần mở rộng truy cập thông tin tổ chức AIA (mã OID 1.3.6.1.5.5.7.1.1)
            byte[] aiaExtensionValue = cert.getExtensionValue(Extension.authorityInfoAccess.getId());
            if (aiaExtensionValue == null) {
                return null;
            }

            try (ASN1InputStream asn1In = new ASN1InputStream(new ByteArrayInputStream(aiaExtensionValue))) {
                ASN1OctetString octetString = (ASN1OctetString) asn1In.readObject();
                try (ASN1InputStream asn1InOctet = new ASN1InputStream(new ByteArrayInputStream(octetString.getOctets()))) {
                    ASN1Sequence seq = (ASN1Sequence) asn1InOctet.readObject();

                    // Nếu sequence bị rỗng hoặc lỗi cấu trúc
                    if (seq == null || seq.size() == 0) {
                        return null; // Trả về null an toàn để luồng chính báo "OCSP_URL_NOT_FOUND_IN_CERT"
                    }

                    // duyệt qua từng phần tử mô tả truy cập trong chuỗi tuần tự
                    for (int i = 0; i < seq.size(); i++) {
                        ASN1Sequence accessDescription = (ASN1Sequence) seq.getObjectAt(i);
                        ASN1ObjectIdentifier accessMethod = (ASN1ObjectIdentifier) accessDescription.getObjectAt(0);

                        // Kiểm tra nếu trùng khớp mã OID định danh giao thức OCSP (1.3.6.1.5.5.7.48.1)
                        if (accessMethod.getId().equals("1.3.6.1.5.5.7.48.1")) {
                            ASN1TaggedObject taggedObject = (ASN1TaggedObject) accessDescription.getObjectAt(1);
                            return new String(ASN1OctetString.getInstance(taggedObject.getBaseObject()).getOctets());
                        }
                    }
                }
            }
        } catch (IOException e) {
            // Bỏ qua ngoại lệ đọc file để trả về null
        }
        return null;
    }
}
