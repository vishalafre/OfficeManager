using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class OrderManagement : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        byte[] lPath;

        string company;
        int orderID;

        Dictionary<string, string> items = new Dictionary<string, string>();
        Dictionary<string, string> customers = new Dictionary<string, string>();
        Dictionary<string, string> agents = new Dictionary<string, string>();

        Dictionary<string, string> itemUnits = new Dictionary<string, string>();

        string mCustomer;
        string mProduct;
        string mAgent;
        string mRate;
        string mPymtDeadline;

        int txnFlag;    //1: update //2: delete

        public OrderManagement(string company)
        {
            InitializeComponent();
            this.company = company;
        }

        public OrderManagement(string company, int orderID)
        {
            InitializeComponent();
            this.company = company;
            this.orderID = orderID;
        }

        private void OrderManagement_Load(object sender, EventArgs e)
        {
            label1.Text = company;
            comboBox4.SelectedIndex = 0;
            // Set customer

            String query = "select CID, CNAME from customer where firm = @FIRM order by CNAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    customers.Add(oReader["CID"].ToString(), oReader["CNAME"].ToString());
                }
            }

            if (customers.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(customers, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";
            }

            // Set products

            query = "select ITEM_ID, ITEM_NAME, UNIT from item where firm = @FIRM order by ITEM_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    items.Add(oReader["ITEM_ID"].ToString(), oReader["ITEM_NAME"].ToString());
                    itemUnits.Add(oReader["ITEM_ID"].ToString(), oReader["UNIT"].ToString());
                }
            }

            if (items.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(items, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";

                string unit = itemUnits.Values.ToArray()[0].Split('-')[0];
                label8.Text = unit;
            }

            // set agents

            query = "select AID, A_NAME from AGENT where firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            Dictionary<string, string> agents = new Dictionary<string, string>();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    agents.Add(oReader["AID"].ToString(), oReader["A_NAME"].ToString());
                }
            }

            if (agents.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(agents, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            if(orderID != 0)
            {
                button6.Visible = false;
                button11.Visible = true;
                button12.Visible = true;

                String query1 = "select * from orders where order_id = @ORDER_ID";
                SqlCommand oCmd1 = new SqlCommand(query1, con);
                oCmd1.Parameters.AddWithValue("@ORDER_ID", orderID);

                using (SqlDataReader oReader = oCmd1.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        string orderDt = oReader["ORDER_DATE"].ToString();
                        mCustomer = oReader["CUSTOMER"].ToString();
                        mProduct = oReader["PRODUCT"].ToString();
                        string quantity = oReader["QTY"].ToString();
                        mRate = oReader["RATE"].ToString();
                        mPymtDeadline = oReader["PYMT_DEADLINE"].ToString();
                        mAgent = oReader["AGENT"].ToString();
                        string commType = oReader["COMMISSION_TYPE"].ToString();
                        string commAmt = oReader["COMMISSION_AMT"].ToString();
                        string notes = oReader["NOTES"].ToString();
                        string status = oReader["STATUS"].ToString();
                        string discount = oReader["DISCOUNT"].ToString();

                        if (status.Equals("P"))
                        {
                            button13.Visible = true;
                        }

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                        dateTimePicker1.Value = DateTime.ParseExact(orderDt.Split(' ')[0], sysFormat, ci);

                        comboBox1.SelectedIndex = comboBox1.FindString(customers[mCustomer]);
                        comboBox2.SelectedIndex = comboBox2.FindString(items[mProduct]);

                        switch(commType)
                        {
                            case "NA":
                                comboBox4.SelectedIndex = 0;
                                break;

                            case "Percent":
                                comboBox4.SelectedIndex = 1;
                                break;

                            case "Meter":
                                comboBox4.SelectedIndex = 2;
                                break;
                        }

                        if(textBox4.Visible)
                        {
                            textBox4.Text = commAmt;
                        }

                        if (agents.ContainsKey(mAgent))
                        {
                            comboBox3.SelectedIndex = comboBox3.FindString(agents[mAgent]);
                        }
                        else
                        {
                            comboBox3.SelectedIndex = comboBox3.FindString("NA");
                        }

                        textBox5.Text = quantity;
                        textBox3.Text = mRate;
                        textBox1.Text = mPymtDeadline;
                        textBox2.Text = notes;
                        this.discount.Text = discount;
                    }
                }
            }

            con.Close();
        }

        private void saveOrder()
        {
            string deadline = "0";
            string agent = "";
            string commissionAmt = "0";

            if (!comboBox4.SelectedItem.ToString().Equals("NA"))
            {
                commissionAmt = textBox4.Text;
            }

            if (!textBox1.Text.Equals(""))
            {
                deadline = textBox1.Text;
            }

            string agt = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Value;
            if (!agt.Equals("0"))
            {
                agent = agt;
            }

            string URI = "http://www.afrestudios.com/office-manager/insert_order.php";

            string response = "";
            using (WebClient client = new WebClient())
            {
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                reqparm.Add("firm", company);
                reqparm.Add("orderDt", dateTimePicker1.Value.ToString("yyyy-MM-dd"));
                reqparm.Add("customer", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Value);
                reqparm.Add("product", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Value);
                reqparm.Add("qty", textBox5.Text);
                reqparm.Add("unit", label8.Text);
                reqparm.Add("rate", textBox3.Text);
                reqparm.Add("deadline", deadline);
                reqparm.Add("agent", comboBox3.SelectedItem.ToString());
                reqparm.Add("commType", comboBox4.SelectedItem.ToString());
                reqparm.Add("commAmount", commissionAmt);
                reqparm.Add("notes", textBox2.Text);

                try
                {
                    byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                    response = Encoding.UTF8.GetString(responsebytes);
                }
                catch
                {
                    button6.Enabled = true;
                    button11.Enabled = true;
                    button12.Enabled = true;
                    button13.Enabled = true;

                    button6.Text = "Save";
                    button11.Text = "Update";
                    button12.Text = "Delete";
                    button13.Text = "Invoice";
                    //MessageBox.Show("No connection to network");
                    //return;
                }
            }

            if(true)  //response.Equals("SUCCESS")
            {
                deadline = "null";
                agent = "null";
                commissionAmt = "null";

                double disc = 0;
                string discTxt = discount.Text;
                if (!discTxt.Equals(""))
                {
                    try
                    {
                        disc = Double.Parse(discTxt);
                    }
                    catch
                    {
                        MessageBox.Show("Please enter a valid discount value");
                    }
                }

                if (!comboBox4.SelectedItem.ToString().Equals("NA"))
                {
                    commissionAmt = textBox4.Text;
                }

                if (!textBox1.Text.Equals(""))
                {
                    deadline = textBox1.Text;
                }

                agt = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Value;
                if (!agt.Equals("NA"))
                {
                    agent = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
                }

                con.Open();
                SqlCommand cmd1 = new SqlCommand("INSERT INTO ORDERS (FIRM, ORDER_DATE, CUSTOMER, PRODUCT, QTY, RATE, DISCOUNT, PYMT_DEADLINE, AGENT, COMMISSION_TYPE, COMMISSION_AMT, NOTES, STATUS) VALUES (@FIRM, @ORDER_DATE, @CUSTOMER, @PRODUCT, @QTY, @RATE, @DISCOUNT, " + deadline + ", " + agent + ", @COMMISSION_TYPE, " + commissionAmt + ", @NOTES, 'P')", con);
                cmd1.Parameters.AddWithValue("@FIRM", company);
                cmd1.Parameters.AddWithValue("@ORDER_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd1.Parameters.AddWithValue("@CUSTOMER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@QTY", textBox5.Text);
                cmd1.Parameters.AddWithValue("@RATE", textBox3.Text);
                cmd1.Parameters.AddWithValue("@DISCOUNT", discount.Text);
                cmd1.Parameters.AddWithValue("@COMMISSION_TYPE", comboBox4.SelectedItem.ToString());
                cmd1.Parameters.AddWithValue("@NOTES", textBox2.Text);

                cmd1.ExecuteNonQuery();
            }
            else
            {
                //MessageBox.Show("Error connecting to network");
                con.Close();
                //return;
            }

            response = "";
            agent = "(select aid from agent where a_name = 'NA' and firm = '" + company + "')";
            agt = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
            if (!agt.Equals("0"))
            {
                agent = agt;
            }

            double meters = 0;

            String query = "select (select max(order_id) from orders) order_id, b.bill_dt, sum(bi.mtr) qty, b.bill_id " +
                "from bill b, bill_item bi where b.BILL_ID = bi.BILL_ID and b.BILL_TO = @CUSTOMER " +
                "and b.AGENT = " + agent + " and bi.ITEM = @PRODUCT and bi.rate = @RATE and b.BILL_DT >= @ORDER_DATE and bi.ORDER_ID = 0 " +
                "and b.bill_id not in (select bill_id from order_supply)" +
                "group by b.bill_dt, b.BILL_ID order by b.bill_dt, b.bill_id";
            SqlCommand cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@CUSTOMER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@RATE", textBox3.Text);
            cmd.Parameters.AddWithValue("@ORDER_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            using (SqlDataReader oReader = cmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string newOrderID = oReader["order_id"].ToString();
                    string billDt = ((DateTime)oReader["bill_dt"]).ToString("dd-MMM-yyyy");
                    string billDtServer = ((DateTime)oReader["bill_dt"]).ToString("yyyy-MM-dd");
                    double qty = Double.Parse(oReader["qty"].ToString());
                    string billId = oReader["bill_id"].ToString();

                    URI = "http://www.afrestudios.com/office-manager/insert_order_supply.php";

                    using (WebClient client = new WebClient())
                    {
                        var reqparm = new System.Collections.Specialized.NameValueCollection();
                        reqparm.Add("orderId", newOrderID);
                        reqparm.Add("txnDt", billDtServer);
                        reqparm.Add("delQty", qty + "");
                        reqparm.Add("billId", billId);

                        try
                        {
                            byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                            response = Encoding.UTF8.GetString(responsebytes);
                        }
                        catch
                        {
                            con.Close();
                            con1.Close();

                            button6.Enabled = true;
                            button11.Enabled = true;
                            button12.Enabled = true;
                            button13.Enabled = true;

                            button6.Text = "Save";
                            button11.Text = "Update";
                            button12.Text = "Delete";
                            button13.Text = "Invoice";
                            MessageBox.Show("No connection to network");
                            return;
                        }
                    }

                    if (true)   // response.Equals("SUCCESS")
                    {
                        SqlCommand cmd2 = new SqlCommand("insert into order_supply (order_id, txn_date, del_qty, bill_id) values (@ORDER_ID, @TXN_DATE, @DEL_QTY, @BILL_ID)", con1);
                        cmd2.Parameters.AddWithValue("@ORDER_ID", newOrderID);
                        cmd2.Parameters.AddWithValue("@TXN_DATE", billDt);
                        cmd2.Parameters.AddWithValue("@DEL_QTY", qty);
                        cmd2.Parameters.AddWithValue("@BILL_ID", billId);
                        cmd2.ExecuteNonQuery();
                    }
                    else
                    {
                        //MessageBox.Show("Error connecting to network");
                        con1.Close();
                        con.Close();
                        //return;
                    }

                    // UPDATE BILL ITEM WITH ORDER ID

                    SqlCommand cmd1 = new SqlCommand("UPDATE BILL_ITEM SET ORDER_ID = @ORDER_ID WHERE BILL_ID = @BILL_ID AND ITEM = @PRODUCT AND RATE = @RATE", con1);
                    cmd1.Parameters.AddWithValue("@ORDER_ID", newOrderID);
                    cmd1.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                    cmd1.Parameters.AddWithValue("@RATE", textBox3.Text);
                    cmd1.Parameters.AddWithValue("@BILL_ID", billId);
                    cmd1.ExecuteNonQuery();

                    meters += qty;

                    response = "";
                    if (meters >= Double.Parse(textBox5.Text))
                    {
                        URI = "http://www.afrestudios.com/office-manager/mark_order_confirm.php";

                        using (WebClient client = new WebClient())
                        {
                            var reqparm = new System.Collections.Specialized.NameValueCollection();
                            reqparm.Add("orderId", newOrderID);

                            try
                            {
                                byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                                response = Encoding.UTF8.GetString(responsebytes);
                            }
                            catch
                            {
                                con.Close();
                                con1.Close();

                                button6.Enabled = true;
                                button11.Enabled = true;
                                button12.Enabled = true;
                                button13.Enabled = true;

                                button6.Text = "Save";
                                button11.Text = "Update";
                                button12.Text = "Delete";
                                button13.Text = "Invoice";
                                MessageBox.Show("No connection to network");
                                return;
                            }
                        }

                        if (true) //response.Equals("SUCCESS")
                        {
                            cmd1 = new SqlCommand("UPDATE ORDERS SET STATUS = 'C' WHERE ORDER_ID = @ORDER_ID", con1);
                            cmd1.Parameters.AddWithValue("@ORDER_ID", newOrderID);
                            cmd1.ExecuteNonQuery();
                            break;
                        }
                        else
                        {
                            //MessageBox.Show("Error connecting to network");
                            con1.Close();
                            con.Close();
                            //return;
                        }
                    }
                }
            }

            if (txnFlag == 1)
            {
                MessageBox.Show("Order updated");
            }
            else
            {
                MessageBox.Show("Order created Successfully");
            }
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            button6.Text = "Saving";
            button6.Enabled = false;
            saveOrder();
            button6.Enabled = true;
            button6.Text = "Save";
        }

        private void comboBox2_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            string pid = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key;
            string unit = itemUnits[pid].Split('-')[0];
            label8.Text = unit;
        }

        private void comboBox4_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            if (comboBox4.SelectedItem.ToString().Equals("Percent"))
            {
                panel1.Visible = true;
                label14.Text = "%";
            }
            else if (comboBox4.SelectedItem.ToString().Equals("Meter"))
            {
                panel1.Visible = true;
                label14.Text = "paise";
            }
            else if (comboBox4.SelectedItem.ToString().Equals("NA"))
            {
                panel1.Visible = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var invList = new ViewOrders(company);
            invList.MdiParent = ParentForm;
            invList.Show();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            var invList = new AddInvoice(company, mCustomer, mProduct, mAgent, mRate, mPymtDeadline, discount.Text);
            invList.MdiParent = ParentForm;
            invList.Show();
        }

        private Boolean deleteOrder()
        {
            con.Open();

            string URI = "http://www.afrestudios.com/office-manager/delete_order.php";

            string response = "";
            using (WebClient client = new WebClient())
            {
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                reqparm.Add("orderId", orderID + "");

                try
                {
                    byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                    response = Encoding.UTF8.GetString(responsebytes);
                }
                catch
                {
                    con.Close();

                    button6.Enabled = true;
                    button11.Enabled = true;
                    button12.Enabled = true;
                    button13.Enabled = true;

                    button6.Text = "Save";
                    button11.Text = "Update";
                    button12.Text = "Delete";
                    button13.Text = "Invoice";
                    MessageBox.Show("No connection to network");
                    return false;
                }
            }

            if (true) // response.Equals("SUCCESS")
            {
                SqlCommand cmd = new SqlCommand("DELETE FROM ORDER_SUPPLY WHERE ORDER_ID = @ORDER_ID", con);
                cmd.Parameters.AddWithValue("@ORDER_ID", orderID);

                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ORDERS WHERE ORDER_ID = @ORDER_ID", con);
                cmd.Parameters.AddWithValue("@ORDER_ID", orderID);

                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("UPDATE BILL_ITEM SET ORDER_ID = 0 WHERE ORDER_ID = @ORDER_ID", con);
                cmd.Parameters.AddWithValue("@ORDER_ID", orderID);

                int i = cmd.ExecuteNonQuery();
            }
            else
            {
                MessageBox.Show("Error connecting to network");
            }

            con.Close();

            if(txnFlag == 2)
            {
                MessageBox.Show("Order Deleted");
            }

            return true;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            txnFlag = 2;
            button12.Text = "Deleting";
            button12.Enabled = false;
            button11.Enabled = false;
            button13.Enabled = false;

            if (!deleteOrder())
            {
                button12.Text = "Delete";
                button12.Enabled = true;
                button11.Enabled = true;
                button13.Enabled = true;

                return;
            }

            comboBox1.SelectedIndex = 0;
            comboBox2.SelectedIndex = 0;
            comboBox3.SelectedIndex = 0;
            comboBox4.SelectedIndex = 0;

            textBox1.Text = "";
            textBox2.Text = "";
            textBox3.Text = "";
            textBox4.Text = "";
            textBox5.Text = "";

            button12.Text = "Delete";
            button12.Enabled = true;
            button11.Enabled = true;
            button13.Enabled = true;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            txnFlag = 1;
            button11.Text = "Updating";
            button12.Enabled = false;
            button11.Enabled = false;
            button13.Enabled = false;

            if(!deleteOrder())
            {
                return;
            }
            saveOrder();

            button11.Text = "Update";
            button12.Enabled = true;
            button11.Enabled = true;
            button13.Enabled = true;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
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

        private void button7_Click(object sender, EventArgs e)
        {
            var concompanyResult = MessageBox.Show("Are you sure you want to delete " + company + "?",
                                     "Concompany Delete",
                                     MessageBoxButtons.YesNo);
            if (concompanyResult == DialogResult.Yes)
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                int i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ITEM WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM AGENT WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL WHERE company = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM company WHERE NAME = @company", con);
                cmd.Parameters.AddWithValue("@company", company);
                i = cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Firm Deleted Successfully!!");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();

            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
        }
    }
}
