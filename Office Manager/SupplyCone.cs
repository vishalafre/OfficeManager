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
    public partial class SupplyCone : Form
    {
        private string firm;
        private byte[] logo;
        private int entryId = -1;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> beams = new Dictionary<string, string>();
        Dictionary<string, string> entities = new Dictionary<string, string>();

        public SupplyCone()
        {
            InitializeComponent();
        }

        public SupplyCone(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public SupplyCone(string firm, byte[] logo, int entryId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.entryId = entryId;
        }

        private void SupplyCone_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;
            con.Open();
            // set quality

            String query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Yarn' order by TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    beams.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (beams.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(beams, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            // set godown

            query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    entities.Add("G" + oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            // set weaver

            query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    entities.Add("W" + oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            if (entities.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(entities, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";

                comboBox2.DataSource = new BindingSource(entities, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            if(entryId != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;

                SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con.Open();

                query = "select * from supply_CONE where ENTRY_ID = @ENTRY_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox1.Text = oReader["QTY"].ToString();
                        textBox2.Text = oReader["BOXES"].ToString();

                        string supplyToType = oReader["SUPPLY_TO_TYPE"].ToString();
                        string supplyFromType = oReader["SUPPLY_FROM_TYPE"].ToString();

                        comboBox1.SelectedIndex = comboBox1.FindString(entities[supplyFromType + oReader["SUPPLY_FROM"].ToString()]);
                        comboBox2.SelectedIndex = comboBox2.FindString(entities[supplyToType + oReader["SUPPLY_TO"].ToString()]);
                        comboBox3.SelectedIndex = comboBox3.FindString(beams[oReader["YARN"].ToString()]);

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        dateTimePicker1.Value = DateTime.ParseExact(oReader["TXN_DATE"].ToString().Split(' ')[0], CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, ci);
                    }
                }
            }

            con.Close();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (entryId == -1)
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, BOXES, SUPPLY_TO, " +
                    "SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE) values(@FIRM, @TXN_DATE, @YARN, " +
                        "@QTY, @BOXES, @SUPPLY_TO, @SUPPLY_TO_TYPE, @SUPPLY_FROM, @SUPPLY_FROM_TYPE)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@YARN", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@QTY", textBox1.Text);
                cmd.Parameters.AddWithValue("@BOXES", textBox2.Text);
                cmd.Parameters.AddWithValue("@SUPPLY_TO", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(1));
                cmd.Parameters.AddWithValue("@SUPPLY_TO_TYPE", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(0, 1));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM_TYPE", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(0, 1));

                cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Cone Supplied Successfully");
            }
            else
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("UPDATE SUPPLY_CONE SET TXN_DATE = @TXN_DATE, YARN = @YARN, QTY = @QTY, BOXES = @BOXES, SUPPLY_TO = @SUPPLY_TO, SUPPLY_TO_TYPE = @SUPPLY_TO_TYPE, SUPPLY_FROM = @SUPPLY_FROM, SUPPLY_FROM_TYPE = @SUPPLY_FROM_TYPE WHERE ENTRY_ID = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@YARN", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@QTY", textBox1.Text);
                cmd.Parameters.AddWithValue("@BOXES", textBox2.Text);
                cmd.Parameters.AddWithValue("@SUPPLY_TO", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(1));
                cmd.Parameters.AddWithValue("@SUPPLY_TO_TYPE", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(0, 1));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
                cmd.Parameters.AddWithValue("@SUPPLY_FROM_TYPE", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(0, 1));

                cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Entry Updated");
            }
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyConeList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE ENTRY_ID = @ENTRY_ID", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);

            cmd.ExecuteNonQuery();
            con.Close();

            textBox1.Text = "";
            textBox2.Text = "";

            MessageBox.Show("Tranaction Cancelled");
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
