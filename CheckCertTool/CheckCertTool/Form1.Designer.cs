namespace CheckCertTool
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCheck = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.txtUserFile = new System.Windows.Forms.TextBox();
            this.btnBrowseUser = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.gbResult = new System.Windows.Forms.GroupBox();
            this.lblOcspStatus = new System.Windows.Forms.Label();
            this.lblCrlStatus = new System.Windows.Forms.Label();
            this.lblValidTo = new System.Windows.Forms.Label();
            this.lblValidFrom = new System.Windows.Forms.Label();
            this.lblSerialNumber = new System.Windows.Forms.Label();
            this.lblCaProvider = new System.Windows.Forms.Label();
            this.btnAddNewCa = new System.Windows.Forms.Button();
            this.gbResult.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCheck
            // 
            this.btnCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCheck.Location = new System.Drawing.Point(282, 79);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(217, 56);
            this.btnCheck.TabIndex = 1;
            this.btnCheck.Text = "Kiểm tra";
            this.btnCheck.UseVisualStyleBackColor = true;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(113, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(177, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "1. User Certificate File";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtUserFile
            // 
            this.txtUserFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUserFile.Location = new System.Drawing.Point(357, 25);
            this.txtUserFile.Multiline = true;
            this.txtUserFile.Name = "txtUserFile";
            this.txtUserFile.Size = new System.Drawing.Size(384, 39);
            this.txtUserFile.TabIndex = 3;
            this.txtUserFile.TextChanged += new System.EventHandler(this.txtUserFile_TextChanged);
            // 
            // btnBrowseUser
            // 
            this.btnBrowseUser.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseUser.Location = new System.Drawing.Point(791, 25);
            this.btnBrowseUser.Name = "btnBrowseUser";
            this.btnBrowseUser.Size = new System.Drawing.Size(160, 41);
            this.btnBrowseUser.TabIndex = 0;
            this.btnBrowseUser.Text = "Chọn File...";
            this.btnBrowseUser.UseVisualStyleBackColor = true;
            this.btnBrowseUser.Click += new System.EventHandler(this.btnBrowseUser_Click);
            // 
            // txtResult
            // 
            this.txtResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResult.Location = new System.Drawing.Point(136, 152);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Size = new System.Drawing.Size(833, 163);
            this.txtResult.TabIndex = 7;
            this.txtResult.TextChanged += new System.EventHandler(this.txtResult_TextChanged);
            // 
            // gbResult
            // 
            this.gbResult.Controls.Add(this.lblOcspStatus);
            this.gbResult.Controls.Add(this.lblCrlStatus);
            this.gbResult.Controls.Add(this.lblValidTo);
            this.gbResult.Controls.Add(this.lblValidFrom);
            this.gbResult.Controls.Add(this.lblSerialNumber);
            this.gbResult.Controls.Add(this.lblCaProvider);
            this.gbResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbResult.Location = new System.Drawing.Point(136, 340);
            this.gbResult.Name = "gbResult";
            this.gbResult.Size = new System.Drawing.Size(833, 294);
            this.gbResult.TabIndex = 8;
            this.gbResult.TabStop = false;
            this.gbResult.Text = "Kết quả phân tích từ hệ thống";
            this.gbResult.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // lblOcspStatus
            // 
            this.lblOcspStatus.AutoSize = true;
            this.lblOcspStatus.Location = new System.Drawing.Point(57, 253);
            this.lblOcspStatus.Name = "lblOcspStatus";
            this.lblOcspStatus.Size = new System.Drawing.Size(53, 20);
            this.lblOcspStatus.TabIndex = 5;
            this.lblOcspStatus.Text = "label8";
            // 
            // lblCrlStatus
            // 
            this.lblCrlStatus.AutoSize = true;
            this.lblCrlStatus.Location = new System.Drawing.Point(57, 213);
            this.lblCrlStatus.Name = "lblCrlStatus";
            this.lblCrlStatus.Size = new System.Drawing.Size(53, 20);
            this.lblCrlStatus.TabIndex = 4;
            this.lblCrlStatus.Text = "label7";
            // 
            // lblValidTo
            // 
            this.lblValidTo.AutoSize = true;
            this.lblValidTo.Location = new System.Drawing.Point(57, 169);
            this.lblValidTo.Name = "lblValidTo";
            this.lblValidTo.Size = new System.Drawing.Size(53, 20);
            this.lblValidTo.TabIndex = 3;
            this.lblValidTo.Text = "label6";
            // 
            // lblValidFrom
            // 
            this.lblValidFrom.AutoSize = true;
            this.lblValidFrom.Location = new System.Drawing.Point(57, 129);
            this.lblValidFrom.Name = "lblValidFrom";
            this.lblValidFrom.Size = new System.Drawing.Size(53, 20);
            this.lblValidFrom.TabIndex = 2;
            this.lblValidFrom.Text = "label5";
            // 
            // lblSerialNumber
            // 
            this.lblSerialNumber.AutoSize = true;
            this.lblSerialNumber.Location = new System.Drawing.Point(57, 86);
            this.lblSerialNumber.Name = "lblSerialNumber";
            this.lblSerialNumber.Size = new System.Drawing.Size(53, 20);
            this.lblSerialNumber.TabIndex = 1;
            this.lblSerialNumber.Text = "label4";
            // 
            // lblCaProvider
            // 
            this.lblCaProvider.AutoSize = true;
            this.lblCaProvider.Location = new System.Drawing.Point(57, 42);
            this.lblCaProvider.Name = "lblCaProvider";
            this.lblCaProvider.Size = new System.Drawing.Size(53, 20);
            this.lblCaProvider.TabIndex = 0;
            this.lblCaProvider.Text = "label3";
            // 
            // btnAddNewCa
            // 
            this.btnAddNewCa.BackColor = System.Drawing.Color.LightSkyBlue;
            this.btnAddNewCa.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAddNewCa.Location = new System.Drawing.Point(616, 79);
            this.btnAddNewCa.Name = "btnAddNewCa";
            this.btnAddNewCa.Size = new System.Drawing.Size(196, 56);
            this.btnAddNewCa.TabIndex = 2;
            this.btnAddNewCa.Text = "Thêm CA mới";
            this.btnAddNewCa.UseVisualStyleBackColor = false;
            this.btnAddNewCa.Click += new System.EventHandler(this.btnAddNewCa_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1098, 656);
            this.Controls.Add(this.btnAddNewCa);
            this.Controls.Add(this.gbResult);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.btnBrowseUser);
            this.Controls.Add(this.txtUserFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCheck);
            this.Name = "Form1";
            this.Text = "PKI Certificate Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.gbResult.ResumeLayout(false);
            this.gbResult.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserFile;
        private System.Windows.Forms.Button btnBrowseUser;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.GroupBox gbResult;
        private System.Windows.Forms.Label lblOcspStatus;
        private System.Windows.Forms.Label lblCrlStatus;
        private System.Windows.Forms.Label lblValidTo;
        private System.Windows.Forms.Label lblValidFrom;
        private System.Windows.Forms.Label lblSerialNumber;
        private System.Windows.Forms.Label lblCaProvider;
        private System.Windows.Forms.Button btnAddNewCa;
    }
}

