using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CheckCertTool
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        public Form1()
        {
            InitializeComponent();

            lblCaProvider.Text = string.Empty;
            lblSerialNumber.Text = string.Empty;
            lblValidFrom.Text = string.Empty;
            lblValidTo.Text = string.Empty;
            lblCrlStatus.Text = string.Empty;
            lblOcspStatus.Text = string.Empty;

            // ĐĂNG KÝ SỰ KIỆN: Ép Form lắng nghe lúc bật và lúc tắt để điều khiển Java
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void txtUserFile_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtCaFile_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnBrowseCa_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtCaFile.Text = ofd.FileName;  // đổ đường dẫn file vào textbox
                }    
            }    
        }

        private void btnBrowseUser_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt|All files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtUserFile.Text = ofd.FileName;    // đổ đường dẫn file vào textbox
                }    
            }    
        }

        private void txtResult_TextChanged(object sender, EventArgs e)
        {

        }

        private async void btnCheck_Click(object sender, EventArgs e)
        {
            string userPath = txtUserFile.Text;
            string caPath = txtCaFile.Text;

            // 1. Kiểm tra ràng buộc đầu vào dữ liệu mẫu
            if (string.IsNullOrEmpty(userPath) || string.IsNullOrEmpty(caPath))
            {
                MessageBox.Show("Vui lòng chọn đầy đủ cả 2 tệp tin chứng chỉ trước khi kiểm tra!",
                                "Thông báo vận hành", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtResult.Text = "Hệ thống đang kết nối luồng xử lý...";
            btnCheck.Enabled = false; // Khóa nút bấm lại tránh người dùng spam click khi đang đợi mạng

            try
            {
                // 2. Đóng gói dữ liệu dạng MultipartFormData tương tự cấu trúc Form bên React
                using (var formData = new MultipartFormDataContent())
                {
                    // Đọc tệp tin User Certificate và nạp vào gói tin
                    var userStream = new FileStream(userPath, FileMode.Open, FileAccess.Read);
                    var userContent = new StreamContent(userStream);
                    formData.Add(userContent, "userFile", Path.GetFileName(userPath));

                    // Đọc tệp tin CA Certificate và nạp vào gói tin
                    var caStream = new FileStream(caPath, FileMode.Open, FileAccess.Read);
                    var caContent = new StreamContent(caStream);
                    formData.Add(caContent, "caFile", Path.GetFileName(caPath));

                    // 3. LIÊN KẾT LIÊN TẦNG: Bắn gói tin HTTP POST sang cổng Backend Spring Boot
                    // Hãy khớp đúng đường dẫn endpoint trong CertificateController.java của bạn
                    string urlTarget = "http://localhost:8080/certificate/info";
                    HttpResponseMessage response = await client.PostAsync(urlTarget, formData);

                    if (response.IsSuccessStatusCode)
                    {
                        // 1. Đọc chuỗi JSON thô từ mạng về
                        string jsonResponse = await response.Content.ReadAsStringAsync();

                        string prettyJson = JToken.Parse(jsonResponse).ToString(Formatting.Indented);

                        // Đổ nguyên chuỗi dữ liệu sạch lên màn hình hiển thị TextBox
                        txtResult.Text = prettyJson;

                        // 2. Chuyển dịch chuỗi JSON thô thành đối tượng C# mẫu mực
                        CertificateInfoResponse result = JsonConvert.DeserializeObject<CertificateInfoResponse>(jsonResponse);

                        // 3. Đổ từng trường dữ liệu sạch lên các nhãn giao diện riêng biệt
                        lblCaProvider.Text = "Nhà cung cấp (CA): " + result.caProvider;
                        lblSerialNumber.Text = "Số Serial (Hex): " + result.serialNumber.ToUpper();
                        lblValidFrom.Text = "Có hiệu lực từ: " + result.validFrom;
                        lblValidTo.Text = "Hết hạn vào: " + result.validTo;

                        // 4. Xử lý hiển thị màu sắc trực quan cho trạng thái kiểm tra tĩnh CRL
                        lblCrlStatus.Text = "Trạng thái CRL: " + result.crlStatus;
                        if (result.crlStatus == "VALID")
                        {
                            lblCrlStatus.ForeColor = System.Drawing.Color.Green;
                        }
                        else
                        {
                            lblCrlStatus.ForeColor = System.Drawing.Color.Red;
                        }

                        // 5. Xử lý hiển thị màu sắc thông minh cho trạng thái thời gian thực OCSP
                        if (result.ocspStatus.Contains("6") || result.ocspStatus == "UNAUTHORIZED_BY_CA")
                        {
                            lblOcspStatus.Text = "Trạng thái OCSP: UNAUTHORIZED_BY_CA (* Bị chặn truy vấn tự do)";
                            lblOcspStatus.ForeColor = System.Drawing.Color.OrangeRed;
                        }
                        else
                        {
                            lblOcspStatus.Text = "Trạng thái OCSP: " + result.ocspStatus;
                            lblOcspStatus.ForeColor = (result.ocspStatus == "GOOD" || result.ocspStatus == "VALID")
                                ? System.Drawing.Color.Green : System.Drawing.Color.Red;
                        }

                        MessageBox.Show("Xác thực và phân tích dữ liệu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                txtResult.Text = "Lỗi kết nối liên tầng: " + ex.Message;
                MessageBox.Show("Không thể kết nối đến core Java ngầm. Vui lòng kiểm tra xem Spring Boot đã khởi động chưa!",
                                "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCheck.Enabled = true; // Mở khóa lại nút bấm sau khi xử lý xong
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Khởi tạo một tiến trình hệ thống để điều khiển Windows
                System.Diagnostics.Process startJava = new System.Diagnostics.Process();

                // Chỉ định tên tệp thực thi Java chạy ngầm nằm chung thư mục
                startJava.StartInfo.FileName = "JavaBackend.exe";

                // Ép cấu trúc Windows ẩn hoàn toàn cửa sổ dòng lệnh (cmd), chạy ngầm 100%
                startJava.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startJava.StartInfo.CreateNoWindow = true;

                // Kích hoạt Server nền Spring Boot
                startJava.Start();
            }
            catch (Exception ex)
            {
                // Hiển thị thông báo nếu người vận hành xếp thiếu file trong thư mục
                MessageBox.Show("Không tìm thấy tệp core xử lý nền (JavaBackend.exe)! " + ex.Message,
                                "Lỗi cấu trúc Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Tìm kiếm xem có tiến trình nào tên là "JavaBackend" đang chạy ngầm không
                foreach (var process in System.Diagnostics.Process.GetProcessesByName("JavaBackend"))
                {
                    // Ra lệnh đóng và giải phóng hoàn toàn Server Java khỏi bộ nhớ RAM của Windows
                    process.Kill();
                }
            }
            catch (Exception)
            {
                // Bỏ qua nếu tiến trình đã tự đóng trước đó
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
