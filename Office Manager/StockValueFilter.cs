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
    public partial class StockValueFilter : Form
    {
        string firm;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, double[]> yarnRates = new Dictionary<string, double[]>();
        Dictionary<string, double[]> yarnRatesFetched = new Dictionary<string, double[]>();
        double[] zero = { 0, 0, 0 };
        Dictionary<string, double> totalYarnBalance = new Dictionary<string, double>();
        StockValue sv;
        Dictionary<string, bool> waterMarkActive = new Dictionary<string, bool>();
        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        public StockValueFilter(string firm, Dictionary<string, double> totalYarnBalance, StockValue sv, Dictionary<string, double[]> yarnRts)
        {
            InitializeComponent();
            this.firm = firm;
            this.totalYarnBalance = totalYarnBalance;
            this.sv = sv;
            this.yarnRatesFetched = yarnRts;
        }

        private void StockValueFilter_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;
            textBox4.Text = StockValue.asOnDate;
            setTextboxWatermark(textBox4);

            calculateAvg();
            con.Open();

            // fetch all yarns

            string query = "select distinct tech_name from product where category = 'Yarn' and firm = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            int index = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(!yarnRates.ContainsKey(oReader["TECH_NAME"].ToString()))
                    {
                        yarnRates.Add(oReader["TECH_NAME"].ToString(), zero);
                    }

                    string rate1 = "0";
                    string rate2 = "0";
                    string rate3 = "0";

                    string key = oReader["TECH_NAME"].ToString();
                    if (yarnRatesFetched.ContainsKey(key))
                    {
                        rate1 = yarnRatesFetched[key][0].ToString();
                        rate2 = yarnRatesFetched[key][1].ToString();
                        rate3 = yarnRatesFetched[key][2].ToString();
                    }

                    var yLabel = new Label()
                    {
                        Name = "yarn" + index,
                        Location = new Point(yarn.Location.X, yarn.Location.Y + 25 * index),
                        Size = yarn.Size,
                        Text = oReader["TECH_NAME"].ToString(),
                        Font = yarn.Font
                    };

                    var aBox = new TextBox()
                    {
                        Name = "avg" + index,
                        Location = new Point(avg.Location.X, avg.Location.Y + 25 * index),
                        Size = avg.Size,
                        Text = rate1
                    };

                    var wBox = new TextBox()
                    {
                        Name = "warp" + index,
                        Location = new Point(warp.Location.X, warp.Location.Y + 25 * index),
                        Size = warp.Size,
                        Text = rate2
                    };

                    var fBox = new TextBox()
                    {
                        Name = "freight" + index,
                        Location = new Point(freight.Location.X, freight.Location.Y + 25 * index),
                        Size = freight.Size,
                        Text = rate3
                    };

                    panel1.Controls.Add(yLabel);
                    panel1.Controls.Add(aBox);
                    panel1.Controls.Add(wBox);
                    panel1.Controls.Add(fBox);

                    index++;
                }
            }

            con.Close();
        }

        private void setTextboxWatermark(TextBox textBox)
        {
            waterMarkActive.Add(textBox.Name, true);
            textBox.ForeColor = Color.Gray;
            Boolean flag = false;
            if (textBox.Text.Equals("") || textBox.Text.Equals("dd-mm-yy"))
            {
                textBox.Text = "dd-mm-yy";
                flag = true;
            }

            textBox.GotFocus += (source, e) =>
            {
                if (waterMarkActive[textBox.Name])
                {
                    waterMarkActive[textBox.Name] = false;
                    if (flag)
                    {
                        textBox.Text = "";
                    }
                    textBox.ForeColor = Color.Black;
                }
            };

            textBox.LostFocus += (source, e) =>
            {
                if (!waterMarkActive[textBox.Name] && string.IsNullOrEmpty(textBox.Text))
                {
                    waterMarkActive[textBox.Name] = true;
                    textBox.Text = "dd-mm-yy";
                    textBox.ForeColor = Color.Gray;
                }
            };
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

                    if (!yarnRates.ContainsKey(product))
                    {
                        double ta = Double.Parse(oReader["BILL_AMT"].ToString());
                        double tq = Double.Parse(oReader["QTY"].ToString());
                        double rate = AddInvoice.round(ta / tq, 2);
                        double[] rates = { rate, 0, 0};

                        yarnRates.Add(product, rates);
                        totalYarnQty.Add(product, 0);
                        totalYarnAmt.Add(product, 0);
                    }
                    else
                    {
                        double qty = totalYarnQty[product];
                        double[] rates = yarnRates[product];
                        
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

        private void button1_Click(object sender, EventArgs e)
        {
            for(int i=0; i<yarnRates.Count; i++)
            {
                Label yarnL = (Label)panel1.Controls.Find("yarn" + i, true)[0];
                TextBox avgT = (TextBox)panel1.Controls.Find("avg" + i, true)[0];
                TextBox warpT = (TextBox)panel1.Controls.Find("warp" + i, true)[0];
                TextBox freightT = (TextBox)panel1.Controls.Find("freight" + i, true)[0];

                double[] ratesA = { Double.Parse(avgT.Text), Double.Parse(warpT.Text), Double.Parse(freightT.Text) };
                yarnRates[yarnL.Text] = ratesA;
            }
            
            StockValue.asOnDate = textBox4.Text;
            sv.populate(yarnRates);
            Close();
        }
    }
}
