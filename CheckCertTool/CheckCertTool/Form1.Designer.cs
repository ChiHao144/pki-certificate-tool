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
            this.label2 = new System.Windows.Forms.Label();
            this.txtCaFile = new System.Windows.Forms.TextBox();
            this.btnBrowseCa = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lblOcspStatus = new System.Windows.Forms.Label();
            this.lblCrlStatus = new System.Windows.Forms.Label();
            this.lblValidTo = new System.Windows.Forms.Label();
            this.lblValidFrom = new System.Windows.Forms.Label();
            this.lblSerialNumber = new System.Windows.Forms.Label();
            this.lblCaProvider = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCheck
            // 
            this.btnCheck.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCheck.Location = new System.Drawing.Point(449, 147);
            this.btnCheck.Name = "btnCheck";
            this.btnCheck.Size = new System.Drawing.Size(217, 56);
            this.btnCheck.TabIndex = 2;
            this.btnCheck.Text = "Kiểm tra";
            this.btnCheck.UseVisualStyleBackColor = true;
            this.btnCheck.Click += new System.EventHandler(this.btnCheck_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(116, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(177, 20);
            this.label1.TabIndex = 1;
            this.label1.Text = "1. User Certificate File";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // txtUserFile
            // 
            this.txtUserFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtUserFile.Location = new System.Drawing.Point(360, 32);
            this.txtUserFile.Multiline = true;
            this.txtUserFile.Name = "txtUserFile";
            this.txtUserFile.Size = new System.Drawing.Size(384, 39);
            this.txtUserFile.TabIndex = 3;
            this.txtUserFile.TextChanged += new System.EventHandler(this.txtUserFile_TextChanged);
            // 
            // btnBrowseUser
            // 
            this.btnBrowseUser.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseUser.Location = new System.Drawing.Point(794, 32);
            this.btnBrowseUser.Name = "btnBrowseUser";
            this.btnBrowseUser.Size = new System.Drawing.Size(160, 41);
            this.btnBrowseUser.TabIndex = 0;
            this.btnBrowseUser.Text = "Chọn File...";
            this.btnBrowseUser.UseVisualStyleBackColor = true;
            this.btnBrowseUser.Click += new System.EventHandler(this.btnBrowseUser_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(116, 102);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(164, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "2. CA Certificate File";
            // 
            // txtCaFile
            // 
            this.txtCaFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCaFile.Location = new System.Drawing.Point(360, 92);
            this.txtCaFile.Multiline = true;
            this.txtCaFile.Name = "txtCaFile";
            this.txtCaFile.Size = new System.Drawing.Size(384, 39);
            this.txtCaFile.TabIndex = 4;
            this.txtCaFile.TextChanged += new System.EventHandler(this.txtCaFile_TextChanged);
            // 
            // btnBrowseCa
            // 
            this.btnBrowseCa.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowseCa.Location = new System.Drawing.Point(794, 92);
            this.btnBrowseCa.Name = "btnBrowseCa";
            this.btnBrowseCa.Size = new System.Drawing.Size(160, 41);
            this.btnBrowseCa.TabIndex = 1;
            this.btnBrowseCa.Text = "Chọn File...";
            this.btnBrowseCa.UseVisualStyleBackColor = true;
            this.btnBrowseCa.Click += new System.EventHandler(this.btnBrowseCa_Click);
            // 
            // txtResult
            // 
            this.txtResult.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResult.Location = new System.Drawing.Point(136, 220);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtResult.Size = new System.Drawing.Size(833, 163);
            this.txtResult.TabIndex = 7;
            this.txtResult.TextChanged += new System.EventHandler(this.txtResult_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lblOcspStatus);
            this.groupBox1.Controls.Add(this.lblCrlStatus);
            this.groupBox1.Controls.Add(this.lblValidTo);
            this.groupBox1.Controls.Add(this.lblValidFrom);
            this.groupBox1.Controls.Add(this.lblSerialNumber);
            this.groupBox1.Controls.Add(this.lblCaProvider);
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(136, 415);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(833, 294);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Kết quả phân tích từ hệ thống";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
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
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1098, 741);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.btnBrowseCa);
            this.Controls.Add(this.txtCaFile);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnBrowseUser);
            this.Controls.Add(this.txtUserFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCheck);
            this.Name = "Form1";
            this.Text = "PKI Certificate Tool";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCheck;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtUserFile;
        private System.Windows.Forms.Button btnBrowseUser;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtCaFile;
        private System.Windows.Forms.Button btnBrowseCa;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblOcspStatus;
        private System.Windows.Forms.Label lblCrlStatus;
        private System.Windows.Forms.Label lblValidTo;
        private System.Windows.Forms.Label lblValidFrom;
        private System.Windows.Forms.Label lblSerialNumber;
        private System.Windows.Forms.Label lblCaProvider;
    }
}

