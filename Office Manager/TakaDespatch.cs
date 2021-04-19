using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class TakaDespatch : Form
    {
        string firm;
        byte[] logo;
        int tdId = -1;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public TakaDespatch(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public TakaDespatch(string firm, byte[] logo, int tdId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.tdId = tdId;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (tdId == -1)
            {
                con.Open();

                SqlCommand cmd1 = new SqlCommand("INSERT INTO TAKA_DESPATCH (FIRM, TAKA_CNT, MTR, QUALITY, DESPATCH_DATE, GODOWN) VALUES (@FIRM, @TAKA_CNT, @MTR, @QUALITY, @DESPATCH_DATE, @GODOWN)", con);
                cmd1.Parameters.AddWithValue("@FIRM", firm);
                cmd1.Parameters.AddWithValue("@TAKA_CNT", Int32.Parse(textBox1.Text));
                cmd1.Parameters.AddWithValue("@MTR", Int32.Parse(textBox2.Text));
                cmd1.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@DESPATCH_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd1.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);

                cmd1.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Takas Despatched");
            }
            else
            {
                con.Open();

                SqlCommand cmd1 = new SqlCommand("UPDATE TAKA_DESPATCH SET TAKA_CNT = @TAKA_CNT, MTR = @MTR, QUALITY = @QUALITY, DESPATCH_DATE = @DESPATCH_DATE, GODOWN = @GODOWN WHERE TD_ID = @TD_ID", con);
                cmd1.Parameters.AddWithValue("@TD_ID", tdId);
                cmd1.Parameters.AddWithValue("@TAKA_CNT", Int32.Parse(textBox1.Text));
                cmd1.Parameters.AddWithValue("@MTR", Double.Parse(textBox2.Text));
                cmd1.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@DESPATCH_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd1.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);

                cmd1.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Entry Updated");
            }
        }

        private void TakaDespatch_Load(object sender, EventArgs e)
        {
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

            // set quality

            Dictionary<string, string> qualities = new Dictionary<string, string>();

            query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Cloth' order by TECH_NAME";
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

            if (tdId != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;

                query = "select * from taka_despatch where TD_ID = @TD_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@TD_ID", tdId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox1.Text = oReader["TAKA_CNT"].ToString();
                        textBox2.Text = oReader["MTR"].ToString();

                        comboBox2.SelectedIndex = comboBox2.FindString(godowns[oReader["GODOWN"].ToString()]);
                        comboBox3.SelectedIndex = comboBox3.FindString(qualities[oReader["QUALITY"].ToString()]);

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        dateTimePicker1.Value = DateTime.ParseExact(oReader["DESPATCH_DATE"].ToString().Split(' ')[0], "dd-MMM-yy", ci);
                    }
                }
            }

            con.Close();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaDespatchList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd1 = new SqlCommand("delete from taka_despatch where TD_ID = @TD_ID", con);
            cmd1.Parameters.AddWithValue("@TD_ID", tdId);

            cmd1.ExecuteNonQuery();
            con.Close();

            textBox1.Text = "";
            textBox2.Text = "";

            MessageBox.Show("Despatch undone");
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
