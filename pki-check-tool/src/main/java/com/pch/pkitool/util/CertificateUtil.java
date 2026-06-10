package com.pch.pkitool.util;

import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.BCStyle;
import org.bouncycastle.asn1.x500.style.IETFUtils;

public class CertificateUtil {

    // Lấy thông tin nhà cung cấp bằng BouncyCastle
    public static String detectCAProvider(String issuerDN) {
        try {
            X500Name x500Name = new X500Name(issuerDN);
            String rawProvider = "";

            // Extract Organization attribute from issuer string
            var orgs = x500Name.getRDNs(BCStyle.O);
            if (orgs.length > 0) {
                rawProvider = IETFUtils.valueToString(orgs[0].getFirst().getValue()).toUpperCase();
            } else {
                var cns = x500Name.getRDNs(BCStyle.CN);
                if (cns.length > 0) {
                    rawProvider = IETFUtils.valueToString(cns[0].getFirst().getValue()).toUpperCase();
                }
            }

            // Map long organization text to short uppercase file keys
            if (rawProvider.contains("NACENCOMM") || rawProvider.contains("CA2"))  {
                return "CA2";
            }
            if (rawProvider.contains("POSTS AND TELECOMMUNICATIONS") || rawProvider.contains("VNPT")) {
                return "VNPT";
            }
            if (rawProvider.contains("FPT")) {
                return "FPT";
            }
            if (rawProvider.contains("FASTCA")) {
                return "FASTCA";
            }
            if (rawProvider.contains("CMC")) {
                return "CMC";
            }
            if (rawProvider.contains("VIETTEL")) {
                return "VIETTEL";
            }
           
            return "UNKNOWN_CA";
        } catch (Exception e) {
            return "UNKNOWN_CA";
        }
    }
}
