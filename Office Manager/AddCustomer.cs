using Office_Manager;
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
    public partial class AddCustomer : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        int cid = -1;
        string company;
		byte[] lPath;
        public AddCustomer(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
			lPath = logoPath;
            label1.Text = cName;
        }

        public AddCustomer(String cName, byte[] logoPath, int cid)
        {
            InitializeComponent();
            this.cid = cid;
            company = cName;
            lPath = logoPath;
            label1.Text = cName;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string dist = "null";
            int n;
            if(!distance.Text.Equals("") && int.TryParse(distance.Text, out n))
            {
                dist = distance.Text;
            }
            con.Open();
            SqlCommand cmd = new SqlCommand("insert into CUSTOMER (FIRM, CNAME, GSTIN, ADDRESS, CITY, DISTANCE, TALLY_LEDGER) values(@FIRM, @CNAME, " +
                "@GSTIN, @ADDRESS, @CITY, "+ dist + ", @TALLY_LEDGER)", con);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@CNAME", textBox1.Text);
            cmd.Parameters.AddWithValue("@GSTIN", textBox2.Text);
            cmd.Parameters.AddWithValue("@CITY", city.Text);
            cmd.Parameters.AddWithValue("@TALLY_LEDGER", textBox4.Text);

            String address = "";
            if(pin.Text.Equals(""))
            {
                if (textBox3.Text == "")
                {
                    address = city.Text;
                }
                else
                {
                    address = textBox3.Text + ", " + city.Text;
                }
            } else
            {
                if (textBox3.Text == "")
                {
                    address = city.Text + " - " + pin.Text;
                }
                else
                {
                    address = textBox3.Text + ", " + city.Text + " - " + pin.Text;
                }
            }

            cmd.Parameters.AddWithValue("@ADDRESS", address);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Customer Created Successfully");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var home = new Home();
            home.MdiParent = ParentForm;
            home.Show();
            
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete " + company + "?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                int i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM AGENT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM COMPANY WHERE NAME = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Firm Deleted Successfully!!");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();
                
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var addItem = new AddItem(company, lPath);
            addItem.MdiParent = ParentForm;
            addItem.Show();
            
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();
            
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            var invList = new InvList(company, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
            
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void AddCustomer_Load(object sender, EventArgs e)
        {
            if(cid != -1)
            {
                button6.Visible = false;
                updateBtn.Visible = true;
                deleteBtn.Visible = true;

                string query = "SELECT * from customer where CID = @CID";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@CID", cid);
                con.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox1.Text = oReader["CNAME"].ToString();
                        textBox2.Text = oReader["GSTIN"].ToString();
                        city.Text = oReader["CITY"].ToString();
                        distance.Text = oReader["DISTANCE"].ToString();
                        textBox4.Text = oReader["TALLY_LEDGER"].ToString();

                        string addr = oReader["ADDRESS"].ToString();
                        string[] addrParts = addr.Split(',');
                        string cityInfo = addrParts[addrParts.Length - 1];
                        if (cityInfo.Contains("-"))
                        {
                            pin.Text = addrParts[addrParts.Length - 1].Split('-')[1].Trim();
                        }
                        if (addr.Contains(","))
                        {
                            textBox3.Text = addr.Replace("," + cityInfo, "").Trim();
                        }
                    }
                }
                con.Close();
            }
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            string dist = "null";
            int n;
            if (!distance.Text.Equals("") && int.TryParse(distance.Text, out n))
            {
                dist = distance.Text;
            }

            con.Open();
            SqlCommand cmd = new SqlCommand("update CUSTOMER set CNAME = @CNAME, GSTIN = @GSTIN, ADDRESS = @ADDRESS, CITY = @CITY, DISTANCE = "+ dist + ", TALLY_LEDGER = @TALLY_LEDGER WHERE CID = @CID AND FIRM = @FIRM", con);
            cmd.Parameters.AddWithValue("@CID", cid);
            cmd.Parameters.AddWithValue("@CNAME", textBox1.Text);
            cmd.Parameters.AddWithValue("@GSTIN", textBox2.Text);
            cmd.Parameters.AddWithValue("@CITY", city.Text);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@TALLY_LEDGER", textBox4.Text);

            String address = "";
            if (pin.Text.Equals(""))
            {
                if (textBox3.Text == "")
                {
                    address = city.Text;
                }
                else
                {
                    address = textBox3.Text + ", " + city.Text;
                }
            }
            else
            {
                if (textBox3.Text == "")
                {
                    address = city.Text + " - " + pin.Text;
                }
                else
                {
                    address = textBox3.Text + ", " + city.Text + " - " + pin.Text;
                }
            }

            cmd.Parameters.AddWithValue("@ADDRESS", address);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Customer Updated");
            }
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE CID = @CID", con);
            cmd.Parameters.AddWithValue("@CID", cid);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Customer Deleted Successfully");
            }

            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
            
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
            
        }
    }
}
