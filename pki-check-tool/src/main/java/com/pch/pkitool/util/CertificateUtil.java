package com.pch.pkitool.util;

import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.BCStyle;
import org.bouncycastle.asn1.x500.style.IETFUtils;

public class CertificateUtil {

    // Lấy thông tin nhà cung cấp bằng BouncyCastle
    public static String detectCAProvider(String issuerDN) {
        try {
            // Chuyển chuỗi chữ DN thông thường thành đối tượng cấu trúc X500Name của BouncyCastle
            X500Name x500Name = new X500Name(issuerDN);
            
            // Tìm và lọc ra danh sách các mảng chứa thuộc tính "O" (Organization - Tổ chức) trong chuỗi
            var orgs = x500Name.getRDNs(BCStyle.O);
            if (orgs.length > 0) { // Kiểm tra nếu chuỗi DN thực sự có tồn tại trường O
                // Bóc tách giá trị nhị phân ASN1 bên trong trường O và biến đổi thành chuỗi chữ thường
                return IETFUtils.valueToString(orgs[0].getFirst().getValue());
            }
            
            // Dự phòng: Nếu không có trường O thì lấy trường CN (Common Name)
            var cns = x500Name.getRDNs(BCStyle.CN);
            if (cns.length > 0) {
                return IETFUtils.valueToString(cns[0].getFirst().getValue());
            }
        } catch (Exception e) {
            return "UNKNOWN CA (BC ERROR)";
        }
        return "UNKNOWN CA";
    }
}