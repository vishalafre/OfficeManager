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
    public partial class BeamReport : Form
    {
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        int gridCount;
        int reportCount;
        int coneCount;

        Dictionary<string, string> godowns = new Dictionary<string, string>();
        Dictionary<string, string> setNos;
        Boolean loading = true;
        string whereClause;
        string dateFilter;
        Boolean setNoChanged = false;

        public BeamReport(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        Boolean isOpen = false;

        private void BeamReport_Load(object sender, EventArgs e)
        {
            whereClause = "WHERE FIRM = '"+ firm +"'";

            if (!isOpen)
            {
                con.Open();
                isOpen = true;
            }

            String query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

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

            // populate periods

            populatePeriod();

            // populate set nos
            dateFilter = "";

            if (comboBox2.Items.Count > 0)
            {
                string[] period = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Value.Split(' ');
                dateFilter = " AND TXN_DATE BETWEEN '" + period[0] + "' AND '" + period[2] + "'";
            }

            if (isOpen)
            {
                con.Close();
                isOpen = false;
            }

            if (godowns.Count() > 0)
            {
                fetchSets(dateFilter);
            }
        }

        private void populatePeriod()
        {
            string query = "select ENTRY_ID, FROM_DT, TO_DT from BEAM_PERIOD where firm = @FIRM AND GODOWN = @GODOWN order by FROM_DT";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);

            Dictionary<string, string> periods = new Dictionary<string, string>();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    periods.Add(oReader["ENTRY_ID"].ToString(), ((DateTime)oReader["FROM_DT"]).ToString("dd-MMM-yyyy") + " to " + ((DateTime)oReader["TO_DT"]).ToString("dd-MMM-yyyy"));
                }
            }

            if (periods.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(periods, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }
        }

        public void fetchSets(string dateFilter)
        {
            if (!isOpen)
            {
                con.Open();
                isOpen = true;
            }

            loading = true;
            setNos = new Dictionary<string, string>();

            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string query = "select distinct set_no from SUPPLY_BEAM " + whereClause + " and set_no is not null and (supply_from_type = 'G' and supply_from = " + godown + ") " + dateFilter + " ORDER BY SET_NO";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    setNos.Add(oReader["SET_NO"].ToString(), oReader["SET_NO"].ToString());
                }
            }

            if (setNos.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(setNos, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";

                pictureBox26.Visible = true;
            }
            else
            {
                pictureBox25.Visible = false;
                pictureBox26.Visible = false;
            }

            SalaryReport.d1W = dgv.Width;
            SalaryReport.d1H = dgv.Height;

            loading = false;

            if (isOpen)
            {
                con.Close();
                isOpen = false;
            }

            if (setNos.Count() > 0)
            {
                string setNo = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Value;
                fetchBeams(whereClause, setNo, dateFilter);
            }
        }

        private void fetchBeams(string whereClause, string setNo, string periodFilter)
        {
            if (!isOpen)
            {
                con.Open();
                isOpen = true;
            }

            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string sql = "select distinct set_no, CONVERT(VARCHAR(12), txn_date, 106) txn_date, TECH_NAME from SUPPLY_BEAM SB, PRODUCT P, PRODUCT_REQ PR where SB.FIRM = '" + firm +"' AND PR.PRODUCT = P.PID AND SB.BEAM = PR.PID AND P.CATEGORY = 'Yarn' and (supply_from_type = 'G' and supply_from = " + godown + ") and set_no = " + setNo + " " + dateFilter + periodFilter;

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    gridCount++;
                    reportCount++;
                    int prevIndex = reportCount - 1;
                    int yLoc;
                    if (reportCount > 1)
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
                        Text = "Set No " + oReader["SET_NO"].ToString(),
                        Font = set.Font,
                        Visible = true
                    };

                    updateReport(grid, whereClause, oReader["SET_NO"].ToString(), periodFilter);
                    SalaryReport.formatDataGridView(grid);

                    addCustomer.Controls.Add(grid);
                    addCustomer.Controls.Add(beamNo);

                    fetchConeBalance(whereClause, oReader["TECH_NAME"].ToString(), oReader["SET_NO"].ToString(), grid);
                }
            }
            if (isOpen)
            {
                con.Close();
                isOpen = false;
            }
        }

        public void updateReport(DataGridView dataGridView1, string whereClause, string setNo, string periodFilter)
        {
            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string sql = "select DISTINCT SB1.set_no, SB1.beam_no, SB1.cuts, CONVERT(VARCHAR(12), isnull((select txn_date from supply_beam where supply_from_type = 'S' and supply_from = sb1.supply_from and beam_no = sb1.beam_no and set_no = sb1.set_no "+ periodFilter +" "+ dateFilter +"), sb1.txn_date), 107) txn_date, SB1.do_no, (case do_no when null then null else (select w_name from weaver where wid = SB1.supply_to) end) supply_to, (SELECT TECH_NAME FROM PRODUCT WHERE PID = SB1.BEAM) BEAM from SUPPLY_BEAM SB1 where SB1.firm = '" + firm + "' and SB1.set_no = " + setNo + " and (SB1.supply_from_type = 'G' and SB1.supply_from = " + godown + ") "+ dateFilter +" " + periodFilter + " ORDER BY SET_NO, BEAM_NO";

            dataGridView1.ColumnCount = 7;
            dataGridView1.Columns[0].Name = "Set No";
            dataGridView1.Columns[1].Name = "Beam No";
            dataGridView1.Columns[2].Name = "Cuts";
            dataGridView1.Columns[3].Name = "Date";
            dataGridView1.Columns[4].Name = "D.O. No";
            dataGridView1.Columns[5].Name = "Weaver";
            dataGridView1.Columns[6].Name = "Beam";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string[] row = new string[] { oReader["SET_NO"].ToString(), oReader["BEAM_NO"].ToString(),
                        oReader["CUTS"].ToString(), oReader["TXN_DATE"].ToString(), oReader["DO_NO"].ToString(),
                        oReader["SUPPLY_TO"].ToString(), oReader["BEAM"].ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            SalaryReport.resizeGrid(dataGridView1);
        }

        private void fetchConeBalance(string whereClause, string pName, string setNo, DataGridView masterGrid)
        {
            gridCount++;
            coneCount++;

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = dgv1.Size,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                Visible = true,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dgv.Location.X + dgv.Width + 20, masterGrid.Location.Y)
            };

            var beamNo = new Label()
            {
                Name = "beam" + gridCount,
                Location = new Point(dgv.Location.X + dgv.Width + 20, masterGrid.Location.Y - 22),
                Text = "Total Yarn",
                Font = set.Font,
                Visible = true
            };

            updateReportCone(grid, whereClause, setNo);
            GodownStockReport.formatDataGridView(grid, Color.Aquamarine);

            addCustomer.Controls.Add(grid);
            addCustomer.Controls.Add(beamNo);
        }

        public void updateReportCone(DataGridView dataGridView1, string whereClause, string setNo)
        {
            string godown = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string sql = "SELECT ISNULL(INPUT, 0) - isnull(WARP_WEIGHT, 0) + isnull(EXCESS, 0) OPEN_BAL, (select sum(i) from (select ISNULL(sum(qty), 0) i from supply_CONE " + whereClause + " AND  supply_to = " + godown + " and SUPPLY_TO_TYPE = 'G' and supply_from_type <> 'E' and txn_date between (select ISNULL(DATEADD(DAY, 1, MAX(txn_date)), '01-01-1900') from supply_beam SB where set_no < " + setNo + " and supply_from = " + godown + " " + dateFilter + " and SUPPLY_FROM_TYPE = 'G') and (select max(txn_date) from supply_beam " + whereClause + " AND  set_no = " + setNo + " and supply_from = " + godown + " and SUPPLY_FROM_TYPE = 'G') and supply_from_type <> 'O' union select isnull(sum(qty), 0) from PURCHASE " + whereClause + " AND  godown = " + godown + " and txn_date between (select ISNULL(DATEADD(DAY, 1, MAX(txn_date)), '01-01-1900') from supply_beam SB where set_no < " + setNo + " and supply_from = " + godown + " " + dateFilter + " and SUPPLY_FROM_TYPE = 'G') and (select max(txn_date) from supply_beam " + whereClause + " AND  set_no = " + setNo + " and supply_from = " + godown + " and SUPPLY_FROM_TYPE = 'G')) T) INP,  ISNULL((SELECT QTY FROM SUPPLY_CONE WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO = (SELECT min(TXN_ID) FROM SUPPLY_BEAM " + whereClause + " AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = " + godown + " AND SET_NO = " + setNo + "  " + dateFilter + ")), 0) WW,  ISNULL((SELECT qty from supply_cone where supply_from_type = 'E' and supply_from = (select MIN(txn_id) from supply_beam " + whereClause + " AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = " + godown + " AND SET_NO = " + setNo + " " + dateFilter + ")), 0) EX  FROM ( SELECT( select sum(input) from (select isnull(sum(qty), 0) input from supply_CONE " + whereClause + " AND  supply_to = " + godown + " and SUPPLY_TO_TYPE = 'G' and (txn_date <= (select max(txn_date) from supply_beam " + whereClause + " AND  set_no < " + setNo + " and supply_from = " + godown + " and SUPPLY_FROM_TYPE = 'G') or supply_from_type = 'O') union select isnull(sum(qty), 0) from PURCHASE " + whereClause + " AND  godown = " + godown + " and txn_date <= (select max(txn_date) from supply_beam " + whereClause + " AND  set_no < " + setNo + " and supply_from = " + godown + " and SUPPLY_FROM_TYPE = 'G')) T) input,  (SELECT SUM(WW) FROM ((SELECT ISNULL(QTY, 0) WW FROM SUPPLY_CONE WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO in (select min(txn_id) from (SELECT set_no, txn_id FROM SUPPLY_BEAM " + whereClause + " AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = " + godown + " AND SET_NO < " + setNo + "  " + dateFilter + " group by set_no, txn_id) x group by set_no))) T) warp_weight,  (SELECT SUM(EXCESS) FROM (SELECT ISNULL(qty, 0) EXCESS from supply_cone where supply_from_type = 'E' and supply_from = (select MIN(txn_id) from supply_beam " + whereClause + " AND SET_NO < " + setNo + " " + dateFilter + ")) T) excess) T ";

            dataGridView1.ColumnCount = 2;
            dataGridView1.Columns[0].Name = "Entity";
            dataGridView1.Columns[1].Name = "Value";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            SqlCommand oCmd = new SqlCommand(sql, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    //MessageBox.Show("Hi");
                    string[] row = new string[] {"OB", oReader["OPEN_BAL"].ToString() };
                    dataGridView1.Rows.Add(row);

                    row = new string[] { "Input", oReader["INP"].ToString() };
                    dataGridView1.Rows.Add(row);

                    row = new string[] { "Total", (Double.Parse(oReader["OPEN_BAL"].ToString()) + Double.Parse(oReader["INP"].ToString())).ToString() };
                    dataGridView1.Rows.Add(row);

                    row = new string[] { "Warp Weight", oReader["WW"].ToString() };
                    dataGridView1.Rows.Add(row);

                    row = new string[] { "Excess", oReader["EX"].ToString() };
                    dataGridView1.Rows.Add(row);

                    row = new string[] { "CB", (Double.Parse(oReader["OPEN_BAL"].ToString()) + Double.Parse(oReader["INP"].ToString()) - Double.Parse(oReader["WW"].ToString()) + Double.Parse(oReader["EX"].ToString())).ToString() };
                    dataGridView1.Rows.Add(row);
                }
            }

            con.Close();

            //SalaryReport.resizeGrid(dataGridView1);
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
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
                reportCount = 0;
                coneCount = 0;

                if (setNoChanged)
                {
                    if (setNos.Count() > 0)
                    {
                        string setNo = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Value;
                        string txnDate = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Split(':')[0];
                        fetchBeams(whereClause, setNo, dateFilter);
                    }
                }
                else
                {
                    if (godowns.Count() > 0)
                    {
                        fetchSets(dateFilter);
                    }
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!loading)
            {
                dateFilter = "";

                if (!isOpen)
                {
                    con.Open();
                    isOpen = true;
                }

                populatePeriod();
                if (comboBox2.Items.Count > 0)
                {
                    string[] period = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Value.Split(' ');
                    dateFilter = " AND TXN_DATE BETWEEN '" + period[0] + "' AND '" + period[2] + "'";
                }
                fetchSets(dateFilter);

                if (isOpen)
                {
                    con.Close();
                    isOpen = false;
                }
            }
            clearAndPopulate(whereClause, dateFilter);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            setNoChanged = true;
            clearAndPopulate(whereClause, dateFilter);
            setNoChanged = false;

            if (comboBox1.SelectedIndex == comboBox1.Items.Count - 1)
            {
                pictureBox26.Visible = false;
            }
            else
            {
                pictureBox26.Visible = true;
            }

            if (comboBox1.SelectedIndex == 0)
            {
                pictureBox25.Visible = false;
            }
            else
            {
                pictureBox25.Visible = true;
            }
            
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new BeamFilter(firm, this).Show();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                dateFilter = "";
                
                if (comboBox2.Items.Count > 0)
                {
                    string[] period = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Value.Split(' ');
                    dateFilter = " AND TXN_DATE BETWEEN '" + period[0] + "' AND '" + period[2] + "'";
                }

                if (isOpen)
                {
                    con.Close();
                    isOpen = false;
                }
            }
            clearAndPopulate(whereClause, dateFilter);
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

        private void pictureBox29_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox28_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox27_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex += 1;

            if(comboBox1.SelectedIndex == comboBox1.Items.Count - 1)
            {
                pictureBox26.Visible = false;
            }

            if(!pictureBox25.Visible)
            {
                pictureBox25.Visible = true;
            }
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex -= 1;

            if (comboBox1.SelectedIndex == 0)
            {
                pictureBox25.Visible = false;
            }

            if (!pictureBox26.Visible)
            {
                pictureBox26.Visible = true;
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
