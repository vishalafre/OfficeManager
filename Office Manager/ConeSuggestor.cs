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
    public partial class ConeSuggestor : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> weavers = new Dictionary<string, string>();
        int d1H;
        int d1W;
        Boolean loading = true;

        public ConeSuggestor()
        {
            InitializeComponent();
        }

        private void ConeSuggestor_Load(object sender, EventArgs e)
        {
            d1H = dataGridView0.Height;
            d1W = dataGridView0.Width;

            con.Open();
            String query = "select distinct W_NAME from WEAVER order by W_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weavers.Add(oReader["W_NAME"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            con.Close();

            if (weavers.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(weavers, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";
            }

            loading = false;
            comboBox1_SelectedIndexChanged(comboBox1, null);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                con.Open();
                populateData(dataGridView0, null);

                int x = 1;
                while (true)
                {
                    int len = Controls.Find("firm" + x, true).Length;
                    if (len > 0)
                    {
                        Controls.Remove(Controls.Find("firm" + x, true)[0]);
                        Controls.Remove(Controls.Find("dataGridView" + x, true)[0]);
                    }
                    else
                    {
                        break;
                    }
                    x++;
                }

                String query = "select distinct firm from supply_cone where ((SUPPLY_FROM_TYPE = 'W' and supply_from in (select wid from weaver where w_name = @WEAVER)) or (SUPPLY_to_TYPE = 'W' and supply_to in (select wid from weaver where w_name = @WEAVER)))";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);

                int i = 1;

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        DataGridView prevGrid = (DataGridView)Controls.Find("dataGridView" + (i - 1), true)[0];
                        int yLoc = prevGrid.Location.Y + prevGrid.Height + 30;

                        var beamNo = new Label()
                        {
                            Name = "firm" + i,
                            Location = new Point(firm0.Location.X, yLoc),
                            Text = oReader["FIRM"].ToString(),
                            Font = firm0.Font,
                            Size = firm0.Size,
                            ForeColor = firm0.ForeColor
                        };

                        var grid = new DataGridView()
                        {
                            Name = "dataGridView" + i,
                            Size = dataGridView0.Size,
                            BorderStyle = BorderStyle.None,
                            RowHeadersVisible = false,
                            BackgroundColor = dataGridView0.BackgroundColor,
                            AllowUserToAddRows = false,
                            AllowUserToOrderColumns = false,
                            AllowUserToDeleteRows = false,
                            Location = new Point(dataGridView0.Location.X, yLoc + 28),
                            ColumnHeadersDefaultCellStyle = dataGridView0.ColumnHeadersDefaultCellStyle,
                            ColumnHeadersHeight = dataGridView0.ColumnHeadersHeight
                        };

                        populateData(grid, oReader["FIRM"].ToString());

                        Controls.Add(grid);
                        Controls.Add(beamNo);

                        i++;
                    }
                }

                con.Close();
            }
        }

        private void populateData(DataGridView dataGridView, string firm)
        {
            dataGridView.Rows.Clear();
            dataGridView.Refresh();

            dataGridView.ColumnCount = 8;
            dataGridView.Columns[0].Name = "Yarn";
            dataGridView.Columns[1].Name = "Balance";
            dataGridView.Columns[2].Name = "Qty Manufactured last week";
            dataGridView.Columns[3].Name = "Qty Manufactured last to last week";
            dataGridView.Columns[4].Name = "Current Expected Balance";
            dataGridView.Columns[5].Name = "Suggested Supply Qty";
            dataGridView.Columns[6].Name = "Avg weight of last 10 cartons";
            dataGridView.Columns[7].Name = "Suggested Cartons to Supply";

            Dictionary<string, int> yarnIndices = new Dictionary<string, int>();
            int i = 0;

            string firmFilter = "";
            if(firm != null)
            {
                firmFilter = "AND P.FIRM = '" + firm + "'";
            }

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con.Open();

            // BALANCE
            String query = "select tech_name, sum(case when SUPPLY_FROM_TYPE = 'W' and w_name = @WEAVER then -qty else qty end) balance from product p, SUPPLY_CONE sc left outer join weaver w on w.wid = sc.SUPPLY_FROM where ((SUPPLY_FROM_TYPE = 'W' and supply_from in (select wid from weaver where w_name = @WEAVER)) or (SUPPLY_to_TYPE = 'W' and supply_to in (select wid from weaver where w_name = @WEAVER))) and yarn = pid " + firmFilter +" group by tech_name ORDER BY TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string[] row = new string[] { oReader["TECH_NAME"].ToString(), oReader["BALANCE"].ToString(), "0", "0", "0", "0", "0" };
                    dataGridView.Rows.Add(row);
                    yarnIndices.Add(oReader["TECH_NAME"].ToString(), i);
                    i++;
                }
            }

            // W1
            query = "select tech_name, sum(qty) QTY from SUPPLY_CONE sc, product p where SUPPLY_FROM_TYPE = 'W' and supply_from in (select wid from weaver where w_name = @WEAVER) and SUPPLY_TO_TYPE in ('R', 'T') and txn_date between (SELECT DATEADD(day, -8 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and (SELECT DATEADD(day, -2 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and yarn = pid " + firmFilter +" group by tech_name ORDER BY TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WEAVER", comboBox1.Text);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    dataGridView[2, yarnIndices[oReader["TECH_NAME"].ToString()]].Value = oReader["QTY"].ToString();
                }
            }

            // W2
            query = "select tech_name, sum(qty) QTY from SUPPLY_CONE sc, product p where SUPPLY_FROM_TYPE = 'W' and supply_from in (select wid from weaver where w_name = @WEAVER) and SUPPLY_TO_TYPE in ('R', 'T') and txn_date between (SELECT DATEADD(day, -15 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and (SELECT DATEADD(day, -9 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and yarn = pid " + firmFilter + " group by tech_name ORDER BY TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WEAVER", comboBox1.Text);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    dataGridView[3, yarnIndices[oReader["TECH_NAME"].ToString()]].Value = oReader["QTY"].ToString();

                    double balance = Double.Parse(dataGridView[1, yarnIndices[oReader["TECH_NAME"].ToString()]].Value.ToString());
                    double w1 = Double.Parse(dataGridView[2, yarnIndices[oReader["TECH_NAME"].ToString()]].Value.ToString());
                    double w2 = Double.Parse(dataGridView[3, yarnIndices[oReader["TECH_NAME"].ToString()]].Value.ToString());
                }
            }

            // W0
            List<string> currentWeekQualities = new List<string>();

            query = "select tech_name from SUPPLY_CONE sc, product p where SUPPLY_FROM_TYPE = 'W' and supply_from in (select wid from weaver where w_name = @WEAVER) and SUPPLY_TO_TYPE in ('R', 'T') and txn_date between (SELECT DATEADD(day, -8 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and (SELECT DATEADD(day, -2 - (DATEPART(weekday, GETDATE()) + @@DATEFIRST - 2) % 7, GETDATE())) and yarn = pid " + firmFilter;
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WEAVER", comboBox1.Text);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    currentWeekQualities.Add(oReader["TECH_NAME"].ToString());
                }
            }

            foreach (int index in yarnIndices.Values)
            {
                double ob = Double.Parse(dataGridView[1, index].Value.ToString());
                double bal1 = Double.Parse(dataGridView[2, index].Value.ToString());
                double bal2 = Double.Parse(dataGridView[3, index].Value.ToString());
                string quality = dataGridView[0, index].Value.ToString();

                if (!currentWeekQualities.Contains(quality))
                {
                    dataGridView[4, index].Value = AddInvoice.round(ob - Math.Max(bal1, bal2), 3);
                }
                else
                {
                    if (bal1/bal2 < 0.8)
                    {
                        dataGridView[4, index].Value = AddInvoice.round(ob - (bal2 - bal1), 3);
                    }
                    else
                    {
                        dataGridView[4, index].Value = ob;
                    }
                }
                double current = Double.Parse(dataGridView[4, index].Value.ToString());
                double yarnRequired = 0.5 * Math.Max(bal1, bal2) - current;

                String[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", };

                DateTime dt = DateTime.Now;
                String today = dt.DayOfWeek.ToString();
                int day = days.ToList().IndexOf(today);

                if(day >= 3 && day <= 5)
                {
                    yarnRequired = Math.Max(bal1, bal2) - current;
                }

                if (yarnRequired > 0)
                {
                    dataGridView[5, index].Value = AddInvoice.round(yarnRequired, 3);
                }
                else
                {
                    dataGridView[5, index].Value = 0;
                }
            }

            // Suggested Cartons
            query = "";

            foreach (string yarn in yarnIndices.Keys)
            {
                query += "select yarn, round(avg(qty), 3) qty from ( select top 10 tech_name yarn, qty/boxes qty from supply_cone sc, product p where boxes > 0 and p.pid = sc.yarn and SUPPLY_FROM_TYPE = 'G' and tech_name = '" + yarn + "' order by txn_date desc) t group by yarn union ";
            }
            query = query.Substring(0, query.Length - 6);

            oCmd = new SqlCommand(query, con);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    double yarnReq = Double.Parse(dataGridView[5, yarnIndices[oReader["YARN"].ToString()]].Value.ToString());
                    double avgWt = Double.Parse(oReader["QTY"].ToString());
                    int cartons = (int)Math.Ceiling(yarnReq / avgWt);

                    dataGridView[6, yarnIndices[oReader["YARN"].ToString()]].Value = avgWt;
                    dataGridView[7, yarnIndices[oReader["YARN"].ToString()]].Value = cartons;
                }
            }

            GodownStockReport.formatDataGridView(dataGridView, Color.Aquamarine);
            resizeGrid(dataGridView);
            con.Close();
        }

        public void resizeGrid(DataGridView dataGridView1)
        {
            dataGridView1.RowTemplate.Height = 30;

            int totalWidth = 0;

            for (int i = 0; i <= dataGridView1.Columns.Count - 1; i++)
            {
                int colw = dataGridView1.Columns[i].GetPreferredWidth(DataGridViewAutoSizeColumnMode.AllCells, true);
                totalWidth += colw;

                dataGridView1.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                dataGridView1.Columns[i].Width = colw;
            }

            int width = totalWidth + (5 * dataGridView1.ColumnCount / 2);
            int height = 60 + 30 * dataGridView1.RowCount;

            int x = (width > d1W) ? width : d1W;
            int y = (height > d1H) ? d1H : height;

            dataGridView1.Size = new Size(x, y);
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var targetForm = new AggregateCartonStock();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
