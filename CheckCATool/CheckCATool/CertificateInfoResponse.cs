using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CheckCATool
{
    public class CertificateInfoResponse
    {
        public string Subject { get; set; }
        public string Issuer { get; set; }
        public string SerialNumber { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
        public string CrlStatus { get; set; }
        public string OcspStatus { get; set; }
        public string CaProvider { get; set; }
        public string CertValidityStatus { get; set; }
        public string CaValidityStatus { get; set; }
        public string CrlValidityStatus { get; set; }
        public string UserSubject { get; set; }
        public string UserSerialNumber { get; set; }
        public string UserValidTo { get; set; }
    }
}
