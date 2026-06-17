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

namespace CheckCATool
{
    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
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
                // Khởi động Server Spring Boot ngầm chạy song song
                //System.Diagnostics.Process startJava = new System.Diagnostics.Process();
                //startJava.StartInfo.FileName = "JavaBackend.exe";
                //startJava.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                //startJava.StartInfo.CreateNoWindow = true;
                //startJava.Start();

                // Chờ 2 giây để đảm bảo Spring Boot khởi động xong cổng 8080 rồi mới nạp ComboBox
                await Task.Delay(2000);
                await LoadCaToComboBox();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tìm thấy tệp core xử lý nền (JavaBackend.exe)! " + ex.Message,
                                "Lỗi cấu trúc Tool", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

                    cbCaTrustStore.Items.Clear(); 
                    foreach (var caName in caList)
                    {
                        cbCaTrustStore.Items.Add(caName);
                    }

                    if (cbCaTrustStore.Items.Count > 0)
                    {
                        cbCaTrustStore.Items.Insert(0, "--Chọn CA--");
                        cbCaTrustStore.SelectedIndex = 0;
                    }
                    else
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

            string selectedFolderName = cbCaTrustStore.SelectedItem.ToString(); // Rút ra chữ "VNPT" chẳng hạn
            ClearLabels(); // Xóa sạch kết quả cũ của lần test trước đó
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
                    label4.Text = $"3. Serial Number: {result.SerialNumber.ToUpper()}";
                    label4.ForeColor = System.Drawing.Color.DimGray;

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
                    label9.Text = $"Tên khách hàng (Chủ thể): {displayName}";
                    label9.ForeColor = Color.DarkBlue;

                    // Trường 2: Hiển thị mã định danh Serial của User Cert (Viết hoa cho chuẩn IT)
                    label10.Text = $"Mã Serial định danh CTS: {result.UserSerialNumber.ToUpper()}";
                    label10.ForeColor = Color.Black;

                    // Trường 3: Hiển thị ngày hết hạn của User Cert kèm logic đổi màu đỏ nếu đã hết hạn
                    if (result.CertValidityStatus == "EXPIRED")
                    {
                        label11.Text = $"Hạn dùng CTS khách hàng: {result.UserValidTo} (* ĐÃ HẾT HẠN SỬ DỤNG!)";
                        label11.ForeColor = Color.Red;
                    }
                    else
                    {
                        label11.Text = $"Hạn dùng CTS khách hàng: {result.UserValidTo} (Đang hoạt động)";
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

    }
}
