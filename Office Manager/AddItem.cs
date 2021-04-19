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
    public partial class AddItem : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        SqlConnection conParm;
        int pId;
        Boolean onloadCalled = false;

        string company;
		byte[] lPath;
        SqlCommand cmd;
        List<SqlCommand> cmdList;
        SqlCommand delCmd;
        String itemName;

        public AddItem(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
			lPath = logoPath;
        }

        public AddItem(String cName, byte[] logoPath, int pId)
        {
            InitializeComponent();
            this.pId = pId;
            company = cName;
            lPath = logoPath;
        }

        public AddItem(String cName, byte[] logoPath, int pId, SqlCommand cmd, List<SqlCommand> cmdList, SqlCommand delCmd, String itemName, SqlConnection conParm)
        {
            InitializeComponent();
            this.pId = pId;
            company = cName;
            lPath = logoPath;
            this.cmd = cmd;
            this.cmdList = cmdList;
            this.delCmd = delCmd;
            this.itemName = itemName;
            this.conParm = conParm;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
            Close();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();
            Close();
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            var invList = new InvList(company, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();
            Close();
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            Close();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var home = new Home();
            home.MdiParent = ParentForm;
            home.Show();
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
                Close();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
            Close();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();
            Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            var invList = new InvList(company, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();
            Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            Close();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
            Close();
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            if (pId != -1)
            {
                var home = new CompanyHome(company, lPath);
                home.MdiParent = ParentForm;
                home.Show();
                Close();
            }
            else
            {
                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();
                Close();
            }
        }

        private void AddItem_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            onloadCalled = true;
            AcceptButton = button6;

            if (pId != -1)
            {
                button6.Text = "Update";

                string query = "SELECT * from item where pid_pk = @PID";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@PID", pId);
                con.Open();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        textBox2.Text = oReader["HSN"].ToString();
                        textBox3.Text = oReader["RATE"].ToString();
                        textBox4.Text = oReader["DESCR"].ToString();
                        comboBox1.SelectedIndex = comboBox1.FindString(oReader["UNIT"].ToString());
                        textBox1.Text = oReader["TALLY_LEDGER"].ToString();
                        textBox5.Text = oReader["TALLY_UNIT"].ToString();
                    }
                }
                con.Close();
            }
            onloadCalled = false;
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            
        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM ITEM WHERE PID_PK = @ITEM_ID", con);
            cmd.Parameters.AddWithValue("@ITEM_ID", pId);
            int i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Item Deleted Successfully");
            }

            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
            Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
            Close();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox4.Text = comboBox2.Text;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            if (delCmd != null)
            {
                delCmd.ExecuteNonQuery();
            }

            if (cmd != null)
            {
                cmd.ExecuteNonQuery();
            }

            if (cmdList.Count > 0)
            {
                foreach (SqlCommand command in cmdList)
                {
                    command.ExecuteNonQuery();
                }
            }
            conParm.Close();

            con.Open();
            if (pId == -1)
            {
                SqlCommand cmd = new SqlCommand("insert into item (FIRM, ITEM_NAME, HSN, RATE, DESCR, UNIT, TALLY_LEDGER, TALLY_UNIT, PID_PK) values(@FIRM, " +
                    "@ITEM_NAME, @HSN, @RATE, @DESCR, @UNIT, @TALLY_LEDGER, @TALLY_UNIT, (select max(pid) from product))", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                cmd.Parameters.AddWithValue("@ITEM_NAME", itemName);
                cmd.Parameters.AddWithValue("@HSN", textBox2.Text);
                cmd.Parameters.AddWithValue("@DESCR", textBox4.Text);
                cmd.Parameters.AddWithValue("@UNIT", comboBox1.Text);
                cmd.Parameters.AddWithValue("@PID_PK", pId);
                cmd.Parameters.AddWithValue("@TALLY_LEDGER", textBox1.Text);
                cmd.Parameters.AddWithValue("@TALLY_UNIT", textBox5.Text);

                double n;
                if (Double.TryParse(textBox3.Text, out n))
                {
                    cmd.Parameters.AddWithValue("@RATE", textBox3.Text);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@RATE", "");
                }
                int i = cmd.ExecuteNonQuery();

                MessageBox.Show("Item Created Successfully");
            }
            else
            {
                SqlCommand cmd = new SqlCommand("update ITEM set ITEM_NAME = @ITEM_NAME, TALLY_UNIT = @TALLY_UNIT, HSN = @HSN, PID_PK = @PID_PK, TALLY_LEDGER = @TALLY_LEDGER, RATE = @RATE, DESCR = @DESCR, UNIT = @UNIT WHERE PID_PK = @ITEM_ID AND FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@ITEM_ID", pId);
                cmd.Parameters.AddWithValue("@ITEM_NAME", itemName);
                cmd.Parameters.AddWithValue("@HSN", textBox2.Text);
                cmd.Parameters.AddWithValue("@RATE", textBox3.Text);
                cmd.Parameters.AddWithValue("@FIRM", company);
                cmd.Parameters.AddWithValue("@DESCR", textBox4.Text);
                cmd.Parameters.AddWithValue("@UNIT", comboBox1.Text);
                cmd.Parameters.AddWithValue("@PID_PK", pId);
                cmd.Parameters.AddWithValue("@TALLY_LEDGER", textBox1.Text);
                cmd.Parameters.AddWithValue("@TALLY_UNIT", textBox5.Text);

                int i = cmd.ExecuteNonQuery();

                MessageBox.Show("Item Updated");
            }

            con.Close();
            Close();
        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
            if (!onloadCalled)
            {
                String query = "select distinct DESCR from ITEM where HSN = @HSN and firm = @FIRM";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@HSN", textBox2.Text);
                oCmd.Parameters.AddWithValue("@FIRM", company);
                con.Open();

                var desc = new List<String>();
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        desc.Add(oReader["DESCR"].ToString());
                    }
                }
                con.Close();

                if (desc.Count() > 0)
                {
                    comboBox2.DisplayMember = "Key";
                    comboBox2.ValueMember = "Value";
                    comboBox2.DataSource = desc;
                    comboBox2.Visible = true;
                    textBox4.Visible = false;
                }
                else
                {
                    comboBox2.Visible = false;
                    textBox4.Visible = true;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            textBox4.Text = comboBox2.Text;
        }
    }
}
