package com.pch.pkitool.util;

import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.BCStyle;
import org.bouncycastle.asn1.x500.style.IETFUtils;

public class CertificateUtil {
    public static String detectCAProvider(String issuerDN) {
        try {
            X500Name x500Name = new X500Name(issuerDN);

            // Ưu tiên lấy trường O (Organization - Tên tổ chức) trước
            var orgs = x500Name.getRDNs(BCStyle.O);
            if (orgs.length > 0) {
                return cleanProviderName(
                        IETFUtils.valueToString(orgs[0].getFirst().getValue()).toUpperCase());
            }

            var cns = x500Name.getRDNs(BCStyle.CN);
            if (cns.length > 0) {
                return cleanProviderName(
                        IETFUtils.valueToString(cns[0].getFirst().getValue())
                );
            }

            return "UNKNOWN_CA";
        } catch (Exception e) {
            return "UNKNOWN_CA";
        }
    }

    // Hàm phụ trợ để cắt bỏ bớt các chữ thừa thãi, giữ lại tên cốt lõi cho đẹp giao diện
    private static String cleanProviderName(String rawName) {
        String clean = rawName.replace("CORPORATION", "")
                .replace("JOINT STOCK COMPANY", "")
                .replace("MẠNG VÀ TRUYỀN THÔNG", "")
                .replace("TẬP ĐOÀN", "")
                .trim();

        return clean.replaceAll("\\s+", "_");
    }
}
