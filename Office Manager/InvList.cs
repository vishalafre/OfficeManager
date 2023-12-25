using NPOI.XSSF.UserModel;
using Office_Manager;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Collections.ObjectModel;
using OpenQA.Selenium.Interactions;
using System.Threading;
using System.Drawing.Printing;

namespace Office_Manager
{
    public partial class InvList : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Boolean custom = false;
        Boolean loading = true;

        string agentFilter = "";
        string transporterFilter = "";

        string company;
		byte[] lPath;

        private bool exitClicked;
        private bool fakeCaptchaSubmitted;

        public static Dictionary<string, string> eWayBillIds = new Dictionary<string, string>();
        public InvList(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
			lPath = logoPath;
            label1.Text = cName;
        }
        Boolean oneTimeFlag = false;

        private void button1_Click_1(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var addItem = new AddItem(company, lPath);
            addItem.MdiParent = ParentForm;
            addItem.Show();
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();
            
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var home = new Home();
            home.MdiParent = ParentForm;
            home.Show();
            
        }

        private void InvList_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;

            SalaryReport.d1H = dataGridView1.Height;
            SalaryReport.d1W = dataGridView1.Width;

            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm and b.bill_dt between (getDate() - 7) and getDate() GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";

            populateList(query);

            dataGridView1.CellClick += (s, evt) =>
            {
                cellClick(s, evt);
            };

