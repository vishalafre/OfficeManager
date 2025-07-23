using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class TallyXML : Form
    {
        public static string firm;

        Dictionary<string, string> states = new Dictionary<string, string>
        {
            { "01", "Jammu & Kashmir" },
            { "02", "Himachal Pradesh" },
            { "03", "Punjab" },
            { "04", "Chandigarh" },
            { "05", "Uttarakhand" },
            { "06", "Haryana" },
            { "07", "Delhi" },
            { "08", "Rajasthan" },
            { "09", "Uttar Pradesh" },
            { "10", "Bihar" },
            { "11", "Sikkim" },
            { "12", "Arunachal Pradesh" },
            { "13", "Nagaland" },
            { "14", "Manipur" },
            { "15", "Mizoram" },
            { "16", "Tripura" },
            { "17", "Meghalaya" },
            { "18", "Assam" },
            { "19", "West Bengal" },
            { "20", "Jharkhand" },
            { "21", "Orissa" },
            { "22", "Chhattisgarh" },
            { "23", "Madhya Pradesh" },
            { "24", "Gujarat" },
            { "25", "Daman & Diu" },
            { "26", "Dadra & Nagar Haveli" },
            { "27", "Maharashtra" },
            { "28", "Andhra Pradesh (Old)" },
            { "29", "Karnataka" },
            { "30", "Goa" },
            { "31", "Lakshadweep" },
            { "32", "Kerala" },
            { "33", "Tamil Nadu" },
            { "34", "Puducherry" },
            { "35", "Andaman & Nicobar Islands" },
            { "36", "Telengana" },
            { "37", "Andhra Pradesh (New)" }
        };

        public TallyXML(string company)
        {
            InitializeComponent();
            firm = company;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String data = textBox1.Text.ToUpper();
            String input = "(";
            Boolean singleBill = false;

            String[] parts;
            if (data.Contains(","))
            {
                parts = data.Split(',');

                foreach (String s in parts)
                {
                    if (s.Contains(":"))
                    {
                        string p1 = TallyXML.parseBillIds(s);
                        input += p1;
                    }
                    else
                    {
                        int n;
                        if (Int32.TryParse(s.Trim(), out n))
                        {
                            string billNo = s.Trim();
                            string sDate = DateTime.Now.ToString();
                            DateTime datevalue = (Convert.ToDateTime(sDate.ToString()));
                            int month = Int32.Parse(datevalue.Month.ToString());

                            int year = Int32.Parse(datevalue.Year.ToString().Substring(datevalue.Year.ToString().Length - 2)) - 1;
                            if (month > 3)
                            {
                                year++;
                            }

                            string yearInit = year + "-" + (year + 1);

                            String compInit;
                            switch (firm.Substring(0, 1))
                            {
                                case "A":
                                    compInit = "AE";
                                    break;

                                case "E":
                                    compInit = "ET";
                                    break;

                                default:
                                    compInit = "XX";
                                    break;
                            }

                            String billId = "" + billNo;
                            if ((billNo + "").Length == 1)
                            {
                                billId = "00" + billNo;
                            }
                            else if ((billNo + "").Length == 2)
                            {
                                billId = "0" + billNo;
                            }

                            string invNo = compInit + "/" + yearInit + "/" + billId;
                            input += "'" + invNo + "', ";
                        }
                        else
                        {
                            input += "'" + s.Trim() + "', ";
                        }
                    }
                }
            }
            else
            {
                if (data.Contains(":"))
                {
                    string p1 = TallyXML.parseBillIds(data);
                    input += p1;
                }
                else if(data.Contains(","))
                {
                    input += "'" + data.Trim() + "', ";
                } else
                {
                    singleBill = true;

                    string billNo = data.Trim();
                    string sDate = DateTime.Now.ToString();
                    DateTime datevalue = (Convert.ToDateTime(sDate.ToString()));
                    int month = Int32.Parse(datevalue.Month.ToString());

                    int year = Int32.Parse(datevalue.Year.ToString().Substring(datevalue.Year.ToString().Length - 2)) - 1;
                    if (month > 3)
                    {
                        year++;
                    }

                    string yearInit = year + "-" + (year + 1);

                    String compInit;
                    switch (firm.Substring(0, 1))
                    {
                        case "A":
                            compInit = "AE";
                            break;

                        case "E":
                            compInit = "ET";
                            break;

                        default:
                            compInit = "XX";
                            break;
                    }

                    String billId = "" + billNo;
                    if ((billNo + "").Length == 1)
                    {
                        billId = "00" + billNo;
                    }
                    else if ((billNo + "").Length == 2)
                    {
                        billId = "0" + billNo;
                    }

                    string invNo = compInit + "/" + yearInit + "/" + billId;
                    input += "'" + invNo + "'";
                }
            }

            string output;
            if (singleBill)
            {
                output = input + ")";
            } 
            else
            {
                output = input.Substring(0, input.Length - 2) + ")";
            }

            string mainFile = File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\tally_xml_template.xml");
            string cgstFile = File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\cgst_xml_template.txt");
            string igstFile = File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\igst_xml_template.txt");
            string itemFile = File.ReadAllText(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\item_template.xml");

            string messages = "";
            int c = 0;
            string itemString = "";

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

            string query = "select distinct cast(b.bill_dt as varchar) bill_dt, c1.GSTIN, c1.TALLY_LEDGER " +
                "PARTY_NAME, b.BILL_ID, b.LR_NO, c2.CITY, c2.GSTIN SUPPLY_GSTIN, b.bill_amt, CGST_AMT, " +
                "SGST_AMT, IGST_AMT, b.DISCOUNT, (select sum(qty)/count(qty) from bill_item where bill_id = b.bill_id) " +
                "cnt, (round(bill_amt,0) - (NET_AMT + CGST_AMT + SGST_AMT + IGST_AMT)) round_off, (select  " +
                "stuff(list,1,1,'') from (select  ',' + cast(ROLL_NO as varchar(16)) as [text()] from " +
                "BILL_ITEM WHERE BILL_ID = B.BILL_ID AND ITEM = BI.ITEM for xml path('')) as Sub(list)) " +
                "ROLL_NO, i.TALLY_LEDGER ITEM_NAME, BI.RATE, i.TALLY_UNIT, b.NET_AMT, " +
                "(SELECT SUM(MTR) FROM BILL_ITEM WHERE BILL_ID = B.BILL_ID and ITEM = BI.ITEM) MTR2, (SELECT SUM(MTR) " +
                "FROM BILL_ITEM WHERE BILL_ID = B.BILL_ID) mtr, T_NAME, LOT_NO, OS_CLASS, OS_LEDGER, " +
                "LS_CLASS, LS_LEDGER, TC.CGST CL, TC.SGST SL, TC.IGST IL, TC.ROUND_OFF RL from bill b, " +
                "customer c1, customer c2, bill_item bi, item i, transport t, tally_configure tc " +
                "where b.transporter = t.tid and b.firm = tc.firm and b.bill_to = c1.cid and b.ship_to = " +
                "c2.cid and b.bill_id = bi.bill_id and bi.ITEM = i.ITEM_ID and b.bill_id in "+ output +" " +
                "AND B.FIRM = @FIRM order by b.bill_id";

            string prevBillId = "";
            bool isBillIdSame = false;  // to check if prev bill id is same
            bool updateFound = false;   // if prev bill id found same
            bool lastUpdatePending = false;     // to check if single bill item updated

            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    c++;

                    string date = oReader["BILL_DT"].ToString().Replace("-", "");
                    string gstIN = oReader["GSTIN"].ToString();
                    string supplyGstIN = oReader["SUPPLY_GSTIN"].ToString();

                    string toState = states[gstIN.Substring(0, 2)];
                    string supplyState = states[supplyGstIN.Substring(0, 2)];

                    string partyName = oReader["PARTY_NAME"].ToString();
                    string billId = oReader["BILL_ID"].ToString();
                    if(billId.Equals(prevBillId))
                    {
                        isBillIdSame = true;
                    } 
                    else
                    {
                        isBillIdSame = false;
                    }
                    prevBillId = billId;

                    string lrNo = oReader["LR_NO"].ToString();
                    string city = oReader["CITY"].ToString();
                    string billAmt = oReader["bill_amt"].ToString();
                    string cgst = oReader["CGST_AMT"].ToString();
                    string sgst = oReader["SGST_AMT"].ToString();
                    string igst = oReader["IGST_AMT"].ToString();
                    string roundOff = oReader["ROUND_OFF"].ToString();
                    string count = oReader["CNT"].ToString();

                    string rollNo = parseRollNos(oReader["ROLL_NO"].ToString(), count);

                    string itemName = oReader["ITEM_NAME"].ToString();
                    string tallyUnit = oReader["TALLY_UNIT"].ToString();
                    double netAmtActual = Double.Parse(oReader["NET_AMT"].ToString());
                    double qty = AddInvoice.round(Double.Parse(oReader["MTR"].ToString()), 2);
                    double qtyIndividual = Double.Parse(oReader["MTR2"].ToString());
                    double rate = AddInvoice.round(netAmtActual/qty, 2);
                    double rateActual = Double.Parse(oReader["RATE"].ToString());
                    double discountPer = Double.Parse(oReader["DISCOUNT"].ToString());
                    string netAmt = Math.Round(rateActual* qtyIndividual * (100-discountPer)/100, 2) + "";
                    if(discountPer > 0)
                    {
                        netAmt = netAmtActual + "";
                    }

                    string tName = oReader["T_NAME"].ToString();
                    string lotNo = oReader["LOT_NO"].ToString();
                    string saleClassOS = oReader["OS_CLASS"].ToString();
                    string saleLedgerOS = oReader["OS_LEDGER"].ToString();
                    string saleClassLS = oReader["LS_CLASS"].ToString();
                    string saleLedgerLS = oReader["LS_LEDGER"].ToString();
                    string cgstLedger = oReader["CL"].ToString();
                    string sgstLedger = oReader["SL"].ToString();
                    string igstLedger = oReader["IL"].ToString();
                    string roundLedger = oReader["RL"].ToString();

                    if (partyName.Equals(""))
                    {
                        con.Close();
                        MessageBox.Show("Tally Ledger for customer not updated for Bill ID : " + billId);
                        return;
                    }

                    if (itemName.Equals(""))
                    {
                        con.Close();
                        MessageBox.Show("Tally Ledger for item not updated for Bill ID : " + billId);
                        return;
                    }

                    if (tallyUnit.Equals(""))
                    {
                        con.Close();
                        MessageBox.Show("Tally Unit for item not updated for Bill ID : " + billId);
                        return;
                    }

                    if (Double.Parse(cgst) != 0)
                    {
                        messages+= cgstFile.Replace("##--LOT_NO_HERE--##", lotNo)
                            .Replace("##--DATE_HERE--##", date)
                            .Replace("##--PARTY_GSTIN_HERE--##", gstIN)
                            .Replace("##--PARTY_NAME_HERE--##", partyName)
                            .Replace("##--BILL_NO_HERE--##", billId)
                            .Replace("##--BILL_AMT_HERE--##", billAmt)
                            .Replace("##--SGST_AMT_HERE--##", sgst)
                            .Replace("##--CGST_AMT_HERE--##", cgst)
                            .Replace("##--ITEM_NAME_HERE--##", itemName)
                            .Replace("##--RATE_HERE--##", rate + "")
                            .Replace("##--ITEM_UNIT_HERE--##", tallyUnit)
                            .Replace("##--NET_AMT_HERE--##", netAmt)
                            .Replace("##--ITEM_QTY_HERE--##", qty + "")
                            .Replace("##--ROUND_OFF_HERE--##", roundOff)
                            .Replace("##--SUPPLY_CITY_HERE--##", city)
                            .Replace("##--CLASS_NAME_HERE--##", saleClassLS)
                            .Replace("##--SGST_LEDGER_HERE--##", sgstLedger)
                            .Replace("##--CGST_LEDGER_HERE--##", cgstLedger)
                            .Replace("##--ROUND_OFF_LEDGER_HERE--##", roundLedger)
                            .Replace("##--SALE_LEDGER_HERE--##", saleLedgerLS)
                            .Replace("##--STATE_NAME_HERE--##", toState)
                            .Replace("##--POS_HERE--##", supplyState);
                    }
                    else
                    {
                        if (isBillIdSame)
                        {
                            itemString += itemFile.Replace("##--ROLL_NOS_HERE--##", rollNo)
                                .Replace("##--ITEM_NAME_HERE--##", itemName)
                                .Replace("##--RATE_HERE--##", rate + "")
                                .Replace("##--ITEM_UNIT_HERE--##", tallyUnit)
                                .Replace("##--NET_AMT_HERE--##", netAmt)
                                .Replace("##--SALE_LEDGER_HERE--##", saleLedgerOS)
                                .Replace("##--ITEM_QTY_HERE--##", qtyIndividual + "");

                            updateFound = true;
                        }
                        else
                        {
                            if (updateFound)
                            {
                                messages = messages.Replace("##--ITEM_DETAILS_HERE--##", itemString);
                            }
                            else
                            {
                                if(lastUpdatePending)
                                {
                                    messages = messages.Replace("##--ITEM_DETAILS_HERE--##", itemString);
                                }
                                lastUpdatePending = true;
                            }

                            messages += igstFile.Replace("##--DATE_HERE--##", date)
                                .Replace("##--PARTY_GSTIN_HERE--##", gstIN)
                                .Replace("##--PARTY_NAME_HERE--##", partyName)
                                .Replace("##--BILL_NO_HERE--##", billId)
                                .Replace("##--BILL_AMT_HERE--##", billAmt)
                                .Replace("##--IGST_AMT_HERE--##", igst)
                                .Replace("##--LR_NO_HERE--##", lrNo)

                                //.Replace("##--ITEM_DETAILS_HERE--##", itemString)

                                .Replace("##--ROUND_OFF_HERE--##", roundOff)
                                .Replace("##--SUPPLY_CITY_HERE--##", city)
                                .Replace("##--CLASS_NAME_HERE--##", saleClassOS)
                                .Replace("##--ROUND_OFF_LEDGER_HERE--##", roundLedger)
                                .Replace("##--IGST_LEDGER_HERE--##", igstLedger)
                                .Replace("##--TRANSPORTER_HERE--##", tName)
                                .Replace("##--STATE_NAME_HERE--##", toState)
                                .Replace("##--POS_HERE--##", supplyState);

                            itemString = itemFile.Replace("##--ROLL_NOS_HERE--##", rollNo)
                                .Replace("##--ITEM_NAME_HERE--##", itemName)
                                .Replace("##--RATE_HERE--##", rate + "")
                                .Replace("##--ITEM_UNIT_HERE--##", tallyUnit)
                                .Replace("##--NET_AMT_HERE--##", netAmt)
                                .Replace("##--SALE_LEDGER_HERE--##", saleLedgerOS)
                                .Replace("##--ITEM_QTY_HERE--##", qtyIndividual + "");

                            updateFound = false;
                        }
                    }
                }
            }
            con.Close();

            if (messages.Contains("##--ITEM_DETAILS_HERE--##"))
            {
                messages = messages.Replace("##--ITEM_DETAILS_HERE--##", itemString);
            }

            string finalXML = mainFile.Replace("##--FIRM_HERE--##", firm).Replace("##--TALLY_MSG_HERE--##", messages);
            if (c > 0)
            {
                File.WriteAllText(@"C:\Invoices\tally.xml", finalXML);
                MessageBox.Show("XML file generated on path C:\\Invoices\\tally.xml");
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Bill ID(s)");
            }
        }

        public static String parseBillIds(String data)
        {
            string output = "";
            string[] parts = data.Split(':');

            int num1;
            if(!Int32.TryParse(parts[0].Trim(), out num1)) {
                num1 = Int32.Parse(parts[0].Trim().Split('/')[2]);
            }

            int num2;
            if (!Int32.TryParse(parts[1].Trim(), out num2))
            {
                num2 = Int32.Parse(parts[1].Trim().Split('/')[2]);
            }

            // get prefix

            string sDate = DateTime.Now.ToString();
            DateTime datevalue = (Convert.ToDateTime(sDate.ToString()));
            int month = Int32.Parse(datevalue.Month.ToString());

            int year = Int32.Parse(datevalue.Year.ToString().Substring(datevalue.Year.ToString().Length - 2)) - 1;
            if (month > 3)
            {
                year++;
            }

            string yearInit = year + "-" + (year + 1);

            String compInit;
            switch (firm.Substring(0, 1))
            {
                case "A":
                    compInit = "AE";
                    break;

                case "E":
                    compInit = "ET";
                    break;

                default:
                    compInit = "XX";
                    break;
            }

            string prefix = compInit + "/" + yearInit + "/";

            //string prefix = parts[0].Split('-')[0].Trim();

            for(int i = num1; i<=num2; i++)
            {
                string billId = i + "";
                if ((i + "").Length == 1)
                {
                    billId = "00" + i;
                }
                else if ((i + "").Length == 2)
                {
                    billId = "0" + i;
                }
                /*
                string numPrefix = "";
                if (i < 100)
                {
                    int n = (3 - i.ToString().Length) * 10;
                    numPrefix = n.ToString().Substring(1);
                }*/
                output += "'" + prefix + billId + "', ";
            }

            return output;
        }

        private String parseRollNos(String data, string count)
        {
            String output = "";
            int min = 99999;
            int max = -1;
            if(data.Contains(","))
            {
                String[] parts = data.Split(',');
                foreach(String s in parts)
                {
                    int n;
                    if (int.TryParse(s.Trim(), out n))
                    {
                        int x = Int32.Parse(s.Trim());
                        if (x < min)
                        {
                            min = x;
                        }
                        if (x > max)
                        {
                            max = x;
                        }
                    }
                    else
                    {
                        min = -1;
                        max = -1;
                    }
                }
            }
            else
            {
                if (count.Equals("1"))
                {
                    return "Roll No " + data;
                }
                else
                {
                    return "Bale No " + data;
                }
            }

            if (min != -1)
            {
                if (min == max)
                {
                    output = min + "";
                }
                else
                {
                    output = min + " to " + max;
                }
            }
            else
            {
                return "";
            }

            string type;
            if(count.Equals("1"))
            {
                type = "Roll No ";
            }
            else
            {
                type = "Bale No ";
            }
            return type + output;
        }

        private void TallyXML_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            AcceptButton = button1;
        }
    }
}
