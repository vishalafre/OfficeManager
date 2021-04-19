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
    public partial class NewUnit : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;
        string uName;
        int uId = -1;

        public NewUnit()
        {
            InitializeComponent();
        }

        public NewUnit(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public NewUnit(string firm, byte[] logo, string uName)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.uName = uName;
        }

        private void NewUnit_Load(object sender, EventArgs e)
        {
            con.Open();

            if (uName != null)
            {
                button1.Text = "Update";
                button2.Visible = true;

                string query = "select * from unit where unit_name = @UNIT_NAME AND FIRM = @FIRM";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@UNIT_NAME", uName);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        uId = Int32.Parse(oReader["UID"].ToString());
                        textBox1.Text = uName;
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
            con.Open();
            if (uId == -1)
            {
                SqlCommand cmd = new SqlCommand("insert into UNIT (FIRM, UNIT_NAME) values(@FIRM, @UNIT_NAME)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@UNIT_NAME", textBox1.Text);

                cmd.ExecuteNonQuery();
                
            }
            else
            {
                SqlCommand cmd = new SqlCommand("UPDATE UNIT SET UNIT_NAME = @UNIT_NAME WHERE UID = @UID", con);
                cmd.Parameters.AddWithValue("@UID", uId);
                cmd.Parameters.AddWithValue("@UNIT_NAME", textBox1.Text);

                cmd.ExecuteNonQuery();
            }

            con.Close();
            MessageBox.Show("Unit "+ button1.Text +"d Successfully");
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new UnitList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String query = "select pid from product where unit = @UID UNION SELECT PID FROM PRODUCT WHERE MFG_UID = @UID";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@UID", uId);
            con.Open();

            Boolean canDelete = true;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    canDelete = false;
                }
            }

            if (canDelete)
            {
                SqlCommand cmd = new SqlCommand("delete from unit where uid = @UID", con);
                cmd.Parameters.AddWithValue("@UID", uId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Unit Deleted");
                Close();
            }
            else
            {
                MessageBox.Show("Cannot delete : " + uName + "\nRemove the dependencies first");
            }
            con.Close();
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
