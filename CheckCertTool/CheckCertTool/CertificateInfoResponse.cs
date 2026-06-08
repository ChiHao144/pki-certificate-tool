using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckCertTool
{
    public class CertificateInfoResponse
    {
        public string subject { get; set; }
        public string issuer { get; set; }
        public string serialNumber { get; set; }
        public string validFrom { get; set; }
        public string validTo { get; set; }
        public string crlStatus { get; set; }
        public string ocspStatus { get; set; }
        public string caProvider { get; set; }
    }
}
