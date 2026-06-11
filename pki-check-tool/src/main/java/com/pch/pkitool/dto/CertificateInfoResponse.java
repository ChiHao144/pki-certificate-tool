package com.pch.pkitool.dto;

/**
 *
 * @author Chi Hao
 */
// Bộ khung chuẩn hóa dữ liệu bao gồm kết quả xử lý và trả về cho phía giao diện
public class CertificateInfoResponse {
    private String subject;
    private String issuer;
    private String serialNumber;
    private String validFrom;
    private String validTo;
    private String crlStatus;
    private String ocspStatus;
    private String caProvider;
    private String certValidityStatus;
    private String caValidityStatus;   
    private String crlValidityStatus; 

    /**
     * @return the subject
     */
    public String getSubject() {
        return subject;
    }

    /**
     * @param subject the subject to set
     */
    public void setSubject(String subject) {
        this.subject = subject;
    }

    /**
     * @return the issuer
     */
    public String getIssuer() {
        return issuer;
    }

    /**
     * @param issuer the issuer to set
     */
    public void setIssuer(String issuer) {
        this.issuer = issuer;
    }

    /**
     * @return the serialNumber
     */
    public String getSerialNumber() {
        return serialNumber;
    }

    /**
     * @param serialNumber the serialNumber to set
     */
    public void setSerialNumber(String serialNumber) {
        this.serialNumber = serialNumber;
    }

    /**
     * @return the validFrom
     */
    public String getValidFrom() {
        return validFrom;
    }

    /**
     * @param validFrom the validFrom to set
     */
    public void setValidFrom(String validFrom) {
        this.validFrom = validFrom;
    }

    /**
     * @return the validTo
     */
    public String getValidTo() {
        return validTo;
    }

    /**
     * @param validTo the validTo to set
     */
    public void setValidTo(String validTo) {
        this.validTo = validTo;
    }

    /**
     * @return the crlStatus
     */
    public String getCrlStatus() {
        return crlStatus;
    }

    /**
     * @param crlStatus the crlStatus to set
     */
    public void setCrlStatus(String crlStatus) {
        this.crlStatus = crlStatus;
    }

    /**
     * @return the ocspStatus
     */
    public String getOcspStatus() {
        return ocspStatus;
    }

    /**
     * @param ocspStatus the ocspStatus to set
     */
    public void setOcspStatus(String ocspStatus) {
        this.ocspStatus = ocspStatus;
    }

    /**
     * @return the caProvider
     */
    public String getCaProvider() {
        return caProvider;
    }

    /**
     * @param caProvider the caProvider to set
     */
    public void setCaProvider(String caProvider) {
        this.caProvider = caProvider;
    }

    /**
     * @return the certValidityStatus
     */
    public String getCertValidityStatus() {
        return certValidityStatus;
    }

    /**
     * @param certValidityStatus the certValidityStatus to set
     */
    public void setCertValidityStatus(String certValidityStatus) {
        this.certValidityStatus = certValidityStatus;
    }

    /**
     * @return the caValidityStatus
     */
    public String getCaValidityStatus() {
        return caValidityStatus;
    }

    /**
     * @param caValidityStatus the caValidityStatus to set
     */
    public void setCaValidityStatus(String caValidityStatus) {
        this.caValidityStatus = caValidityStatus;
    }

    /**
     * @return the crlValidityStatus
     */
    public String getCrlValidityStatus() {
        return crlValidityStatus;
    }

    /**
     * @param crlValidityStatus the crlValidityStatus to set
     */
    public void setCrlValidityStatus(String crlValidityStatus) {
        this.crlValidityStatus = crlValidityStatus;
    }
}
