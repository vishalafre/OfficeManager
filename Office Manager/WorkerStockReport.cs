using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class WorkerStockReport : Form
    {
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        int gridCount = 0;
        Dictionary<string, string> weavers = new Dictionary<string, string>();
        Boolean loading = true;
        string whereClause;

        public WorkerStockReport(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void WorkerStockReport_Load(object sender, EventArgs e)
        {
            whereClause = "FIRM = '" + firm + "' AND TXN_DATE >= DATEADD(DAY, -30, GETDATE()) AND txn_date <= GETDATE()";

            SalaryReport.d1W = dataGridView.Width;
            SalaryReport.d1H = dataGridView.Height;

            // Populate weavers

            con.Open();
            String query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weavers.Add(oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }
            con.Close();

            if (weavers.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(weavers, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }
            loading = false;

            if(weavers.Count() > 0)
            {
                // fetch qualities
                fetchQualities(whereClause);

                // fetch cone
                fetchCone(whereClause);
            }
        }

        private void fetchQualities(string whereClause)
        {
            String weaver = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dataGridView.Height;
            
            //updateReport(dataGridView, whereClause, weaver, "21");
            SalaryReport.formatDataGridView(dataGridView);

            // Fetch all beams for weaver

            string sql = "select DISTINCT BEAM, (SELECT TECH_NAME FROM PRODUCT WHERE PID = SB.BEAM) TECH_NAME from supply_beam SB where "+ whereClause +" AND ((SUPPLY_TO = @WEAVER and SUPPLY_TO_TYPE = 'W') or (SUPPLY_FROM = @WEAVER and SUPPLY_FROM_TYPE = 'W'))";
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);
            //oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@WEAVER", weaver);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    gridCount++;
                    int prevIndex = gridCount - 2;
                    int yLoc;
                    if (gridCount > 2)
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + prevIndex, true)[0];
                        yLoc = prevGrid.Location.Y + prevGrid.Height + 25;
                    }
                    else
                    {
                        yLoc = dataGridView.Location.Y;
                    }


                    var grid = new DataGridView()
                    {
                        Name = "dataGridView" + gridCount,
                        Size = dataGridView.Size,
                        BorderStyle = BorderStyle.None,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        Visible = true,
                        AllowUserToAddRows = false,
                        AllowUserToOrderColumns = false,
                        AllowUserToDeleteRows = false,
                        Location = new Point(dataGridView.Location.X + (dataGridView.Width + 13) * ((gridCount - 1) % 2), yLoc + 22)
                    };

                    var beamNo = new Label()
                    {
                        Name = "beam" + gridCount,
                        Location = new Point(dataGridView.Location.X + (dataGridView.Width + 13) * ((gridCount - 1) % 2), yLoc),
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = beam.Font,
                        Visible = true
                    };

                    updateReport(grid, whereClause, weaver, oReader["BEAM"].ToString());
                    SalaryReport.formatDataGridView(grid);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);
                }
            }
            con.Close();
        }

        private void fetchCone(string whereClause)
        {
            String weaver = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dataGridView.Height;
            
            //updateReport(dataGridView, whereClause, weaver, "21");
            SalaryReport.formatDataGridView(dataGridView);

            // Fetch all cones for weaver

            string sql = "select DISTINCT YARN, (SELECT TECH_NAME FROM PRODUCT WHERE PID = SC.YARN) TECH_NAME from supply_CONE SC where "+ whereClause +" AND ((SUPPLY_TO = @WEAVER and SUPPLY_TO_TYPE = 'W') or (SUPPLY_FROM = @WEAVER and SUPPLY_FROM_TYPE = 'W'))";
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);
            //oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@WEAVER", weaver);

            int coneCount = 0;

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    gridCount++;
                    coneCount++;
                    int prevIndex = gridCount - 1;
                    int yLoc;
                    if (coneCount > 1)
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + prevIndex, true)[0];
                        yLoc = prevGrid.Location.Y + prevGrid.Height + 25;
                    }
                    else
                    {
                        yLoc = dataGridView.Location.Y;
                    }

                    var grid = new DataGridView()
                    {
                        Name = "dataGridView" + gridCount,
                        Size = dataGridView.Size,
                        BorderStyle = BorderStyle.None,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        Visible = true,
                        AllowUserToAddRows = false,
                        AllowUserToOrderColumns = false,
                        AllowUserToDeleteRows = false,
                        Location = new Point(dataGridView.Location.X + (dataGridView.Width) * 2, yLoc + 22)
                    };

                    var beamNo = new Label()
                    {
                        Name = "beam" + gridCount,
                        Location = new Point(dataGridView.Location.X + (dataGridView.Width) * 2, yLoc),
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = beam.Font,
                        Visible = true
                    };

                    updateReportCone(grid, whereClause, weaver, oReader["YARN"].ToString());
                    formatDataGridViewCone(grid);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);
                    
                }
            }
            con.Close();
        }

        public void updateReport(DataGridView dataGridView1, string whereClause, string weaver, string beam)
        {
            string sql = "select (case when SUPPLY_TO_TYPE = 'R' THEN 'MFG' when SUPPLY_TO_TYPE = 'T' THEN 'MFG' WHEN (supply_to_type = 'G' OR (supply_from_type = 'W' AND SUPPLY_FROM = " + weaver + ")) and SUPPLY_FROM_TYPE = 'W' AND SUPPLY_FROM = " + weaver + " THEN 'RETURN' WHEN SUPPLY_FROM_TYPE = 'O' THEN 'OB' ELSE CONCAT(COUNT(CUTS), ' BEAM') END) \"Txn Type\", CONVERT(VARCHAR(12), txn_date, 107) \"Date\", sum(CUTS) \"Cuts\" from supply_beam SB where " + whereClause + " AND ((supply_to_type = 'W' and supply_from_type IN ('G', 'S', 'O', 'W') AND (EXCESS IS NOT NULL or SUPPLY_FROM_TYPE = 'O') AND supply_to = " + weaver + ") OR (supply_from_type = 'W' AND supply_from = " + weaver + ")) AND BEAM = " + beam + " group by SUPPLY_TO_TYPE, txn_date, SUPPLY_FROM, supply_from_type ORDER BY TXN_DATE";

            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Txn Type";
            dataGridView1.Columns[1].Name = "Date";
            dataGridView1.Columns[2].Name = "Cuts";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            //fetch opening balance

            string[] parts = Regex.Split(whereClause.ToUpper(), @"AND TXN_DATE <=.*");
            string obDateFilter1 = parts[0].Replace(">=", "<").Replace("TXN_DATE", "(select max(txn_date) from (SELECT sb1.txn_date union select txn_date from SUPPLY_BEAM sb2 where SUPPLY_FROM_TYPE = 'S' and sb1.set_no = sb2.set_no and sb1.beam_no = sb2.beam_no and sb1.firm = sb2.firm and txn_date between (select from_dt from BEAM_PERIOD where sb1.txn_date between from_dt and to_dt AND GODOWN = SB1.SUPPLY_FROM) and (select to_dt from BEAM_PERIOD where sb1.txn_date between from_dt and to_dt AND GODOWN = SB1.SUPPLY_FROM))t)");
            string obDateFilter2 = parts[0].Replace(">=", "<");

            String query = "select (select isnull(sum(qty),0) qty from(select round(2*sum(cuts),0)/2 qty from supply_beam SB1 where SUPPLY_TO = " + weaver + " and SUPPLY_TO_TYPE = 'W' and supply_from_type <> 'S' and beam = " + beam + " AND " + obDateFilter1 + " group by txn_date) t) - (select isnull(sum(qty), 0) from (select round(2*sum(cuts),0)/2 qty from supply_beam SB1 where SUPPLY_from = " + weaver + " and SUPPLY_from_TYPE = 'W' and beam = " + beam + " AND " + obDateFilter2 + " group by txn_date) t) qty";
            SqlCommand oCmd = new SqlCommand(query, con);

            double balance = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    if (!oReader["QTY"].ToString().Equals(""))
                    {
                        balance = Double.Parse(oReader["QTY"].ToString());
                    }
                }
            }

            oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string txnType = oReader["Txn Type"].ToString();
                    string x = oReader["Cuts"].ToString();
                    double cuts = AddInvoice.round(Double.Parse(oReader["Cuts"].ToString())*2.0)/2.0;
                    if (txnType.Contains("BEAM") || txnType.Equals("OB"))
                    {
                        balance += cuts;
                    }
                    else
                    {
                        balance -= cuts;
                    }
                    string[] row = new string[] { txnType, oReader["Date"].ToString(), cuts.ToString(), balance.ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        public void updateReportCone(DataGridView dataGridView1, string whereClause, string weaver, string yarn)
        {
            string sql = "select (case when SUPPLY_TO_TYPE = 'R' THEN 'MFG' when SUPPLY_TO_TYPE = 'T' THEN 'MFG' WHEN (supply_from_type = 'W' AND SUPPLY_FROM = @WEAVER) THEN 'RETURN' WHEN SUPPLY_FROM_TYPE = 'O' THEN 'OB' ELSE 'CONE' END) TXN_TYPE, CONVERT(VARCHAR(12), txn_date, 107) DATE, sum(QTY) QTY from supply_CONE SC where " + whereClause +" AND ((supply_to_type = 'W' AND supply_to = @WEAVER) OR (supply_from_type = 'W' AND supply_from = @WEAVER)) and yarn = @YARN group by SUPPLY_TO_TYPE, supply_from_type, txn_date, supply_from ORDER BY TXN_DATE";

            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Txn Type";
            dataGridView1.Columns[1].Name = "Date";
            dataGridView1.Columns[2].Name = "Qty";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            //fetch opening balance

            string[] parts = Regex.Split(whereClause.ToUpper(), @"AND TXN_DATE <=.*");
            string obDateFilter = parts[0].Replace(">=", "<");

            String query = "select (select sum(qty) qty from(select sum(qty) qty from supply_cone where SUPPLY_TO = " + weaver +" and SUPPLY_TO_TYPE = 'W' and yarn = "+ yarn +" AND "+ obDateFilter + " group by txn_date) t) - (select sum(qty) from (select sum(qty) qty from supply_cone where SUPPLY_from = " + weaver +" and SUPPLY_from_TYPE = 'W' and yarn = "+ yarn +" AND "+ obDateFilter +" group by txn_date) t) qty";
            SqlCommand oCmd = new SqlCommand(query, con);

            double balance = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    if (!oReader["QTY"].ToString().Equals(""))
                    {
                        balance = Double.Parse(oReader["QTY"].ToString());
                    }
                }
            }

            oCmd = new SqlCommand(sql, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@WEAVER", weaver);
            oCmd.Parameters.AddWithValue("@YARN", yarn);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string txnType = oReader["TXN_TYPE"].ToString();
                    double cuts = Double.Parse(oReader["QTY"].ToString());
                    if (txnType.Contains("CONE") || txnType.Equals("OB"))
                    {
                        balance += cuts;
                    }
                    else
                    {
                        balance -= cuts;
                    }
                    balance = AddInvoice.round(balance*40)/40.0;

                    string[] row = new string[] { txnType, oReader["DATE"].ToString(), cuts.ToString(), balance.ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        public void clearAndPopulate(string whereClause)
        {
            if (!loading)
            {
                // clear screen
                for (int i = 1; i <= gridCount; i++)
                {
                    addCustomer.Controls.Remove(Controls.Find("dataGridView" + i, true)[0]);
                    addCustomer.Controls.Remove(addCustomer.Controls.Find("beam" + i, true)[0]);
                }

                gridCount = 0;
                fetchQualities(whereClause);
                fetchCone(whereClause);
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            clearAndPopulate(whereClause);
        }

        public static void formatDataGridViewCone(DataGridView dataGridView1)
        {
            dataGridView1.RowsDefaultCellStyle.BackColor = Color.Aquamarine;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;

            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.Aquamarine;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            //dataGridView1.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                c.DefaultCellStyle.Font = new Font("Arial", 12F, GraphicsUnit.Pixel);
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            }
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new WorkerStockFilter(firm, this).Show();
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

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
