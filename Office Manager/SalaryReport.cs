using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class SalaryReport : Form
    {
        private string firm;
        private byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public static int d1W;
        public static int d2W;
        public static int d1H;
        public static int d2H;

        Bitmap bmp;
        string whereClause;

        public SalaryReport()
        {
            InitializeComponent();
        }

        public SalaryReport(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void SalaryReport_Load(object sender, EventArgs e)
        {
            d1W = dataGridView1.Width;
            d1H = dataGridView1.Height;

            d2W = dataGridView2.Width;
            d2H = dataGridView2.Height;

            string whereClause = "SS.FIRM = '" + firm + "' AND SS.TO_DATE = (SELECT MAX(SS1.TO_DATE) FROM SALARY_SUMMARY SS1 WHERE SS1.FIRM = SS.FIRM)";
            updateReport(whereClause);

            formatDataGridView(dataGridView1);
            formatSumGridView(dataGridView2);

            pictureBox10.Location = new Point(dataGridView2.Location.X + dataGridView2.Width - pictureBox10.Width, pictureBox10.Location.Y);
            pictureBox24.Location = new Point(pictureBox10.Location.X - 7 - pictureBox24.Width, pictureBox24.Location.Y);
        }

        public void updateReport(string whereClause)
        {
            this.whereClause = whereClause;

            string sql = "select W_NAME \"Weaver\", concat(CONVERT(VARCHAR(12), min(to_DATE), 107), ' - ', CONVERT(VARCHAR(12), max(TO_DATE), 107)) \"Period\", sum(TOTAL_VALUE) \"Net Salary\", sum(TP) TP, sum(NET_SALARY) \"Gross Salary\", sum(SS.TDS) TDS, sum(PAYABLE_SALARY) \"Payable Salary\", sum(CGST) CGST, sum(SGST) SGST from salary_summary SS, WEAVER W WHERE SS.WEAVER = W.WID AND " + whereClause + " group by w_name";
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();
            con.Open();

            //MessageBox.Show(whereClause);
            //MessageBox.Show(sql);

            dataadapter.Fill(ds, "SALARY_SUMMARY");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "SALARY_SUMMARY";

            sql = "select Entity, Sum from( select 'Net Salary' Entity, 0 ordinal, sum(total_value) Sum from salary_summary SS where " + whereClause + " union select 'TP', 1, sum(TP) from salary_summary SS where " + whereClause + " union select 'Gross Salary', 2, sum(net_salary) from salary_summary SS where " + whereClause + " union select 'TDS', 3, sum(tds) from salary_summary SS where " + whereClause + " union select 'Payable Salary', 4, sum(payable_salary) from salary_summary SS where " + whereClause + " union select 'CGST', 5, sum(CGST) from salary_summary SS where " + whereClause + " union select 'SGST', 6, sum(SGST) from salary_summary SS where " + whereClause + ") x order by ordinal";
            dataadapter = new SqlDataAdapter(sql, con);
            ds = new DataSet();
            dataadapter.Fill(ds, "SALARY_SUMMARY");
            con.Close();
            dataGridView2.DataSource = ds;
            dataGridView2.DataMember = "SALARY_SUMMARY";

            resizeGrid(dataGridView1);
            resizeSumGrid(dataGridView2);

            /*DataGridViewRow row = (DataGridViewRow)dataGridView1.Rows[0].Clone();
            row.Cells[0].Value = "XYZ";
            row.Cells[1].Value = "44";
            dataGridView1.Rows.Add(row);*/
        }

        public static void formatDataGridView(DataGridView dataGridView1)
        {
            dataGridView1.RowsDefaultCellStyle.BackColor = Color.Bisque;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;

            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.Bisque;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                c.DefaultCellStyle.Font = new Font("Arial", 12F, GraphicsUnit.Pixel);
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        public static void resizeGrid(DataGridView dataGridView1)
        {
            dataGridView1.RowTemplate.Height = 30;

            int totalWidth = 0;

            for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
            {
                int colw = dataGridView1.Columns[i].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
                totalWidth += colw;

                dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns[i].Width = colw;
            }

            int width = totalWidth + (5*dataGridView1.ColumnCount/2);
            int height = 40 + 30 * dataGridView1.RowCount;

            int x = (width > d1W) ? d1W : width;
            int y = (height > d1H) ? d1H : height;

            dataGridView1.Size = new Size(x, y);
        }

        private void formatSumGridView(DataGridView dataGridView1)
        {
            dataGridView1.RowsDefaultCellStyle.BackColor = Color.PaleTurquoise;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.WhiteSmoke;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;

            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.SteelBlue;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        }

        private void resizeSumGrid(DataGridView dataGridView1)
        {
            dataGridView1.RowTemplate.Height = 30;

            int totalWidth = 0;

            for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
            {
                int colw = dataGridView1.Columns[i].Width;
                totalWidth += colw;
            }

            int width = totalWidth + 2;
            int height = 23 + 30 * dataGridView1.RowCount;

            int x = (width > d2W) ? d1W : width;
            int y = (height > d2H) ? d2H : height;

            dataGridView1.Size = new Size(x, y);
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new SalaryFilter(firm, this).Show();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {
            var targetForm = new NewWeaver(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            var targetForm = new NewGodown(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            var targetForm = new NewProduct(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            var targetForm = new NewUnit(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox22_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var targetForm = new RollEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyBeam(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyCone(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            var targetForm = new Purchase(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            var targetForm = new SaleHome(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox23_Click(object sender, EventArgs e)
        {
            var targetForm = new CalculateSalary(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            var targetForm = new SalaryReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            var targetForm = new GodownStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            var targetForm = new StockValue(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            var targetForm = new WorkerStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox27_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            bmp = new Bitmap(this.dataGridView1.Width, this.dataGridView1.Height);
            dataGridView1.DrawToBitmap(bmp, new Rectangle(0, 0, this.dataGridView1.Width, this.dataGridView1.Height));
            e.Graphics.DrawImage(bmp, 200, 200);
        }

        private void pictureBox24_Click(object sender, EventArgs e)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(this.printDocument1_PrintPage_1);

            PrintDialog printdlg = new PrintDialog();
            PrintPreviewDialog printPrvDlg = new PrintPreviewDialog();

            // preview the assigned document or you can create a different previewButton for it
            printPrvDlg.Document = pd;
            printPrvDlg.ShowDialog(); // this shows the preview and then show the Printer Dlg below

            printdlg.Document = pd;

            if (printdlg.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }


            /*dataGridView1.Columns["Net Salary"].Visible = false;
            dataGridView1.Columns["TP"].Visible = false;
            dataGridView1.Columns["Period"].Visible = false;
            dataGridView1.Columns["Payable Salary"].Visible = false;
            dataGridView1.Columns["CGST"].Visible = false;
            dataGridView1.Columns["SGST"].Visible = false;

            var targetForm = new PrintTDSReport(dataGridView1, whereClause, firm);

            targetForm.MdiParent = ParentForm;
            targetForm.Show();

            dataGridView1.Columns["Net Salary"].Visible = true;
            dataGridView1.Columns["TP"].Visible = true;
            dataGridView1.Columns["Period"].Visible = true;
            dataGridView1.Columns["Payable Salary"].Visible = true;
            dataGridView1.Columns["CGST"].Visible = true;
            dataGridView1.Columns["SGST"].Visible = true;*/
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void printDocument1_PrintPage_1(object sender, PrintPageEventArgs e)
        {
            Graphics graphic = e.Graphics;
            SolidBrush brush = new SolidBrush(ColorTranslator.FromHtml("#655c62"));

            Font font = new Font("Arial", 16, FontStyle.Bold);

            e.PageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            float pageWidth = e.PageSettings.PrintableArea.Width;
            float pageHeight = e.PageSettings.PrintableArea.Height;

            float fontHeight = font.GetHeight();

            int startY = 100;
            int offsetY = 40;

            //firm
            SizeF stringSize = new SizeF();
            stringSize = e.Graphics.MeasureString(firm, font);
            int stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString(firm, font, brush, stringCenterX, 70);
            graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height));
            graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height));

            font = new Font("Arial", 14, FontStyle.Bold);
            brush = new SolidBrush(Color.Black);
            // stock report
            stringSize = e.Graphics.MeasureString("TDS Report", font);
            stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString("TDS Report", font, brush, stringCenterX, 110);

            // report period

            string[] parts = whereClause.Split(new string[] { "SS.TO_DATE" }, StringSplitOptions.None);
            int i = 0;

            string startDt = "01-Oct-2018";
            string endDt = DateTime.Now.ToString("dd-MMM-yyyy");

            foreach (string p in parts)
            {
                if (i > 0)
                {
                    if (p.Contains(">="))
                    {
                        startDt = p.Split(new string[] { ">= '" }, StringSplitOptions.None)[1].Split('\'')[0];
                    }
                    else if (p.Contains("<="))
                    {
                        endDt = p.Split(new string[] { "<= '" }, StringSplitOptions.None)[1].Split('\'')[0];
                    }
                }
                i++;
            }

            string asOnDate = startDt + " to " + endDt;

            font = new Font("Arial", 12);
            stringSize = e.Graphics.MeasureString("Period : " + asOnDate, font);
            stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString("Period : " + asOnDate, font, brush, stringCenterX, 140);

            offsetY += 50;
            int[] headerX = new int[dataGridView1.ColumnCount];

            int locX = 60;
            font = new Font("Arial", 16, FontStyle.Bold);
            brush = new SolidBrush(ColorTranslator.FromHtml("#007171"));
            for (int j = 0; j < dataGridView1.ColumnCount; j++)
            {
                if((j>=1 && j<=3) || j>=6)
                {
                    continue;
                }

                stringSize = e.Graphics.MeasureString(dataGridView1.Columns[j].HeaderText, font);
                graphic.DrawString(dataGridView1.Columns[j].HeaderText, font, brush, locX, startY + offsetY);
                headerX[j] = locX;

                if (j <= 3)
                {
                    locX += ((int)stringSize.Width + 200);
                }
                else
                {
                    locX += ((int)stringSize.Width + 110);
                }
            }
            offsetY += ((int)font.GetHeight() + 10);

            for (int j = 0; j < dataGridView1.RowCount; j++)
            {
                for (int k = 0; k < dataGridView1.ColumnCount; k++)
                {
                    if ((k >= 1 && k <= 3) || k >= 6)
                    {
                        continue;
                    }

                    if (k == 0)
                    {
                        font = new Font("Arial", 14, FontStyle.Bold);
                        brush = new SolidBrush(Color.Olive);
                        graphic.DrawString(dataGridView1[k, j].Value.ToString(), font, brush, 60, startY + offsetY);
                    }
                    else
                    {
                        font = new Font("Arial", 14, FontStyle.Bold);
                        brush = new SolidBrush(Color.Black);
                        graphic.DrawString(dataGridView1[k, j].Value.ToString(), font, brush, headerX[k], startY + offsetY);
                    }

                }
                offsetY += ((int)font.GetHeight() + 10);
            }
        }
    }
}
