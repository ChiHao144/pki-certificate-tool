package com.pch.pkitool.controller;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;

/**
 *
 * @author Chi Hao
 */
@RestController
public class TestController {

    @GetMapping("/test")
    public String test() {

        return "PKI TOOL OK";
    }
}
