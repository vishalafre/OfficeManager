using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class PrintTDSReport : Form
    {
        DataGridView dgv;
        string filter;
        string firm;

        public PrintTDSReport(DataGridView dgv, string filter, string firm)
        {
            InitializeComponent();
            this.dgv = dgv;
            this.filter = filter;
            this.firm = firm;
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void PrintTDSReport_Load(object sender, EventArgs e)
        {
            dgv.Location = new Point(129, 120);
            //dgv.BorderStyle = BorderStyle.Fixed3D;
            foreach (DataGridViewColumn c in dgv.Columns)
            {
                c.DefaultCellStyle.Font = new Font(dgv.DefaultCellStyle.Font.FontFamily, 8);
            }
            int dgvWidth = dgv.Columns[0].Width + dgv.Columns[1].Width + dgv.Columns[2].Width;

            panel1.Controls.Add(dgv);

            string[] parts = filter.Split(new string[] { "SS.TO_DATE" }, StringSplitOptions.None);
            int i = 0;

            string startDt = "01-Oct-2018";
            string endDt = DateTime.Now.ToString("dd-MMM-yyyy");

            foreach(string p in parts)
            {
                if(i > 0)
                {
                    if(p.Contains(">="))
                    {
                        startDt = p.Split(new string[] { ">= '" }, StringSplitOptions.None)[1].Split('\'')[0];
                    }
                    else if(p.Contains("<="))
                    {
                        endDt = p.Split(new string[] { "<= '" }, StringSplitOptions.None)[1].Split('\'')[0];
                    }
                }
                i++;
            }

            label3.Text = startDt + " to " + endDt;
            label1.Text = firm;

            label1.Location = new Point((panel1.Width - label1.Width) / 2, label1.Location.Y);
            label2.Location = new Point((panel1.Width - label2.Width) / 2, label2.Location.Y);
            label3.Location = new Point((panel1.Width - label3.Width) / 2, label3.Location.Y);
            //dgv.Location = new Point((panel1.Width - dgvWidth) / 2, dgv.Location.Y);
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Bitmap bmp = new Bitmap(panel1.Width, panel1.Height);
            panel1.DrawToBitmap(bmp, new Rectangle(0, 0, 2480, 3508));
            e.Graphics.ScaleTransform(1.25f, 1.25f);
            e.Graphics.DrawImage(bmp, 0, 0);
            //e.Graphics.ScaleTransform(2480 / panel1.Width, 3508 / panel1.Height);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            printDocument1.Print();
        }
    }
}
