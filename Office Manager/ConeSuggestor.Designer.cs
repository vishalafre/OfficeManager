namespace Office_Manager
{
    partial class ConeSuggestor
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
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.firm0 = new System.Windows.Forms.Label();
            this.dataGridView0 = new System.Windows.Forms.DataGridView();
            this.button1 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.pictureBox18 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView0)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox18)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBox1
            // 
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(787, 167);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(4);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(329, 24);
            this.comboBox1.TabIndex = 276;
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(665, 167);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 24);
            this.label2.TabIndex = 275;
            this.label2.Text = "Weaver";
            // 
            // firm0
            // 
            this.firm0.AutoSize = true;
            this.firm0.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.firm0.ForeColor = System.Drawing.Color.Red;
            this.firm0.Location = new System.Drawing.Point(369, 250);
            this.firm0.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.firm0.Name = "firm0";
            this.firm0.Size = new System.Drawing.Size(266, 24);
            this.firm0.TabIndex = 278;
            this.firm0.Text = "All Firms                             ";
            // 
            // dataGridView0
            // 
            this.dataGridView0.AllowUserToAddRows = false;
            this.dataGridView0.AllowUserToDeleteRows = false;
            this.dataGridView0.AllowUserToOrderColumns = true;
            this.dataGridView0.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridView0.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dataGridView0.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView0.Location = new System.Drawing.Point(373, 278);
            this.dataGridView0.Margin = new System.Windows.Forms.Padding(4);
            this.dataGridView0.Name = "dataGridView0";
            this.dataGridView0.ReadOnly = true;
            this.dataGridView0.RowHeadersVisible = false;
            this.dataGridView0.RowTemplate.Height = 30;
            this.dataGridView0.Size = new System.Drawing.Size(1245, 366);
            this.dataGridView0.TabIndex = 277;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.MediumSeaGreen;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(1575, 12);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(229, 49);
            this.button1.TabIndex = 279;
            this.button1.Text = "Aggregate Carton Stock";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Office_Manager.Properties.Resources.suggestor_text;
            this.pictureBox1.Location = new System.Drawing.Point(473, 13);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(959, 124);
            this.pictureBox1.TabIndex = 274;
            this.pictureBox1.TabStop = false;
            // 
            // pictureBox18
            // 
            this.pictureBox18.Image = global::Office_Manager.Properties.Resources.back;
            this.pictureBox18.Location = new System.Drawing.Point(13, 13);
            this.pictureBox18.Margin = new System.Windows.Forms.Padding(4);
            this.pictureBox18.Name = "pictureBox18";
            this.pictureBox18.Size = new System.Drawing.Size(147, 78);
            this.pictureBox18.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox18.TabIndex = 273;
            this.pictureBox18.TabStop = false;
            this.pictureBox18.Click += new System.EventHandler(this.pictureBox18_Click);
            // 
            // ConeSuggestor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(1816, 912);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.firm0);
            this.Controls.Add(this.dataGridView0);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.pictureBox18);
            this.Name = "ConeSuggestor";
            this.Text = "ConeSuggestor";
            this.Load += new System.EventHandler(this.ConeSuggestor_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView0)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox18)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox18;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label firm0;
        private System.Windows.Forms.DataGridView dataGridView0;
        private System.Windows.Forms.Button button1;
    }
}