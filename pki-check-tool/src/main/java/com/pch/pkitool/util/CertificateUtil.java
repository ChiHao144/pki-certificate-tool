package com.pch.pkitool.util;

import java.util.List;
import java.util.Map;
import java.security.cert.X509Certificate;
import java.io.InputStream;
import java.io.ByteArrayOutputStream;
import java.net.URI;
import java.net.URL;
import java.util.Locale;
import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.BCStyle;
import org.bouncycastle.asn1.x500.style.IETFUtils;

public class CertificateUtil {

    private static final Map<String, List<String>> CA_PROVIDERS = Map.ofEntries(
            Map.entry("VNPT", List.of("VNPT")),
            Map.entry("Viettel", List.of("VIETTEL")),
            Map.entry("BKAV", List.of("BKAV")),
            Map.entry("FPT", List.of("FPT")),
            Map.entry("CA2", List.of("CA2", "NACENCOMM")),
            Map.entry("SAFECA", List.of("SAFE")),
            Map.entry("SmartSign", List.of("SMARTSIGN", "VINA-CA", "VINA")),
            Map.entry("Newtel", List.of("NEWTEL", "NEW-CA")),
            Map.entry("EFY", List.of("EFY")),
            Map.entry("FASTCA", List.of("FASTCA")),
            Map.entry("MISA-CA", List.of("MISA-CA")),
            Map.entry("NCCA", List.of("NC-CA")),
            Map.entry("LCSCA", List.of("LCS-CA")),
            Map.entry("CMC", List.of("CMC-CA", "CMC")),
            Map.entry("EASYCA", List.of("EASYCA")),
            Map.entry("ICA", List.of("I-CA")),
            Map.entry("LCA", List.of("LA")),
            Map.entry("HILO", List.of("HILO-CA")),
            Map.entry("ONECA", List.of("ONE-CA")),
            Map.entry("WINCA", List.of("WINCA")),
            Map.entry("VGCA", List.of("Ban Cơ yếu Chính phủ")),
            Map.entry("MATBAOCA", List.of("MATBAO-CA")),
            Map.entry("ECA", List.of("E-CA")),
            Map.entry("MOBIFONE", List.of("MOBIFONE CA")),
            Map.entry("VNPAY", List.of("VNPAY-CA")),
            Map.entry("IntrustCA", List.of("INTRUSTCA")),
            Map.entry("MKCA", List.of("MK CA")),
            Map.entry("TrustCA", List.of("TRUSTCA")));

    public static String detectCAProvider(String issuerDN) {
        try {
            String dnUpper = issuerDN.toUpperCase();

            for (var entry : CA_PROVIDERS.entrySet()) {
                for (String keyword : entry.getValue()) {
                    if (dnUpper.contains(keyword)) {
                        return entry.getKey();
                    }
                }
            }

            for (var entry : CA_PROVIDERS.entrySet()) {
                for (String keyword : entry.getValue()) {
                    if (dnUpper.contains(keyword.toUpperCase(Locale.ROOT))) {
                        return entry.getKey();
                    }
                }
            }

            X500Name x500Name = new X500Name(issuerDN);

            var orgs = x500Name.getRDNs(BCStyle.O);
            if (orgs.length > 0) {
                return IETFUtils.valueToString(orgs[0].getFirst().getValue()).toUpperCase();
            }

            var cns = x500Name.getRDNs(BCStyle.CN);
            if (cns.length > 0) {
                return IETFUtils.valueToString(cns[0].getFirst().getValue()).toUpperCase();
            }

            return "UNKNOWN_CA";
        } catch (Exception e) {
            return "UNKNOWN_CA";
        }
    }

    // Lấy URL tải CA từ thông tin AIA trong file user.cer
    public static String getCaIssuerUrl(X509Certificate cert) {
        try {
            byte[] extVal = cert.getExtensionValue("1.3.6.1.5.5.7.1.1"); // authorityInfoAccess OID
            if (extVal == null) {
                return null;
            }
            byte[] octets = org.bouncycastle.asn1.ASN1OctetString.getInstance(extVal).getOctets();
            org.bouncycastle.asn1.x509.AuthorityInformationAccess aia = org.bouncycastle.asn1.x509.AuthorityInformationAccess
                    .getInstance(octets);
            for (org.bouncycastle.asn1.x509.AccessDescription ad : aia.getAccessDescriptions()) {
                if (ad.getAccessMethod().equals(org.bouncycastle.asn1.x509.X509ObjectIdentifiers.id_ad_caIssuers)) {
                    org.bouncycastle.asn1.x509.GeneralName location = ad.getAccessLocation();
                    if (location.getTagNo() == org.bouncycastle.asn1.x509.GeneralName.uniformResourceIdentifier) {
                        return location.getName().toString();
                    }
                }
            }
        } catch (Exception e) {
        }
        return null;
    }

    // Download file từ URL
    public static byte[] downloadFile(String urlStr) throws Exception {
        URL url = new URI(urlStr).toURL();
        try (InputStream in = url.openStream(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = in.read(buffer)) != -1) {
                out.write(buffer, 0, bytesRead);
            }
            return out.toByteArray();
        }
    }
}
