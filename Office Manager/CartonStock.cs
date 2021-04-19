using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class CartonStock : Form
    {
        int gridCount;
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        Dictionary<string, string> godowns = new Dictionary<string, string>();
        Dictionary<string, string> yarns = new Dictionary<string, string>();

        Boolean loading = true;
        Boolean collapsed = false;
        string whereClause;
        string dateFilter = "";

        public CartonStock(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void CartonStock_Load(object sender, EventArgs e)
        {
            SalaryReport.d1W = dgv.Width;
            SalaryReport.d1H = dgv.Height;

            whereClause = "WHERE FIRM = '" + firm + "'";
            dateFilter = "AND TXN_DATE >= DATEADD(DAY, -30, GETDATE())";
            con.Open();

            // godown drop down

            string query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            godowns.Add("0", "All");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godowns.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(godowns, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            // yarn drop down

            query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Yarn' order by TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            yarns.Add("0", "All");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    yarns.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            con.Close();

            if (yarns.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(yarns, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            loading = false;
            performTask(whereClause, dateFilter);
        }

        public void performTask(string firmFilter, string dateFilter)
        {
            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string yarn = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key;

            string godownFilter = "";
            string supplyToFilter = "";
            string supplyFromFilter = "";
            string supplyFilter = "";
            string yarnFilter = "";
            string productFilter = "";

            if (!godown.Equals("0"))
            {
                godownFilter = "and godown = " + godown;
                supplyFromFilter = "and SUPPLY_FROM = " + godown;
                supplyToFilter = "and SUPPLY_TO = " + godown;
                supplyFilter = "AND ((SUPPLY_FROM_TYPE ='G' AND SUPPLY_FROM = " + godown + ") OR (SUPPLY_TO_TYPE ='G' AND SUPPLY_TO = " + godown + "))";
            }

            if (!yarn.Equals("0"))
            {
                yarnFilter = "and yarn = " + yarn;
                productFilter = "and product = " + yarn;
            }

            string origDateFilter = dateFilter;
            Boolean zeroBalance = dateFilter.Contains("<") && !dateFilter.Contains(">");

            double balance = 0;

            if (!zeroBalance)
            {
                if (dateFilter.Contains("<"))
                {
                    dateFilter = dateFilter.Substring(0, dateFilter.IndexOf('<') - 13);
                }
                dateFilter = dateFilter.Replace(">=", "<");
            }

            con.Open();

            String query = "select (isnull((SELECT sum(boxes) BOXES FROM PURCHASE " + firmFilter + " " + godownFilter + " " + dateFilter + " " + productFilter + "), 0) + isnull((SELECT SUM(BOXES) FROM SUPPLY_CONE " + firmFilter + " " + yarnFilter + " " + dateFilter + " " + supplyToFilter + " AND SUPPLY_TO_TYPE = 'G'), 0) - isnull((SELECT SUM(BOXES) FROM SUPPLY_CONE " + firmFilter + " " + yarnFilter + " " + dateFilter + " " + supplyFromFilter + " AND SUPPLY_FROM_TYPE = 'G'), 0)) OB";
            SqlCommand oCmd = new SqlCommand(query, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    balance = Double.Parse(oReader["OB"].ToString());
                }
            }

            con.Close();
            dateFilter = origDateFilter;

            if (zeroBalance)
            {
                balance = 0;
            }

            string sql1 = "select txn_date td, CONVERT(VARCHAR(12), txn_date, 107) txn_date, ISNULL((SELECT SUM(BOXES) FROM ( SELECT sum(boxes) BOXES FROM PURCHASE " + firmFilter + " " + godownFilter + " " + productFilter + " AND TXN_DATE = T.TXN_DATE UNION SELECT SUM(BOXES) FROM SUPPLY_CONE " + firmFilter + " AND TXN_DATE = T.TXN_DATE " + supplyToFilter + " " + yarnFilter + " AND SUPPLY_TO_TYPE = 'G') T), 0) INPUT, ISNULL((SELECT SUM(BOXES) FROM SUPPLY_CONE " + firmFilter + " AND TXN_DATE = T.TXN_DATE " + supplyFromFilter + " " + yarnFilter + " AND SUPPLY_FROM_TYPE = 'G'), 0) OUTPUT from (SELECT TXN_DATE FROM SUPPLY_CONE " + firmFilter + " AND BOXES > 0 "+ supplyFilter + " " + yarnFilter + " UNION SELECT TXN_DATE FROM PURCHASE " + firmFilter + " " + godownFilter + " " + productFilter + " AND BOXES > 0) T WHERE " + dateFilter.Substring(4) + " ORDER BY 1";

            // populate table
            fetchData(whereClause, dgv, sql1, balance);
        }

        private void fetchData(string whereClause, DataGridView dataGridView, string sql, Double balance)
        {
            String godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dataGridView.Height;

            gridCount++;
            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
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

            updateReport(sql, grid, whereClause, ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key, balance);
            GodownStockReport.formatDataGridView(grid, Color.Aquamarine);

            addCustomer.Controls.Add(grid);
        }

        public void updateReport(String sql, DataGridView dataGridView1, string whereClause, string godown, double balance)
        {
            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Date";
            dataGridView1.Columns[1].Name = "Input";
            dataGridView1.Columns[2].Name = "Output";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string[] row;
                    balance = balance + Double.Parse(oReader["INPUT"].ToString()) - Double.Parse(oReader["OUTPUT"].ToString());
                    row = new string[] { oReader["TXN_DATE"].ToString(), oReader["INPUT"].ToString(), oReader["OUTPUT"].ToString(), balance.ToString() };

                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();
        }

        public void clearAndPopulate(string whereClause, string dateFilter)
        {
            if (!loading)
            {
                // clear screen
                for (int i = 1; i <= gridCount; i++)
                {
                    addCustomer.Controls.Remove(Controls.Find("dataGridView" + i, true)[0]);
                }

                gridCount = 0;

                performTask(whereClause, dateFilter);
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearAndPopulate(whereClause, dateFilter);
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearAndPopulate(whereClause, dateFilter);
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new CartonFilter(firm, this).Show();
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

        private void pictureBox28_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox27_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox21_Click_1(object sender, EventArgs e)
        {
            var targetForm = new NewWeaver(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
