namespace Office_Manager
{
    partial class QR
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
            this.pictureBox17 = new System.Windows.Forms.PictureBox();
            this.signedInvoiceTb = new System.Windows.Forms.TextBox();
            this.updateBtn = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox17)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox17
            // 
            this.pictureBox17.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.pictureBox17.Location = new System.Drawing.Point(320, 18);
            this.pictureBox17.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox17.Name = "pictureBox17";
            this.pictureBox17.Size = new System.Drawing.Size(381, 344);
            this.pictureBox17.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox17.TabIndex = 60;
            this.pictureBox17.TabStop = false;
            // 
            // signedInvoiceTb
            // 
            this.signedInvoiceTb.Location = new System.Drawing.Point(13, 377);
            this.signedInvoiceTb.Margin = new System.Windows.Forms.Padding(4);
            this.signedInvoiceTb.Multiline = true;
            this.signedInvoiceTb.Name = "signedInvoiceTb";
            this.signedInvoiceTb.Size = new System.Drawing.Size(1046, 159);
            this.signedInvoiceTb.TabIndex = 61;
            this.signedInvoiceTb.TextChanged += new System.EventHandler(this.signedInvoiceTb_TextChanged);
            // 
            // updateBtn
            // 
            this.updateBtn.Location = new System.Drawing.Point(463, 556);
            this.updateBtn.Margin = new System.Windows.Forms.Padding(4);
            this.updateBtn.Name = "updateBtn";
            this.updateBtn.Size = new System.Drawing.Size(100, 28);
            this.updateBtn.TabIndex = 62;
            this.updateBtn.Text = "Update";
            this.updateBtn.UseVisualStyleBackColor = true;
            this.updateBtn.Click += new System.EventHandler(this.updateBtn_Click);
            // 
            // QR
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1072, 611);
            this.Controls.Add(this.updateBtn);
            this.Controls.Add(this.signedInvoiceTb);
            this.Controls.Add(this.pictureBox17);
            this.Name = "QR";
            this.Text = "QR";
            this.Load += new System.EventHandler(this.QR_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox17)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox17;
        private System.Windows.Forms.TextBox signedInvoiceTb;
        private System.Windows.Forms.Button updateBtn;
    }
}