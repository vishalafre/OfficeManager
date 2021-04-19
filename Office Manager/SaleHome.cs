using Office_Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class SaleHome : Form
    {
        string company;
        byte[] lPath;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public SaleHome(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
            lPath = logoPath;
            label1.Text = cName;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var addItem = new AddItem(company, lPath);
            addItem.MdiParent = ParentForm;
            addItem.Show();
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();
            
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var invList = new InvList(company, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Close();
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

        private void button8_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
            
        }

        private void SaleHome_Load(object sender, EventArgs e)
        {
            // for customer

            con.Open();
            string query = "SELECT CID, CNAME from customer where FIRM = @FIRM ORDER BY CNAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            int i = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string cName = oReader["CNAME"].ToString();
                    int cid = Int32.Parse(oReader["CID"].ToString());
                    var label = new LinkLabel
                    {
                        Name = "cname" + i,
                        Location = new Point(custHeader.Location.X, 50 + (custHeader.Location.Y + 9)*i),
                        Text = cName,
                        Size = new Size(200, 25)
                    };
                    label.Click += (s, evt) =>
                    {
                        var c = new AddCustomer(company, lPath, cid);
                        c.MdiParent = ParentForm;
                        c.Show();
                        
                    };
                    customer.Controls.Add(label);
                    i++;
                }
            }

            // for transporter

            query = "SELECT tid, t_name from transport where FIRM = @FIRM ORDER BY T_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            i = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string tp = oReader["T_NAME"].ToString();
                    int tid = Int32.Parse(oReader["TID"].ToString());
                    if (!tp.Equals("NA"))
                    {
                        var label = new LinkLabel
                        {
                            Name = "item" + i,
                            Location = new Point(tranHeader.Location.X, 45 + (tranHeader.Location.Y + 9) * i),
                            Text = tp,
                            Size = new Size(200, 25)
                        };
                        label.Click += (s, evt) =>
                        {
                            var c = new AddTransporter(company, lPath, tid);
                            c.MdiParent = ParentForm;
                            c.Show();
                            
                        };
                        transporter.Controls.Add(label);
                        i++;
                    }
                }
            }

            // for agent

            query = "SELECT aid, a_name from agent where FIRM = @FIRM ORDER BY A_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            i = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string agt = oReader["A_NAME"].ToString();
                    int aid = Int32.Parse(oReader["AID"].ToString());
                    if (!agt.Equals("NA"))
                    {
                        var label = new LinkLabel
                        {
                            Name = "agent" + i,
                            Location = new Point(agentHeader.Location.X, 45 + (agentHeader.Location.Y + 9) * i),
                            Text = agt,
                            Size = new Size(200, 25)
                        };
                        label.Click += (s, evt) =>
                        {
                            var c = new AddAgent(company, lPath, aid);
                            c.MdiParent = ParentForm;
                            c.Show();
                            
                        };
                        agent.Controls.Add(label);
                        i++;
                    }
                }
            }

            con.Close();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var targetForm = new TallyConfigure(company);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var invList = new OrderManagement(company);
            invList.MdiParent = ParentForm;
            invList.Show();
        }
    }
}
