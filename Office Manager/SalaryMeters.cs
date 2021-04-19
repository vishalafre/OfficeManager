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
    public partial class SalaryMeters : Form
    {
        string wvr;
        string qlty;
        string fromDt;
        string toDt;
        string wName;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public SalaryMeters(string weaver, string quality, string fromDt, string toDt, string wName)
        {
            InitializeComponent();
            wvr = weaver;
            qlty = quality;
            this.fromDt = fromDt;
            this.toDt = toDt;
            this.wName = wName;
        }

        private void SalaryMeters_Load(object sender, EventArgs e)
        {
            CenterToScreen();

            weaver.Text = wName;
            quality.Text = qlty;
            label2.Text = fromDt + " to " + toDt;

            con.Open();
            string sql = "SELECT CONVERT(VARCHAR(12), TXN_DATE, 107) \"DATE\", MTR METER FROM ROLL_ENTRY RE WHERE TXN_DATE BETWEEN '" + fromDt +"' AND '"+ toDt +"' AND WEAVER = "+ wvr +" AND QUALITY = (SELECT PID FROM PRODUCT WHERE FIRM = RE.FIRM AND TECH_NAME = '" + qlty + "') union all SELECT CONVERT(VARCHAR(12), TXN_DATE, 107) \"DATE\", MTR METER FROM TAKA_ENTRY RE WHERE TXN_DATE BETWEEN '" + fromDt + "' AND '" + toDt + "' AND WEAVER = " + wvr + " AND QUALITY = (SELECT PID FROM PRODUCT WHERE FIRM = RE.FIRM AND TECH_NAME = '" + qlty + "')";
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();

            dataadapter.Fill(ds, "TAKA_ENTRY");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "TAKA_ENTRY";

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
