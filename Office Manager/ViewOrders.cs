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
    public partial class ViewOrders : Form
    {
        string firm;
        byte[] lPath;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<int, int> indexMap = new Dictionary<int, int>();
        int totalRows;
        int gridHeight;
        int gridWidth;

        Dictionary<string, string> items = new Dictionary<string, string>();
        Dictionary<string, string> customers = new Dictionary<string, string>();
        Dictionary<string, string> agents = new Dictionary<string, string>();

        string statusFilter = " AND O.STATUS = 'P' ";
        string agentFilter = "";
        string productFilter = "";
        string customerFilter = "";

        Boolean loading = true;
        int currentPage;
        string naAgent;

        public ViewOrders(String firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void ViewOrders_Load(object sender, EventArgs e)
        {
            label1.Text = firm;
            comboBox4.SelectedIndex = 0;

            gridHeight = dataGridView1.Height;
            gridWidth = dataGridView1.Width;
            /*
            DataGridViewLinkColumn col = new DataGridViewLinkColumn();
            col.DataPropertyName = "INDEX";
            col.Name = "IND";
            dataGridView1.Columns.Add(col);*/

            setEntities();
            fillData();

            dataGridView1.CellClick += (s, evt) =>
            {
                cellClick(s, evt);
            };

            loading = false;
        }

        private void setEntities()
        {
            // Set customer

            String query = "select CID, CNAME from customer where firm = @FIRM order by CNAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            customers.Add("-1", "All");
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
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            items.Add("-1", "All");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    items.Add(oReader["ITEM_ID"].ToString(), oReader["ITEM_NAME"].ToString());
                }
            }

            if (items.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(items, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            // set agents

            query = "select AID, A_NAME from AGENT where firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            Dictionary<string, string> agents = new Dictionary<string, string>();
            agents.Add("-1", "All");

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    agents.Add(oReader["AID"].ToString(), oReader["A_NAME"].ToString());
                    if (oReader["A_NAME"].ToString().Equals("NA"))
                    {
                        naAgent = oReader["AID"].ToString();
                    }
                }
            }

            if (agents.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(agents, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            con.Close();
        }

        private void fillData()
        {
            indexMap = new Dictionary<int, int>();

            con.Open();
            string query = "select ROW_NUMBER() OVER (ORDER BY ORDER_DATE DESC, ORDER_ID DESC) AS IND, ORDER_ID FROM ORDERS O WHERE FIRM = '" + firm + "'" + statusFilter + agentFilter + productFilter + customerFilter;
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            totalRows = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    totalRows++;
                    indexMap.Add(Int32.Parse(oReader["IND"].ToString()), Int32.Parse(oReader["ORDER_ID"].ToString()));
                }
            }

            Dictionary<int, int> pageNos = new Dictionary<int, int>();
            for (int i = 1; i <= Math.Ceiling((double)totalRows / 10); i++)
            {
                pageNos.Add(i, i);
            }

            if (pageNos.Count() > 0)
            {
                comboBox5.DataSource = new BindingSource(pageNos, null);
                comboBox5.DisplayMember = "Value";
                comboBox5.ValueMember = "Key";
            }

            if(currentPage != 0)
            {
                comboBox5.SelectedIndex = currentPage - 1;
            }

            int startIndex = comboBox5.SelectedIndex * 10 + 1;
            int endIndex = startIndex + 9;

            if (endIndex >= totalRows)
            {
                next.Visible = false;
                endIndex = totalRows;
            }
            else
            {
                next.Visible = true;
            }

            if (startIndex <= 1)
            {
                prev.Visible = false;
                startIndex = 1;
            }
            else
            {
                prev.Visible = true;
            }

            string sql = "SELECT IND \"Index\", ORDER_DATE\" Order Date\", CNAME \"Customer\", ITEM_NAME \"Product\", RATE \"Rate\", isnull(A_NAME, 'NA') \"Agent\", QTY \"Order Qty\", DEL_QTY \"Delivered Qty\", (QTY - DEL_QTY) \"Remaining Qty\", ISNULL(CONVERT(VARCHAR(12), LAST_DEL, 107), 'NA') \"Last Delivered\" FROM ( SELECT ROW_NUMBER() OVER (ORDER BY ORDER_DATE DESC, ORDER_ID DESC) AS IND, O.ORDER_DATE, O.ORDER_ID, C.CNAME, I.ITEM_NAME, O.RATE, (select a_name from agent a where a.aid = o.agent) A_NAME, O.QTY, ISNULL((SELECT SUM(DEL_QTY) FROM ORDER_SUPPLY OS WHERE OS.ORDER_ID = O.ORDER_ID), 0) DEL_QTY, (SELECT MAX(TXN_DATE) FROM ORDER_SUPPLY OS1 WHERE OS1.ORDER_ID = O.ORDER_ID) LAST_DEL FROM ORDERS O, CUSTOMER C, ITEM I WHERE O.CUSTOMER = C.CID AND I.ITEM_ID = O.PRODUCT AND O.FIRM = '"+ firm + "'"+ statusFilter + agentFilter + productFilter + customerFilter + ") T WHERE IND >= " + startIndex + " AND IND <= " + endIndex + " ORDER BY ORDER_DATE DESC, ORDER_ID DESC";
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();

            dataadapter.Fill(ds, "ORDERS");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "ORDERS";

            con.Close();

            SalaryReport.d1H = gridHeight;
            SalaryReport.d1W = gridWidth;

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }

        private void cellClick(object sender, DataGridViewCellEventArgs e)
        {
            int row = e.RowIndex;

            if (dataGridView1.CurrentCell.ColumnIndex.Equals(0) && e.RowIndex != -1)
            {
                if (dataGridView1.CurrentCell != null && dataGridView1.CurrentCell.Value != null)
                {
                    var targetForm = new OrderManagement(firm, indexMap[Int32.Parse(dataGridView1[0, row].Value.ToString())]);
                    targetForm.MdiParent = ParentForm;
                    targetForm.Show();
                }
            }
            else if (dataGridView1.CurrentCell.ColumnIndex.Equals(7) && e.RowIndex != -1)
            {
                var targetForm = new OrderDetails(dataGridView1[2, row].Value.ToString(),
                    dataGridView1[3, row].Value.ToString(),
                    dataGridView1[4, row].Value.ToString(),
                    ((DateTime)dataGridView1[1, row].Value).ToString("dd-MMM-yyyy"),
                    indexMap[Int32.Parse(dataGridView1[0, row].Value.ToString())]);
                targetForm.Show();
            }
        }

        private DataGridViewCellStyle GetHyperLinkStyleForGridCell()
        {
            // Set the Font and Uderline into the Content of the grid cell .  
            {
                DataGridViewCellStyle l_objDGVCS = new DataGridViewCellStyle();
                System.Drawing.Font l_objFont = new System.Drawing.Font(dataGridView1.DefaultCellStyle.Font.FontFamily, 10, FontStyle.Underline);
                l_objDGVCS.Font = l_objFont;
                l_objDGVCS.ForeColor = Color.Blue;
                return l_objDGVCS;
            }
        }

        private void SetHyperLinkOnGrid()
        {
            if (dataGridView1.Columns.Contains("Delivered Qty"))
            {
                dataGridView1.Columns["Delivered Qty"].DefaultCellStyle = GetHyperLinkStyleForGridCell();
            }

            if (dataGridView1.Columns.Contains("Index"))
            {
                dataGridView1.Columns["Index"].DefaultCellStyle = GetHyperLinkStyleForGridCell();
            }
        }

        private void dataGridView1_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            SetHyperLinkOnGrid();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                string customer = ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key;
                if (!customer.Equals("-1"))
                {
                    customerFilter = " AND O.CUSTOMER = " + customer + " ";
                }
                else
                {
                    customerFilter = "";
                }
                fillData();
                loading = false;
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                string product = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key;
                if (!product.Equals("-1"))
                {
                    productFilter = " AND O.PRODUCT = " + product + " ";
                }
                else
                {
                    productFilter = "";
                }
                fillData();
                loading = false;
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!loading)
            {
                loading = true;
                string agent = ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key;
                if (!agent.Equals("-1"))
                {
                    if (agent.Equals(naAgent))
                    {
                        agentFilter = " AND O.AGENT IS NULL ";
                    }
                    else
                    {
                        agentFilter = " AND O.AGENT = " + agent + " ";
                    }
                }
                else
                {
                    agentFilter = "";
                }
                fillData();
                loading = false;
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                if (comboBox4.SelectedItem.ToString().Equals("Pending"))
                {
                    statusFilter = " AND O.STATUS = 'P' ";
                }
                else if (comboBox4.SelectedItem.ToString().Equals("Complete"))
                {
                    statusFilter = " AND O.STATUS = 'C' ";
                }
                fillData();
                loading = false;
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(firm, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(firm, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(firm, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();

        }

        private void button4_Click(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(firm, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();

        }

        private void button5_Click(object sender, EventArgs e)
        {
            var invList = new InvList(firm, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();

        }

        private void button7_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete " + firm + "?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                int i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM AGENT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM firm WHERE NAME = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                i = cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Firm Deleted Successfully!!");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();

            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                currentPage = comboBox5.SelectedIndex + 1;
                fillData();
                currentPage = 0;
                loading = false;
            }
        }

        private void next_Click(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                comboBox5.SelectedIndex++;
                currentPage = comboBox5.SelectedIndex + 1;
                fillData();
                currentPage = 0;
                loading = false;
            }
        }

        private void prev_Click(object sender, EventArgs e)
        {
            if (!loading)
            {
                loading = true;
                comboBox5.SelectedIndex--;
                currentPage = comboBox5.SelectedIndex + 1;
                fillData();
                currentPage = 0;
                loading = false;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(firm, lPath);
            home.MdiParent = ParentForm;
            home.Show();
        }
    }
}
