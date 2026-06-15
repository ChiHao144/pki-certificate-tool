package com.pch.pkitool.controller;

import com.pch.pkitool.dto.CertificateInfoResponse;
import com.pch.pkitool.service.CertificateService;
import java.io.File;
import java.io.FileNotFoundException;
import java.io.IOException;
import java.security.cert.CRLException;
import java.security.cert.CertificateException;
import java.util.ArrayList;
import java.util.List;
import org.bouncycastle.cert.ocsp.OCSPException;
import org.bouncycastle.operator.OperatorCreationException;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.CrossOrigin;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.multipart.MultipartFile;

/**
 *
 * @author Chi Hao
 */
@RestController
@RequestMapping("certificate")
@CrossOrigin
public class CertificateController {

    @Autowired
    private CertificateService certificateService;

    @PostMapping("/info")
    public CertificateInfoResponse getCertificateInfoResponse(@RequestParam("userFile") MultipartFile userFile,
            @RequestParam(value = "caFile", required = false) MultipartFile caFile) throws CertificateException, IOException, FileNotFoundException, CRLException, OperatorCreationException, OCSPException {
        return certificateService.readCertificate(userFile, caFile);
    }

    @PostMapping("/upload-ca")
    public ResponseEntity<String> uploadCaFile(
            @RequestParam("caFile") MultipartFile caFile) {

        try {

            String caName = certificateService.saveCaToTrustStore(caFile);
            return ResponseEntity.ok(caName);

        } catch (IllegalArgumentException e) {

            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body("INVALID_CA_FILE_IS_USER");

        } catch (Exception e) {

            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(e.getMessage());
        }
    }

    @GetMapping("/list-ca")
    public ResponseEntity<List<String>> getTrustStoreCaList() {
        List<String> caList = new ArrayList<>();
        File rootFolder = new File("DataCrlOcsp");
        if (rootFolder.exists() && rootFolder.isDirectory()) {
            File[] folders = rootFolder.listFiles(File::isDirectory);
            if (folders != null) {
                for (File folder : folders) {
                    File[] cerFiles = folder.listFiles(
                            (dir, name) -> name.toLowerCase().endsWith(".cer")
                    );
                    if (cerFiles != null && cerFiles.length > 0) {
                        caList.add(folder.getName());
                    }
                }
            }
        }
        return ResponseEntity.ok(caList);
    }

    @GetMapping("/ca/{caName}")
    public ResponseEntity<?> getInfoCa(
            @PathVariable String caName) {

        File caFolder = new File("DataCrlOcsp/" + caName);

        if (!caFolder.exists() || !caFolder.isDirectory()) {
            return ResponseEntity.badRequest()
                    .body("CA not found");
        }

        File[] cerFiles = caFolder.listFiles(
                (dir, name) -> name.toLowerCase().endsWith(".cer")
        );

        if (cerFiles == null || cerFiles.length == 0) {
            return ResponseEntity.badRequest()
                    .body("No certificate found");
        }

        List<String> certNames = new ArrayList<>();

        for (File file : cerFiles) {
            certNames.add(file.getName());
        }
        
        return ResponseEntity.ok(certNames);
    }
    
    // API thực hiện Health Check hệ thống dựa trên thư mục CA được chỉ định
    @PostMapping("/check-by-folder/{caName}")
    public ResponseEntity<?> checkCertificateByFolder(@PathVariable String caName) {
        try {
            // Gọi xuống Service để bóc tách cặp file ca.cer và user.cer trong thư mục caName
            CertificateInfoResponse response = certificateService.processCaAndUserFromFolder(caName);
            return ResponseEntity.ok(response);
        } catch (Exception e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR).body("Lỗi hệ thống ngầm: " + e.getMessage());
        }
    }
}
