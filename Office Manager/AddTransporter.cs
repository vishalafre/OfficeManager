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
    public partial class AddTransporter : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        int tid = -1;

        string company;
		byte[] lPath;
        public AddTransporter(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
			lPath = logoPath;
            label1.Text = cName;
        }

        public AddTransporter(String cName, byte[] logoPath, int tid)
        {
            InitializeComponent();
            this.tid = tid;
            company = cName;
            lPath = logoPath;
            label1.Text = cName;
        }

        private void button9_Click(object sender, EventArgs e)
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

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
            
        }

        private void button6_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("insert into TRANSPORT (FIRM, T_NAME, TRANS_ID) values(@FIRM, " +
                "@T_NAME, @TRANS_ID)", con);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@T_NAME", textBox1.Text);
            cmd.Parameters.AddWithValue("@TRANS_ID", textBox2.Text);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Transporter Created Successfully");
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

        private void button8_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void AddTransporter_Load(object sender, EventArgs e)
        {
            if (tid != -1)
            {
                button6.Visible = false;
                updateBtn.Visible = true;
                deleteBtn.Visible = true;

                string query = "SELECT * from transport where TID = @TID";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@TID", tid);
                con.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox1.Text = oReader["T_NAME"].ToString();
                        textBox2.Text = oReader["TRANS_ID"].ToString();
                    }
                }
                con.Close();
            }
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("update TRANSPORT set T_NAME = @T_NAME, TRANS_ID = @TRANS_ID WHERE TID = @TID AND FIRM = @FIRM", con);
            cmd.Parameters.AddWithValue("@TID", tid);
            cmd.Parameters.AddWithValue("@T_NAME", textBox1.Text);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@TRANS_ID", textBox2.Text);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Transporter Updated");
            }
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE TID = @TID", con);
            cmd.Parameters.AddWithValue("@TID", tid);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Transporter Deleted Successfully");
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
