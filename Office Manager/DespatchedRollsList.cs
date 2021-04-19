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
    public partial class DespatchedRollsList : Form
    {
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<int, int> indexMap = new Dictionary<int, int>();
        int totalRows;
        int gridHeight;
        int gridWidth;

        string fromFyDate;
        string toFyDate;

        public DespatchedRollsList(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void DespatchedRollsList_Load(object sender, EventArgs e)
        {
            gridHeight = dataGridView1.Height;
            gridWidth = dataGridView1.Width;

            DataGridViewLinkColumn col = new DataGridViewLinkColumn();
            col.DataPropertyName = "INDEX";
            col.Name = "INDEX";
            dataGridView1.Columns.Add(col);

            col = new DataGridViewLinkColumn();
            col.DataPropertyName = "DETAILS";
            col.Name = "VIEW";
            dataGridView1.Columns.Add(col);

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;

            if (month < 4)
            {
                year--;
            }

            fromFyDate = "01-APR-" + year;
            toFyDate = "31-MAR-" + (year + 1);

            con.Open();

            string query = "select ROW_NUMBER() OVER (ORDER BY ROLL_NO DESC) AS IND, ROLL_NO FROM ROLL WHERE FIRM = '" + firm + "' and despatch_date between '" + fromFyDate + "' and '" + toFyDate + "'";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            totalRows = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    totalRows++;
                    indexMap.Add(Int32.Parse(oReader["IND"].ToString()), Int32.Parse(oReader["ROLL_NO"].ToString()));
                }
            }
            con.Close();

            Dictionary<int, int> pageNos = new Dictionary<int, int>();
            for (int i = 1; i <= Math.Ceiling((double)totalRows / 10); i++)
            {
                pageNos.Add(i, i);
            }

            if (pageNos.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(pageNos, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";
            }

            fillData();

            dataGridView1.CellClick += (s, evt) =>
            {
                cellClick(s, evt);
            };
        }

        private void fillData()
        {
            int startIndex = comboBox1.SelectedIndex * 10 + 1;
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

            try
            {
                con.Open();
            }
            catch
            {

            }
            string sql = "select IND \"INDEX\", 'View Details' DETAILS, DESPATCH_DATE \"DATE\", ROLL_NO \"ROLL NO\", (select tech_name from product where pid = quality) QUALITY , MTR \"METER\", ELONGATION \"ELONGATION\", EXTENDED_MTR \"EXTENDED MTR\", WIGHT \"WEIGHT\", WIDTH FROM (select ROW_NUMBER() OVER (ORDER BY ROLL_NO DESC) AS IND, DESPATCH_DATE DD, CONVERT(VARCHAR(12), DESPATCH_DATE, 107) DESPATCH_DATE, ROLL_NO, (SELECT top 1 QUALITY FROM ROLL_ENTRY RE, ROLL_CONTENT RC WHERE RE.ENTRY_ID = RC.ENTRY_ID AND RC.ROLL_NO = DR.ROLL_NO order by txn_date desc) QUALITY, MTR, ELONGATION, EXTENDED_MTR, WIGHT, WIDTH FROM ROLL DR WHERE FIRM = '" + firm + "' and despatch_date between '" + fromFyDate + "' and '" + toFyDate + "') T WHERE IND >= " + startIndex + " AND IND <= " + endIndex + " ORDER BY ROLL_NO DESC";
            SqlDataAdapter dataadapter = new SqlDataAdapter(sql, con);
            DataSet ds = new DataSet();

            dataadapter.Fill(ds, "TAKA_ENTRY");
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "TAKA_ENTRY";

            con.Close();

            SalaryReport.d1H = gridHeight;
            SalaryReport.d1W = gridWidth;

            GodownStockReport.formatDataGridView(dataGridView1, Color.Aquamarine);
            SalaryReport.resizeGrid(dataGridView1);
        }

        private void cellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex.Equals(0) && e.RowIndex != -1)
            {
                if (dataGridView1.CurrentCell != null && dataGridView1.CurrentCell.Value != null)
                {
                    var targetForm = new DespatchRoll(firm, indexMap[Int32.Parse(dataGridView1.CurrentCell.Value.ToString())]);
                    targetForm.StartPosition = FormStartPosition.CenterParent;
                    targetForm.Show();
                }
            }
            else if (dataGridView1.CurrentCell.ColumnIndex.Equals(1) && e.RowIndex != -1)
            {
                int row = e.RowIndex;
                int col = e.ColumnIndex - 1;
                var targetForm = new DespatchedRollDetails(indexMap[Int32.Parse(dataGridView1[col, row].Value.ToString())], firm);
                targetForm.Show();
            }
        }

        private void prev_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex--;
            fillData();
        }

        private void next_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex++;
            fillData();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            fillData();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
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
