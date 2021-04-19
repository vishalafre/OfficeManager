namespace Office_Manager
{
    partial class PeriodManagement
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
            this.comboBox2 = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.fromDt0 = new System.Windows.Forms.DateTimePicker();
            this.toDt0 = new System.Windows.Forms.DateTimePicker();
            this.toLbl0 = new System.Windows.Forms.Label();
            this.del0 = new System.Windows.Forms.PictureBox();
            this.add0 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label10 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.del0)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.add0)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBox2
            // 
            this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox2.FormattingEnabled = true;
            this.comboBox2.Location = new System.Drawing.Point(452, 55);
            this.comboBox2.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.comboBox2.Name = "comboBox2";
            this.comboBox2.Size = new System.Drawing.Size(207, 24);
            this.comboBox2.TabIndex = 11;
            this.comboBox2.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.Teal;
            this.label3.Location = new System.Drawing.Point(345, 58);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 24);
            this.label3.TabIndex = 10;
            this.label3.Text = "Godown";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(116, 106);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(82, 24);
            this.label1.TabIndex = 12;
            this.label1.Text = "Periods";
            // 
            // fromDt0
            // 
            this.fromDt0.Location = new System.Drawing.Point(223, 106);
            this.fromDt0.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.fromDt0.Name = "fromDt0";
            this.fromDt0.Size = new System.Drawing.Size(265, 22);
            this.fromDt0.TabIndex = 21;
            // 
            // toDt0
            // 
            this.toDt0.Location = new System.Drawing.Point(560, 106);
            this.toDt0.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.toDt0.Name = "toDt0";
            this.toDt0.Size = new System.Drawing.Size(265, 22);
            this.toDt0.TabIndex = 22;
            // 
            // toLbl0
            // 
            this.toLbl0.AutoSize = true;
            this.toLbl0.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toLbl0.Location = new System.Drawing.Point(504, 106);
            this.toLbl0.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.toLbl0.Name = "toLbl0";
            this.toLbl0.Size = new System.Drawing.Size(34, 24);
            this.toLbl0.TabIndex = 57;
            this.toLbl0.Text = "To";
            // 
            // del0
            // 
            this.del0.Image = global::Office_Manager.Properties.Resources.remove;
            this.del0.Location = new System.Drawing.Point(880, 106);
            this.del0.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.del0.Name = "del0";
            this.del0.Size = new System.Drawing.Size(32, 26);
            this.del0.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.del0.TabIndex = 59;
            this.del0.TabStop = false;
            this.del0.Visible = false;
            // 
            // add0
            // 
            this.add0.Image = global::Office_Manager.Properties.Resources.add;
            this.add0.Location = new System.Drawing.Point(840, 106);
            this.add0.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.add0.Name = "add0";
            this.add0.Size = new System.Drawing.Size(32, 26);
            this.add0.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.add0.TabIndex = 58;
            this.add0.TabStop = false;
            this.add0.Click += new System.EventHandler(this.add0_Click);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(920, 5);
            this.button1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(100, 28);
            this.button1.TabIndex = 60;
            this.button1.Text = "SAVE";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label10.ForeColor = System.Drawing.Color.Blue;
            this.label10.Location = new System.Drawing.Point(480, 9);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(0, 19);
            this.label10.TabIndex = 76;
            // 
            // PeriodManagement
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1025, 402);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.del0);
            this.Controls.Add(this.add0);
            this.Controls.Add(this.toLbl0);
            this.Controls.Add(this.toDt0);
            this.Controls.Add(this.fromDt0);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBox2);
            this.Controls.Add(this.label3);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "PeriodManagement";
            this.Text = "PeriodManagement";
            this.Load += new System.EventHandler(this.PeriodManagement_Load);
            ((System.ComponentModel.ISupportInitialize)(this.del0)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.add0)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBox2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker fromDt0;
        private System.Windows.Forms.DateTimePicker toDt0;
        private System.Windows.Forms.Label toLbl0;
        private System.Windows.Forms.PictureBox del0;
        private System.Windows.Forms.PictureBox add0;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label10;
    }
}