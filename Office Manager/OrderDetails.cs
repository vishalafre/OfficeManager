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
    public partial class OrderDetails : Form
    {
        string customer;
        string product;
        string agent;
        string orderDate;
        int orderID;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public OrderDetails(string customer, string product, string agent, string orderDate, int orderID)
        {
            InitializeComponent();
            this.customer = customer;
            this.product = product;
            this.agent = agent;
            this.orderDate = orderDate;
            this.orderID = orderID;
        }

        private void OrderDetails_Load(object sender, EventArgs e)
        {
            CenterToScreen();

            party.Text = customer;
            quality.Text = product;
            orderDt.Text = orderDate;
            agt.Text = agent;

            con.Open();
            string sql = "SELECT CONVERT(VARCHAR(12), TXN_DATE, 107) \"DATE\", DEL_QTY QTY, BILL_ID \"BILL ID\" FROM order_supply where order_id = "+ orderID +" order by txn_date";
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();

            dataadapter.Fill(ds, "ORDER_SUPPLY");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "ORDER_SUPPLY";

            con.Close();

            SalaryReport.d1H = dataGridView1.Height;
            SalaryReport.d1W = dataGridView1.Width;

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
