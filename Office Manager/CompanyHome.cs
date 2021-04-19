
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class CompanyHome : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;

        public CompanyHome()
        {
            InitializeComponent();
        }

        public CompanyHome(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void CompanyHome_Load(object sender, EventArgs e)
        {
            SalaryReport.d1W = dgv.Width;
            SalaryReport.d1H = dgv.Height;

            string query = "SELECT * from company where NAME = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    byte[] photo_aray = (byte[])oReader["LOGO_IMG"];

                    MemoryStream ms = new MemoryStream(photo_aray);
                    pictureBox17.Image = Image.FromStream(ms);
                }
            }
            
            performTask();
        }

        public void performTask()
        {
            String query = "SELECT BI.ROLL_NO, CONVERT(VARCHAR(12), BILL_DT, 107) BILL_DT, ITEM_NAME, MTR, CAST(case when item_name in ('Tamil Nadu Exp.', 'Andhra Exp.', 'Gitanjali exp.', 'Karnataka Exp.', 'G.T. Exp. 52 ( White Synthetic Cloth)') then round((mtr-5)/1.02,0) when item_name = 'Pavan Exp. old' then round((mtr+495)/1.01,0) else round((mtr-5)/1.01,0) end AS INTEGER) ROLL_MTR, (SELECT G_NAME FROM GODOWN WHERE GID = GODOWN) GODOWN FROM BILL_ITEM BI, BILL B, ITEM I WHERE B.BILL_ID = BI.BILL_ID AND BILL_DT > '30-SEP-18' AND QTY = 1 and bi.firm = '" + firm +"' AND ISNUMERIC(BI.ROLL_NO) = 1 AND BI.ROLL_NO NOT IN (SELECT ROLL_NO FROM ROLL rr where rr.fy = bi.fy) AND I.ITEM_ID = BI.ITEM order by item_name, godown";

            // populate table
            fetchData(dgv, query);
        }

        private void fetchData(DataGridView dataGridView, string sql)
        {
            int dHeight = dataGridView.Height;

            var grid = new DataGridView()
            {
                Name = "dataGridView0",
                Size = new Size(dgv.Width, addCustomer.Height - 223),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                Visible = true,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = dataGridView.Location
            };

            grid.RowTemplate.Height = 35;

            updateReport(sql, grid);
            SalaryReport.formatDataGridView(grid);

            addCustomer.Controls.Add(grid);
        }

        public void updateReport(String sql, DataGridView dataGridView1)
        {
            dataGridView1.ColumnCount = 6;
            dataGridView1.Columns[0].Name = "Roll No";
            dataGridView1.Columns[1].Name = "Bill Date";
            dataGridView1.Columns[2].Name = "Item Name";
            dataGridView1.Columns[3].Name = "Meter";
            dataGridView1.Columns[4].Name = "Expected Roll Meter";
            dataGridView1.Columns[5].Name = "Godown";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string[] row;
                    row = new string[] { oReader["ROLL_NO"].ToString(), oReader["BILL_DT"].ToString(), oReader["ITEM_NAME"].ToString(), oReader["MTR"].ToString(), oReader["ROLL_MTR"].ToString(), oReader["GODOWN"].ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox7_Click_1(object sender, EventArgs e)
        {
            var targetForm = new NewGodown(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox6_Click_1(object sender, EventArgs e)
        {
            var targetForm = new NewProduct(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox8_Click_1(object sender, EventArgs e)
        {
            var targetForm = new NewUnit(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox21_Click_1(object sender, EventArgs e)
        {
            var targetForm = new NewWeaver(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            var targetForm = new RollEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox2_Click_1(object sender, EventArgs e)
        {
            var targetForm = new SupplyBeam(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox3_Click_1(object sender, EventArgs e)
        {
            var targetForm = new SupplyCone(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox4_Click_1(object sender, EventArgs e)
        {
            var targetForm = new Purchase(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox22_Click_1(object sender, EventArgs e)
        {
            var targetForm = new TakaEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox23_Click_1(object sender, EventArgs e)
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

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            var targetForm = new WorkerStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            var targetForm = new GodownStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox24_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            var targetForm = new StockValue(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox5_Click_1(object sender, EventArgs e)
        {
            var targetForm = new SaleHome(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox20_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox19_Click(object sender, EventArgs e)
        {
            
        }
    }
}
