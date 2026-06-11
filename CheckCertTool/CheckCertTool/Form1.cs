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

            // Kiểm tra đầu vào
            if (string.IsNullOrEmpty(userPath))
            {
                MessageBox.Show("Vui lòng chọn tệp tin chứng chỉ trước khi kiểm tra!",
                                "Thông báo vận hành", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            txtResult.Text = "Hệ thống đang kết nối luồng xử lý và tải tệp tin...";
            btnCheck.Enabled = false;
       
            lblCaProvider.Text = string.Empty;
            lblSerialNumber.Text = string.Empty;
            lblValidFrom.Text = string.Empty;
            lblValidTo.Text = string.Empty;
            lblCrlStatus.Text = string.Empty;
            lblOcspStatus.Text = string.Empty;

            try
            {
                using (var formData = new MultipartFormDataContent())
                {
                    // Mở luồng đọc file user certificate
                    using (var userStream = new FileStream(userPath, FileMode.Open, FileAccess.Read))
                    {
                        var userContent = new StreamContent(userStream);
                        userContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        formData.Add(userContent, "userFile", Path.GetFileName(userPath));

                        // Gọi API POST thông tin để nhận về kết quả
                        string urlTarget = "http://localhost:8080/certificate/info";
                        HttpResponseMessage response = await client.PostAsync(urlTarget, formData);

                        if (response.IsSuccessStatusCode)
                        {                           
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            CertificateInfoResponse result = JsonConvert.DeserializeObject<CertificateInfoResponse>(jsonResponse);

                            // CẢNH BÁO SAI LỆCH NHÀ CUNG CẤP CA VỚI USER
                            if (result.caValidityStatus == "MISMATCHED_CA_CHAIN")
                            {
                                txtResult.Text = string.Empty;
                                MessageBox.Show("CẢNH BÁO NGUY HIỂM:\nTệp tin chứng chỉ CA được chọn không trùng khớp với chữ ký gốc trên chứng chỉ User!\nLuồng kiểm tra chuỗi tin cậy (Chain of Trust) bị thất bại.",
                                                "Sai lệch nhà cấp phát CA", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                return;
                            }

                            // HIỂN THỊ LỖI KHI CA HẾT HẠN - KHÔNG TÌM THẤY CA PHÙ HỢP ĐỂ KIỂM TRA
                            if (result.caValidityStatus == "NOT_FOUND_IN_TRUSTSTORE" || result.caValidityStatus == "EXPIRED")
                            {
                                string warningMsg = result.caValidityStatus == "EXPIRED"
                                    ? $"Chứng chỉ CA tự động của mạng [{result.caProvider}] đã hết hạn sử dụng!\nBạn có muốn tự chọn file CA khác từ máy tính để kiểm tra dự phòng không?"
                                    : $"Không tìm thấy file CA của mạng [{result.caProvider}] trong kho!\nBạn có muốn tự chọn file CA bằng tay không?";

                                DialogResult dialogResult = MessageBox.Show(warningMsg, "Hệ thống CA có bất ổn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                                if (dialogResult == DialogResult.Yes)
                                {
                                    using (OpenFileDialog openFileDialog = new OpenFileDialog())
                                    {
                                        openFileDialog.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt";
                                        openFileDialog.Title = "Chọn file CA dự phòng để kiểm tra";
                                        
                                        // Mở luồng browser file CA mới để kiểm tra user nếu CA đó hết hạn hoặc lỗi
                                        if (openFileDialog.ShowDialog() == DialogResult.OK)
                                        {
                                            string manualCaPath = openFileDialog.FileName;

                                            await ReCheckWithManualCA(userPath, manualCaPath);
                                            return; // Thoát khỏi luồng
                                        }
                                    }
                                }
                                if (dialogResult == DialogResult.No)
                                {
                                    txtResult.Text = "";
                                }    
                                if (result.caValidityStatus == "NOT_FOUND_IN_TRUSTSTORE") return;
                            }
                            string prettyJson = JToken.Parse(jsonResponse).ToString(Formatting.Indented);
                            txtResult.Text = prettyJson;

                            // 1. TRƯỜNG HỢP NGOẠI LỆ: Thiếu file CA trong kho lưu trữ TrustStore
                            if (result.caValidityStatus == "NOT_FOUND_IN_TRUSTSTORE")
                            {
                                txtResult.Text = string.Empty;
                                MessageBox.Show($"Hệ thống chưa tích hợp tệp chứng chỉ CA của nhà mạng [{result.caProvider}] vào kho TrustStore!\nVui lòng sao chép tệp tin `{result.caProvider}.cer` vào thư mục hệ thống.",
                                                "Thiếu dữ liệu hạ tầng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            // 2. ĐỔ DỮ LIỆU ĐỊNH DANH CƠ BẢN LÊN GIAO DIỆN
                            lblValidFrom.Text = "Có hiệu lực từ: " + result.validFrom;
                            lblValidTo.Text = "Hết hạn vào: " + result.validTo;

                            // 3. HIỂN THỊ TRẠNG THÁI VÀ MÀU SẮC HẠN CHỨNG CHỈ USER 
                            if (result.certValidityStatus == "EXPIRED")
                            {
                                lblSerialNumber.Text = "Số Serial (Hex): " + result.serialNumber.ToUpper() + " (* HẾT HẠN)";
                                lblSerialNumber.ForeColor = System.Drawing.Color.Red;
                            }
                            else
                            {
                                lblSerialNumber.Text = "Số Serial (Hex): " + result.serialNumber.ToUpper();
                                lblSerialNumber.ForeColor = System.Drawing.Color.DarkBlue;
                            }

                            // 4. HIỂN THỊ TRẠNG THÁI VÀ CẢNH BÁO CHỨNG CHỈ CA NỀN 
                            if (result.caValidityStatus == "EXPIRED")
                            {
                                lblCaProvider.Text = "Nhà cung cấp (CA): " + result.caProvider + " (* CA ĐÃ HẾT HẠN!)";
                                lblCaProvider.ForeColor = System.Drawing.Color.DarkRed;
                            }
                            else
                            {
                                lblCaProvider.Text = "Nhà cung cấp (CA): " + result.caProvider;
                                lblCaProvider.ForeColor = System.Drawing.Color.Black;
                            }

                            // 5. HIỂN THỊ VÀ TÔ MÀU TRẠNG THÁI KIỂM TRA TĨNH CRL 
                            if (result.crlStatus == "VALID" && result.crlValidityStatus == "VALID")
                            {
                                lblCrlStatus.Text = "Trạng thái CRL: Hợp lệ (VALID)";
                                lblCrlStatus.ForeColor = System.Drawing.Color.Green;
                            }
                            else if (result.crlStatus == "CRL_EXPIRED" || result.crlValidityStatus == "EXPIRED")
                            {
                                lblCrlStatus.Text = "Trạng thái CRL: Bản tin tĩnh từ CA đã quá hạn cập nhật (CRL_EXPIRED)";
                                lblCrlStatus.ForeColor = System.Drawing.Color.OrangeRed;
                            }
                            else if (result.crlStatus == "REVOKED")
                            {
                                lblCrlStatus.Text = "Trạng thái CRL: Đã bị thu hồi hoặc khóa (REVOKED)";
                                lblCrlStatus.ForeColor = System.Drawing.Color.Red;
                            }
                            else
                            {
                                lblCrlStatus.Text = "Trạng thái CRL: " + result.crlStatus;
                                lblCrlStatus.ForeColor = System.Drawing.Color.DarkOrange;
                            }

                            // 6. HIỂN THỊ VÀ TÔ MÀU TRẠNG THÁI TRỰC TUYẾN OCSP
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
            }
            catch (Exception ex)
            {
                txtResult.Text = "Lỗi kết nối liên tầng: " + ex.Message;
                MessageBox.Show("Không thể kết nối đến core Java ngầm. Vui lòng kiểm tra xem Spring Boot đã khởi động chưa!",
                                "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCheck.Enabled = true; // Nút được bật lại kể cả khi bị lỗi kết nối
            }
        }

        // Muốn chọn file CA certificate thủ công khi gặp vấn đề về hết hạn, không có CA trong kho lưu trữ
        private async Task ReCheckWithManualCA(string userPath, string manualCaPath)
        {
            try
            {
                txtResult.Text = "Đang kiểm tra dự phòng với file CA chọn thủ công...";
                using (var formData = new MultipartFormDataContent())
                {
                    using (var userStream = new FileStream(userPath, FileMode.Open, FileAccess.Read))
                    using (var caStream = new FileStream(manualCaPath, FileMode.Open, FileAccess.Read))
                    {
                        var userContent = new StreamContent(userStream);
                        userContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        formData.Add(userContent, "userFile", Path.GetFileName(userPath));

                        var caContent = new StreamContent(caStream);
                        caContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                        formData.Add(caContent, "caFile", Path.GetFileName(manualCaPath));

                        HttpResponseMessage response = await client.PostAsync("http://localhost:8080/certificate/info", formData);
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonResponse = await response.Content.ReadAsStringAsync();
                            txtResult.Text = JToken.Parse(jsonResponse).ToString(Formatting.Indented);

                            // Display response values directly onto the UI labels
                            CertificateInfoResponse result = JsonConvert.DeserializeObject<CertificateInfoResponse>(jsonResponse);
                            if (result.caValidityStatus == "MISMATCHED_CA_CHAIN")
                            {
                                txtResult.Text = string.Empty;
                                MessageBox.Show("CẢNH BÁO NGUY HIỂM:\nTệp tin chứng chỉ CA được chọn không trùng khớp với chữ ký gốc trên chứng chỉ User!\nLuồng kiểm tra chuỗi tin cậy (Chain of Trust) bị thất bại.",
                                                "Sai lệch nhà cấp phát CA", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                                return;
                            }

                            lblValidFrom.Text = "Có hiệu lực từ: " + result.validFrom;
                            lblValidTo.Text = "Hết hạn vào: " + result.validTo;
                            lblCaProvider.Text = "Nhà cung cấp (CA thủ công): " + result.caProvider;
                            lblSerialNumber.Text = "Số Serial (Hex): " + result.serialNumber.ToUpper();
                            lblCrlStatus.Text = "Trạng thái CRL: " + result.crlStatus;
                            lblOcspStatus.Text = "Trạng thái OCSP: " + result.ocspStatus;

                            MessageBox.Show("Kiểm tra dự phòng bằng CA thủ công hoàn tất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi luồng dự phòng: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        // THÊM CA MỚI VÀO KHO LƯU TRỮ 
        private async void btnAddNewCa_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt";
                openFileDialog.Title = "Chọn file CA để tự động tích hợp";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string sourceFilePath = openFileDialog.FileName;
                    btnAddNewCa.Enabled = false; // Khóa nút bấm để tránh spam click

                    try
                    {
                        using (var formData = new MultipartFormDataContent())
                        {
                            using (var caStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                            {
                                var caContent = new StreamContent(caStream);
                                caContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                formData.Add(caContent, "caFile", Path.GetFileName(sourceFilePath));

                                // Bắn file sang API Java để Java tự lưu vào đúng vị trí của nó
                                HttpResponseMessage response = await client.PostAsync("http://localhost:8080/certificate/upload-ca", formData);

                                if (response.IsSuccessStatusCode)
                                {
                                    string caNameParsed = await response.Content.ReadAsStringAsync();
                                    MessageBox.Show($"Hệ thống đã tự động lưu file vào đúng kho ngầm của Java: [{caNameParsed}]!",
                                                    "Tự động hóa hoàn tất", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi kết nối luồng nạp tự động: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnAddNewCa.Enabled = true; // Mở lại nút bấm
                    }
                }
            }
        }
    }
}
