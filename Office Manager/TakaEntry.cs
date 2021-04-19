using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class TakaEntry : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;
        private int entryId = -1;

        public TakaEntry()
        {
            InitializeComponent();
        }

        public TakaEntry(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public TakaEntry(string firm, byte[] logo, int entryId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.entryId = entryId;
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TakaEntry_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;

            MemoryStream ms = new MemoryStream(logo);
            pictureBox17.Image = Image.FromStream(ms);

            // set godown

            Dictionary<string, string> godowns = new Dictionary<string, string>();

            String query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godowns.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(godowns, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            // set weaver

            Dictionary<string, string> weavers = new Dictionary<string, string>();

            query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            oCmd = new SqlCommand(query, con);
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
                comboBox1.DataSource = new BindingSource(weavers, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";
            }

            // set quality

            Dictionary<string, string> qualities = new Dictionary<string, string>();

            query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Cloth' and TAKA = 'Y' order by TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    qualities.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (qualities.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(qualities, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            if(entryId != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;

                query = "select * from taka_entry where entry_id = @ENTRY_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@ENTRY_ID", entryId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox1.Text = oReader["TAKA_CNT"].ToString();
                        textBox3.Text = oReader["MTR"].ToString();
                        
                        comboBox2.SelectedIndex = comboBox2.FindString(godowns[oReader["GODOWN"].ToString()]);
                        comboBox1.SelectedIndex = comboBox1.FindString(weavers[oReader["WEAVER"].ToString()]);
                        comboBox3.SelectedIndex = comboBox3.FindString(qualities[oReader["QUALITY"].ToString()]);

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        dateTimePicker1.Value = DateTime.ParseExact(oReader["TXN_DATE"].ToString().Split(' ')[0], CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, ci);
                    }
                }
            }

            con.Close();
        }

        private Boolean isSalaryCalculated()
        {
            Boolean ret = false;
            String query = "select * FROM SALARY_SUMMARY WHERE WEAVER = @WEAVER AND FIRM = @FIRM AND FROM_DATE <= @TXN_DATE AND TO_DATE >= @TXN_DATE";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
            oCmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    ret = true;
                }
            }
            con.Close();
            return ret;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            /*if(isSalaryCalculated())
            {
                MessageBox.Show("Unable to process transaction. Salary is calculated for the weaver for selected date.");
                return;
            }*/

            if (entryId == -1)
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("insert into TAKA_ENTRY (FIRM, TXN_DATE, GODOWN, WEAVER, TAKA_CNT, QUALITY, MTR) " +
                    "values(@FIRM, @TXN_DATE, @GODOWN, @WEAVER, @TAKA_CNT, @QUALITY, @MTR)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@TAKA_CNT", textBox1.Text);
                cmd.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@MTR", textBox3.Text);

                cmd.ExecuteNonQuery();

                // INSERT IN SUPPLY_CONE

                cmd = new SqlCommand("INSERT INTO SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_FROM, SUPPLY_FROM_TYPE, SUPPLY_TO, SUPPLY_TO_TYPE) (select '" + firm + "', cast('" + dateTimePicker1.Value.ToString("dd-MMM-yyyy") + "' as date) txn_date, pr.product, round(40*(CAST(" + textBox3.Text + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty),0)/40 qty, " + ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key + " SUPPLY_FROM, 'W' FROM_TYPE, (SELECT MAX(ENTRY_ID) FROM TAKA_ENTRY) SUPPLY_TO, 'T' TO_TYPE from product_req pr, product p, PRODUCT P1 where P1.PID = PR.PID AND p.pid = pr.product and p.CATEGORY = 'Yarn' and pr.pid = " + ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key + ")", con);
                cmd.ExecuteNonQuery();

                // INSERT IN SUPPLY_BEAM

                cmd = new SqlCommand("INSERT INTO SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, EXCESS) (select @FIRM, @TXN_DATE, pr.product, CAST(" + textBox3.Text + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty qty, (SELECT MAX(ENTRY_ID) FROM TAKA_ENTRY), 'T', @SUPPLY_FROM, 'W', 0 from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Beam' and pr.pid = " + ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key + ")", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
                cmd.ExecuteNonQuery();

                con.Close();

                MessageBox.Show("Entry Successful");
            }
            else
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("UPDATE TAKA_ENTRY SET TXN_DATE = @TXN_DATE, GODOWN = @GODOWN, WEAVER = @WEAVER, TAKA_CNT = @TAKA_CNT, QUALITY = @QUALITY, MTR = @MTR WHERE ENTRY_ID = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@TAKA_CNT", textBox1.Text);
                cmd.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@MTR", textBox3.Text);

                cmd.ExecuteNonQuery();

                // UPDATE SUPPLY_CONE

                cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_TO_TYPE = 'T' AND SUPPLY_TO = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("INSERT INTO SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_FROM, SUPPLY_FROM_TYPE, SUPPLY_TO, SUPPLY_TO_TYPE) (select '" + firm + "', cast('" + dateTimePicker1.Value.ToString("dd-MMM-yyyy") + "' as date) txn_date, pr.product, round(40*(CAST(" + textBox3.Text + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty),0)/40 qty, " + ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key + " SUPPLY_FROM, 'W' FROM_TYPE, "+ entryId +" SUPPLY_TO, 'T' TO_TYPE from product_req pr, product p, PRODUCT P1 where P1.PID = PR.PID AND p.pid = pr.product and p.CATEGORY = 'Yarn' and pr.pid = " + ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key + ")", con);
                cmd.ExecuteNonQuery();

                // UPDATE SUPPLY_BEAM

                cmd = new SqlCommand("DELETE FROM SUPPLY_BEAM WHERE SUPPLY_TO_TYPE = 'T' AND SUPPLY_TO = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("INSERT INTO SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, EXCESS) (select @FIRM, @TXN_DATE, pr.product, CAST(" + textBox3.Text + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty qty, "+ entryId +", 'T', @SUPPLY_FROM, 'W', 0 from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Beam' and pr.pid = " + ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key + ")", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
                cmd.ExecuteNonQuery();

                con.Close();
                MessageBox.Show("Entry Updated");
            }
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaEntryList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void save0_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaDespatch(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("delete from taka_entry WHERE ENTRY_ID = @ENTRY_ID", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand("delete from SUPPLY_CONE WHERE SUPPLY_TO = @ENTRY_ID AND SUPPLY_TO_TYPE = 'T'", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand("delete from SUPPLY_BEAM WHERE SUPPLY_TO = @ENTRY_ID AND SUPPLY_TO_TYPE = 'T'", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
            cmd.ExecuteNonQuery();

            con.Close();

            textBox1.Text = "";
            textBox3.Text = "";

            MessageBox.Show("Transaction Cancelled");
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

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
