using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class StockValue : Form
    {
        string firm;
        byte[] logo;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        Dictionary<string, Dictionary<string, double>> cutsData = new Dictionary<string, Dictionary<string, double>>();
        Dictionary<string, Dictionary<string, double>> yarnData = new Dictionary<string, Dictionary<string, double>>();
        List<string> beams = new List<string>();
        List<string> yarns = new List<string>();
        Dictionary<string, string> weaverIds = new Dictionary<string, string>();
        Dictionary<string, string> godownIds = new Dictionary<string, string>();
        Dictionary<string, double> cutsTotal = new Dictionary<string, double>();
        Dictionary<string, double> conesTotal = new Dictionary<string, double>();
        Dictionary<string, string> conesRequired = new Dictionary<string, string>();
        Dictionary<string, double> conesQty = new Dictionary<string, double>();
        Dictionary<string, double> conesQtyBeam = new Dictionary<string, double>();
        Dictionary<string, double> totalYarnBalance = new Dictionary<string, double>();
        public Dictionary<string, double[]> yarnRates = new Dictionary<string, double[]>();
        public Dictionary<string, double> clothRates = new Dictionary<string, double>();
        int gridCount = 1;
        public static string asOnDate;
        List<int> yarnGridIndices = new List<int>();
        Dictionary<string, double> valuationSummary = new Dictionary<string, double>();
        int panelHeight;

        public StockValue(String firm, Byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void StockValue_Load(object sender, EventArgs e)
        {
            SalaryReport.d1W = dataGridView.Width;
            SalaryReport.d1H = dataGridView.Height;

            DateTime dt = DateTime.Now;
            string dtYear = dt.Year.ToString();
            asOnDate = dt.Day + "-" + dt.Month + "-" + dtYear.Substring(dtYear.Length - 2);

            con.Open();
            // initialize weaver ids

            string query = "select wid, w_name from weaver where firm = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weaverIds.Add(oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            // initialize godown ids

            query = "select gid, g_name from godown where firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godownIds.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            con.Close();

            calculateAndDisplayStockValue();
        }

        private void calculateAndDisplayStockValue()
        {
            // fetch date
            string date = asOnDate;
            int day = Int32.Parse(date.Split('-')[0]);
            int month = Int32.Parse(date.Split('-')[1]);
            string yy = date.Split('-')[2];
            date = day + "-" + month + "-" + yy;

            string year = DateTime.Now.Year.ToString();
            string century = year.Substring(0, year.Length - 2);

            date = date.Replace("-" + month + "-", "-" + months[month - 1] + "-" + century);

            con.Open();

            Dictionary<string, Dictionary<string, Dictionary<string, double>>> supplyToWeaver = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();
            Dictionary<string, Dictionary<string, Dictionary<string, double>>> supplyFromWeaver = new Dictionary<string, Dictionary<string, Dictionary<string, double>>>();

            // initialize cutsData dictionary
            string query = "select txn_date, supply_to_type, supply_to, supply_from_type, supply_from, (select tech_name from product where pid = beam) beam, sum(cuts) cuts from supply_beam where firm = @FIRM and txn_date <= @DATE and cuts <> 0 and     ((supply_to_type = 'W' and supply_from_type IN ('G', 'S', 'O', 'W') AND (EXCESS IS NOT NULL or SUPPLY_FROM_TYPE = 'O')) OR supply_from_type = 'W') group by txn_date, supply_to_type, supply_to, supply_from_type, supply_from, beam";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@DATE", date);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string txnDate = ((DateTime)oReader["TXN_DATE"]).ToString("dd-MM-yyyy");
                    string supplyToType = oReader["SUPPLY_TO_TYPE"].ToString();
                    string supplyFromType = oReader["SUPPLY_FROM_TYPE"].ToString();
                    string supplyFrom = oReader["SUPPLY_FROM"].ToString();
                    string supplyTo = oReader["SUPPLY_TO"].ToString();
                    string beam = oReader["BEAM"].ToString();
                    double cuts = Double.Parse(oReader["CUTS"].ToString());

                    if(supplyToType.Equals("W"))
                    {
                        if (supplyToWeaver.ContainsKey(supplyTo))
                        {
                            Dictionary<string, Dictionary<string, double>> dict1 = supplyToWeaver[supplyTo];

                            if(dict1.ContainsKey(beam))
                            {
                                Dictionary<string, double> dict2 = dict1[beam];

                                if(dict2.ContainsKey(txnDate))
                                {
                                    dict2[txnDate] += cuts;
                                }
                                else
                                {
                                    dict2.Add(txnDate, cuts);
                                }

                                dict1[beam] = dict2;
                                supplyToWeaver[supplyTo] = dict1;
                            }
                            else
                            {
                                Dictionary<string, double> dict2 = new Dictionary<string, double>();
                                dict2.Add(txnDate, cuts);
                                dict1.Add(beam, dict2);

                                supplyToWeaver[supplyTo] = dict1;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> dict1 = new Dictionary<string, double>();
                            dict1.Add(txnDate, cuts);

                            Dictionary<string, Dictionary<string, double>> dict2 = new Dictionary<string, Dictionary<string, double>>();
                            dict2.Add(beam, dict1);

                            supplyToWeaver.Add(supplyTo, dict2);
                        }
                    }

                    if(supplyFromType.Equals("W"))
                    {
                        if (supplyFromWeaver.ContainsKey(supplyFrom))
                        {
                            Dictionary<string, Dictionary<string, double>> dict1 = supplyFromWeaver[supplyFrom];

                            if (dict1.ContainsKey(beam))
                            {
                                Dictionary<string, double> dict2 = dict1[beam];

                                if (dict2.ContainsKey(txnDate))
                                {
                                    dict2[txnDate] += cuts;
                                }
                                else
                                {
                                    dict2.Add(txnDate, cuts);
                                }

                                dict1[beam] = dict2;
                                supplyFromWeaver[supplyFrom] = dict1;
                            }
                            else
                            {
                                Dictionary<string, double> dict2 = new Dictionary<string, double>();
                                dict2.Add(txnDate, cuts);
                                dict1.Add(beam, dict2);

                                supplyFromWeaver[supplyFrom] = dict1;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> dict1 = new Dictionary<string, double>();
                            dict1.Add(txnDate, cuts);

                            Dictionary<string, Dictionary<string, double>> dict2 = new Dictionary<string, Dictionary<string, double>>();
                            dict2.Add(beam, dict1);

                            supplyFromWeaver.Add(supplyFrom, dict2);
                        }
                    }
                    
                    if (!beams.Contains(beam))
                    {
                        beams.Add(beam);
                    }
                    /*
                    if (supplyFromType.Equals("W"))
                    {
                        if (cutsData.ContainsKey(supplyFrom))
                        {
                            Dictionary<string, double> cutsBalance = cutsData[supplyFrom];
                            if (cutsBalance.ContainsKey(beam))
                            {
                                cutsBalance[beam] -= cuts;
                                cutsData[supplyFrom] = cutsBalance;
                            }
                            else
                            {
                                cutsBalance.Add(beam, -cuts);
                                cutsData[supplyFrom] = cutsBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(beam, -cuts);
                            cutsData.Add(supplyFrom, cutsBalance);
                        }
                    }

                    if (supplyToType.Equals("W"))
                    {
                        if (cutsData.ContainsKey(supplyTo))
                        {
                            Dictionary<string, double> cutsBalance = cutsData[supplyTo];
                            if (cutsBalance.ContainsKey(beam))
                            {
                                cutsBalance[beam] += cuts;
                                cutsData[supplyTo] = cutsBalance;
                            }
                            else
                            {
                                cutsBalance.Add(beam, cuts);
                                cutsData[supplyTo] = cutsBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(beam, cuts);
                            cutsData.Add(supplyTo, cutsBalance);
                        }
                    }*/
                }
            }

            foreach(string weaver in supplyToWeaver.Keys)
            {
                foreach(string beam in supplyToWeaver[weaver].Keys)
                {
                    foreach(string txnDate in supplyToWeaver[weaver][beam].Keys)
                    {
                        double totalCuts = AddInvoice.round(supplyToWeaver[weaver][beam][txnDate] * 2) / 2.0;

                        if (cutsData.ContainsKey(weaver))
                        {
                            Dictionary<string, double> cutsBalance = cutsData[weaver];
                            if (cutsBalance.ContainsKey(beam))
                            {
                                cutsBalance[beam] += totalCuts;
                                cutsData[weaver] = cutsBalance;
                            }
                            else
                            {
                                cutsBalance.Add(beam, totalCuts);
                                cutsData[weaver] = cutsBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(beam, totalCuts);
                            cutsData.Add(weaver, cutsBalance);
                        }
                    }
                }
            }

            foreach (string weaver in supplyFromWeaver.Keys)
            {
                foreach (string beam in supplyFromWeaver[weaver].Keys)
                {
                    foreach (string txnDate in supplyFromWeaver[weaver][beam].Keys)
                    {
                        double totalCuts = AddInvoice.round(supplyFromWeaver[weaver][beam][txnDate] * 2) / 2.0;

                        if (cutsData.ContainsKey(weaver))
                        {
                            Dictionary<string, double> cutsBalance = cutsData[weaver];
                            if (cutsBalance.ContainsKey(beam))
                            {
                                cutsBalance[beam] -= totalCuts;
                                cutsData[weaver] = cutsBalance;
                            }
                            else
                            {
                                cutsBalance.Add(beam, -totalCuts);
                                cutsData[weaver] = cutsBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(beam, -totalCuts);
                            cutsData.Add(weaver, cutsBalance);
                        }
                    }
                }
            }

            // initialize yarnData dictionary

            query = "select supply_to_type, supply_to, supply_from_type, supply_from, (select tech_name from product where pid = yarn) yarn, sum(qty) qty from supply_cone where firm = @FIRM and txn_date <= @DATE and (supply_to_type = 'W' or supply_from_type = 'W') group by supply_to_type, supply_to, supply_from_type, supply_from, yarn";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@DATE", date);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string supplyToType = oReader["SUPPLY_TO_TYPE"].ToString();
                    string supplyFromType = oReader["SUPPLY_FROM_TYPE"].ToString();
                    string supplyFrom = oReader["SUPPLY_FROM"].ToString();
                    string supplyTo = oReader["SUPPLY_TO"].ToString();
                    string yarn = oReader["YARN"].ToString();
                    double qty = AddInvoice.round(Double.Parse(oReader["QTY"].ToString()) * 40, 0) / 40;

                    if (!yarns.Contains(yarn))
                    {
                        yarns.Add(yarn);
                    }

                    if (supplyFromType.Equals("W"))
                    {
                        if (yarnData.ContainsKey(supplyFrom))
                        {
                            Dictionary<string, double> yarnBalance = yarnData[supplyFrom];
                            if (yarnBalance.ContainsKey(yarn))
                            {
                                yarnBalance[yarn] -= qty;
                                yarnData[supplyFrom] = yarnBalance;
                            }
                            else
                            {
                                yarnBalance.Add(yarn, -qty);
                                yarnData[supplyFrom] = yarnBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(yarn, -qty);
                            yarnData.Add(supplyFrom, cutsBalance);
                        }
                    }

                    if (supplyToType.Equals("W"))
                    {
                        if (yarnData.ContainsKey(supplyTo))
                        {
                            Dictionary<string, double> yarnBalance = yarnData[supplyTo];
                            if (yarnBalance.ContainsKey(yarn))
                            {
                                yarnBalance[yarn] += qty;
                                yarnData[supplyTo] = yarnBalance;
                            }
                            else
                            {
                                yarnBalance.Add(yarn, qty);
                                yarnData[supplyTo] = yarnBalance;
                            }
                        }
                        else
                        {
                            Dictionary<string, double> cutsBalance = new Dictionary<string, double>();
                            cutsBalance.Add(yarn, qty);
                            yarnData.Add(supplyTo, cutsBalance);
                        }
                    }
                }
            }

            // initialize cone required

            query = "select p1.tech_name PID, P2.TECH_NAME product, pr.qty from product_req pr, product p1, product p2 where p1.pid = pr.pid and p2.pid = pr.product and p1.category = 'Beam' and p2.category = 'Yarn' and pr.firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    conesRequired.Add(oReader["PID"].ToString(), oReader["PRODUCT"].ToString() + "-" + oReader["QTY"].ToString());
                }
            }

            // initialize cloth ratess

            query = "SELECT X.ITEM, X.RATE FROM ( SELECT tech_name ITEM, RATE, 2 RANK FROM ITEM I, product p where p.pid = i.PID_PK and p.CATEGORY = 'Cloth' and p.firm = @FIRM union select tech_name ITEM_ID, '0' RATE, 3 RANK FROM PRODUCT WHERE FIRM = @FIRM AND CATEGORY = 'Cloth') X order by x.item, x.rank";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string item = oReader["ITEM"].ToString();
                    if (!clothRates.ContainsKey(item))
                    {
                        string rate = oReader["RATE"].ToString();
                        if (rate.Trim().Equals(""))
                        {
                            rate = "0";
                        }
                        clothRates.Add(item, Double.Parse(rate));
                    }
                }
            }

            addBeamGrid();
            gridCount++;

            yarnGridIndices.Add(gridCount);
            displayConeSummary();   // weaver warping cone
            gridCount++;
            addConeGrid();
            gridCount++;

            yarnGridIndices.Add(gridCount);
            displayConeSummary2();  // weaver cone balance
            gridCount++;

            // beams in godown

            query = "select distinct supply_FROM godown from supply_beam where firm = @FIRM AND (SUPPLY_FROM_TYPE = 'S' OR SUPPLY_TO_TYPE IS NULL)";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string godown = oReader["GODOWN"].ToString();

                    query = "select supply_to_type, supply_to, supply_from_type, supply_from, (select tech_name from product where pid = beam) beam, sum(cuts) cuts from supply_beam where ((SUPPLY_FROM_TYPE IN ('G', 'S') and SUPPLY_FROM = @GODOWN) or (SUPPLY_TO_TYPE = 'G' and SUPPLY_TO = @GODOWN)) and firm = @FIRM and set_no is not null and txn_date <= @DATE GROUP BY supply_to_type, supply_to, supply_from_type, supply_from, beam";
                    SqlCommand oCmd2 = new SqlCommand(query, con1);
                    oCmd2.Parameters.AddWithValue("@FIRM", firm);
                    oCmd2.Parameters.AddWithValue("@GODOWN", godown);
                    oCmd2.Parameters.AddWithValue("@DATE", date);

                    Dictionary<string, double> godownBeamData = new Dictionary<string, double>();

                    using (SqlDataReader oReader1 = oCmd2.ExecuteReader())
                    {
                        while (oReader1.Read())
                        {
                            string supplyToType = oReader1["SUPPLY_TO_TYPE"].ToString();
                            string supplyFromType = oReader1["SUPPLY_FROM_TYPE"].ToString();
                            string supplyFrom = oReader1["SUPPLY_FROM"].ToString();
                            string supplyTo = oReader1["SUPPLY_TO"].ToString();
                            string beam = oReader1["BEAM"].ToString();
                            double cuts = Double.Parse(oReader1["CUTS"].ToString());

                            if (supplyToType.Equals("G") || supplyFromType.Equals("G"))
                            {
                                if (godownBeamData.ContainsKey(beam))
                                {
                                    godownBeamData[beam] += cuts;
                                }
                                else
                                {
                                    godownBeamData.Add(beam, cuts);
                                }
                            }

                            if (supplyFromType.Equals("S"))
                            {
                                if (godownBeamData.ContainsKey(beam))
                                {
                                    godownBeamData[beam] -= cuts;
                                }
                                else
                                {
                                    godownBeamData.Add(beam, -cuts);
                                }
                            }
                        }
                    }

                    addGodownBeamGrid(godownBeamData, godownIds[godown]);
                    gridCount++;

                    // yarn required for beams in godown
                    yarnGridIndices.Add(gridCount);
                    displayConeSummary3();
                    gridCount++;
                }
            }

            // cones in godown

            query = "select distinct supply_to godown from supply_cone where firm = @FIRM and supply_to_type = 'G' union select distinct supply_from godown from supply_cone where firm = @FIRM and supply_from_type = 'G' union select distinct godown from purchase where firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string godown = oReader["GODOWN"].ToString();

                    query = "select supply_to_type, supply_to, supply_from_type, supply_from, (select tech_name from product where pid = yarn) yarn, sum(qty) qty from supply_cone where ((SUPPLY_FROM_TYPE = 'G' and SUPPLY_FROM = @GODOWN) or (SUPPLY_TO_TYPE = 'G' and SUPPLY_TO = @GODOWN)) and firm = @FIRM and txn_date <= @DATE GROUP BY supply_to_type, supply_to, supply_from_type, supply_from, yarn UNION SELECT 'G', GODOWN, 'P', 0, (select tech_name from product where pid = PRODUCT) yarn, SUM(QTY) FROM PURCHASE WHERE FIRM = @FIRM AND GODOWN = @GODOWN GROUP BY GODOWN, PRODUCT";
                    SqlCommand oCmd2 = new SqlCommand(query, con1);
                    oCmd2.Parameters.AddWithValue("@FIRM", firm);
                    oCmd2.Parameters.AddWithValue("@GODOWN", godown);
                    oCmd2.Parameters.AddWithValue("@DATE", date);

                    Dictionary<string, double> godownYarnData = new Dictionary<string, double>();

                    using (SqlDataReader oReader1 = oCmd2.ExecuteReader())
                    {
                        while (oReader1.Read())
                        {
                            string supplyToType = oReader1["SUPPLY_TO_TYPE"].ToString();
                            string supplyFromType = oReader1["SUPPLY_FROM_TYPE"].ToString();
                            string supplyFrom = oReader1["SUPPLY_FROM"].ToString();
                            string supplyTo = oReader1["SUPPLY_TO"].ToString();
                            string yarn = oReader1["YARN"].ToString();
                            double qty = AddInvoice.round(Double.Parse(oReader1["QTY"].ToString()) * 40, 0) / 40;

                            if (supplyToType.Equals("G"))
                            {
                                if (godownYarnData.ContainsKey(yarn))
                                {
                                    godownYarnData[yarn] += qty;
                                }
                                else
                                {
                                    godownYarnData.Add(yarn, qty);
                                }
                            }

                            if (supplyFromType.Equals("G"))
                            {
                                if (godownYarnData.ContainsKey(yarn))
                                {
                                    godownYarnData[yarn] -= qty;
                                }
                                else
                                {
                                    godownYarnData.Add(yarn, qty);
                                }
                            }
                        }
                    }

                    yarnGridIndices.Add(gridCount);
                    addGodownYarnGrid(godownYarnData, godownIds[godown]);
                    gridCount++;
                }
            }

            // Cloth in godown

            query = "select (select TECH_name from product where pid = quality) quality, sum(mtr) mtr from roll_entry RE where firm = @FIRM AND DESPATCHED = 'N' group by godown, quality union select (select TECH_name from product where pid = quality) quality, sum(mtr) mtr from taka_entry where firm = @FIRM and txn_date <= @DATE group by godown, quality union select TECH_NAME, sum(mtr) from bill_item bi, bill b, item i, PRODUCT P where P.FIRM = @FIRM AND P.PID = I.PID_PK AND b.bill_id = bi.bill_id and b.bill_dt > @DATE AND P.TAKA = 'N' AND B.BILL_DT > '30-SEP-18' and i.item_id = bi.item and godown is not null group by godown, pid_pk, TECH_NAME union select TECH_NAME, -sum(mtr) from bill_item bi, bill b, item i, PRODUCT P where P.PID = I.PID_PK AND b.bill_id = bi.bill_id and b.bill_dt <= @DATE AND P.TAKA = 'Y' AND B.BILL_DT > '30-SEP-18' and i.item_id = bi.item and godown is not null AND B.FIRM = @FIRM group by godown, pid_pk, TECH_NAME";
            SqlCommand oCmd1 = new SqlCommand(query, con1);
            oCmd1.Parameters.AddWithValue("@FIRM", firm);
            oCmd1.Parameters.AddWithValue("@DATE", date);

            Dictionary<string, double> godownClothData = new Dictionary<string, double>();

            using (SqlDataReader oReader1 = oCmd1.ExecuteReader())
            {
                while (oReader1.Read())
                {
                    string quality = oReader1["QUALITY"].ToString();
                    double mtr = AddInvoice.round(2.0*Double.Parse(oReader1["MTR"].ToString()))/2.0;

                    if (godownClothData.ContainsKey(quality))
                    {
                        godownClothData[quality] += mtr;
                    }
                    else
                    {
                        godownClothData.Add(quality, mtr);
                    }
                }
            }

            addGodownClothGrid(godownClothData);
            gridCount++;

            con1.Close();
            con.Close();


            calculateAvg();

            foreach (int i in yarnGridIndices)
            {
                DataGridView dgv = (DataGridView)addCustomer.Controls.Find("dataGridView" + i, true)[0];
                double totalValue = 0;
                for (int j = 0; j < dgv.RowCount; j++)
                {
                    if (j == dgv.RowCount - 1)
                    {
                        dgv[3, j].Value = totalValue.ToString();

                        if (i == 2)
                        {
                            valuationSummary.Add("Worker Beam Stock Value", totalValue);
                        }
                        else if (i == 4)
                        {
                            valuationSummary.Add("Worker Cone Stock Value", totalValue);
                        }
                        else
                        {
                            Label prevLabel;
                            if (addCustomer.Controls.Find("caption" + i, true).Length == 0)
                            {
                                prevLabel = (Label)addCustomer.Controls.Find("caption" + (i - 1), true)[0];
                            }
                            else
                            {
                                prevLabel = (Label)addCustomer.Controls.Find("caption" + i, true)[0];
                            }
                            valuationSummary.Add(prevLabel.Text, totalValue);
                        }
                    }
                    else if (yarnRates.ContainsKey(dgv[0, j].Value.ToString()))
                    {
                        double[] rates = yarnRates[dgv[0, j].Value.ToString()];

                        if (dgv.Name.Contains("4"))
                        {
                            dgv[2, j].Value = (rates[0] + rates[2]).ToString();
                        }
                        else
                        {
                            dgv[2, j].Value = (rates[0] + rates[1] + rates[2]).ToString();
                        }

                        dgv[3, j].Value = (AddInvoice.round(Double.Parse(dgv[2, j].Value.ToString()) * Double.Parse(dgv[1, j].Value.ToString()))).ToString();
                        totalValue += Double.Parse(dgv[3, j].Value.ToString());
                    }
                }
            }
            
            // display final valuation
            displaySummary();
        }
        
        public void populate(Dictionary<string, double[]> ratesDict)
        {
            clearScreen();
            yarnRates = ratesDict;

            conesRequired = new Dictionary<string, string>();
            cutsData = new Dictionary<string, Dictionary<string, double>>();
            yarnData = new Dictionary<string, Dictionary<string, double>>();
            calculateAndDisplayStockValue();
        }

        private void addBeamGrid()
        {
            var label = new Label()
            {
                Name = "caption" + gridCount,
                Location = caption.Location,
                Size = new Size(dataGridView.Width, caption.Height),
                Font = caption.Font,
                Text = caption.Text,
                ForeColor = caption.ForeColor
            };

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = dataGridView.Location
            };

            populate(grid);
            formatDataGridView(grid, Color.Aquamarine);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);
            addCustomer.Controls.Add(label);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
            label.Location = new Point((addCustomer.Width - grid.Width) / 2, label.Location.Y);
        }

        private void addConeGrid()
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var label = new Label()
            {
                Name = "caption" + gridCount,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height),
                Size = new Size(dataGridView.Width, caption.Height),
                Font = caption.Font,
                Text = "Weaver Cone Stock Value",
                ForeColor = caption.ForeColor
            };

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height + label.Height)
            };

            populateCone(grid);
            formatDataGridView(grid, Color.Aquamarine);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);
            addCustomer.Controls.Add(label);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
            label.Location = new Point((addCustomer.Width - grid.Width) / 2, label.Location.Y);
        }

        private void addGodownBeamGrid(Dictionary<string, double> beamData, string godown)
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var label = new Label()
            {
                Name = "caption" + gridCount,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height),
                Size = new Size(dataGridView.Width, caption.Height),
                Font = caption.Font,
                Text = godown + " Beam Stock Value",
                ForeColor = caption.ForeColor
            };

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height + label.Height)
            };

            populateBeam(grid, beamData);
            formatDataGridView(grid, Color.Aquamarine);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);
            addCustomer.Controls.Add(label);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
            label.Location = new Point((addCustomer.Width - grid.Width) / 2, label.Location.Y);
        }

        private void addGodownYarnGrid(Dictionary<string, double> yarnData, string godown)
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var label = new Label()
            {
                Name = "caption" + gridCount,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height),
                Size = new Size(dataGridView.Width, caption.Height),
                Font = caption.Font,
                Text = godown + " Yarn Stock Value",
                ForeColor = caption.ForeColor
            };

            //MessageBox.Show(label.Name);

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height + label.Height)
            };

            addCustomer.Controls.Add(label);
            populateYarn(grid, yarnData);
            formatDataGridView(grid, Color.LightGreen);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
            label.Location = new Point((addCustomer.Width - grid.Width) / 2, label.Location.Y);
        }

        private void addGodownClothGrid(Dictionary<string, double> clothData)
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var label = new Label()
            {
                Name = "caption" + gridCount,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height),
                Size = new Size(dataGridView.Width, caption.Height),
                Font = caption.Font,
                Text = "Godown Cloth Stock Value",
                ForeColor = caption.ForeColor
            };

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height + label.Height)
            };

            addCustomer.Controls.Add(label);
            populateCloth(grid, clothData);
            formatDataGridView(grid, Color.LightGreen);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
            label.Location = new Point((addCustomer.Width - grid.Width) / 2, label.Location.Y);
        }

        private void populate(DataGridView grid)
        {
            grid.ColumnCount = beams.Count + 1;
            grid.Columns[0].Name = "";
            int i = 1;

            foreach (string s in beams)
            {
                grid.Columns[i].Name = s;
                i++;
            }

            foreach (string s in cutsData.Keys)
            {
                Dictionary<string, double> balance = cutsData[s];

                string[] row = new string[beams.Count + 1];
                row[0] = weaverIds[s];
                i = 1;
                foreach (string b in beams)
                {
                    if (balance.ContainsKey(b))
                    {
                        row[i] = AddInvoice.round(2*Double.Parse(balance[b].ToString()))/2.0 + "";
                    }
                    else
                    {
                        row[i] = "0";
                    }

                    if (cutsTotal.ContainsKey(b))
                    {
                        cutsTotal[b] += Double.Parse(row[i]);
                    }
                    else
                    {
                        cutsTotal.Add(b, Double.Parse(row[i]));
                    }

                    i++;
                }

                grid.Rows.Add(row);
            }

            // calculate total row and cones qty required

            string[] totalRow = new string[beams.Count + 1];
            totalRow[0] = "TOTAL";
            i = 1;
            foreach (string s in cutsTotal.Keys)
            {
                totalRow[i] = cutsTotal[s].ToString();
                
                string[] reqCone = conesRequired[s].Split('-');
                string cone = reqCone[0];

                if (conesQty.ContainsKey(cone))
                {
                    conesQty[cone] += cutsTotal[s]*Double.Parse(reqCone[1]);
                }
                else
                {
                    conesQty.Add(cone, cutsTotal[s] * Double.Parse(reqCone[1]));
                }

                i++;
            }
            grid.Rows.Add(totalRow);
        }

        private void populateCone(DataGridView grid)
        {
            grid.ColumnCount = yarns.Count + 1;
            grid.Columns[0].Name = "";
            int i = 1;

            foreach (string s in yarns)
            {
                grid.Columns[i].Name = s;
                i++;
            }

            foreach (string s in yarnData.Keys)
            {
                Dictionary<string, double> balance = yarnData[s];

                string[] row = new string[yarns.Count + 1];
                row[0] = weaverIds[s];
                i = 1;
                foreach (string y in yarns)
                {
                    if (balance.ContainsKey(y))
                    {
                        row[i] = AddInvoice.round(balance[y], 3).ToString();
                    }
                    else
                    {
                        row[i] = "0";
                    }

                    if (conesTotal.ContainsKey(y))
                    {
                        conesTotal[y] += AddInvoice.round(Double.Parse(row[i]), 3);
                    }
                    else
                    {
                        conesTotal.Add(y, Double.Parse(row[i]));
                    }

                    i++;
                }

                grid.Rows.Add(row);
            }

            // calculate total row and cones qty required

            string[] totalRow = new string[yarns.Count + 1];
            totalRow[0] = "TOTAL";
            i = 1;
            foreach (string s in conesTotal.Keys)
            {
                totalRow[i] = conesTotal[s].ToString();
                i++;
            }
            grid.Rows.Add(totalRow);
        }

        private void populateBeam(DataGridView grid, Dictionary<string, double> beamData)
        {
            grid.ColumnCount = 2;
            grid.Columns[0].Name = "";
            grid.Columns[1].Name = "Cuts";

            foreach(string key in beamData.Keys)
            {
                string[] row = new string[] { key, beamData[key].ToString() };
                grid.Rows.Add(row);
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in beamData.Keys)
            {
                totalQty += beamData[s];
                totalAmount += beamData[s] * 100;

                string[] reqCone = conesRequired[s].Split('-');
                string cone = reqCone[0];

                if (conesQtyBeam.ContainsKey(cone))
                {
                    conesQtyBeam[cone] += beamData[s] * Double.Parse(reqCone[1]);
                }
                else
                {
                    conesQtyBeam.Add(cone, beamData[s] * Double.Parse(reqCone[1]));
                }
            }
            string[] totalRow = { "TOTAL", totalQty.ToString() };
            grid.Rows.Add(totalRow);
        }

        private void populateYarn(DataGridView grid, Dictionary<string, double> yarnData)
        {
            grid.ColumnCount = 4;
            grid.Columns[0].Name = "";
            grid.Columns[1].Name = "Qty";
            grid.Columns[2].Name = "Rate";
            grid.Columns[3].Name = "Value";

            foreach (string key in yarnData.Keys)
            {
                double[] rates = { 0, 0, 0 };
                if(!yarnRates.ContainsKey(key))
                {
                    yarnRates.Add(key, rates);
                }
                string[] row = new string[] { key, "" + AddInvoice.round(yarnData[key] * 40, 0) / 40, "100.000", (199.99 * yarnRates[key][0]).ToString() };
                grid.Rows.Add(row);
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in yarnData.Keys)
            {
                totalQty += AddInvoice.round(yarnData[s] * 40, 0) / 40;
                totalAmount += yarnData[s] * 199.99;

                if (totalYarnBalance.ContainsKey(s))
                {
                    totalYarnBalance[s] += AddInvoice.round(yarnData[s] * 40, 0) / 40;
                }
                else
                {
                    totalYarnBalance.Add(s, AddInvoice.round(yarnData[s] * 40, 0) / 40);
                }
            }
            string[] totalRow = { "TOTAL", totalQty.ToString(), "", totalAmount.ToString() };
            grid.Rows.Add(totalRow);
        }

        private void populateCloth(DataGridView grid, Dictionary<string, double> clothData)
        {
            grid.ColumnCount = 4;
            grid.Columns[0].Name = "";
            grid.Columns[1].Name = "Qty";
            grid.Columns[2].Name = "Rate";
            grid.Columns[3].Name = "Value";

            foreach (string key in clothData.Keys)
            {
                string[] row = new string[] { key, clothData[key].ToString(), clothRates[key].ToString(), AddInvoice.round((clothData[key] * clothRates[key])).ToString() };
                grid.Rows.Add(row);
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in clothData.Keys)
            {
                totalQty += clothData[s];
                totalAmount += AddInvoice.round((clothData[s] * clothRates[s]));
            }
            string[] totalRow = { "TOTAL", totalQty.ToString(), "", totalAmount.ToString() };
            grid.Rows.Add(totalRow);

            Label prevLabel = (Label)addCustomer.Controls.Find("caption" + gridCount, true)[0];
            valuationSummary.Add(prevLabel.Text, totalAmount);
        }

        // yarn required for worker's beam
        private void displayConeSummary()
        {
            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, dataGridView.Location.Y + (gridCount - 1) * (addCustomer.Controls.Find("dataGridView1", true)[0]).Height)
            };

            grid.ColumnCount = 4;
            grid.Columns[0].Name = "Cone";
            grid.Columns[1].Name = "Quantity";
            grid.Columns[2].Name = "Rate";
            grid.Columns[3].Name = "Value";

            foreach (string s in conesQty.Keys)
            {
                double[] rates = { 0, 0, 0 };
                if (!yarnRates.ContainsKey(s))
                {
                    yarnRates.Add(s, rates);
                }

                string[] row = { s, AddInvoice.round(40*conesQty[s])/40.0 + "", "100.000", (AddInvoice.round(40 * conesQty[s]) / 40.0 * (199.99 + yarnRates[s][1])).ToString() };
                grid.Rows.Add(row);
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in conesQty.Keys)
            {
                totalQty += conesQty[s];
                totalAmount += conesQty[s] * (199.99 + yarnRates[s][1]);

                if (totalYarnBalance.ContainsKey(s))
                {
                    totalYarnBalance[s] += conesQty[s];
                }
                else
                {
                    totalYarnBalance.Add(s, conesQty[s]);
                }
            }
            string[] totalRow = { "TOTAL", totalQty.ToString(), "", totalAmount.ToString() };
            grid.Rows.Add(totalRow);

            formatDataGridView(grid, Color.LightGreen);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
        }

        // yarn required for beams in godown
        private void displayConeSummary3()
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height)
            };

            grid.ColumnCount = 4;
            grid.Columns[0].Name = "Cone";
            grid.Columns[1].Name = "Quantity";
            grid.Columns[2].Name = "Rate";
            grid.Columns[3].Name = "Value";

            foreach (string s in conesQtyBeam.Keys)
            {
                double[] rates = { 0, 0, 0 };
                if (!yarnRates.ContainsKey(s))
                {
                    yarnRates.Add(s, rates);
                }

                string[] row = { s, conesQtyBeam[s].ToString(), "100.000", (conesQtyBeam[s] * (199.99 + yarnRates[s][1])).ToString() };
                grid.Rows.Add(row);
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in conesQtyBeam.Keys)
            {
                totalQty += conesQtyBeam[s];
                totalAmount += conesQtyBeam[s] * (199.99 + yarnRates[s][1]);

                if (totalYarnBalance.ContainsKey(s))
                {
                    totalYarnBalance[s] += conesQtyBeam[s];
                }
                else
                {
                    totalYarnBalance.Add(s, conesQtyBeam[s]);
                }
            }
            string[] totalRow = { "TOTAL", totalQty.ToString(), "", totalAmount.ToString() };
            grid.Rows.Add(totalRow);

            formatDataGridView(grid, Color.LightGreen);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
        }

        // yarn with weaver
        private void displayConeSummary2()
        {
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (gridCount - 1), true)[0];

            var grid = new DataGridView()
            {
                Name = "dataGridView" + gridCount,
                Size = new Size(dataGridView.Width, dataGridView.Height),
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                Visible = true,
                BackgroundColor = Color.White,
                AllowUserToAddRows = false,
                AllowUserToOrderColumns = false,
                AllowUserToDeleteRows = false,
                Location = new Point(dataGridView.Location.X, prevGrid.Location.Y + prevGrid.Height)
            };

            grid.ColumnCount = 4;
            grid.Columns[0].Name = "Cone";
            grid.Columns[1].Name = "Quantity";
            grid.Columns[2].Name = "Rate";
            grid.Columns[3].Name = "Value";

            foreach (string s in conesTotal.Keys)
            {
                double[] rates = { 0, 0, 0 };
                if (!yarnRates.ContainsKey(s))
                {
                    yarnRates.Add(s, rates);
                }

                string[] row = { s, conesTotal[s].ToString(), "100.000", (conesTotal[s] * 199.99).ToString() };
                grid.Rows.Add(row);

                if(totalYarnBalance.ContainsKey(s))
                {
                    totalYarnBalance[s] += conesTotal[s];
                }
                else
                {
                    totalYarnBalance.Add(s, conesTotal[s]);
                }
            }

            // calculate total row

            double totalAmount = 0;
            double totalQty = 0;
            foreach (string s in conesTotal.Keys)
            {
                totalQty += conesTotal[s];
                totalAmount += conesTotal[s] * 199.99;
            }
            string[] totalRow = { "TOTAL", totalQty.ToString(), "", totalAmount.ToString() };
            grid.Rows.Add(totalRow);

            formatDataGridView(grid, Color.LightGreen);
            SalaryReport.resizeGrid(grid);
            addCustomer.Controls.Add(grid);

            grid.Location = new Point((addCustomer.Width - grid.Width) / 2, grid.Location.Y);
        }

        public static void formatDataGridView(DataGridView dataGridView1, Color color)
        {
            dataGridView1.RowsDefaultCellStyle.BackColor = color;
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.Sunken;

            dataGridView1.RowsDefaultCellStyle.SelectionBackColor = color;
            dataGridView1.RowsDefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView1.DefaultCellStyle.Font = new Font(dataGridView1.DefaultCellStyle.Font.FontFamily, 10);
            Boolean flag = true;

            foreach (DataGridViewColumn c in dataGridView1.Columns)
            {
                c.Width = dataGridView1.Width / dataGridView1.ColumnCount - 4;
                if(flag)
                {
                    c.DefaultCellStyle.Font = new Font(dataGridView1.DefaultCellStyle.Font, FontStyle.Bold);
                    flag = false;
                }
            }
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            new StockValueFilter(firm, totalYarnBalance, this, yarnRates).Show();
        }

        private void calculateAvg()
        {
            Dictionary<string, double> totalYarnQty = new Dictionary<string, double>();
            Dictionary<string, double> totalYarnAmt = new Dictionary<string, double>();

            con.Open();
            string query = "select (select tech_name from product where pid = product) product, qty, cast((bill_amt + freight)/qty as decimal(10,3)) rate, (bill_amt + freight) bill_amt from purchase where firm = @FIRM order by txn_date DESC";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string product = oReader["PRODUCT"].ToString();

                    if (yarnRates[product][0] == 0)
                    {
                        double ta = Double.Parse(oReader["BILL_AMT"].ToString());
                        double tq = Double.Parse(oReader["QTY"].ToString());
                        double rate = AddInvoice.round(ta / tq, 2);
                        double[] rates = { rate, 0, 0 };

                        yarnRates[product] = rates;
                        totalYarnQty[product] = tq;
                        totalYarnAmt[product] = ta;
                    }
                    else
                    {
                        if (!totalYarnQty.ContainsKey(product))
                        {
                            totalYarnQty.Add(product, Double.Parse(oReader["QTY"].ToString()));
                            totalYarnAmt.Add(product, Double.Parse(oReader["BILL_AMT"].ToString()));
                        }
                        double qty = totalYarnQty[product];
                        double[] rates = yarnRates[product];

                        //MessageBox.Show(product + " : " + qty + ", " + totalYarnBalance[product]);
                        if (qty < totalYarnBalance[product])
                        {
                            double ta = totalYarnAmt[product] + Double.Parse(oReader["BILL_AMT"].ToString());
                            double tq = totalYarnQty[product] + Double.Parse(oReader["QTY"].ToString());
                            rates[0] = AddInvoice.round(ta / tq, 2);

                            yarnRates[product] = rates;
                            totalYarnQty[product] += Double.Parse(oReader["QTY"].ToString());
                            totalYarnAmt[product] += Double.Parse(oReader["BILL_AMT"].ToString());
                        }
                    }
                }
            }

            con.Close();
        }

        public void clearScreen()
        {
            for(int i=0; i < gridCount; i++)
            {
                Control[] grids = addCustomer.Controls.Find("dataGridView" + i, true);
                Control[] captions = addCustomer.Controls.Find("caption" + i, true);

                if(grids.Length > 0)
                {
                    addCustomer.Controls.Remove(grids[0]);
                }

                if (captions.Length > 0)
                {
                    addCustomer.Controls.Remove(captions[0]);
                }
            }

            for (int i = gridCount; i < captionCount; i++)
            {
                Control[] captions = addCustomer.Controls.Find("caption" + i, true);
                
                if (captions.Length > 0)
                {
                    addCustomer.Controls.Remove(captions[0]);
                }
            }

            gridCount = 1;
            captionCount = 0;
            cutsData = new Dictionary<string, Dictionary<string, double>>();
            yarnData = new Dictionary<string, Dictionary<string, double>>();
            List<string> beams = new List<string>();
            List<string> yarns = new List<string>();
            cutsTotal = new Dictionary<string, double>();
            conesTotal = new Dictionary<string, double>();
            conesQty = new Dictionary<string, double>();
            conesQtyBeam = new Dictionary<string, double>();
            totalYarnBalance = new Dictionary<string, double>();
            yarnGridIndices = new List<int>();
            valuationSummary = new Dictionary<string, double>();
            yarnRates = new Dictionary<string, double[]>();
        }

        int captionCount;

        private void displaySummary()
        {
            captionCount = gridCount;
            DataGridView prevGrid = (DataGridView)addCustomer.Controls.Find("dataGridView" + (captionCount - 1), true)[0];
            int y = prevGrid.Location.Y + prevGrid.Height + 15;
            double totalValue = 0;

            foreach(string s in valuationSummary.Keys)
            {
                var label = new Label()
                {
                    Name = "caption" + captionCount,
                    Location = new Point(192, y),
                    Size = new Size(300, caption.Height),
                    Font = caption.Font,
                    Text = s,
                    ForeColor = Color.DarkOrange
                };
                captionCount++;

                var colon = new Label()
                {
                    Name = "caption" + captionCount,
                    Location = new Point(492, y),
                    Size = new Size(15, caption.Height),
                    Font = caption.Font,
                    Text = ":",
                    ForeColor = Color.Black
                };
                captionCount++;

                var valueLabel = new Label()
                {
                    Name = "caption" + captionCount,
                    Location = new Point(600, y),
                    Size = new Size(300, caption.Height),
                    Font = caption.Font,
                    Text = valuationSummary[s].ToString(),
                    ForeColor = Color.DarkGreen
                };
                captionCount++;

                y += caption.Height + 5;
                totalValue += valuationSummary[s];
                addCustomer.Controls.Add(label);
                addCustomer.Controls.Add(colon);
                addCustomer.Controls.Add(valueLabel);
            }

            // total value

            y += 10;
            var labelT = new Label()
            {
                Name = "caption" + captionCount,
                Location = new Point(192, y),
                Size = new Size(300, total.Height),
                Font = total.Font,
                Text = "TOTAL",
                ForeColor = Color.DarkBlue
            };
            captionCount++;

            var colonT = new Label()
            {
                Name = "caption" + captionCount,
                Location = new Point(492, y),
                Size = new Size(15, total.Height),
                Font = total.Font,
                Text = ":",
                ForeColor = Color.Black
            };
            captionCount++;

            var valueLabelT = new Label()
            {
                Name = "caption" + captionCount,
                Location = new Point(600, y),
                Size = new Size(300, total.Height),
                Font = total.Font,
                Text = "Rs. " + AddInvoice.round(totalValue) + "/-",
                ForeColor = Color.DarkRed
            };
            captionCount++;

            y += caption.Height;
            addCustomer.Controls.Add(labelT);
            addCustomer.Controls.Add(colonT);
            addCustomer.Controls.Add(valueLabelT);
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

        private void pictureBox27_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox24_Click(object sender, EventArgs e)
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

        Boolean lastPage;
        int indexNo = 1;

        private void printDocument1_PrintPage(object sender, System.Drawing.Printing.PrintPageEventArgs e)
        {
            // print plain text
            Graphics graphic = e.Graphics;
            SolidBrush brush = new SolidBrush(Color.Red);

            Font font = new Font("Arial", 12, FontStyle.Bold);

            e.PageSettings.PaperSize = new PaperSize("A4", 827, 1169);

            float pageWidth = e.PageSettings.PrintableArea.Width;
            float pageHeight = e.PageSettings.PrintableArea.Height;

            float fontHeight = font.GetHeight();

            int startY = 100;
            int offsetY = 40;

            //firm
            SizeF stringSize = new SizeF();
            stringSize = e.Graphics.MeasureString(firm, font);
            int stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

            if (indexNo == 1)
            {
                graphic.DrawString(firm, font, brush, stringCenterX, 70);
                graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 70 + (int)stringSize.Height));
                graphic.DrawLine(new Pen(brush), new Point((int)pageWidth / 2 - (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height), new Point((int)pageWidth / 2 + (int)(stringSize.Width) / 2, 73 + (int)stringSize.Height));

                font = new Font("Arial", 10, FontStyle.Bold);
                brush = new SolidBrush(Color.Black);
                // stock report
                stringSize = e.Graphics.MeasureString("Stock Report", font);
                stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

                graphic.DrawString("Stock Report", font, brush, stringCenterX, 100);

                // as on
                font = new Font("Arial", 8);
                stringSize = e.Graphics.MeasureString("Stock as on : " + asOnDate, font);
                stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

                graphic.DrawString("Stock as on : " + asOnDate, font, brush, stringCenterX, 117);
            }
            else
            {
                startY = 0;
            }

            // grids

            if (!lastPage)
            {
                while (indexNo < gridCount)
                {
                    DataGridView dgv = (DataGridView)Controls.Find("dataGridView" + indexNo, true)[0];
                    Control[] lbl = (Control[])Controls.Find("caption" + indexNo, true);

                    if (lbl.Length > 0)
                    {
                        font = new Font("Arial", 8, FontStyle.Bold);
                        stringSize = e.Graphics.MeasureString(((Label)(lbl[0])).Text, font);
                        stringCenterX = (int)pageWidth / 2 - (int)(stringSize.Width) / 2;

                        brush = new SolidBrush(Color.Blue);
                        graphic.DrawString(((Label)(lbl[0])).Text, font, brush, 30, startY + offsetY);
                    }

                    brush = new SolidBrush(Color.Red);

                    offsetY += 20;
                    int[] headerX = new int[dgv.ColumnCount];

                    int locX = 130;
                    if(!dgv.Columns[0].HeaderText.Equals(""))
                    {
                        locX = 30;
                    }
                    font = new Font("Arial", 8, FontStyle.Bold);
                    for (int j = 0; j < dgv.ColumnCount; j++)
                    {
                        stringSize = e.Graphics.MeasureString(dgv.Columns[j].HeaderText, font);
                        graphic.DrawString(dgv.Columns[j].HeaderText, font, brush, locX, startY + offsetY);
                        headerX[j] = locX;
                        locX += ((int)stringSize.Width + 35);

                        if(indexNo == 1)
                        {
                            locX -= 20;
                        }
                    }
                    offsetY += ((int)font.GetHeight() + 3);

                    for (int j = 0; j < dgv.RowCount; j++)
                    {
                        for (int k = 0; k < dgv.ColumnCount; k++)
                        {
                            if (k == 0)
                            {
                                font = new Font("Arial", 8, FontStyle.Bold);
                                if (j == (dgv.RowCount - 1))
                                {
                                    brush = new SolidBrush(Color.DarkRed);
                                }
                                else
                                {
                                    brush = new SolidBrush(Color.Green);
                                }

                                graphic.DrawString(dgv[k, j].Value.ToString(), font, brush, 30, startY + offsetY);
                            }
                            else
                            {
                                if (j == (dgv.RowCount - 1))
                                {
                                    font = new Font("Arial", 8, FontStyle.Bold);
                                    brush = new SolidBrush(Color.DarkCyan);
                                }
                                else
                                {
                                    font = new Font("Arial", 8);
                                    brush = new SolidBrush(Color.Black);
                                }
                                graphic.DrawString(dgv[k, j].Value.ToString(), font, brush, headerX[k], startY + offsetY);
                            }

                        }
                        offsetY += (int)font.GetHeight();
                    }
                    
                    int nextHeight = offsetY + 20;
                    if (indexNo < (gridCount - 1))
                    {
                        font = new Font("Arial", 8, FontStyle.Bold);
                        nextHeight += ((int)font.GetHeight() + 3);
                        nextHeight += (int)font.GetHeight() * ((DataGridView)Controls.Find("dataGridView" + (indexNo + 1), true)[0]).RowCount;
                    }
                    else
                    {
                        font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Underline);
                        stringSize = e.Graphics.MeasureString("Summary", font);
                        nextHeight += ((int)stringSize.Height + 20);

                        font = new Font("Arial", 9);
                        for (int i = indexNo; i < captionCount; i = i + 3)
                        {
                            String text1 = ((Label)Controls.Find("caption" + i, true)[0]).Text;
                            stringSize = e.Graphics.MeasureString(text1, font);
                            if (i >= (captionCount - 3))
                            {
                                nextHeight += 5;
                            }

                            nextHeight += (int)stringSize.Height;
                        }
                    }

                    nextHeight += (70 + (int)font.GetHeight());

                    if (nextHeight > pageHeight)
                    {
                        e.HasMorePages = true;
                        indexNo++;
                        return;
                    }
                    else
                    {
                        offsetY += ((int)font.GetHeight() + 10);
                        indexNo++;
                    }
                }

                offsetY += 20;
            }
            else
            {
                startY = 0;
                offsetY = 20;
            }
            

            int newtHeight = offsetY + 20;
            int maxLabelSize = 0;

            font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Underline);
            stringSize = e.Graphics.MeasureString("Summary", font);
            newtHeight += ((int)stringSize.Height + 20);

            font = new Font("Arial", 9);
            for (int i = indexNo; i<captionCount; i = i + 3)
            {
                String text1 = ((Label)Controls.Find("caption" + i, true)[0]).Text;
                stringSize = e.Graphics.MeasureString(text1, font);
                if (i >= (captionCount - 3))
                {
                    newtHeight += 5;
                }

                if (stringSize.Width > maxLabelSize)
                {
                    maxLabelSize = (int)stringSize.Width;
                }

                newtHeight += (int)stringSize.Height;
            }

            newtHeight += 40;
            if(newtHeight > pageHeight)
            {
                e.HasMorePages = true;
                lastPage = true;
                return;
            }

            font = new Font("Arial", 12, FontStyle.Bold | FontStyle.Underline);
            stringSize = e.Graphics.MeasureString("Summary", font);
            graphic.DrawString("Summary", font, brush, pageWidth / 2 - (int)stringSize.Width/2, startY + offsetY);
            offsetY += (20 + (int)stringSize.Height);

            font = new Font("Arial", 9);
            for (int i = indexNo; i < captionCount; i = i + 3)
            {
                String text1 = ((Label)Controls.Find("caption" + i, true)[0]).Text;
                stringSize = e.Graphics.MeasureString(text1, font);
                brush = new SolidBrush(Color.DarkOrange);
                if (i >= (captionCount - 3))
                {
                    brush = new SolidBrush(Color.DarkBlue);
                    font = new Font("Arial", 10, FontStyle.Bold);
                    offsetY += 5;
                }

                graphic.DrawString(text1, font, brush, pageWidth/2 - 30 - maxLabelSize, startY + offsetY);

                brush = new SolidBrush(Color.Black);
                graphic.DrawString(":", font, brush, pageWidth / 2, startY + offsetY);

                if (i >= (captionCount - 3))
                {
                    brush = new SolidBrush(Color.Red);
                }
                else
                {
                    brush = new SolidBrush(Color.DarkGreen);
                }
                String text2 = ((Label)Controls.Find("caption" + (i + 2), true)[0]).Text;
                graphic.DrawString(text2, font, brush, pageWidth/2 + 30, startY + offsetY);

                offsetY += (int) stringSize.Height;
            }
            lastPage = false;
            indexNo = 1;

            // print screen

            //SolidBrush brush = new SolidBrush(Color.Black);

            //Font font = new Font("Courier New", 12);

            /*e.PageSettings.PaperSize = new PaperSize("A4", 850, 1100);

            float pageWidth = e.PageSettings.PrintableArea.Width;
            float pageHeight = e.PageSettings.PrintableArea.Height;

            //float fontHeight = font.GetHeight();
            int startX = 0;
            int startY = 0;
            int offsetY = 0;

            Bitmap bmp = new Bitmap(2480, 3508);
            int incrementY = addCustomer.Height - SystemInformation.HorizontalScrollBarHeight;

            int l1 = addCustomer.Width;
            int l2 = dataGridView.Location.X + dataGridView.Width;

            while (offsetY + (addCustomer.Height - SystemInformation.HorizontalScrollBarHeight) < pageHeight)
            {
                addCustomer.DrawToBitmap(bmp, new Rectangle(0, 0, addCustomer.Width - SystemInformation.VerticalScrollBarWidth, addCustomer.Height - SystemInformation.HorizontalScrollBarHeight));
                if (offsetY == 0)
                {
                    e.Graphics.ScaleTransform(0.84f, 0.84f);
                }
                e.Graphics.DrawImage(bmp, startX, offsetY);
                offsetY += (addCustomer.Height - SystemInformation.HorizontalScrollBarHeight);

                addCustomer.VerticalScroll.Value += (addCustomer.Height - 2*SystemInformation.HorizontalScrollBarHeight - 3);
                panelHeight += incrementY;
            }

            //graphic.DrawString("Line: " + i, font, brush, startX, startY + offsetY);
            //offsetY += (int)fontHeight;
            
            if ((addCustomer.VerticalScroll.Value + incrementY) <= addCustomer.VerticalScroll.Maximum)
            {
                //MessageBox.Show(addCustomer.VerticalScroll.Value + " + " + incrementY + " / " + addCustomer.VerticalScroll.Maximum);
                addCustomer.VerticalScroll.Value += (addCustomer.Height - SystemInformation.HorizontalScrollBarHeight);
                panelHeight += incrementY;
                if(alternateFlag)
                {
                    addCustomer.VerticalScroll.Value += (addCustomer.Height - SystemInformation.HorizontalScrollBarHeight);
                    panelHeight += 2*incrementY;
                    alternateFlag = false;
                }
                else
                {
                    alternateFlag = true;
                }
                e.HasMorePages = true;
                return;
            }
            else
            {
                e.HasMorePages = false;
                addCustomer.VerticalScroll.Value = 0;
            }*/
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void printPreviewDialog1_Load(object sender, EventArgs e)
        {

        }
    }
}
