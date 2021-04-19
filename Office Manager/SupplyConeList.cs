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
    public partial class SupplyConeList : Form
    {
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<int, int> indexMap = new Dictionary<int, int>();
        int totalRows;
        int gridHeight;
        int gridWidth;

        public SupplyConeList(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void SupplyConeList_Load(object sender, EventArgs e)
        {
            gridHeight = dataGridView1.Height;
            gridWidth = dataGridView1.Width;

            DataGridViewLinkColumn col = new DataGridViewLinkColumn();
            col.DataPropertyName = "INDEX";
            col.Name = "INDEX";
            dataGridView1.Columns.Add(col);

            con.Open();
            string query = "select ROW_NUMBER() OVER (ORDER BY txn_date DESC, ENTRY_ID DESC) AS IND, ENTRY_ID FROM SUPPLY_CONE WHERE FIRM = '" + firm + "' AND SUPPLY_FROM_TYPE not in ('O', 'E') AND SUPPLY_TO_TYPE NOT IN ('R', 'T', 'B')";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            totalRows = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    totalRows++;
                    indexMap.Add(Int32.Parse(oReader["IND"].ToString()), Int32.Parse(oReader["ENTRY_ID"].ToString()));
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

            con.Open();
            string sql = "select IND \"INDEX\", DATE, F \"FROM\", T \"TO\", YARN, QTY FROM (select ROW_NUMBER() OVER (ORDER BY txn_date DESC, entry_id desc) AS IND, ENTRY_ID, txn_date DATE, case supply_from_type when 'G' then (SELECT G_NAME FROM GODOWN WHERE GID = SUPPLY_FROM) WHEN 'W' THEN (SELECT W_NAME FROM WEAVER WHERE WID = SUPPLY_FROM) end F, case supply_TO_type when 'G' then (SELECT G_NAME FROM GODOWN WHERE GID = SUPPLY_TO) WHEN 'W' THEN (SELECT W_NAME FROM WEAVER WHERE WID = SUPPLY_TO) end T, (SELECT TECH_NAME FROM PRODUCT WHERE PID = YARN) YARN, QTY FROM SUPPLY_CONE WHERE FIRM = '" + firm + "' AND SUPPLY_FROM_TYPE not in ('O', 'E') AND SUPPLY_TO_TYPE NOT IN ('R', 'T', 'B')) T WHERE IND >= " + startIndex + " AND IND <= " + endIndex + " ORDER BY DATE DESC, ENTRY_ID DESC";
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
                    var targetForm = new SupplyCone(firm, logo, indexMap[Int32.Parse(dataGridView1.CurrentCell.Value.ToString())]);
                    targetForm.MdiParent = ParentForm;
                    targetForm.Show();
                }
            }
        }

        private void next_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex++;
            fillData();
        }

        private void prev_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex--;
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
