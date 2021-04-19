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
    public partial class SalarySummary : Form
    {
        Dictionary<string, string> qMtrs;
        Dictionary<string, string> qRates;
        Dictionary<string, double> balance = new Dictionary<string, double>();
        Dictionary<string, double> balanceWithPid = new Dictionary<string, double>();
        Dictionary<string, string> productIds = new Dictionary<string, string>();

        int looms;
        String weaver;
        String firm;
        String fromDt;
        String toDt;
        String[] labelNames = {"sNo", "quality", "taka", "rate", "value", "tp", "netSalary", "tds", "payable", "cgst", "sgst"};
        int totalValue;
        int weftCount;
        CalculateSalary cs;
        double tdsWeaver;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public SalarySummary(String firm, Dictionary<string, string> qMtrs, Dictionary<string, string> qRates, double tdsWeaver, int looms, String weaver, String fromDt, String toDt, CalculateSalary cs)
        {
            InitializeComponent();
            this.firm = firm;
            this.qMtrs = qMtrs;
            this.qRates = qRates;
            this.tdsWeaver = tdsWeaver;
            this.looms = looms;
            this.weaver = weaver;
            this.fromDt = fromDt;
            this.toDt = toDt;
            this.cs = cs;
        }

        private void SalarySummary_Load(object sender, EventArgs e)
        {
            if(cs.entryId != -1)
            {
                button1.Visible = true;
            }

            CenterToScreen();
            int i;
            for (i = 0; i < qMtrs.Count; i++)
            {
                if (i > 0)
                {
                    for(int j = 0; j < 5; j++)
                    {
                        addLabel((Label) panel1.Controls.Find(labelNames[j] + "0", true)[0], i, panel1, false, j);
                    }
                }
                
                panel1.Controls.Find("sNo" + i, true)[0].Text = (i + 1) + "";
                panel1.Controls.Find("quality" + i, true)[0].Text = qMtrs.Keys.ToList()[i];
                panel1.Controls.Find("taka" + i, true)[0].Text = qMtrs[panel1.Controls.Find("quality" + i, true)[0].Text];
                panel1.Controls.Find("rate" + i, true)[0].Text = qRates[panel1.Controls.Find("quality" + i, true)[0].Text];
                panel1.Controls.Find("value" + i, true)[0].Text = Math.Floor(Double.Parse(panel1.Controls.Find("taka" + i, true)[0].Text) * Double.Parse(panel1.Controls.Find("rate" + i, true)[0].Text)) + "";
                totalValue += Int32.Parse(panel1.Controls.Find("value" + i, true)[0].Text);
            }

            i = qMtrs.Count;
            for (int j = 0; j < labelNames.Length; j++)
            {
                if (j == 1 || j == 2 || j == 3)
                    continue;

                addLabel((Label)panel1.Controls.Find(labelNames[j] + "0", true)[0], i, panel1, true, j);
            }

            panel1.Controls.Find("sNo" + i, true)[0].Text = "TOTAL";
            panel1.Controls.Find("sNo" + i, true)[0].Size = new Size(51, 16);
            panel1.Controls.Find("value" + i, true)[0].Text = totalValue + "";

            int netAmt = Int32.Parse(panel1.Controls.Find("value" + i, true)[0].Text) - looms;
            int adjustment = totalValue % 10;

            if (adjustment > 5)
            {
                adjustment = adjustment - 10;
            }

            panel1.Controls.Find("tp" + i, true)[0].Text = looms + "";
            panel1.Controls.Find("netSalary" + i, true)[0].Text = (Int32.Parse(panel1.Controls.Find("value" + i, true)[0].Text) - Int32.Parse(panel1.Controls.Find("tp" + i, true)[0].Text)) + "";
            panel1.Controls.Find("tds" + i, true)[0].Text = tdsWeaver*AddInvoice.round(Double.Parse(panel1.Controls.Find("netSalary" + i, true)[0].Text) / 100.00) + "";
            panel1.Controls.Find("payable" + i, true)[0].Text = (Int32.Parse(panel1.Controls.Find("netSalary" + i, true)[0].Text) - Int32.Parse(panel1.Controls.Find("tds" + i, true)[0].Text)) + "";
            panel1.Controls.Find("cgst" + i, true)[0].Text = (Double.Parse(panel1.Controls.Find("netSalary" + i, true)[0].Text) / 40.00) + "";
            panel1.Controls.Find("sgst" + i, true)[0].Text = panel1.Controls.Find("cgst" + i, true)[0].Text;

            findWeaverBalance();
            displayWeaverBalance();
        }

        private void findWeaverBalance()
        {
            String products = "(";
            int count = 0;
            foreach (KeyValuePair<string, string> entry in qMtrs)
            {
                if(count > 0)
                {
                    products += ",'" + entry.Key + "'";
                }
                else
                {
                    products += "'" + entry.Key + "'";
                }
                count++;
            }
            products += ")";

            con.Open();
            String query = "SELECT P.PID, PRODUCT, P.TECH_NAME, QTY FROM PRODUCT_REQ PR, PRODUCT P, PRODUCT P1 WHERE P.PID = PR.PID AND P1.PID = PR.PRODUCT AND P1.CATEGORY = 'Yarn' and p.firm = @firm AND P.TECH_NAME IN "+ products;
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WID", weaver);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    // SET PIDs TO BE USED IN SALARY_DETAIL TABLE

                    if (productIds.Keys.ToList().IndexOf(oReader["TECH_NAME"].ToString()) == -1)
                    {
                        productIds.Add(oReader["TECH_NAME"].ToString(), oReader["PID"].ToString());
                    }

                    // FIND CONE USED PER QUALITY

                    double unitQty = Double.Parse(oReader["QTY"].ToString());
                    double totalQty = unitQty * Double.Parse(qMtrs[oReader["TECH_NAME"].ToString()]);

                    if (balance.Keys.ToList().IndexOf(oReader["PRODUCT"].ToString()) != -1)
                    {
                        balance[oReader["PRODUCT"].ToString()] += totalQty;
                    }
                    else
                    {
                        balance.Add(oReader["PRODUCT"].ToString(), totalQty);
                    }
                }
            }

            con.Close();
        }

        private void addLabel(Label source, int position, Panel p, bool bold)
        {
            var lbl = new Label()
            {
                Name = source.Name.Replace("0", "" + position),
                Location = new Point(source.Location.X, source.Location.Y + position * 20),
                Font = source.Font,
                ForeColor = source.ForeColor,
                Text = source.Text,
                Size = source.Size
            };

            if(source.Name.StartsWith("balance"))
            {
                lbl.Size = new Size(83, 16);
            }

            if(bold)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            }

            p.Controls.Add(lbl);
        }

        private void addLabel(Label source, int position, Panel p, bool bold, int idIndex)
        {
            var lbl = new Label()
            {
                Name = source.Name.Replace("0", "" + position),
                Location = new Point(source.Location.X, source.Location.Y + position * 20),
                Font = source.Font,
                ForeColor = source.ForeColor,
                Text = source.Text,
                Size = ((Label) panel1.Controls.Find("label" + idIndex, true)[0]).Size
            };

            if (source.Name.StartsWith("balance"))
            {
                lbl.Size = new Size(83, 16);
            }

            if (bold)
            {
                lbl.Font = new Font(lbl.Font, FontStyle.Bold);
            }

            p.Controls.Add(lbl);
        }

        private void displayWeaverBalance()
        {
            con.Open();
            String query = "SELECT YARN PID, TECH_NAME, SUM(BALANCE) BALANCE FROM (select YARN, ISNULL(SUM(QTY), 0) BALANCE from SUPPLY_CONE WHERE SUPPLY_TO = @WID AND SUPPLY_TO_TYPE = 'W' AND FIRM = @FIRM GROUP BY YARN UNION  select YARN, ISNULL(-SUM(QTY), 0) BALANCE from SUPPLY_CONE WHERE SUPPLY_FROM = @WID AND SUPPLY_from_TYPE = 'W' AND FIRM = @FIRM GROUP BY YARN) X, PRODUCT P WHERE X.YARN = P.PID GROUP BY YARN, TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WID", weaver);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            int count = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(count > 0)
                    {
                        addLabel(weft0, count, panel2, false);
                        addLabel(balance0, count, panel2, false);
                        weftCount++;
                    }

                    panel2.Controls.Find("weft" + count, true)[0].Text = oReader["TECH_NAME"].ToString() + "          ";
                    panel2.Controls.Find("balance" + count, true)[0].Text = oReader["BALANCE"].ToString();

                    count++;
                }
            }
            con.Close();
        }

        private void save0_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd;
            if (cs.entryId != -1)
            {
                // delete salary detail
                cmd = new SqlCommand("DELETE FROM SALARY_DETAIL WHERE SUMMARY_ID = @SUMMARY_ID", con);
                cmd.Parameters.AddWithValue("@SUMMARY_ID", cs.entryId);

                cmd.ExecuteNonQuery();

                // delete salary summary
                cmd = new SqlCommand("DELETE FROM SALARY_SUMMARY WHERE ENTRY_ID = @ENTRY_ID", con);
                cmd.Parameters.AddWithValue("@ENTRY_ID", cs.entryId);

                cmd.ExecuteNonQuery();
            }

            // INSERT IN SALARY_SUMMARY

            cmd = new SqlCommand("INSERT INTO SALARY_SUMMARY (FIRM, WEAVER, FROM_DATE, TO_DATE, TOTAL_VALUE, TP, NET_SALARY, TDS, PAYABLE_SALARY, CGST, SGST) VALUES (@FIRM, @WEAVER, @FROM_DATE, @TO_DATE, @TOTAL_VALUE, @TP, @NET_SALARY, @TDS, @PAYABLE_SALARY, @CGST, @SGST)", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@WEAVER", weaver);
            cmd.Parameters.AddWithValue("@FROM_DATE", fromDt);
            cmd.Parameters.AddWithValue("@TO_DATE", toDt);
            cmd.Parameters.AddWithValue("@TOTAL_VALUE", totalValue);
            cmd.Parameters.AddWithValue("@TP", panel1.Controls.Find("tp" + qMtrs.Count, true)[0].Text);
            cmd.Parameters.AddWithValue("@NET_SALARY", panel1.Controls.Find("netSalary" + qMtrs.Count, true)[0].Text);
            cmd.Parameters.AddWithValue("@TDS", panel1.Controls.Find("tds" + qMtrs.Count, true)[0].Text);
            cmd.Parameters.AddWithValue("@PAYABLE_SALARY", panel1.Controls.Find("payable" + qMtrs.Count, true)[0].Text);
            cmd.Parameters.AddWithValue("@CGST", panel1.Controls.Find("cgst" + qMtrs.Count, true)[0].Text);
            cmd.Parameters.AddWithValue("@SGST", panel1.Controls.Find("sgst" + qMtrs.Count, true)[0].Text);

            cmd.ExecuteNonQuery();

            // INSERT IN SALARY_DETAIL

            for(int i = 0; i < qMtrs.Count; i++)
            {
                cmd = new SqlCommand("INSERT INTO SALARY_DETAIL (FIRM, SUMMARY_ID, QUALITY, TAKA, RATE) VALUES (@FIRM, (select max(ENTRY_ID) FROM SALARY_SUMMARY), @QUALITY, @TAKA, @RATE)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@QUALITY", productIds[panel1.Controls.Find("quality" + i, true)[0].Text]);
                cmd.Parameters.AddWithValue("@TAKA", panel1.Controls.Find("taka" + i, true)[0].Text);
                cmd.Parameters.AddWithValue("@RATE", panel1.Controls.Find("rate" + i, true)[0].Text);
                
                cmd.ExecuteNonQuery();
            }

            con.Close();
            cs.changeWeaver();
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // delete salary detail
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM SALARY_DETAIL WHERE SUMMARY_ID = @SUMMARY_ID", con);
            cmd.Parameters.AddWithValue("@SUMMARY_ID", cs.entryId);

            cmd.ExecuteNonQuery();

            // delete salary SUMMARY
            cmd = new SqlCommand("DELETE FROM SALARY_SUMMARY WHERE ENTRY_ID = @ENTRY_ID", con);
            cmd.Parameters.AddWithValue("@ENTRY_ID", cs.entryId);

            cmd.ExecuteNonQuery();

            cs.entryId = -1;
            cs.cb3.Enabled = true;
            cs.dtp1.Enabled = true;
            cs.dtp2.Enabled = true;
            cs.changeWeaver();

            con.Close();
            MessageBox.Show("Transaction Discarded");
            Close();
        }
    }
}
