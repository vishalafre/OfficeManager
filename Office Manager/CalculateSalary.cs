using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class CalculateSalary : Form
    {
        private string firm;
        private byte[] logo;
        public int entryId = -1;
        Boolean loading = true;
        int count;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> weavers = new Dictionary<string, string>();
        Dictionary<string, string> qMtrs;
        Dictionary<string, string> qRates;
        double tds;

        public ComboBox cb3;
        public DateTimePicker dtp1;
        public DateTimePicker dtp2;

        public CalculateSalary()
        {
            InitializeComponent();
        }

        public CalculateSalary(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public CalculateSalary(string firm, byte[] logo, int entryId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.entryId = entryId;
        }

        private void CalculateSalary_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;

            cb3 = comboBox3;
            dtp1 = dateTimePicker1;
            dtp2 = dateTimePicker2;

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

            if (weavers.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(weavers, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            con.Close();
            loading = false;

            if (entryId != -1)
            {
                comboBox3.Enabled = false;
                button1.Text = "View";
                dateTimePicker1.Enabled = false;
                dateTimePicker2.Enabled = false;

                SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con.Open();

                string query1 = "select * from salary_summary where ENTRY_ID = @ENTRY_ID";
                SqlCommand oCmd1 = new SqlCommand(query1, con);
                oCmd1.Parameters.AddWithValue("@ENTRY_ID", entryId);

                using (SqlDataReader oReader = oCmd1.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        CultureInfo ci = CultureInfo.InvariantCulture;
                        string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                        dateTimePicker1.Value = DateTime.ParseExact(oReader["TO_DATE"].ToString().Split(' ')[0], sysFormat, ci);
                        dateTimePicker2.Value = DateTime.ParseExact(oReader["FROM_DATE"].ToString().Split(' ')[0], sysFormat, ci);

                        looms.Text = AddInvoice.round(Double.Parse(oReader["TP"].ToString()) / 10).ToString();
                        comboBox3.SelectedIndex = comboBox3.FindString(weavers[oReader["WEAVER"].ToString()]);
                    }
                }
                con.Close();
            }

            if (weavers.Count() > 0)
            {
                changeWeaver();
            }
        }

        public void changeWeaver()
        {
            qMtrs = new Dictionary<string, string>();
            qRates = new Dictionary<string, string>();

            for (int i = 1; i < count; i++)
            {
                panel3.Controls.Remove(Controls.Find("quality" + i, true)[0]);
                panel3.Controls.Remove(Controls.Find("taka" + i, true)[0]);
            }
            count = 0;

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            String query = "SELECT (SELECT TDS FROM WEAVER WHERE WID = @WEAVER) TDS, QUALITY, TECH_NAME, SUM(MTR) MTR, WEAVING_RATE, UNIT_EQUIVALENT, CALC_RATIO FROM TAKA_ENTRY TE, PRODUCT P WHERE TXN_DATE BETWEEN @FROM AND @TO AND TE.FIRM = @FIRM AND WEAVER = @WEAVER AND TE.QUALITY = P.PID GROUP BY QUALITY, TECH_NAME, WEAVING_RATE, UNIT_EQUIVALENT, CALC_RATIO UNION ALL SELECT (SELECT TDS FROM WEAVER WHERE WID = @WEAVER) TDS, QUALITY, TECH_NAME, SUM(MTR) MTR, WEAVING_RATE, UNIT_EQUIVALENT, CALC_RATIO FROM ROLL_ENTRY RE, PRODUCT P WHERE TXN_DATE BETWEEN @FROM AND @TO AND RE.FIRM = @FIRM AND WEAVER = @WEAVER AND RE.QUALITY = P.PID GROUP BY QUALITY, TECH_NAME, WEAVING_RATE, UNIT_EQUIVALENT, CALC_RATIO";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@FROM", dateTimePicker2.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@TO", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if (!qRates.ContainsKey(oReader["TECH_NAME"].ToString()))
                    {
                        qRates.Add(oReader["TECH_NAME"].ToString(), oReader["WEAVING_RATE"].ToString());
                    }
                    tds = Double.Parse(oReader["TDS"].ToString());
                    if (count == 0)
                    {
                        label2.Visible = true;
                        label7.Visible = true;
                        quality0.Visible = true;
                        taka0.Visible = true;

                        label4.Visible = false;

                        quality0.Text = oReader["TECH_NAME"].ToString();

                        double meters = Double.Parse(oReader["MTR"].ToString());
                        double calcRatio = Double.Parse(oReader["CALC_RATIO"].ToString());
                        double unitEqv = Double.Parse(oReader["UNIT_EQUIVALENT"].ToString());
                        
                        double taka = AddInvoice.round(meters * calcRatio / unitEqv / 100 * 2)/2.0;
                        
                        // DO TAKA ROUNDING HERE

                        taka0.Text = taka + "";
                        qMtrs.Add(oReader["TECH_NAME"].ToString(), taka.ToString());
                        //MessageBox.Show(oReader["TECH_NAME"].ToString());
                    }
                    else
                    {
                        double meters = Double.Parse(oReader["MTR"].ToString());

                        double calcRatio = Double.Parse(oReader["CALC_RATIO"].ToString());
                        double unitEqv = Double.Parse(oReader["UNIT_EQUIVALENT"].ToString());

                        double taka = AddInvoice.round(meters * calcRatio / unitEqv / 100 * 2) / 2.0;

                        var quality = new Label()
                        {
                            Name = "quality" + count,
                            Location = new Point(quality0.Location.X, quality0.Location.Y + count * 20),
                            Font = quality0.Font,
                            ForeColor = quality0.ForeColor,
                            Text = oReader["TECH_NAME"].ToString(),
                            Site = quality0.Site
                        };

                        var takaLbl = new LinkLabel()
                        {
                            Name = "taka" + count,
                            Location = new Point(taka0.Location.X, taka0.Location.Y + count * 20),
                            Font = taka0.Font,
                            ForeColor = taka0.ForeColor,
                            Text = taka + "",
                            Site = taka0.Site
                        };

                        takaLbl.LinkClicked += new LinkLabelLinkClickedEventHandler(view_mtr);

                        panel3.Controls.Add(quality);
                        panel3.Controls.Add(takaLbl);
                        if (!qMtrs.ContainsKey(oReader["TECH_NAME"].ToString()))
                        {
                            qMtrs.Add(oReader["TECH_NAME"].ToString(), taka.ToString());
                        }
                        else
                        {
                            string t = oReader["TECH_NAME"].ToString();
                            qMtrs[t] = (Double.Parse(qMtrs[t]) + taka).ToString();
                        }
                    }
                    count++;
                }
            }

            if(count == 0)
            {
                label2.Visible = false;
                label7.Visible = false;
                quality0.Visible = false;
                taka0.Visible = false;

                label4.Visible = true;
            }

            query = "select * from salary_summary where weaver = @WEAVER AND FROM_DATE BETWEEN @FROM AND @TO AND FIRM = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@FROM", dateTimePicker2.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@TO", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    entryId = Int32.Parse(oReader["ENTRY_ID"].ToString());
                    looms.Text = oReader["TP"].ToString();

                    if (loading)
                    {
                        comboBox3.Enabled = false;
                        dateTimePicker1.Enabled = false;
                        dateTimePicker2.Enabled = false;
                    }
                    button1.Text = "View";
                }
                else
                {
                    button1.Text = "Calculate";
                    entryId = -1;
                }
            }

            con.Close();
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                changeWeaver();
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                dateTimePicker1.Value = dateTimePicker2.Value.AddDays(6);
                changeWeaver();
            }
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var targetForm = new SalarySummary(firm, qMtrs, qRates, tds, Int32.Parse(looms.Text), ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key, dateTimePicker2.Value.ToString("dd-MMM-yyyy"), dateTimePicker1.Value.ToString("dd-MMM-yyyy"), this);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new SalaryList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
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

        private void view_mtr(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string fromDt = dateTimePicker2.Value.ToString("dd-MMM-yyyy");
            string toDt = dateTimePicker1.Value.ToString("dd-MMM-yyyy");
            string weaver = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            string wName = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Value;

            LinkLabel link = (LinkLabel)sender;
            int index = Int32.Parse(link.Name.Replace("taka", ""));

            string quality = ((Label)Controls.Find("quality" + index, true)[0]).Text;

            var targetForm = new SalaryMeters(weaver, quality, fromDt, toDt, wName);
            targetForm.StartPosition = FormStartPosition.CenterParent;
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
