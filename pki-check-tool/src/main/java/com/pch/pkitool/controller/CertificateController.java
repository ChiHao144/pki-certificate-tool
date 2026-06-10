package com.pch.pkitool.controller;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.service.CertificateService;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.security.cert.CRLException;
import java.security.cert.CertificateException;
import org.bouncycastle.cert.ocsp.OCSPException;
import org.bouncycastle.operator.OperatorCreationException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

/**
 *
 * @author Chi Hao
 */
@RestController // Biến class thành 1 API để trả về dạng JSON
@RequestMapping("certificate") // Đường dẫn gốc cho toàn bộ API 
@CrossOrigin
public class CertificateController {

    @Autowired // Kích hoạt cơ chế injection
    private CertificateService certificateService; // Khai báo biến đại diện ở tầng nghiệp vụ

    @PostMapping("/info") // User bây giờ chỉ cần upload userFile
    public CertificateInfoResponse getCertificateInfoResponse(@RequestParam("userFile") MultipartFile userFile,
            @RequestParam(value = "caFile", required = false) MultipartFile caFile) throws CertificateException, IOException, FileNotFoundException, CRLException, OperatorCreationException, OCSPException {
        // Gọi tần service xử lý trả dữ liệu về cho client
        return certificateService.readCertificate(userFile, caFile);
    }

    @PostMapping("/upload-ca")
    public ResponseEntity<String> uploadCaFile(@RequestParam("caFile") MultipartFile caFile) {
        try {
            String caName = certificateService.saveCaToTrustStore(caFile);
            return ResponseEntity.ok(caName);
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(e.getMessage());
        }
    }
}
