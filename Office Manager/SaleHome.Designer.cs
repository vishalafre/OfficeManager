namespace Office_Manager
{
    partial class SaleHome
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
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.addCustomer = new System.Windows.Forms.Panel();
            this.agent = new System.Windows.Forms.Panel();
            this.agentHeader = new System.Windows.Forms.Label();
            this.transporter = new System.Windows.Forms.Panel();
            this.tranHeader = new System.Windows.Forms.Label();
            this.customer = new System.Windows.Forms.Panel();
            this.custHeader = new System.Windows.Forms.Label();
            this.button5 = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.button2 = new System.Windows.Forms.Button();
            this.button9 = new System.Windows.Forms.Button();
            this.addCustomer.SuspendLayout();
            this.agent.SuspendLayout();
            this.transporter.SuspendLayout();
            this.customer.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Olive;
            this.label1.Location = new System.Drawing.Point(835, 30);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(268, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "AFRE ENTERPRISES";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(16, 118);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(172, 54);
            this.button1.TabIndex = 1;
            this.button1.Text = "Create Customer";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(17, 222);
            this.button3.Margin = new System.Windows.Forms.Padding(4);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(172, 54);
            this.button3.TabIndex = 3;
            this.button3.Text = "Create Transporter";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(16, 431);
            this.button4.Margin = new System.Windows.Forms.Padding(4);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(172, 54);
            this.button4.TabIndex = 4;
            this.button4.Text = "Create Invoice";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // addCustomer
            // 
            this.addCustomer.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.addCustomer.Controls.Add(this.agent);
            this.addCustomer.Controls.Add(this.transporter);
            this.addCustomer.Controls.Add(this.customer);
            this.addCustomer.Location = new System.Drawing.Point(241, 94);
            this.addCustomer.Margin = new System.Windows.Forms.Padding(4);
            this.addCustomer.Name = "addCustomer";
            this.addCustomer.Size = new System.Drawing.Size(1485, 693);
            this.addCustomer.TabIndex = 5;
            // 
            // agent
            // 
            this.agent.AutoScroll = true;
            this.agent.BackColor = System.Drawing.Color.Cornsilk;
            this.agent.Controls.Add(this.agentHeader);
            this.agent.Location = new System.Drawing.Point(1019, 24);
            this.agent.Margin = new System.Windows.Forms.Padding(4);
            this.agent.Name = "agent";
            this.agent.Size = new System.Drawing.Size(335, 630);
            this.agent.TabIndex = 52;
            // 
            // agentHeader
            // 
            this.agentHeader.AutoSize = true;
            this.agentHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.agentHeader.Location = new System.Drawing.Point(25, 17);
            this.agentHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.agentHeader.Name = "agentHeader";
            this.agentHeader.Size = new System.Drawing.Size(67, 20);
            this.agentHeader.TabIndex = 3;
            this.agentHeader.Text = "Agents";
            // 
            // transporter
            // 
            this.transporter.AutoScroll = true;
            this.transporter.BackColor = System.Drawing.Color.Cornsilk;
            this.transporter.Controls.Add(this.tranHeader);
            this.transporter.Location = new System.Drawing.Point(577, 24);
            this.transporter.Margin = new System.Windows.Forms.Padding(4);
            this.transporter.Name = "transporter";
            this.transporter.Size = new System.Drawing.Size(303, 630);
            this.transporter.TabIndex = 50;
            // 
            // tranHeader
            // 
            this.tranHeader.AutoSize = true;
            this.tranHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tranHeader.Location = new System.Drawing.Point(21, 17);
            this.tranHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.tranHeader.Name = "tranHeader";
            this.tranHeader.Size = new System.Drawing.Size(117, 20);
            this.tranHeader.TabIndex = 2;
            this.tranHeader.Text = "Transporters";
            // 
            // customer
            // 
            this.customer.AutoScroll = true;
            this.customer.BackColor = System.Drawing.Color.Cornsilk;
            this.customer.Controls.Add(this.custHeader);
            this.customer.Location = new System.Drawing.Point(112, 24);
            this.customer.Margin = new System.Windows.Forms.Padding(4);
            this.customer.Name = "customer";
            this.customer.Size = new System.Drawing.Size(347, 630);
            this.customer.TabIndex = 49;
            // 
            // custHeader
            // 
            this.custHeader.AutoSize = true;
            this.custHeader.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.custHeader.Location = new System.Drawing.Point(21, 17);
            this.custHeader.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.custHeader.Name = "custHeader";
            this.custHeader.Size = new System.Drawing.Size(100, 20);
            this.custHeader.TabIndex = 1;
            this.custHeader.Text = "Customers";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(16, 533);
            this.button5.Margin = new System.Windows.Forms.Padding(4);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(172, 54);
            this.button5.TabIndex = 6;
            this.button5.Text = "Invoice History";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button7
            // 
            this.button7.Location = new System.Drawing.Point(1572, 48);
            this.button7.Margin = new System.Windows.Forms.Padding(4);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(155, 38);
            this.button7.TabIndex = 7;
            this.button7.Text = "Delete Firm";
            this.button7.UseVisualStyleBackColor = true;
            this.button7.Click += new System.EventHandler(this.button7_Click);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(17, 15);
            this.button6.Margin = new System.Windows.Forms.Padding(4);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(100, 28);
            this.button6.TabIndex = 30;
            this.button6.Text = "BACK";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // button8
            // 
            this.button8.Location = new System.Drawing.Point(17, 328);
            this.button8.Margin = new System.Windows.Forms.Padding(4);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(172, 54);
            this.button8.TabIndex = 31;
            this.button8.Text = "Create Agent";
            this.button8.UseVisualStyleBackColor = true;
            this.button8.Click += new System.EventHandler(this.button8_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.Teal;
            this.button2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.Color.White;
            this.button2.Location = new System.Drawing.Point(844, 793);
            this.button2.Margin = new System.Windows.Forms.Padding(4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(251, 54);
            this.button2.TabIndex = 32;
            this.button2.Text = "Tally Configuration";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click_1);
            // 
            // button9
            // 
            this.button9.BackColor = System.Drawing.Color.IndianRed;
            this.button9.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button9.ForeColor = System.Drawing.Color.White;
            this.button9.Location = new System.Drawing.Point(16, 627);
            this.button9.Margin = new System.Windows.Forms.Padding(4);
            this.button9.Name = "button9";
            this.button9.Size = new System.Drawing.Size(176, 72);
            this.button9.TabIndex = 33;
            this.button9.Text = "Order Management";
            this.button9.UseVisualStyleBackColor = false;
            this.button9.Click += new System.EventHandler(this.button9_Click);
            // 
            // SaleHome
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1767, 912);
            this.Controls.Add(this.button9);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button8);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button7);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.addCustomer);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "SaleHome";
            this.Text = "SaleHome";
            this.Load += new System.EventHandler(this.SaleHome_Load);
            this.addCustomer.ResumeLayout(false);
            this.agent.ResumeLayout(false);
            this.agent.PerformLayout();
            this.transporter.ResumeLayout(false);
            this.transporter.PerformLayout();
            this.customer.ResumeLayout(false);
            this.customer.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Panel addCustomer;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Panel agent;
        private System.Windows.Forms.Panel transporter;
        private System.Windows.Forms.Panel customer;
        private System.Windows.Forms.Label agentHeader;
        private System.Windows.Forms.Label tranHeader;
        private System.Windows.Forms.Label custHeader;
        private System.Windows.Forms.Button button8;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button9;
    }
}