            loading = false;
        }

        private void cellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.Columns[0].Name.Equals("BILL_ID"))
            {
                if (dataGridView1.CurrentCell.ColumnIndex.Equals(0) && e.RowIndex != -1)
                {
                    if (dataGridView1.CurrentCell != null && dataGridView1.CurrentCell.Value != null)
                    {
                        var targetForm = new AddInvoice(company, lPath, dataGridView1.CurrentCell.Value.ToString());
                        targetForm.MdiParent = ParentForm;
                        targetForm.Show();
                    }
                }
            }
            else if (comboBox1.SelectedItem.ToString().Equals("Agent"))
            {
                oneTimeFlag = false;
                string agent = dataGridView1.CurrentCell.Value.ToString().ToUpper();
                agentFilter = " and upper(a_name) = '"+ agent +"'";
                searchByBillNo();
            }
            else if (comboBox1.SelectedItem.ToString().Equals("Transporter"))
            {
                oneTimeFlag = false;
                string transporter = dataGridView1.CurrentCell.Value.ToString().ToUpper();
                transporterFilter = " and upper(t_name) = '" + transporter + "'";
                searchByBillNo();
            }
        }

        private void searchByBillNo()
        {
            if (radioButton3.Checked)
            {
                customSearch();
            }
            else
            {
                string dateFilter = "";

                if (radioButton1.Checked)
                {
                    dateFilter = "and b.bill_dt between (getDate() - 7) and getDate()";
                }
                else if (radioButton4.Checked)
                {
                    String currentMonth = DateTime.Now.ToString("MMM");
                    int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                    int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                    String startDt = "01-" + currentMonth + "-" + year;
                    String endDt = lastDay + "-" + currentMonth + "-" + year;

                    dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
                }
                else if (radioButton5.Checked)
                {
                    DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                    String currentMonth = previousMonthDt.ToString("MMM");
                    int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                    int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                    String startDt = "01-" + currentMonth + "-" + year;
                    String endDt = lastDay + "-" + currentMonth + "-" + year;

                    dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
                }
                else if (radioButton3.Checked)
                {
                    dateFilter = "AND B.BILL_DT between '" + dateTimePicker1.Value.ToString("dd-MMM-yy") + "' and '" + dateTimePicker2.Value.ToString("dd-MMM-yy") + "'";
                }

                string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm " + dateFilter + " GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
                string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
                query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";
                populateList(query);
            }
        }

        private void populateList(String sql)
        {
            sql = sql.ToUpper().Replace("GROUP BY", agentFilter + transporterFilter + "GROUP BY");

            if(!agentFilter.Equals(""))
            {
                sql = sql.ToUpper().Replace("ITEM I WHERE", "ITEM I, AGENT AG WHERE B.AGENT = AG.AID AND");
            }
            else if (!transporterFilter.Equals(""))
            {
                sql = sql.ToUpper().Replace("ITEM I WHERE", "ITEM I, TRANSPORT TR WHERE B.TRANSPORTER = TR.TID AND");
            }
            DataGridViewLinkColumn col = new DataGridViewLinkColumn();
            col.DataPropertyName = "BILL_ID";
            col.Name = "BILL_ID";
            if (!oneTimeFlag)
            {
                dataGridView1.Columns.Add(col);
                oneTimeFlag = true;
            }

            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();
            con.Open();

            dataadapter.Fill(ds, "BILL");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "BILL";

            con.Close();

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }

        private void populateTransportList()
        {
            if (!dataGridView1.Columns[0].Name.Equals("TRANSPORTER"))
            {
                DataGridViewLinkColumn col = new DataGridViewLinkColumn();
                col.DataPropertyName = "TRANSPORTER";
                col.Name = "TRANSPORTER";
                dataGridView1.Columns.Add(col);
            }

            string dateFilter = "";

            if (radioButton1.Checked)
            {
                dateFilter = "and b.bill_dt between (getDate() - 7) and getDate()";
            }
            else if (radioButton4.Checked)
            {
                String currentMonth = DateTime.Now.ToString("MMM");
                int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                String startDt = "01-" + currentMonth + "-" + year;
                String endDt = lastDay + "-" + currentMonth + "-" + year;

                dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
            }
            else if (radioButton5.Checked)
            {
                DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                String currentMonth = previousMonthDt.ToString("MMM");
                int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                String startDt = "01-" + currentMonth + "-" + year;
                String endDt = lastDay + "-" + currentMonth + "-" + year;

                dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
            }
            else if (radioButton3.Checked)
            {
                dateFilter = "AND B.BILL_DT between '" + dateTimePicker1.Value.ToString("dd-MMM-yy") + "' and '" + dateTimePicker2.Value.ToString("dd-MMM-yy") + "'";
            }

            string query = "select t_name TRANSPORTER, sum(net_amt) net_amt, sum(round(bill_amt, 0)) bill_amt from bill b, transport t where t.tid = b.TRANSPORTER AND B.firm = '"+ company +"' "+ dateFilter +" and t_name <> 'NA' group by t_name order by t_name";

            SqlDataAdapter dataadapter = new SqlDataAdapter(query, con);
            DataSet ds = new DataSet();
            con.Open();

            dataadapter.Fill(ds, "BILL");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "BILL";

            con.Close();

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }

        private void populateAgentList()
        {
            if(!dataGridView1.Columns[0].Name.Equals("AGENT"))
            {
                DataGridViewLinkColumn col = new DataGridViewLinkColumn();
                col.DataPropertyName = "AGENT";
                col.Name = "AGENT";
                dataGridView1.Columns.Add(col);
            }

            string dateFilter = "";

            if (radioButton1.Checked)
            {
                dateFilter = "and b.bill_dt between (getDate() - 7) and getDate()";
            }
            else if (radioButton4.Checked)
            {
                String currentMonth = DateTime.Now.ToString("MMM");
                int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                String startDt = "01-" + currentMonth + "-" + year;
                String endDt = lastDay + "-" + currentMonth + "-" + year;

                dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
            }
            else if (radioButton5.Checked)
            {
                DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                String currentMonth = previousMonthDt.ToString("MMM");
                int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                String startDt = "01-" + currentMonth + "-" + year;
                String endDt = lastDay + "-" + currentMonth + "-" + year;

                dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
            }
            else if (radioButton3.Checked)
            {
                dateFilter = "AND B.BILL_DT between '" + dateTimePicker1.Value.ToString("dd-MMM-yy") + "' and '" + dateTimePicker2.Value.ToString("dd-MMM-yy") + "'";
            }

            string query = "select a_name Agent, sum(net_amt) \"Taxable Amount\", (select isnull(sum(net_amt), 0) net_amt from bill b, agent t1 where t1.aid = b.agent and t1.a_name = t.a_name AND B.firm = '" + company + "' " + dateFilter + " and a_name <> 'NA' and IGST_AMT > 0) \"Outstation Amount\", (select isnull(sum(net_amt), 0) net_amt from bill b, agent t2 where t2.aid = b.agent and t2.a_name = t.a_name AND B.firm = '" + company + "' " + dateFilter + " and a_name <> 'NA' and IGST_AMT = 0) \"Local Amount\" from bill b, agent t where t.aid = b.agent AND B.firm = '" + company + "' " + dateFilter + " and a_name <> 'NA' group by a_name order by a_name";

            SqlDataAdapter dataadapter = new SqlDataAdapter(query, con);
            DataSet ds = new DataSet();
            con.Open();

            dataadapter.Fill(ds, "BILL");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "BILL";

            con.Close();

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }
        
        private void button7_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete " + company + "?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                int i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ITEM WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM AGENT WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL WHERE FIRM = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM COMPANY WHERE NAME = '"+ company +"'", con);
                cmd.Parameters.AddWithValue("'"+ company +"'", company);
                i = cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Firm Deleted Successfully!!");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();
                
            }
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

        private void button3_Click(object sender, EventArgs e)
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

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
            
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";

            if (comboBox1.SelectedIndex == 0)
            {
                populateList(query);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                populateTransportList();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                populateAgentList();
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (!custom)
            {
                label12.Visible = true;
                dateTimePicker1.Visible = true;
                label13.Visible = true;
                dateTimePicker2.Visible = true;
            } else
            {
                label12.Visible = false;
                dateTimePicker1.Visible = false;
                label13.Visible = false;
                dateTimePicker2.Visible = false;
            }
            custom = !custom;

            if (comboBox1.SelectedIndex == 0)
            {
                customSearch();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                transporterWiseSearch();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                agentWiseSearch();
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm and b.bill_dt between (getDate() - 7) and getDate() GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";

            if (comboBox1.SelectedIndex == 0)
            {
                populateList(query);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                populateTransportList();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                populateAgentList();
            }
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                customSearch();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                transporterWiseSearch();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                agentWiseSearch();
            }
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                customSearch();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                transporterWiseSearch();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                agentWiseSearch();
            }
        }

        private void customSearch()
        {
            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm and b.bill_dt between '" + dateTimePicker1.Value.ToString("dd-MMM-yy") + "' and '" + dateTimePicker2.Value.ToString("dd-MMM-yy") + "' GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";
            populateList(query);
        }

        private void transporterWiseSearch()
        {
            populateTransportList();
        }

        private void agentWiseSearch()
        {
            populateAgentList();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            String currentMonth = DateTime.Now.ToString("MMM");
            int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2,2));
            int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

            String startDt = "01-" + currentMonth + "-" + year;
            String endDt = lastDay + "-" + currentMonth + "-" + year;

            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm and b.bill_dt between '" + startDt + "' and '" + endDt + "' GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";

            if (comboBox1.SelectedIndex == 0)
            {
                populateList(query);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                populateTransportList();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                populateAgentList();
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
            String currentMonth = previousMonthDt.ToString("MMM");
            int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
            int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

            String startDt = "01-" + currentMonth + "-" + year;
            String endDt = lastDay + "-" + currentMonth + "-" + year;

            string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm and b.bill_dt between '" + startDt + "' and '" + endDt + "' GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
            string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";

            if (comboBox1.SelectedIndex == 0)
            {
                populateList(query);
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                populateTransportList();
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                populateAgentList();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String usersFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            String path = usersFolder + "\\Documents\\123.xls";

            String startDt = "";
            String endDt = "";

            if(radioButton1.Checked) // week
            {
                endDt = DateTime.Now.ToString("dd-MMM-yy");
                startDt = DateTime.Now.AddDays(-7).ToString("dd-MMM-yy");
            }
            else if (radioButton2.Checked) // lifetime
            {
                startDt = "01-Jul-17";
                endDt = DateTime.Now.ToString("dd-MMM-yy");
            }
            else if (radioButton3.Checked) // custom
            {
                startDt = dateTimePicker1.Value.ToString("dd-MMM-yy");
                endDt = dateTimePicker2.Value.ToString("dd-MMM-yy");
            }
            else if (radioButton4.Checked) // this month
            {
                String currentMonth = DateTime.Now.ToString("MMM");
                int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                startDt = "01-" + currentMonth + "-" + year;
                endDt = lastDay + "-" + currentMonth + "-" + year;
            }
            else if (radioButton5.Checked) // last month
            {
                DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                String currentMonth = previousMonthDt.ToString("MMM");
                int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                startDt = "01-" + currentMonth + "-" + year;
                endDt = lastDay + "-" + currentMonth + "-" + year;
            }

            MessageBox.Show("Return data will be generated for period : " + startDt + " to " + endDt);

            XSSFWorkbook templateWorkbook;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                templateWorkbook = new XSSFWorkbook(fs);
                fs.Close();
            }

            // B2B

            XSSFSheet sheet = (XSSFSheet)templateWorkbook.GetSheet("B2B");

            string query = "select B.BILL_ID, B.BILL_DT, C.GSTIN, C.CNAME, B.NET_AMT, (B.CGST + B.SGST + B.ISGT) TAX_PER, B.IGST_AMT IGST, B.CGST_AMT CGST, B.SGST_AMT SGST, (B.CGST_AMT + B.SGST_AMT + B.IGST_AMT) TAX_AMT, B.BILL_AMT from bill B, CUSTOMER C, BILL_ITEM BI WHERE B.BILL_TO = C.CID AND B.BILL_ID = BI.BILL_ID AND B.FIRM = '"+ company +"' AND B.BILL_DT BETWEEN '" + startDt + "' AND '" + endDt + "' GROUP BY  B.BILL_ID, B.BILL_DT, C.GSTIN, C.CNAME, B.CGST, B.SGST, B.ISGT, B.IGST_AMT, B.CGST_AMT, B.SGST_AMT, (B.CGST_AMT + B.SGST_AMT + B.IGST_AMT), B.BILL_AMT, DISCOUNT, B.NET_AMT";
           
            SqlCommand oCmd = new SqlCommand(query, con);
            //oCmd.Parameters.AddWithValue("'"+ company +"'", company);
            con.Open();
            int i = 5;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    NPOI.SS.UserModel.IRow x = sheet.CreateRow(i);

                    for(int j=0; j<15; j++)
                    {
                        x.CreateCell(j);
                    }
                    sheet.GetRow(i).GetCell(0).SetCellValue(oReader["BILL_ID"].ToString().Split('/')[0]);
                    sheet.GetRow(i).GetCell(1).SetCellValue(oReader["BILL_DT"].ToString().Substring(0, 9));
                    sheet.GetRow(i).GetCell(2).SetCellValue(oReader["GSTIN"].ToString().Substring(0,2));
                    sheet.GetRow(i).GetCell(3).SetCellValue(oReader["GSTIN"].ToString());
                    sheet.GetRow(i).GetCell(4).SetCellValue(oReader["CNAME"].ToString());
                    sheet.GetRow(i).GetCell(5).SetCellValue(oReader["NET_AMT"].ToString());
                    sheet.GetRow(i).GetCell(6).SetCellValue(oReader["TAX_PER"].ToString());
                    sheet.GetRow(i).GetCell(7).SetCellValue(oReader["IGST"].ToString());
                    sheet.GetRow(i).GetCell(8).SetCellValue(oReader["CGST"].ToString());
                    sheet.GetRow(i).GetCell(9).SetCellValue(oReader["SGST"].ToString());
                    sheet.GetRow(i).GetCell(10).SetCellValue("0");
                    sheet.GetRow(i).GetCell(11).SetCellValue(oReader["TAX_AMT"].ToString());
                    sheet.GetRow(i).GetCell(12).SetCellValue(oReader["BILL_AMT"].ToString());
                    sheet.GetRow(i).GetCell(13).SetCellValue("No");
                    sheet.GetRow(i).GetCell(14).SetCellValue("");
                    i++;
                }
            }
            con.Close();

            // HSN PART 1

            sheet = (XSSFSheet)templateWorkbook.GetSheet("HSN");

            query = "SELECT HSN, DESCR, UNIT, SUM(MTR) TOTAL_QTY, SUM(DISTINCT TOTAL_VALUE) TOTAL_VALUE FROM HSN_1_VIEW H1V WHERE FIRM = '"+ company +"' AND BILL_DT BETWEEN '" + startDt + "' AND '" + endDt + "' GROUP BY HSN, DESCR, UNIT";

            oCmd = new SqlCommand(query, con);
            //oCmd.Parameters.AddWithValue("'"+ company +"'", company);
            con.Open();
            i = 5;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    NPOI.SS.UserModel.IRow x = sheet.CreateRow(i);

                    for (int j = 0; j < 10; j++)
                    {
                        x.CreateCell(j);
                    }
                    sheet.GetRow(i).GetCell(0).SetCellValue(oReader["HSN"].ToString());
                    sheet.GetRow(i).GetCell(1).SetCellValue(oReader["DESCR"].ToString());
                    sheet.GetRow(i).GetCell(2).SetCellValue(oReader["UNIT"].ToString());
                    sheet.GetRow(i).GetCell(3).SetCellValue(oReader["TOTAL_QTY"].ToString());
                    sheet.GetRow(i).GetCell(4).SetCellValue(oReader["TOTAL_VALUE"].ToString());
                    i++;
                }
            }
            con.Close();

            // HSN PART 2

            sheet = (XSSFSheet)templateWorkbook.GetSheet("HSN");

            query = "SELECT SUM(NET_AMT) TAXABLE_AMT, SUM(THV.IGST) IGST, SUM(THV.CGST) CGST, SUM(THV.SGST) SGST " +
                "FROM TAX_HSN_VIEW THV, BILL B WHERE THV.BILL_ID = B.BILL_ID AND B.FIRM = '"+ company +"' " +
                "AND B.BILL_DT BETWEEN '"+ startDt + "' AND '" + endDt + "' " +
                "GROUP BY HSN, descr, unit ORDER BY HSN, descr";

            oCmd = new SqlCommand(query, con);
            //oCmd.Parameters.AddWithValue("'"+ company +"'", company);
            con.Open();
            i = 5;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    sheet.GetRow(i).GetCell(5).SetCellValue(oReader["TAXABLE_AMT"].ToString());
                    sheet.GetRow(i).GetCell(6).SetCellValue(oReader["IGST"].ToString());
                    sheet.GetRow(i).GetCell(7).SetCellValue(oReader["CGST"].ToString());
                    sheet.GetRow(i).GetCell(8).SetCellValue(oReader["SGST"].ToString());
                    sheet.GetRow(i).GetCell(9).SetCellValue("0");
                    i++;
                }
            }
            con.Close();

            String output = usersFolder + "\\Documents\\GSTR-1.xls";

            using (FileStream file = new FileStream(output, FileMode.Create, FileAccess.Write))
            {
                templateWorkbook.Write(file);
                file.Close();
            }

            MessageBox.Show("File generated!!! Click OK to open");
            Process.Start(output);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!loading)
            {
                if (comboBox1.SelectedIndex == 1)
                {
                    agentFilter = "";
                    transporterWiseSearch();
                }
                else if (comboBox1.SelectedIndex == 0)
                {
                    agentFilter = "";
                    transporterFilter = "";
                    if (radioButton3.Checked)
                    {
                        customSearch();
                    }
                    else
                    {
                        string dateFilter = "";

                        if (radioButton1.Checked)
                        {
                            dateFilter = "and b.bill_dt between (getDate() - 7) and getDate()";
                        }
                        else if (radioButton4.Checked)
                        {
                            String currentMonth = DateTime.Now.ToString("MMM");
                            int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                            int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                            String startDt = "01-" + currentMonth + "-" + year;
                            String endDt = lastDay + "-" + currentMonth + "-" + year;

                            dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
                        }
                        else if (radioButton5.Checked)
                        {
                            DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                            String currentMonth = previousMonthDt.ToString("MMM");
                            int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                            int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                            String startDt = "01-" + currentMonth + "-" + year;
                            String endDt = lastDay + "-" + currentMonth + "-" + year;

                            dateFilter = "and b.bill_dt between '" + startDt + "' and '" + endDt + "'";
                        }
                        else if (radioButton3.Checked)
                        {
                            dateFilter = "AND B.BILL_DT between '" + dateTimePicker1.Value.ToString("dd-MMM-yy") + "' and '" + dateTimePicker2.Value.ToString("dd-MMM-yy") + "'";
                        }

                        string query = "select b.BILL_ID, b.BILL_DT, c.CNAME CUSTOMER_NAME, sum(bi.mtr) METER, CASE WHEN NOT ISNUMERIC(MIN(BI.ROLL_NO)) = 1 OR MIN(ROLL_NO) = MAX(ROLL_NO) THEN UPPER(MIN(BI.ROLL_NO)) ELSE concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT))) END ROLL_NO, i.ITEM_NAME ITEM, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight) OTHER, BILL_AMT from bill b, customer c, bill_item bi, item i where ISNUMERIC(BI.ROLL_NO) = 1 AND bi.item = i.item_id and b.firm = '" + company + "' and c.CID = b.bill_to and c.firm = b.firm and b.bill_id = bi.bill_id and b.firm = bi.firm " + dateFilter + " GROUP BY B.BILL_ID, B.BILL_DT, C.CNAME, I.ITEM_NAME, B.NET_AMT, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, (freight), BILL_AMT, DISCOUNT";
                        string query2 = query.Replace("where ISNUMERIC(BI.ROLL_NO) = 1", "where ISNUMERIC(BI.ROLL_NO) <> 1").Replace("concat(min(CAST(bi.ROLL_NO AS INT)), ' - ', max(CAST(bi.roll_no AS INT)))", "concat(min(bi.ROLL_NO), ' - ', max(bi.roll_no))");
            query = "select * from (" + query + " union " + query2 + ") t order by bill_dt";
                        populateList(query);
                    }
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    transporterFilter = "";
                    agentWiseSearch();
                }
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var targetForm = new GenerateJSON(company);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            printDocument1.DefaultPageSettings.Landscape = true;
            printDocument1.Print();
        }

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            Graphics graphic = e.Graphics;
            SolidBrush brush = new SolidBrush(ColorTranslator.FromHtml("#655c62"));

            Font font = new Font("Arial", 16, FontStyle.Bold);

            e.PageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            float pageWidth = e.PageSettings.PrintableArea.Width;
            float pageHeight = e.PageSettings.PrintableArea.Height;

            float fontHeight = font.GetHeight();

            int startY = 100;
            int offsetY = 40;

            //firm
            SizeF stringSize = new SizeF();
            stringSize = e.Graphics.MeasureString(company, font);
            int stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString(company, font, brush, stringCenterX, 70);
            graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height));
            graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height));

            font = new Font("Arial", 14, FontStyle.Bold);
            brush = new SolidBrush(Color.Black);
            // stock report
            stringSize = e.Graphics.MeasureString("Bill Report", font);
            stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString("Bill Report", font, brush, stringCenterX, 110);

            // report period

            string startDt = DateTime.Now.AddDays(-7).ToString("dd-MMM-yyyy"); ;
            string endDt = DateTime.Now.ToString("dd-MMM-yyyy"); ;

            if (radioButton4.Checked)
            {
                String currentMonth = DateTime.Now.ToString("MMM");
                int year = Int32.Parse(DateTime.Now.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month);

                startDt = "01-" + currentMonth + "-" + year;
                endDt = lastDay + "-" + currentMonth + "-" + year;
            }
            else if (radioButton5.Checked)
            {
                DateTime previousMonthDt = DateTime.Now.AddMonths(-1);
                String currentMonth = previousMonthDt.ToString("MMM");
                int year = Int32.Parse(previousMonthDt.Year.ToString().Substring(2, 2));
                int lastDay = DateTime.DaysInMonth(previousMonthDt.Year, previousMonthDt.Month);

                startDt = "01-" + currentMonth + "-" + year;
                endDt = lastDay + "-" + currentMonth + "-" + year;
            }
            else if (radioButton3.Checked)
            {
                startDt = dateTimePicker1.Value.ToString("dd-MMM-yy");
                endDt = dateTimePicker2.Value.ToString("dd-MMM-yy");
            }

            string asOnDate = startDt + " to " + endDt;

            font = new Font("Arial", 12);
            stringSize = e.Graphics.MeasureString("Period : " + asOnDate, font);
            stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            graphic.DrawString("Period : " + asOnDate, font, brush, stringCenterX, 140);

            offsetY += 50;
            int[] headerX = new int[dataGridView1.ColumnCount];

            int locX = 20;
            font = new Font("Arial", 12, FontStyle.Bold);
            brush = new SolidBrush(ColorTranslator.FromHtml("#007171"));
            for (int j = 0; j < dataGridView1.ColumnCount; j++)
            {
                stringSize = e.Graphics.MeasureString(dataGridView1.Columns[j].HeaderText, font);
                graphic.DrawString(dataGridView1.Columns[j].HeaderText, font, brush, locX, startY + offsetY);
                headerX[j] = locX;

                if (j > 0)
                {
                    locX += ((int)stringSize.Width + 50);
                }
                else
                {
                    locX += ((int)stringSize.Width + 150);
                }
            }
            offsetY += ((int)font.GetHeight() + 10);

            for (int j = 0; j < dataGridView1.RowCount; j++)
            {
                for (int k = 0; k < dataGridView1.ColumnCount; k++)
                {
                    if (k == 0)
                    {
                        font = new Font("Arial", 12, FontStyle.Bold);
                        brush = new SolidBrush(Color.Olive);
                        graphic.DrawString(dataGridView1[k, j].Value.ToString(), font, brush, 20, startY + offsetY);
                    }
                    else
                    {
                        font = new Font("Arial", 12, FontStyle.Bold);
                        brush = new SolidBrush(Color.Black);
                        graphic.DrawString(dataGridView1[k, j].Value.ToString(), font, brush, headerX[k], startY + offsetY);
                    }

                }
                offsetY += ((int)font.GetHeight() + 10);
            }
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            var targetForm = new TallyXML(company);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void fillCredentials(IWebDriver driver, string username, string password)
        {
            if(exitClicked)
            {
                return;
            }
            bool error = false;
            driver.FindElement(By.XPath("//*[@id=\"txt_username\"]")).SendKeys(username);
            driver.FindElement(By.Id("txt_password")).SendKeys(password);

            if (!fakeCaptchaSubmitted)
            {
                driver.FindElement(By.Id("txtCaptcha")).SendKeys("XXXXXX");
                driver.FindElement(By.Id("btnLogin")).Click();
                fakeCaptchaSubmitted = true;
            }
            else
            {
                driver.FindElement(By.Id("txtCaptcha")).SendKeys("");
            }

            // Exit click

            WebDriverWait waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
            try
            {
                waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"A1\"]|//*[@id=\"Div2FA\"]/div/div/div[3]/button")));
                if(driver.FindElement(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")) != null)
                {
                    return;
                }
                error = false;
            }
            catch
            {
                error = true;
                try
                {
                    driver.SwitchTo().Alert().Accept();
                }
                catch
                {
                    error = true;
                }
                fillCredentials(driver, username, password);
            }

            try
            {
                driver.FindElement(By.Id("A1")).Click();
                error = false;
            }
            catch
            {
                error = true;
            }
            exitClicked = true;
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            exitClicked = false;
            eWayBillIds = new Dictionary<string, string>();

            con.Open();
            String query = "select EWB_USERNAME, EWB_PASSWORD from company where name = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            string username = "";
            string password = "";

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    username = oReader["EWB_USERNAME"].ToString();
                    password = oReader["EWB_PASSWORD"].ToString();
                }
            }

            IWebDriver driver = null;
            try
            {
                ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                chromeDriverService.HideCommandPromptWindow = true;

                ChromeOptions options = new ChromeOptions();
                options.AddArguments("disable-infobars");

                driver = new ChromeDriver(chromeDriverService, options, TimeSpan.FromSeconds(6000));

                driver.Navigate().GoToUrl("https://ewaybillgst.gov.in/login.aspx");
                driver.Manage().Window.Maximize();

                // fill username & password

                fillCredentials(driver, username, password);

                // Dismiss alert

                WebDriverWait waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));

                waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")));
                driver.FindElement(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")).Click();

                // E-Waybill click

                waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")));

            try
            {
                driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")).Click();
            } catch
            {
                Thread.Sleep(500);
                driver.FindElement(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")).Click();

                waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                waitForElement.Until(ExpectedConditions.InvisibilityOfElementLocated(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")));

                driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")).Click();
            }

                // Generate bulk click

                waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R12\"]/a")));

                driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R12\"]/a")).Click();

                // upload file

                waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                waitForElement.Until(ExpectedConditions.ElementExists(By.Id("ctl00_ContentPlaceHolder1_FileUploadControl")));

                driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_FileUploadControl")).SendKeys(@"C:\Invoices\eWayBill.json");

                // click Upload button

                driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_UploadButton")).Click();

                try
                {
                    while (true)
                    {
                        driver.SwitchTo().Alert().Accept();
                    }
                }
                catch
                {

                }

                // click generate

                waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                waitForElement.Until(ExpectedConditions.ElementIsVisible(By
                    .XPath("//*[@id=\"ctl00_ContentPlaceHolder1_BulkEwayBills\"]/tbody/tr")));

                ScrollToBottom(driver);
                driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_btnGenerate")).Click();

                // get E-waybill no for bill ids

                ReadOnlyCollection<IWebElement> rows = driver.FindElements(By.CssSelector("[id='ctl00_ContentPlaceHolder1_BulkEwayBills'] tr"));

                int i = 1;
                foreach (IWebElement element in rows)
                {
                    if (i > 0)
                    {
                        IWebElement billId = driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_BulkEwayBills\"]/tbody/tr["+ i +"]/td[3]"));
                        IWebElement ewbNo = driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_BulkEwayBills\"]/tbody/tr[" + i + "]/td[9]"));

                        if (ewbNo.Text.Length > 1)
                        {
                            eWayBillIds.Add(billId.Text, ewbNo.Text);

                            // update invoices

                            SqlCommand cmd = new SqlCommand("update bill set EWAYBILL_NO = @EWAYBILL_NO where bill_id = @bill_id and firm = @firm", con);
                            cmd.Parameters.AddWithValue("@FIRM", company);
                            cmd.Parameters.AddWithValue("@BILL_ID", billId.Text);
                            cmd.Parameters.AddWithValue("@EWAYBILL_NO", ewbNo.Text);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    i++;
                }
                con.Close();

                // print ewaybills : 611500129090

                if (eWayBillIds.Count > 0)
                {
                    driver.FindElement(By.Id("ctl00_headercont_lnk_home")).Click();

                    foreach (string billId in eWayBillIds.Keys)
                    {
                        // E-Waybill click

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")));

                        driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")).Click();

                        // Print EWB click

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R15\"]/a")));

                        driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R15\"]/a")).Click();

                        // Enter EWB no and click GO

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.Id("ctl00_ContentPlaceHolder1_txt_ebillno")));

                        driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_txt_ebillno")).SendKeys(eWayBillIds[billId]);
                        driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_btn_go")).Click();

                        // Click Exit button after print
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("window.onafterprint = function () {document.getElementById('ctl00_ContentPlaceHolder1_printtr').getElementsByTagName('a')[1].click();};", null);

                        // Click Print
                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_printtr\"]/td/a[1]")));

                        js.ExecuteScript("document.getElementById('ctl00_ContentPlaceHolder1_printtr').getElementsByTagName('a')[0].click();", null);
                    }

                    waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                    waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")));

                    Thread.Sleep(4000);
                }

                driver.Close();
                driver.Quit();

                AddInvoice.uploadRollNo();

                MessageBox.Show("E-WayBill nos generated and updated. Take print out of bills then click on PRINT E-WAYBILLS to print the generated E-WayBills");
            }
            catch (Exception ex)
            {
                try
                {
                    if (driver != null)
                    {
                        driver.Close();
                        driver.Quit();
                    }
                }
                catch
                {

                }
                MessageBox.Show(ex.Message);
            }
            con.Close();
        }

        static void ScrollToBottom(IWebDriver driver)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;

            // Get the document height
            long documentHeight = (long)js.ExecuteScript("return Math.max( document.body.scrollHeight, document.body.offsetHeight, document.documentElement.clientHeight, document.documentElement.scrollHeight, document.documentElement.offsetHeight );");

            // Set the current scroll position to the top of the page
            long currentPosition = 0;

            // The height of the viewport
            long windowHeight = (long)js.ExecuteScript("return window.innerHeight || document.documentElement.clientHeight || document.body.clientHeight;");

            // Loop until we reach the bottom of the page
            while (currentPosition + windowHeight < documentHeight)
            {
                // Scroll down by the height of the viewport
                js.ExecuteScript($"window.scrollTo(0, {currentPosition += windowHeight});");
            }
        }
        private void pictureBox3_Click(object sender, EventArgs e)
        {
            eWayBillIds = new Dictionary<string, string>();
            string jsonData = File.ReadAllText(@"C:\Invoices\eWayBill.json");
            string[] parts = jsonData.Split(new string[] { "docNo\":\"" }, StringSplitOptions.None);
            int i = 0;

            string billIdFilter = "(";

            foreach(string p in parts) {
                if(p.Contains("docDate"))
                {
                    string billId = parts[i].Split('"')[0];
                    billIdFilter += "'" + billId + "', ";
                }
                i++;
            }
            billIdFilter = billIdFilter.Substring(0, billIdFilter.Length - 2) + ")";
            con.Open();

            String query1 = "select bill_id, ewaybill_no from bill where bill_id in " + billIdFilter;
            SqlCommand oCmd1 = new SqlCommand(query1, con);

            using (SqlDataReader oReader = oCmd1.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(oReader["EWAYBILL_NO"].ToString().Equals(""))
                    {
                        MessageBox.Show("E-Waybill is not generated for Bill ID : " + oReader["BILL_ID"].ToString() + ". Please generate and update E-Waybill first.");
                        con.Close();
                        return;
                    }
                    eWayBillIds.Add(oReader["BILL_ID"].ToString(), oReader["EWAYBILL_NO"].ToString());
                }
            }

            IWebDriver driver = null;
            try
            {
                if (eWayBillIds.Count == 0)
                {
                    MessageBox.Show("No E-WayBills found. Please generate first.");
                }
                else
                {
                    String query = "select EWB_USERNAME, EWB_PASSWORD from company where name = @FIRM";
                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", company);

                    string username = "";
                    string password = "";

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            username = oReader["EWB_USERNAME"].ToString();
                            password = oReader["EWB_PASSWORD"].ToString();
                        }
                    }

                    ChromeDriverService chromeDriverService = ChromeDriverService.CreateDefaultService();
                    chromeDriverService.HideCommandPromptWindow = true;

                    ChromeOptions options = new ChromeOptions();
                    options.AddArguments("disable-infobars");

                    driver = new ChromeDriver(chromeDriverService, options, TimeSpan.FromSeconds(6000));
                    
                    driver.Navigate().GoToUrl("https://ewaybillgst.gov.in/login.aspx");
                    driver.Manage().Window.Maximize();

                    // fill username & password

                    fillCredentials(driver, username, password);

                    // Dismiss alert

                    WebDriverWait waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));

                    waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")));
                    driver.FindElement(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")).Click();

                    foreach (string billId in eWayBillIds.Keys)
                    {
                        // E-Waybill click

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")));

                        try
                        {
                            driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")).Click();
                        }
                        catch
                        {
                            Thread.Sleep(500);
                            driver.FindElement(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")).Click();

                            waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                            waitForElement.Until(ExpectedConditions.InvisibilityOfElementLocated(By.XPath("//*[@id=\"Div2FA\"]/div/div/div[3]/button")));

                            driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")).Click();
                        }
                        // Print EWB click

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R15\"]/a")));

                        driver.FindElement(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R15\"]/a")).Click();

                        // Enter EWB no and click GO

                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementIsVisible(By.Id("ctl00_ContentPlaceHolder1_txt_ebillno")));

                        driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_txt_ebillno")).SendKeys(eWayBillIds[billId]);
                        driver.FindElement(By.Id("ctl00_ContentPlaceHolder1_btn_go")).Click();

                        // Click Exit button after print
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("window.onafterprint = function () {document.getElementById('ctl00_ContentPlaceHolder1_printtr').getElementsByTagName('a')[1].click();};", null);

                        // Click Print
                        waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                        waitForElement.Until(ExpectedConditions.ElementToBeClickable(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_printtr\"]/td/a[1]")));

                        js.ExecuteScript("document.getElementById('ctl00_ContentPlaceHolder1_printtr').getElementsByTagName('a')[0].click();", null);
                    }

                    waitForElement = new WebDriverWait(driver, TimeSpan.FromSeconds(120));
                    waitForElement.Until(ExpectedConditions.ElementIsVisible(By.XPath("//*[@id=\"ctl00_ContentPlaceHolder1_R10\"]/a")));

                    Thread.Sleep(4000);
                    driver.Close();
                    driver.Quit();
                }
            }
            catch (Exception ex)
            {
                try
                {
                    if (driver != null)
                    {
                        driver.Close();
                        driver.Quit();
                    }
                }
                catch
                {

                }
                MessageBox.Show(ex.Message);
            }
            con.Close();
        }

        private void button11_Click_1(object sender, EventArgs e)
        {
            PrintDocument pd = new PrintDocument();
            pd.PrintPage += new PrintPageEventHandler(this.printDocument1_PrintPage);

            PrintDialog printdlg = new PrintDialog();
            PrintPreviewDialog printPrvDlg = new PrintPreviewDialog();

            // preview the assigned document or you can create a different previewButton for it
            printPrvDlg.Document = pd;
            printPrvDlg.ShowDialog(); // this shows the preview and then show the Printer Dlg below

            printdlg.Document = pd;

            if (printdlg.ShowDialog() == DialogResult.OK)
            {
                pd.Print();
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            var invList = new OrderManagement(company);
            invList.MdiParent = ParentForm;
            invList.Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {

        }
    }
}
