using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Net.NetworkInformation;

namespace CheckCATool
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        private List<string> originalCaList = new List<string>();
        public Form1()
        {
            InitializeComponent();
            ClearLabels();

            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        }

        private void ClearLabels()
        {
            label2.Text = "";
            label3.Text = "";
            label4.Text = "";
            label5.Text = "";
            label6.Text = "";
            label7.Text = "";
            label8.Text = "";
            label9.Text = "";
            label10.Text = "";
            label11.Text = "";
        }
        private async void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                // Dọn dẹp các tiến trình JavaBackend cũ bị treo từ trước (nếu có) để tránh xung đột cổng 8080
                foreach (var process in System.Diagnostics.Process.GetProcessesByName("JavaBackend"))
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(1000); // Chờ tiến trình cũ tắt hẳn
                    }
                    catch { }
                }

                // Khởi động Server Spring Boot ngầm chạy song song
                System.Diagnostics.Process startJava = new System.Diagnostics.Process();
                string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JavaBackend.exe");
                startJava.StartInfo.FileName = exePath;
                startJava.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                startJava.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startJava.StartInfo.CreateNoWindow = true;
                startJava.Start();

                // Hiển thị thông báo trạng thái khởi động dịch vụ nền
                label8.Text = "Đang kết nối và khởi động\nVui lòng đợi...";
                label8.ForeColor = Color.Blue;
                label8.Refresh();

                // Chờ 2 giây để đảm bảo Spring Boot khởi động xong cổng 8080 rồi mới nạp ComboBox
                await Task.Delay(2000);
                await LoadCaToComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tìm thấy tệp core xử lý nền (JavaBackend.exe)! " + ex.Message,
                                "Lỗi cấu trúc Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Xóa trạng thái thông báo khi đã tải xong hoặc gặp lỗi
                label8.Text = string.Empty;
            }
        }

        // Hàm tự động gọi API lấy danh sách các file CA đổ vào ComboBox
        private async Task LoadCaToComboBox()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("http://localhost:8080/certificate/list-ca");
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    List<string> caList = JsonConvert.DeserializeObject<List<string>>(jsonResponse);

                    originalCaList = caList ?? new List<string>();
                    FilterCaComboBox();

                    if (originalCaList.Count == 0)
                    {
                        MessageBox.Show("Thư mục kho lưu trữ TrustStore cục bộ hiện đang trống rỗng!\nVui lòng nạp thêm file CA trước.",
                                        "Thông báo hạ tầng", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể kết nối đến core Java để tải danh sách kho CA: " + ex.Message,
                                "Lỗi kết nối liên tầng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                foreach (var process in System.Diagnostics.Process.GetProcessesByName("JavaBackend"))
                {
                    process.Kill(); // Giải phóng Server Java ngầm khỏi RAM Windows khi tắt Form
                }
            }
            catch (Exception) { }
        }

        private async void btnCheckCa_Click(object sender, EventArgs e)
        {
            if (cbCaTrustStore.SelectedItem == null || cbCaTrustStore.SelectedIndex == 0)
            {
                MessageBox.Show("Vui lòng chọn một cấu trúc CA hệ thống để kiểm tra!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Xóa sạch kết quả cũ trước khi thực hiện kiểm tra hoặc chuyển hướng nạp file
            ClearLabels();
            txtUserCert.Text = string.Empty;

            string selectedFolderName = cbCaTrustStore.SelectedItem.ToString(); // Rút ra chữ "VNPT" chẳng hạn

            // Kiểm tra xem CA đã có cấu hình chứng chỉ EndUser chưa
            try
            {
                HttpResponseMessage caFilesResponse = await client.GetAsync($"http://localhost:8080/certificate/ca/{selectedFolderName}");
                if (caFilesResponse.IsSuccessStatusCode)
                {
                    string jsonCaFiles = await caFilesResponse.Content.ReadAsStringAsync();
                    List<string> certFiles = JsonConvert.DeserializeObject<List<string>>(jsonCaFiles);
                    bool hasUserCert = certFiles != null && certFiles.Any(f => f.Equals("user.cer", StringComparison.OrdinalIgnoreCase));
                    if (!hasUserCert)
                    {
                        MessageBox.Show("Nhà cung cấp CA này hiện chưa có cấu hình chứng chỉ EndUser!\nHệ thống sẽ mở hộp thoại để chọn file chứng chỉ User mới để đối soát.", 
                                        "Thiếu chứng chỉ EndUser", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnAddUserCert_Click(sender, e);
                        return;
                    }
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi và để luồng chính xử lý tiếp
            }

            btnCheckCa.Enabled = false;
            label8.Text = "Hệ thống đang xử lý\nVui lòng chờ trong giây lát...";
            label8.ForeColor = Color.Red;


            try
            {


                // Bắn API dạng POST kèm tên biến caName truyền thẳng trên URL
                string urlApi = $"http://localhost:8080/certificate/check-by-folder/{selectedFolderName}";
                HttpResponseMessage response = await client.PostAsync(urlApi, null); // Không cần đóng gói formData phức tạp nữa!

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    CertificateInfoResponse result = JsonConvert.DeserializeObject<CertificateInfoResponse>(jsonResponse);

                    // === PHÂN CHIA ĐỀU NỘI DUNG RA CÁC LABEL ===

                    // LABLE 2: Đơn vị định danh CA
                    label2.Text = $"1. Nhà cung cấp: {result.CaProvider}";
                    label2.ForeColor = System.Drawing.Color.DarkBlue;

                    // LABLE 3: Chuỗi Subject đầy đủ của CA
                    label3.Text = $"2. Thông tin CA: {result.Subject}";
                    label3.ForeColor = System.Drawing.Color.Black;

                    // LABLE 4: Mã Serial Number của CA (Định dạng viết hoa)
                    label4.Text = $"3. Mã Serial: {result.SerialNumber.ToUpper()}";
                    label4.ForeColor = System.Drawing.Color.Black;

                    // LABLE 5: Thời hạn vận hành hệ thống CA kèm logic cảnh báo quá hạn
                    if (result.CaValidityStatus == "EXPIRED")
                    {
                        label5.Text = $"4. Thời hạn chứng chỉ: Hết hạn lúc {result.ValidTo} (* CA ĐÃ QUÁ HẠN!)";
                        label5.ForeColor = System.Drawing.Color.Red;
                    }
                    else
                    {
                        label5.Text = $"4. Thời hạn chứng chỉ: Từ {result.ValidFrom} đến {result.ValidTo}";
                        label5.ForeColor = System.Drawing.Color.Black;
                    }

                    // LABLE 6: Kết quả phân tích đường truyền danh sách thu hồi CRL
                    label6.Text = $"5. Trạng thái CRL: {result.CrlStatus}";
                    if (result.CrlStatus.Contains("VALID"))
                    {
                        label6.ForeColor = System.Drawing.Color.Green;
                    }
                    else
                    {
                        label6.ForeColor = System.Drawing.Color.OrangeRed;
                    }

                    // LABLE 7: Kết quả truy vấn máy chủ phản hồi trực tuyến OCSP
                    label7.Text = $"6. Trạng thái OCSP: {result.OcspStatus}";
                    if (result.OcspStatus.Contains("GOOD") || result.OcspStatus.Contains("VALID"))
                    {
                        label7.ForeColor = System.Drawing.Color.Green;
                    }
                    else
                    {
                        label7.ForeColor = System.Drawing.Color.OrangeRed;
                    }

                    string displayName = result.UserSubject;
                    if (result.UserSubject.Contains("CN="))
                    {
                        string[] parts = result.UserSubject.Split(',');
                        string cnPart = parts.FirstOrDefault(p => p.Trim().StartsWith("CN="));
                        if (cnPart != null) displayName = cnPart.Replace("CN=", "").Trim();
                    }
                    label9.Text = $"7. Tên khách hàng: {displayName}";
                    label9.ForeColor = Color.DarkBlue;

                    // Trường 2: Hiển thị mã định danh Serial của User Cert (Viết hoa cho chuẩn IT)
                    label10.Text = $"8. Mã Serial: {result.UserSerialNumber.ToUpper()}";
                    label10.ForeColor = Color.Black;

                    // Trường 3: Hiển thị ngày hết hạn của User Cert kèm logic đổi màu đỏ nếu đã hết hạn
                    if (result.CertValidityStatus == "EXPIRED")
                    {
                        label11.Text = $"9. Thời hạn chứng chỉ: {result.UserValidTo} (* ĐÃ HẾT HẠN SỬ DỤNG!)";
                        label11.ForeColor = Color.Red;
                    }
                    else
                    {
                        label11.Text = $"9. Thời hạn chứng chỉ: {result.UserValidTo} (Đang hoạt động)";
                        label11.ForeColor = Color.Green;
                    }

                    // In log JSON thô ra ô textbox lớn 
                    //txtResult.Text = JToken.Parse(jsonResponse).ToString(Formatting.Indented);
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(errorMsg, "Lỗi kiểm tra", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi luồng kết nối API: " + ex.Message, "Lỗi kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnCheckCa.Enabled = true;
                label8.Text = string.Empty;
            }
        }

        private void cbCaTrustStore_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {
        }
        private void label3_Click(object sender, EventArgs e)
        {
        }

        private async void btnAddUserCert_Click(object sender, EventArgs e)
        {
            // 1. Kiểm tra chốt chặn: Người dùng bắt buộc phải chọn CA từ ComboBox trước
            if (cbCaTrustStore.SelectedItem == null || cbCaTrustStore.SelectedIndex == 0)
            {
                MessageBox.Show("Vui lòng chọn một cấu trúc CA hệ thống trước khi nạp file User mới!",
                                "Thông báo hạ tầng", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Lấy tên thư mục CA đã chọn (Ví dụ: "VNPT", "Viettel")
            string selectedFolderName = cbCaTrustStore.SelectedItem.ToString();

            // 2. Kích hoạt hộp thoại chọn file từ ổ cứng
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt|All files (*.*)|*.*";
                ofd.Title = "Chọn file chứng chỉ User mới để đối soát";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;
                    txtUserCert.Text = ofd.FileName;

                    // Chuẩn bị giao diện trước khi gọi API
                    ClearLabels();
                    btnAddUserCert.Enabled = false; // Vô hiệu hóa nút bấm để tránh double-click
                    label8.Text = "Hệ thống đang xử lý\nVui lòng chờ trong giây lát...";
                    label8.ForeColor = Color.Red;

                    try
                    {
                        // Đọc file chứng chỉ vừa chọn thành luồng byte nhị phân
                        byte[] fileBytes = File.ReadAllBytes(filePath);

                        // 3. Đóng gói gói tin đa phần tử (MultipartFormDataContent) để gửi lên Java Backend
                        using (var content = new MultipartFormDataContent())
                        {
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                            // "file" phải trùng khớp với @RequestParam("file") phía Spring Boot Service
                            content.Add(fileContent, "file", Path.GetFileName(filePath));

                            // Xóa cấu hình cache cũ nếu có
                            client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                            {
                                NoCache = true,
                                NoStore = true
                            };

                            // Bước 4: Gọi API 1 - Kiểm tra tạm thời cấu trúc chứng chỉ trên RAM
                            string checkUrl = $"http://localhost:8080/certificate/check-temp/{selectedFolderName}";
                            HttpResponseMessage response = await client.PostAsync(checkUrl, content);

                            label8.Text = string.Empty;
                            label8.Refresh();

                            if (response.IsSuccessStatusCode)
                            {
                                string jsonResponse = await response.Content.ReadAsStringAsync();
                                CertificateInfoResponse result = JsonConvert.DeserializeObject<CertificateInfoResponse>(jsonResponse);

                                // === ĐỔ DỮ LIỆU ĐỐI SOÁT RA CÁC LABEL TRÊN FORM ===
                                label2.Text = $"1. Nhà cung cấp: {result.CaProvider}";
                                label2.ForeColor = Color.DarkBlue;

                                label3.Text = $"2. Thông tin CA: {result.Subject}";
                                label3.ForeColor = Color.Black;

                                label4.Text = $"3. Mã Serial: {result.SerialNumber.ToUpper()}";
                                label4.ForeColor = Color.Black;

                                if (result.CaValidityStatus == "EXPIRED")
                                {
                                    label5.Text = $"4. Thời hạn chứng chỉ: Hết hạn lúc {result.ValidTo} (* CA ĐÃ QUÁ HẠN!)";
                                    label5.ForeColor = Color.Red;
                                }
                                else
                                {
                                    label5.Text = $"4. Thời hạn chứng chỉ: Từ {result.ValidFrom} đến {result.ValidTo}";
                                    label5.ForeColor = Color.Black;
                                }

                                label6.Text = $"5. Trạng thái CRL: {result.CrlStatus}";
                                label6.ForeColor = result.CrlStatus.Contains("VALID") ? Color.Green : Color.OrangeRed;

                                label7.Text = $"6. Trạng thái OCSP: {result.OcspStatus}";
                                label7.ForeColor = (result.OcspStatus.Contains("GOOD") || result.OcspStatus.Contains("VALID")) ? Color.Green : Color.OrangeRed;

                                string displayName = result.UserSubject;
                                if (result.UserSubject.Contains("CN="))
                                {
                                    string[] parts = result.UserSubject.Split(',');
                                    string cnPart = parts.FirstOrDefault(p => p.Trim().StartsWith("CN="));
                                    if (cnPart != null) displayName = cnPart.Replace("CN=", "").Trim();
                                }
                                label9.Text = $"7. Tên khách hàng: {displayName}";
                                label9.ForeColor = Color.DarkBlue;

                                label10.Text = $"8. Mã Serial: {result.UserSerialNumber.ToUpper()}";
                                label10.ForeColor = Color.Black;

                                if (result.CertValidityStatus == "EXPIRED")
                                {
                                    label11.Text = $"9. Thời hạn chứng chỉ: {result.UserValidTo} (* ĐÃ HẾT HẠN SỬ DỤNG!)";
                                    label11.ForeColor = Color.Red;
                                }
                                else
                                {
                                    label11.Text = $"9. Thời hạn chứng chỉ: {result.UserValidTo} (Đang hoạt động)";
                                    label11.ForeColor = Color.Green;
                                }

                                // === Bước 5: Hộp thoại xác nhận áp dụng (Apply) dữ liệu vào ổ cứng ===
                                DialogResult dialogResult = MessageBox.Show(
                                    "Đã xử lý thông tin tệp mới thành công. Bạn có muốn áp dụng và ghi đè chứng chỉ này vào hệ thống không?",
                                    "Xác nhận cập nhật hệ thống",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question
                                );

                                if (dialogResult == DialogResult.Yes)
                                {
                                    // Tái đóng gói mảng byte để bắn lên API 2 tiến hành lưu đè vật lý
                                    using (var saveContent = new MultipartFormDataContent())
                                    {
                                        var saveFileContent = new ByteArrayContent(fileBytes);
                                        saveFileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                                        saveContent.Add(saveFileContent, "file", "user.cer");

                                        string saveUrl = $"http://localhost:8080/certificate/save-user/{selectedFolderName}";
                                        HttpResponseMessage saveResponse = await client.PostAsync(saveUrl, saveContent);

                                        if (saveResponse.IsSuccessStatusCode)
                                        {
                                            MessageBox.Show("Hệ thống đã lưu đè tệp user.cer mới thành công!",
                                                            "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }
                                        else
                                        {
                                            string saveError = await saveResponse.Content.ReadAsStringAsync();
                                            MessageBox.Show("Lỗi ghi đè file vật lý: " + saveError, "Lỗi kết nối liên tầng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                        }
                                    }
                                }
                                //if (dialogResult == DialogResult.No)
                                //{
                                //    ClearLabels();
                                //}
                            }
                            else
                            {
                                string errorMsg = await response.Content.ReadAsStringAsync();
                                MessageBox.Show(errorMsg, "Lỗi kiểm tra cấu trúc tệp", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi kết nối luồng API: " + ex.Message, "Lỗi hạ tầng kết nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        // Khôi phục trạng thái nút bấm ban đầu
                        btnAddUserCert.Enabled = true;
                        label8.Text = string.Empty;
                    }
                }
            }
        }

        private async void btnAddCaNew_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Certificate Files (*.cer;*.crt)|*.cer;*.crt|All files (*.*)|*.*";
                ofd.Title = "Chọn tệp chứng chỉ CA gốc/trung gian mới cần cấu hình";
                ofd.RestoreDirectory = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filePath = ofd.FileName;

                    // Dọn dẹp giao diện và hiển thị trạng thái chờ xử lý đồ họa trực quan
                    ClearLabels();
                    btnAddCaNew.Enabled = false;
                    label8.Text = "Hệ thống đang xử lý\nVui lòng chờ trong giây lát...";
                    label8.ForeColor = Color.Red;
                    label8.Refresh(); // Ép WinForms xóa vẽ ngay lập tức

                    try
                    {
                        // Đọc file sang mảng byte nhị phân
                        byte[] fileBytes = File.ReadAllBytes(filePath);

                        // 1. Kiểm tra tính hợp lệ và sự tồn tại của CA trên hệ thống trước
                        using (var validateContent = new MultipartFormDataContent())
                        {
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                            validateContent.Add(fileContent, "file", Path.GetFileName(filePath));

                            string validateUrl = "http://localhost:8080/certificate/validate-new-ca";
                            HttpResponseMessage validateResponse = await client.PostAsync(validateUrl, validateContent);

                            if (!validateResponse.IsSuccessStatusCode)
                            {
                                string errorMsg = await validateResponse.Content.ReadAsStringAsync();
                                label8.Text = string.Empty;
                                MessageBox.Show(errorMsg, "Lỗi kiểm tra CA", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }

                        // Xóa chữ trạng thái chờ để người dùng nhập tên nhà cung cấp CA
                        label8.Text = string.Empty;
                        label8.Refresh();

                        // Lấy danh sách các CA đã có sẵn trong ComboBox để kiểm tra trùng lặp
                        List<string> existingCAs = new List<string>(originalCaList);

                        string defaultCaName = "";
                        try
                        {
                            using (var cert = new System.Security.Cryptography.X509Certificates.X509Certificate2(filePath))
                            {
                                string cn = cert.GetNameInfo(System.Security.Cryptography.X509Certificates.X509NameType.SimpleName, false);
                                if (string.IsNullOrEmpty(cn))
                                {
                                    cn = Path.GetFileNameWithoutExtension(filePath);
                                }
                                // Loại bỏ ký tự không hợp lệ cho tên file/thư mục
                                foreach (char c in Path.GetInvalidFileNameChars())
                                {
                                    cn = cn.Replace(c.ToString(), "");
                                }
                                int year = cert.NotAfter.Year;
                                defaultCaName = $"{cn}_EX_{year}".Replace(" ", "_");
                            }
                        }
                        catch (Exception)
                        {
                            defaultCaName = Path.GetFileNameWithoutExtension(filePath);
                        }

                        string caName = ShowPrompt("Nhập tên nhà cung cấp CA (ví dụ: VNPT, Viettel...):", "Cấu hình tên CA", existingCAs, defaultCaName);
                        if (string.IsNullOrWhiteSpace(caName))
                        {
                            return;
                        }

                        // Hiển thị lại trạng thái chờ khi thực hiện lưu CA
                        label8.Text = "Hệ thống đang xử lý\nVui lòng chờ trong giây lát...";
                        label8.ForeColor = Color.Red;
                        label8.Refresh();

                        using (var content = new MultipartFormDataContent())
                        {
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                            // Gắn vào tham số "file" khớp với @RequestParam("file") của Spring Boot
                            content.Add(fileContent, "file", Path.GetFileName(filePath));

                            // Gắn vào tham số "name" khớp với @RequestParam("name") của Spring Boot
                            content.Add(new StringContent(caName, Encoding.UTF8), "name");

                            // Gọi API thêm mới CA
                            string apiUrl = "http://localhost:8080/certificate/add-new-ca";
                            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                            // Xóa chữ trạng thái đỏ/xanh ngay khi nhận được phản hồi, tránh treo đồ họa
                            label8.Text = string.Empty;
                            label8.Refresh();

                            if (response.IsSuccessStatusCode)
                            {
                                string successMsg = await response.Content.ReadAsStringAsync();
                                MessageBox.Show(successMsg, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                // ĐIỂM SÁNG: Tái nạp lại danh sách ComboBox tự động
                                // Đoạn này gọi lại luồng nạp giúp cập nhật UI tức thì
                                await ReloadCaComboBox();
                            }
                            else
                            {
                                // Bắt trọn các lỗi: "Tệp là chứng chỉ USER", "Hệ thống đã tồn tại nhà cung cấp này..."
                                string errorMsg = await response.Content.ReadAsStringAsync();
                                MessageBox.Show(errorMsg, "Lỗi phân cấp chứng chỉ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        label8.Text = string.Empty;
                        MessageBox.Show("Lỗi kết nối luồng API hệ thống: " + ex.Message, "Lỗi hạ tầng", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        btnAddCaNew.Enabled = true;
                    }
                }
            }
        }

        // Hàm phụ trợ giúp làm sạch và tải lại dữ liệu cho ComboBox ngay lập tức
        private async Task ReloadCaComboBox()
        {
            try
            {
                string url = "http://localhost:8080/certificate/list-ca";
                // Cấu hình không Cache để HttpClient bắt buộc lấy dữ liệu mới từ ổ cứng Backend vừa ghi
                client.DefaultRequestHeaders.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                };

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<String> caList = JsonConvert.DeserializeObject<List<String>>(json);

                    originalCaList = caList ?? new List<string>();
                    FilterCaComboBox();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tự động đồng bộ lại danh sách CA mới: " + ex.Message, "Lỗi đồng bộ UI", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void FilterCaComboBox()
        {
            string filterText = txtSearchCa.Text.Trim();
            string selectedVal = cbCaTrustStore.SelectedItem?.ToString();

            // Lưu trữ vị trí con trỏ chuột trong ô tìm kiếm tránh bị mất tiêu điểm
            int selectionStart = txtSearchCa.SelectionStart;
            int selectionLength = txtSearchCa.SelectionLength;

            cbCaTrustStore.Items.Clear();
            cbCaTrustStore.Items.Add("--Chọn CA--");

            var filtered = originalCaList;
            if (!string.IsNullOrEmpty(filterText))
            {
                filtered = originalCaList.Where(ca => ca.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            foreach (var ca in filtered)
            {
                cbCaTrustStore.Items.Add(ca);
            }

            if (cbCaTrustStore.Items.Count > 1)
            {
                if (selectedVal != null && cbCaTrustStore.Items.Contains(selectedVal))
                {
                    cbCaTrustStore.SelectedItem = selectedVal;
                }
                else
                {
                    cbCaTrustStore.SelectedIndex = 0;
                }
            }
            else
            {
                cbCaTrustStore.SelectedIndex = 0;
            }

            // Nếu người dùng đang thực hiện gõ tìm kiếm, tự động xổ danh sách ComboBox ra để chọn trực tiếp
            if (txtSearchCa.Focused)
            {
                if (cbCaTrustStore.Items.Count > 1 && !string.IsNullOrEmpty(filterText))
                {
                    cbCaTrustStore.DroppedDown = true;
                }
                else
                {
                    cbCaTrustStore.DroppedDown = false;
                }

                // Trả lại tiêu điểm và con trỏ chuột về đúng vị trí cũ trong TextBox tìm kiếm
                txtSearchCa.Focus();
                txtSearchCa.SelectionStart = selectionStart;
                txtSearchCa.SelectionLength = selectionLength;
                Cursor.Current = Cursors.Default;
            }
        }

        private void txtSearchCa_TextChanged(object sender, EventArgs e)
        {
            FilterCaComboBox();
        }

        // Hộp thoại popup lấy input từ người dùng
        private static string ShowPrompt(string text, string caption, List<string> existingNames, string defaultValue = "")
        {
            System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
            {
                Width = 400,
                Height = 180,
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false
            };
            System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label() { Left = 20, Top = 20, Width = 360, Text = text, Font = new Font("Calibri", 11) };
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 50, Width = 340, Font = new Font("Calibri", 11), Text = defaultValue };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "Ok", Left = 260, Width = 100, Top = 90, Font = new Font("Calibri", 11) };
            
            confirmation.Click += (sender, e) => 
            {
                string input = textBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("Tên nhà cung cấp CA không được để trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Giữ nguyên popup mở để nhập lại
                }

                string normalizedInput = input.Replace(" ", "_");
                if (existingNames != null && existingNames.Any(name => name.Replace(" ", "_").Equals(normalizedInput, StringComparison.OrdinalIgnoreCase)))
                {
                    MessageBox.Show("Tên nhà cung cấp CA này đã tồn tại trong hệ thống! Vui lòng nhập tên khác.", "Cảnh báo trùng tên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Giữ nguyên popup mở để nhập lại
                }

                prompt.DialogResult = System.Windows.Forms.DialogResult.OK;
                prompt.Close();
            };

            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK ? textBox.Text.Trim() : "";
        }

      
    }
}
