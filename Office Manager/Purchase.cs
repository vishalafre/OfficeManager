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
    public partial class Purchase : Form
    {
        private string firm;
        private byte[] logo;
        private int entryId = -1;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> products = new Dictionary<string, string>();
        Dictionary<string, string> godowns = new Dictionary<string, string>();

        public Purchase()
        {
            InitializeComponent();
        }

        public Purchase(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public Purchase(string firm, byte[] logo, int entryId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.entryId = entryId;
        }

        private void Purchase_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;
            con.Open();
            // set product

            String query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Yarn' order by TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    products.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (products.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(products, null);
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
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godowns.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(godowns, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            if(entryId != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;

                SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con.Open();

                query = "select * from purchase where ENTRY_ID = @ENTRY_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@ENTRY_ID", entryId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox2.Text = oReader["BILL_AMT"].ToString();
                        textBox1.Text = oReader["BOXES"].ToString();
                        textBox5.Text = oReader["QTY"].ToString();
                        textBox3.Text = oReader["FREIGHT"].ToString();

                        comboBox2.SelectedIndex = comboBox2.FindString(godowns[oReader["GODOWN"].ToString()]);
                        comboBox3.SelectedIndex = comboBox3.FindString(products[oReader["PRODUCT"].ToString()]);

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

                SqlCommand cmd = new SqlCommand("insert into PURCHASE (FIRM, TXN_DATE, PRODUCT, QTY, GODOWN, BOXES, FREIGHT, BILL_AMT) " +
                    "values(@FIRM, @TXN_DATE, @PRODUCT, @QTY, @GODOWN, @BOXES, @FREIGHT, @BILL_AMT)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@QTY", textBox5.Text);
                cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@BOXES", textBox1.Text);
                cmd.Parameters.AddWithValue("@FREIGHT", textBox3.Text);
                cmd.Parameters.AddWithValue("@BILL_AMT", textBox2.Text);

                cmd.ExecuteNonQuery();

                con.Close();
                MessageBox.Show("Product purchased successfully");
            }
            else
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("UPDATE PURCHASE SET TXN_DATE = @TXN_DATE, PRODUCT = @PRODUCT, QTY = @QTY, FREIGHT = @FREIGHT, GODOWN = @GODOWN, BOXES = @BOXES, BILL_AMT = @BILL_AMT WHERE ENTRY_ID = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
                cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@QTY", textBox5.Text);
                cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd.Parameters.AddWithValue("@BOXES", textBox1.Text);
                cmd.Parameters.AddWithValue("@FREIGHT", textBox3.Text);
                cmd.Parameters.AddWithValue("@BILL_AMT", textBox2.Text);

                cmd.ExecuteNonQuery();

                con.Close();
                MessageBox.Show("Purchase updated");
            }
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new PurchaseList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("DELETE FROM PURCHASE WHERE ENTRY_ID = @ENTRY_ID", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", entryId);
            cmd.ExecuteNonQuery();

            con.Close();

            textBox1.Text = "";
            textBox2.Text = "";
            textBox5.Text = "";
            MessageBox.Show("Purchase Cancelled");
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
