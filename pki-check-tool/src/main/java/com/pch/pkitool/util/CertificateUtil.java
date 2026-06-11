package com.pch.pkitool.util;

import java.util.List;
import java.util.Map;
import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.BCStyle;
import org.bouncycastle.asn1.x500.style.IETFUtils;

public class CertificateUtil {
    // Bảng quy đổi CA từ Issuer --> tên CA
    private static final Map<String, List<String>> CA_MAPPING = Map.of(
            "CA2", List.of("NACENCOMM", "CA2"),
            "VNPT", List.of("POSTS AND TELECOMMUNICATIONS", "VNPT"),
            "FPT", List.of("FPT"),
            "FASTCA", List.of("FASTCA"),
            "CMC", List.of("CMC"),
            "VIETTEL", List.of("VIETTEL")
    );

    // Lấy thông tin nhà cung cấp bằng BouncyCastle
    public static String detectCAProvider(String issuerDN) {
        try {
            X500Name x500Name = new X500Name(issuerDN);
            String rawProvider = "";

            // Lấy tên tổ chức hoặc CN từ chuỗi Issuer
            var orgs = x500Name.getRDNs(BCStyle.O);
            if (orgs.length > 0) {
                rawProvider = IETFUtils.valueToString(orgs[0].getFirst().getValue()).toUpperCase();
            } else {
                var cns = x500Name.getRDNs(BCStyle.CN);
                if (cns.length > 0) {
                    rawProvider = IETFUtils.valueToString(cns[0].getFirst().getValue()).toUpperCase();
                }
            }
            
            // Khai báo hàm sử dụng trong lamda
            // Nhiệm vụ là lấy tên file phù hợp với CA từ TrustStore để hiển thị giao diện C#
            final String provider = rawProvider;

            return CA_MAPPING.entrySet()
                    .stream()
                    .filter(entry -> entry.getValue()
                    .stream()
                    .anyMatch(provider::contains))
                    .map(Map.Entry::getKey)
                    .findFirst()
                    .orElse("UNKNOWN_CA");
        } catch (Exception e) {
            return "UNKNOWN_CA";
        }
    }
}
