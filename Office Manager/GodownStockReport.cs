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
    public partial class GodownStockReport : Form
    {
        int gridCount;
        int rollCount;
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        Dictionary<string, string> godowns = new Dictionary<string, string>();
        Dictionary<string, string> cloths = new Dictionary<string, string>();
        Dictionary<string, string> yarns = new Dictionary<string, string>();

        Boolean loading = true;
        Boolean collapsed = false;
        int increment;
        string whereClause;
        string dateFilter = "";
        string ddType = "Cloth";

        public GodownStockReport(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void GodownStockReport_Load(object sender, EventArgs e)
        {
            SalaryReport.d1W = dgv.Width;
            SalaryReport.d1H = dgv.Height;

            increment = addCustomer.Location.Y - pictureBox17.Location.Y;

            whereClause = "WHERE FIRM = '" + firm + "'";
            dateFilter = "AND TXN_DATE >= DATEADD(DAY, -30, GETDATE())";
            con.Open();

            // cloth drop down

            string query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Cloth' order by TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            cloths.Add("0", "All");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    cloths.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (cloths.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(cloths, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";
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

            if (yarns.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(yarns, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            // godown drop down

            query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            godowns.Add("0", "All");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            con.Close();

            if (godowns.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(godowns, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            loading = false;

            performTask(whereClause, dateFilter);
        }

        private void fetchData(string whereClause, DataGridView dataGridView, Label quality, ComboBox cb, string sql, Double balance)
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

            var beamNo = new Label()
            {
                Name = "beam" + gridCount,
                Location = quality.Location,
                Text = "Godown : " + comboBox3.Text + ", " + ddType + " : " + cb.Text,
                Font = quality.Font,
                Visible = true,
                Size = new Size(grid.Width, quality.Height)
            };

            grid.RowTemplate.Height = 35;

            updateReport(sql, grid, whereClause, ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key, ((KeyValuePair<string, string>)cb.SelectedItem).Key, balance);
            formatDataGridView(grid, Color.Aquamarine);

            addCustomer.Controls.Add(grid);
            addCustomer.Controls.Add(beamNo);
        }

        // find opening balance and populate table
        public void performTask(string firmFilter, string dateFilter)
        {
            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string qlty = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key;
            string yarn = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key;

            string godownFilter = "";
            string qualityFilter = "";
            string yarnFilter = "";
            string supplyToFilter = "";
            string supplyFromFilter = "";
            string productFilter = "";

            string origDateFilter = dateFilter;
            Boolean zeroBalance = dateFilter.Contains("<") && !dateFilter.Contains(">");

            double clothBalance = 0;
            double yarnBalance = 0;

            if (!zeroBalance)
            {
                if (dateFilter.Contains("<"))
                {
                    dateFilter = dateFilter.Substring(0, dateFilter.IndexOf('<') - 13);
                }
                dateFilter = dateFilter.Replace(">=", "<");
            }

            string desDateFilter = dateFilter.Replace("TXN_DATE", "DESPATCH_DATE");
            string billDtFilter = desDateFilter.Replace("DESPATCH_DATE", "BILL_DT");
            string sbFirmFilter = firmFilter.Replace("WHERE FIRM = '", "WHERE SB.FIRM = '");

            if (!godown.Equals("0"))
            {
                godownFilter = "and godown = " + godown;
                supplyFromFilter = "and SUPPLY_FROM = " + godown;
                supplyToFilter = "and SUPPLY_TO = " + godown;
            }

            string joins = "";
            string pidPkFilter = "";
            if (!qlty.Equals("0"))
            {
                qualityFilter = "and quality = " + qlty;
                pidPkFilter = "and i.PID_PK = " + qlty;
            }

            if (ddType.Equals("Cloth") && checkBox1.Checked)
            {
                qualityFilter += " and re.quality = p.pid and p.unit = u.uid and UNIT_NAME = 'MTR'";
                firmFilter = firmFilter.ToUpper().Replace("WHERE FIRM", "WHERE RE.FIRM");
                joins = "re, product p, unit u ";

                pidPkFilter += " and i.UNIT like 'MTR-%'";
            }

            if (!yarn.Equals("0"))
            {
                yarnFilter = "and yarn = " + yarn;
                productFilter = "and product = " + yarn;
            }

            con.Open();

            String query = "SELECT ( select ( (select isnull(sum(mtr), 0.00) from roll_entry "+ joins +"" + firmFilter + " " + godownFilter + " " + qualityFilter + " " + dateFilter + ") + (select isnull(sum(mtr), 0.00) from taka_entry "+ joins +"" + firmFilter + " " + godownFilter + " " + qualityFilter + " " + dateFilter + ") - (select isnull((select sum(mtr) output from bill_item bi, bill sb, item i " + sbFirmFilter + " and sb.BILL_ID = bi.BILL_ID " + godownFilter + " " + billDtFilter + " and i.ITEM_ID = bi.item and godown is not null " + pidPkFilter + "), 0) ) )) OB_CLOTH, (SELECT ( (select isnull(sum(QTY), 0.000) from PURCHASE re " + firmFilter + " " + godownFilter + " " + productFilter + " " + dateFilter + ") + (select isnull(sum(QTY), 0.000) from SUPPLY_CONE re " + firmFilter + " " + supplyToFilter + " AND SUPPLY_TO_TYPE = 'G' " + yarnFilter + " " + dateFilter + ") - (select isnull(sum(QTY), 0.000) from SUPPLY_CONE re " + firmFilter + " " + supplyFromFilter + " AND SUPPLY_FROM_TYPE = 'G' " + yarnFilter + " " + dateFilter + "))) OB_YARN";
            SqlCommand oCmd = new SqlCommand(query, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    clothBalance = Double.Parse(oReader["OB_CLOTH"].ToString());
                    yarnBalance = Double.Parse(oReader["OB_YARN"].ToString());
                }
            }

            con.Close();
            dateFilter = origDateFilter;

            if (zeroBalance)
            {
                clothBalance = 0;
                yarnBalance = 0;
            }

            string sql1 = "select distinct txn_date td, CONVERT(VARCHAR(12), txn_date, 107) txn_date, isnull((select sum(input) from ( select distinct txn_date, sum(mtr) input from roll_entry " + joins + firmFilter + " " + godownFilter + " and txn_date = t.txn_date " + qualityFilter + " group by txn_date union select distinct txn_date, sum(mtr) input from taka_entry " + joins + firmFilter + " " + godownFilter + " and txn_date = t.txn_date " + qualityFilter + " group by txn_date) t), 0) INPUT, (select isnull(sum(extended_mtr) - sum(mtr), 0) from roll where despatch_date = t.txn_date and roll_no in (select roll_no from roll_content re " + firmFilter + " and entry_id in (select entry_id from roll_entry " + joins + firmFilter + " " + godownFilter + " " + qualityFilter + "))) ELONGATION, isnull((select sum(output) from (select mtr output from bill_item bi, bill sb, item i " + sbFirmFilter + " and sb.BILL_ID = bi.BILL_ID " + godownFilter + " and bill_dt = t.TXN_DATE and i.ITEM_ID = bi.item and godown is not null " + pidPkFilter + " ) t), 0) OUTPUT from (select txn_date from roll_entry " + joins + firmFilter + " " + godownFilter + "  " + qualityFilter + " union select txn_date from taka_entry " + joins + firmFilter + " " + godownFilter + "  " + qualityFilter + " union select bill_dt from bill_item bi, bill sb, item i " + sbFirmFilter + " and sb.BILL_ID = bi.BILL_ID " + godownFilter + " and i.ITEM_ID = bi.item and godown is not null " + pidPkFilter + ") t WHERE " + dateFilter.Substring(4) + " order by 1";
            string sql2 = "SELECT DISTINCT txn_date td, CONVERT(VARCHAR(12), txn_date, 107) txn_date, ISNULL((SELECT SUM(INPUT) FROM (SELECT SUM(QTY) INPUT FROM SUPPLY_CONE SC " + firmFilter + " " + yarnFilter + " " + supplyToFilter + " and SUPPLY_TO_TYPE = 'G' and txn_date = t.txn_date group by txn_date UNION SELECT SUM(QTY) FROM PURCHASE " + firmFilter + " " + godownFilter + " " + productFilter + " and txn_date = t.txn_date) T), 0) INPUT, ISNULL((select sum(output) from (select sum(qty) OUTPUT from supply_cone sc " + firmFilter + " " + supplyFromFilter + " " + yarnFilter + " and SUPPLY_FROM_TYPE = 'G' and txn_date = t.txn_date group by txn_date) T), 0) OUTPUT FROM (SELECT TXN_DATE FROM SUPPLY_CONE SC " + firmFilter + " AND qty <> 0 AND ((SUPPLY_TO_TYPE = 'G' " + supplyToFilter + ") OR (SUPPLY_FROM_TYPE = 'G' " + supplyFromFilter + ")) " + yarnFilter + " UNION SELECT TXN_DATE FROM PURCHASE P " + firmFilter + " " + godownFilter + " " + productFilter + "  " + productFilter + ") T WHERE " + dateFilter.Substring(4) + " ORDER BY 1";

            // populate table
            if (ddType.Equals("Cloth"))
            {
                fetchData(whereClause, dgv, quality, comboBox1, sql1, clothBalance);
            }
            else
            {
                fetchData(whereClause, dgv, quality, comboBox2, sql2, yarnBalance);
            }
            //fetchData(whereClause, dgv1, quality1, comboBox2, sql2, yarnBalance);
        }

        private void fetchTakas(string whereClause)
        {
            String godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dgv.Height;

            //SalaryReport.formatDataGridView(dataGridView);

            // Fetch all takas for godown

            string sql = "SELECT distinct QUALITY, TECH_NAME FROM ( SELECT TE.FIRM, QUALITY, TXN_DATE, GODOWN FROM TAKA_ENTRY TE UNION SELECT FIRM, QUALITY, DESPATCH_DATE TXN_DATE, GODOWN FROM TAKA_DESPATCH) T, PRODUCT P WHERE P.PID = T.QUALITY AND T.GODOWN = @GODOWN AND T." + whereClause;
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);
            oCmd.Parameters.AddWithValue("@GODOWN", godown);

            int takaCount = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    gridCount++;
                    takaCount++;
                    int prevIndex = takaCount - 1;
                    int yLoc;
                    if (takaCount > 1)
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + prevIndex, true)[0];
                        yLoc = prevGrid.Location.Y + prevGrid.Height + 25;
                    }
                    else
                    {
                        yLoc = dgv.Location.Y;
                    }


                    var grid = new DataGridView()
                    {
                        Name = "dataGridView" + gridCount,
                        Size = dgv.Size,
                        BorderStyle = BorderStyle.None,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        Visible = true,
                        AllowUserToAddRows = false,
                        AllowUserToOrderColumns = false,
                        AllowUserToDeleteRows = false,
                        Location = new Point(dgv.Location.X + dgv.Width, yLoc + 22)
                    };

                    var beamNo = new Label()
                    {
                        Name = "beam" + gridCount,
                        Location = new Point(dgv.Location.X + dgv.Width, yLoc),
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = quality.Font,
                        Visible = true
                    };

                    updateReportTaka(grid, whereClause, godown, oReader["QUALITY"].ToString());
                    formatDataGridView(grid, Color.Yellow);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);
                }
            }
            con.Close();
        }

        private void fetchCone(string whereClause)
        {
            String godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dgv.Height;

            //SalaryReport.formatDataGridView(dataGridView);

            // Fetch all yarns for godown

            string sql = "SELECT distinct yarn, tech_name FROM ( select FIRM, YARN, TXN_DATE from supply_cone SC where ((supply_to_type = 'G' AND supply_to = @GODOWN) OR (supply_from_type = 'G' AND supply_from = @GODOWN)) group by SUPPLY_TO_TYPE, supply_from_type, txn_date, supply_from, FIRM, YARN UNION select FIRM, PRODUCT YARN, TXN_DATE from purchase WHERE GODOWN = @GODOWN) T, product p WHERE p.pid = t.yarn and T." + whereClause;
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);
            //oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@GODOWN", godown);

            int coneCount = 0;

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    gridCount++;
                    coneCount++;
                    int prevIndex = coneCount - 1;
                    int yLoc;
                    if (coneCount > 1)
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + prevIndex, true)[0];
                        yLoc = prevGrid.Location.Y + prevGrid.Height + 25;
                    }
                    else
                    {
                        yLoc = dgv.Location.Y;
                    }

                    var grid = new DataGridView()
                    {
                        Name = "dataGridView" + gridCount,
                        Size = dgv.Size,
                        BorderStyle = BorderStyle.None,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        Visible = true,
                        AllowUserToAddRows = false,
                        AllowUserToOrderColumns = false,
                        AllowUserToDeleteRows = false,
                        Location = new Point(dgv.Location.X + (dgv.Width + 13), yLoc + 22)
                    };

                    var beamNo = new Label()
                    {
                        Name = "beam" + gridCount,
                        Location = new Point(dgv.Location.X + (dgv.Width + 13), yLoc),
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = quality.Font,
                        Visible = true
                    };

                    updateReportCone(grid, whereClause, godown, oReader["YARN"].ToString());
                    //formatDataGridView(grid, Color.Beige);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);

                }
            }
            con.Close();
        }

        private void fetchBeams(string whereClause)
        {
            String godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            int dHeight = dgv.Height;

            //SalaryReport.formatDataGridView(dataGridView);

            // Fetch all beams for godown

            string sql = "select distinct beam, TECH_NAME from supply_beam SB, PRODUCT P where SB." + whereClause + " AND P.PID = SB.BEAM AND ((supply_to_type = 'G' AND supply_to = @GODOWN) OR (supply_from_type = 'G' AND supply_from = @GODOWN)) AND (SUPPLY_FROM_TYPE IS NULL OR (SUPPLY_FROM_TYPE = 'G' AND SUPPLY_TO_TYPE = 'G'))";
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);
            oCmd.Parameters.AddWithValue("@GODOWN", godown);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    gridCount++;
                    rollCount++;
                    int prevIndex = rollCount - 1;
                    int yLoc;
                    if (rollCount > 1)
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + prevIndex, true)[0];
                        yLoc = prevGrid.Location.Y + prevGrid.Height + 25;
                    }
                    else
                    {
                        yLoc = dgv.Location.Y;
                    }


                    var grid = new DataGridView()
                    {
                        Name = "dataGridView" + gridCount,
                        Size = dgv.Size,
                        BorderStyle = BorderStyle.None,
                        RowHeadersVisible = false,
                        BackgroundColor = Color.White,
                        Visible = true,
                        AllowUserToAddRows = false,
                        AllowUserToOrderColumns = false,
                        AllowUserToDeleteRows = false,
                        Location = new Point(dgv.Location.X, yLoc + 22)
                    };

                    var beamNo = new Label()
                    {
                        Name = "beam" + gridCount,
                        Location = new Point(dgv.Location.X, yLoc),
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = quality.Font,
                        Visible = true
                    };

                    updateReportBeam(grid, whereClause, godown, oReader["BEAM"].ToString());
                    formatDataGridView(grid, Color.GreenYellow);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);
                }
            }
            con.Close();
        }

        public void updateReport(String sql, DataGridView dataGridView1, string whereClause, string godown, string quality, double balance)
        {
            if (ddType.Equals("Cloth"))
            {
                dataGridView1.ColumnCount = 4;
                dataGridView1.Columns[0].Name = "Date";
                dataGridView1.Columns[1].Name = "Input";
                //dataGridView1.Columns[2].Name = "Elongation";
                dataGridView1.Columns[2].Name = "Output";
                dataGridView1.Columns[3].Name = "Balance";
            }
            else
            {
                dataGridView1.ColumnCount = 4;
                dataGridView1.Columns[0].Name = "Date";
                dataGridView1.Columns[1].Name = "Input";
                dataGridView1.Columns[2].Name = "Output";
                dataGridView1.Columns[3].Name = "Balance";
            }

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string[] row;
                    if (ddType.Equals("Cloth"))
                    {
                        balance = balance + Double.Parse(oReader["INPUT"].ToString()) + /*Double.Parse(oReader["ELONGATION"].ToString())*/ -Double.Parse(oReader["OUTPUT"].ToString());
                        row = new string[] { oReader["TXN_DATE"].ToString(), oReader["INPUT"].ToString(),/* oReader["ELONGATION"].ToString(),*/ oReader["OUTPUT"].ToString(), balance.ToString() };
                    }
                    else
                    {
                        balance = balance + Double.Parse(oReader["INPUT"].ToString()) - Double.Parse(oReader["OUTPUT"].ToString());
                        row = new string[] { oReader["TXN_DATE"].ToString(), oReader["INPUT"].ToString(), oReader["OUTPUT"].ToString(), balance.ToString() };
                    }

                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            //SalaryReport.resizeGrid(dataGridView1);
        }

        public void updateReportTaka(DataGridView dataGridView1, string whereClause, string godown, string quality)
        {
            string sql = "SELECT TXN_TYPE, CONVERT(VARCHAR(12), txn_date, 107) txn_date, TAKA_CNT, MTR FROM ( SELECT FIRM, QUALITY, GODOWN, 'INPUT' TXN_TYPE, TXN_DATE, TAKA_CNT, MTR FROM TAKA_ENTRY UNION SELECT FIRM, QUALITY, GODOWN, 'DESPATCH' TXN_TYPE, DESPATCH_DATE, TAKA_CNT, MTR FROM TAKA_DESPATCH) T WHERE T.GODOWN = " + godown + " AND T.QUALITY = " + quality + " AND " + whereClause + " ORDER BY TXN_DATE";

            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Txn Type";
            dataGridView1.Columns[1].Name = "Date";
            dataGridView1.Columns[2].Name = "Meter / Taka";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();
            double balanceTaka = 0;
            double balanceMtr = 0;

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string txnType = oReader["TXN_TYPE"].ToString();
                    double cuts = Double.Parse(oReader["MTR"].ToString());
                    double taka = Double.Parse(oReader["TAKA_CNT"].ToString());

                    if (txnType.Contains("INPUT"))
                    {
                        balanceMtr += cuts;
                        balanceTaka += taka;
                    }
                    else
                    {
                        balanceMtr -= cuts;
                        balanceTaka += taka;
                    }
                    string[] row = new string[] { txnType, oReader["TXN_DATE"].ToString(), cuts.ToString() + " / " + taka.ToString(), balanceMtr.ToString() + " / " + balanceTaka.ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        public void updateReportCone(DataGridView dataGridView1, string whereClause, string godown, string yarn)
        {
            string sql = "select distinct CONVERT(VARCHAR(12), txn_date, 107) txn_date, ISNULL((select sum(qty) input from supply_cone sc where sc.SUPPLY_TO = 1 and SUPPLY_TO_TYPE = 'G' and txn_date = t.txn_date group by txn_date), 0) INPUT, ISNULL((select sum(qty) OUTPUT from supply_cone sc where sc.SUPPLY_FROM = 1 and SUPPLY_FROM_TYPE = 'G' and txn_date = t.txn_date group by txn_date), 0) OUTPUT from supply_cone t WHERE (SUPPLY_TO_TYPE = 'G' AND SUPPLY_TO = 1) OR (SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = 1) order by txn_date";

            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Date";
            dataGridView1.Columns[1].Name = "Input";
            dataGridView1.Columns[2].Name = "Output";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();
            double balance = 0;

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    balance = balance + Double.Parse(oReader["INPUT"].ToString()) - Double.Parse(oReader["OUTPUT"].ToString());

                    string[] row = new string[] { oReader["TXN_DATE"].ToString(), oReader["INPUT"].ToString(), oReader["OUTPUT"].ToString(), balance.ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        public void updateReportBeam(DataGridView dataGridView1, string whereClause, string godown, string beam)
        {
            string sql = "select (case when SUPPLY_TO_TYPE = 'G' AND SUPPLY_TO = " + godown + " THEN 'INPUT' when SUPPLY_TO_TYPE = 'O' THEN 'OB' WHEN supply_from_type = 'G' THEN 'OUTPUT' END) TXN_TYPE, CONVERT(VARCHAR(12), txn_date, 107) TXN_DATE, sum(CUTS) CUTS from supply_beam SB where " + whereClause + " AND ((supply_to_type = 'G' AND supply_to = " + godown + ") OR (supply_from_type = 'G' AND supply_from = " + godown + ")) AND (SUPPLY_FROM_TYPE IS NULL OR (SUPPLY_FROM_TYPE = 'G' AND SUPPLY_TO_TYPE = 'G')) and beam = " + beam + " group by SUPPLY_TO, SUPPLY_TO_TYPE, supply_from_type, txn_date, supply_from ORDER BY TXN_DATE";

            dataGridView1.ColumnCount = 4;
            dataGridView1.Columns[0].Name = "Txn Type";
            dataGridView1.Columns[1].Name = "Date";
            dataGridView1.Columns[2].Name = "Cuts";
            dataGridView1.Columns[3].Name = "Balance";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();
            double balance = 0;

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string txnType = oReader["TXN_TYPE"].ToString();
                    double cuts = Double.Parse(oReader["CUTS"].ToString());
                    if (!txnType.Contains("OUTPUT"))
                    {
                        balance += cuts;
                    }
                    else
                    {
                        balance -= cuts;
                    }
                    string[] row = new string[] { txnType, oReader["TXN_DATE"].ToString(), cuts.ToString(), balance.ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        public static void formatDataGridView(DataGridView dataGridView1, Color color)
        {
            dataGridView1.RowsDefaultCellStyle.BackColor = color;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;

            dataGridView1.RowsDefaultCellStyle.SelectionBackColor = color;
            dataGridView1.RowsDefaultCellStyle.SelectionForeColor = Color.Black;

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                c.Width = dataGridView1.Width / dataGridView1.ColumnCount - 4;
            }

            dataGridView1.DefaultCellStyle.Font = new Font(dataGridView1.DefaultCellStyle.Font.FontFamily, 10);
        }

        public void clearAndPopulate(string whereClause, string dateFilter)
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
                rollCount = 0;

                performTask(whereClause, dateFilter);
            }
        }

        private void displayBalance()
        {
            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string quality = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key;
            string yarn = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key;
            string godownFilter = "";
            string supplyToFilter = "";
            string supplyFromFilter = "";
            string qualityFilter = "";
            string productFilter = "";
            string yarnFilter = "";
            string pidFilter = "";
            string pidPkFilter = "";

            if (!godown.Equals("0"))
            {
                godownFilter = "and godown = " + godown;
                supplyToFilter = "and supply_to = " + godown;
                supplyFromFilter = "and supply_from = " + godown;
            }

            if (!quality.Equals("0"))
            {
                qualityFilter = "and quality = " + quality;
                pidPkFilter = "and i.pid_pk = " + quality;
            }

            if (!yarn.Equals("0"))
            {
                productFilter = "and product = " + yarn;
                yarnFilter = "and yarn = " + yarn;
                pidFilter = "and p.pid = " + yarn;
            }

            con.Open();

            string query = "SELECT (ISNULL((select(select isnull(sum(MTR), 0) from roll_entry where firm = @FIRM " + godownFilter + " " + qualityFilter + ") + (select isnull(sum(MTR), 0) from taka_entry where firm = @FIRM " + godownFilter + " " + qualityFilter + ") - (select isnull(sum(mtr), 0) from bill_item bi, bill sb, item i WHERE SB.FIRM = @FIRM " + godownFilter + " " + pidPkFilter + " and sb.BILL_ID = bi.BILL_ID and i.ITEM_ID = bi.item and godown is not null)), 0)) CLOTH_BAL , (SELECT ISNULL((SELECT ((select isnull(sum(QTY), 0.000) from PURCHASE where firm = @FIRM " + godownFilter + " " + productFilter + ") + (select isnull(sum(QTY), 0.000) from SUPPLY_CONE where firm = @FIRM " + supplyToFilter + " AND SUPPLY_TO_TYPE = 'G' " + yarnFilter + ") - (select isnull(sum(QTY), 0.000) from SUPPLY_CONE where firm = @FIRM " + supplyFromFilter + " AND SUPPLY_FROM_TYPE = 'G' " + yarnFilter + "))), 0.000)) YARN_BAL";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    label4.Text = "Total Cloth : " + oReader["CLOTH_BAL"].ToString() + " mtr";
                    label7.Text = "Total Yarn : " + oReader["YARN_BAL"].ToString() + " kg";
                }
            }

            con.Close();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            displayBalance();
            clearAndPopulate(whereClause, dateFilter);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddType = "Cloth";
            clearAndPopulate(whereClause, dateFilter);
            if (!loading)
            {
                displayBalance();
            }
            comboBox1.BackColor = Color.Yellow;
            comboBox2.BackColor = comboBox3.BackColor;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            ddType = "Yarn";
            clearAndPopulate(whereClause, dateFilter);
            if (!loading)
            {
                displayBalance();
            }
            comboBox2.BackColor = Color.Yellow;
            comboBox1.BackColor = comboBox3.BackColor;
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            if (!collapsed)
            {
                panel1.Visible = false;
                pictureBox17.Visible = false;
                pictureBox19.Visible = false;

                addCustomer.Location = new Point(addCustomer.Location.X, pictureBox17.Location.Y);
                addCustomer.Height += increment;
                addCustomer.Controls.Find("dataGridView1", true)[0].Height += increment;

                pictureBox25.Image = Properties.Resources.show;
            }
            else
            {
                panel1.Visible = true;
                pictureBox17.Visible = true;
                pictureBox19.Visible = true;

                addCustomer.Location = new Point(addCustomer.Location.X, pictureBox17.Location.Y + increment);
                addCustomer.Height -= increment;
                addCustomer.Controls.Find("dataGridView1", true)[0].Height -= increment;

                pictureBox25.Image = Properties.Resources.collpase;
            }
            collapsed = !collapsed;
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new GodownStockFilter(firm, this).Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new CurrentStock(firm, ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key, comboBox3.Text).Show();
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ddType = "Cloth";
            clearAndPopulate(whereClause, dateFilter);
            if (!loading)
            {
                displayBalance();
            }

            comboBox1.BackColor = Color.Yellow;
            comboBox2.BackColor = comboBox3.BackColor;
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
