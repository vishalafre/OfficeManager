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
    public partial class GenerateJSON : Form
    {
        string firm;

        public GenerateJSON(string firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void GenerateJSON_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            AcceptButton = button1;
        }

        private void button1_Click(object sender, EventArgs e)
        {/*
            if(!textBox1.Text.Contains("-"))
            {
                MessageBox.Show("Please provide valid Bill IDs");
                return;
            }*/

            String data = textBox1.Text.ToUpper();
            /*String input = "(";

            String[] parts;
            if (data.Contains(","))
            {
                parts = data.Split(',');

                foreach (String s in parts)
                {
                    if (s.Contains(":"))
                    {
                        TallyXML.firm = firm;
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
                    TallyXML.firm = firm;
                    string p1 = TallyXML.parseBillIds(data);
                    input += p1;
                }
                else
                {
                    input += "'" + data.Trim() + "', ";
                }
            }
            input = input.Substring(0, input.Length - 2) + ")";*/
            string input = formatBillIds(data);

            int count = 0;
            string output = "{\n" +
                        "\t\"version\":\"1.0.0621\",\n" +
                        "\t\t\"billLists\":[";
            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

            string query = "SELECT DISTINCT C.CID BILL_TO, C2.CID SHIP_TO, F.GSTIN FROM_GSTIN, B.BILL_ID, CONVERT(VARCHAR(10), B.BILL_DT, 103) BILL_DT, F.CITY, F.PIN FROM_PIN, C.GSTIN TO_GSTIN, C2.GSTIN ACTUAL_TO_GSTIN, C2.CITY TO_CITY, C.CNAME, (SELECT TOP 1 * FROM SPLIT((SELECT ADDRESS FROM CUSTOMER C1 WHERE C1.CID = C2.CID), '-') ORDER BY 1) TO_PIN, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, B.BILL_AMT, C2.DISTANCE, T.T_NAME, T.TRANS_ID, I.ITEM_NAME, I.DESCR, I.HSN, (SELECT SUM(BI1.MTR) FROM BILL_ITEM BI1 WHERE BI1.BILL_ID = B.BILL_ID) QTY, (SELECT TOP 1 * FROM SPLIT((SELECT UNIT FROM ITEM I1 WHERE I1.ITEM_ID = I.ITEM_ID), '-')) UNIT, B.NET_AMT, B.CGST, B.SGST, B.ISGT FROM BILL B, CUSTOMER C, COMPANY F, TRANSPORT T, ITEM I, BILL_ITEM BI, CUSTOMER C2 WHERE C.CID = B.BILL_TO AND C2.CID = B.SHIP_TO AND F.NAME = C.FIRM AND B.TRANSPORTER = T.TID AND I.ITEM_ID = BI.ITEM AND BI.BILL_ID = B.BILL_ID AND F.NAME = @FIRM AND UPPER(B.BILL_ID) IN " + input;
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    count++;
                    string userGst = oReader["FROM_GSTIN"].ToString();
                    string billNo = oReader["BILL_ID"].ToString();
                    string billDt = oReader["BILL_DT"].ToString();

                    string transType = "1";
                    if(!oReader["BILL_TO"].ToString().Equals(oReader["SHIP_TO"].ToString())) {
                        transType = "2";
                    }

                    string city = oReader["CITY"].ToString();
                    string toCity = oReader["TO_CITY"].ToString();
                    string pin = oReader["FROM_PIN"].ToString();
                    string toGst = oReader["TO_GSTIN"].ToString();
                    string actualToGst = oReader["ACTUAL_TO_GSTIN"].ToString();
                    string custName = oReader["CNAME"].ToString();
                    string toPin = oReader["TO_PIN"].ToString().Trim();

                    int n;
                    if(!int.TryParse(toPin, out n))
                    {
                        MessageBox.Show("Customer PIN Code missing for Bill ID : " + billNo);
                        con.Close();
                        return;
                    }
                    
                    string cgstAmt = oReader["CGST_AMT"].ToString();
                    string sgstAmt = oReader["SGST_AMT"].ToString();
                    string igstAmt = oReader["IGST_AMT"].ToString();
                    string billAmt = oReader["BILL_AMT"].ToString();
                    string distance = oReader["DISTANCE"].ToString();
                    
                    if (!int.TryParse(distance, out n))
                    {
                        MessageBox.Show("Transport distance missing for Bill ID : " + billNo);
                        con.Close();
                        return;
                    }

                    string tName = oReader["T_NAME"].ToString();
                    string tId = oReader["TRANS_ID"].ToString();

                    if (tId == null || tId.Equals(""))
                    {
                        MessageBox.Show("Transport ID missing for Bill ID : " + billNo);
                        con.Close();
                        return;
                    }

                    string product = oReader["ITEM_NAME"].ToString();
                    string pDesc = oReader["DESCR"].ToString();

                    if (pDesc == null || pDesc.Equals(""))
                    {
                        MessageBox.Show("Product Description missing for Bill ID : " + billNo);
                        con.Close();
                        return;
                    }

                    string hsn = oReader["HSN"].ToString();
                    string quantity = oReader["QTY"].ToString();
                    string unit = oReader["UNIT"].ToString();
                    string netAmt = oReader["NET_AMT"].ToString();
                    string cgst = oReader["CGST"].ToString();
                    string sgst = oReader["SGST"].ToString();
                    string igst = oReader["ISGT"].ToString();

                    output += "{\n" +
                        "\t\t\t\"userGstin\":\"" + userGst + "\",\n" +
                        "\t\t\t\"supplyType\":\"O\",\n" +
                        "\t\t\t\"subSupplyType\":1,\n" +
                        "\t\t\t\"subSupplyDesc\":\"\",\n" +
                        "\t\t\t\"docType\":\"INV\",\n" +
                        "\t\t\t\"docNo\":\"" + billNo + "\",\n" +
                        "\t\t\t\"docDate\":\"" + billDt + "\",\n" +
                        "\t\t\t\"transType\":"+ transType +",\n" +
                        "\t\t\t\"fromGstin\":\"" + userGst + "\",\n" +
                        "\t\t\t\"fromTrdName\":\"" + firm + "\",\n" +
                        "\t\t\t\"fromAddr1\":\"\",\n" +
                        "\t\t\t\"fromAddr2\":\"\",\n" +
                        "\t\t\t\"fromPlace\":\"" + city + "\",\n" +
                        "\t\t\t\"fromPincode\":" + pin + ",\n" +
                        "\t\t\t\"fromStateCode\":" + userGst.Substring(0, 2) + ",\n" +
                        "\t\t\t\"actualFromStateCode\":" + userGst.Substring(0, 2) + ",\n" +
                        "\t\t\t\"toGstin\":\"" + toGst + "\",\n" +
                        "\t\t\t\"toTrdName\":\""+ custName +"\",\n" +
                        "\t\t\t\"toAddr1\":\"\",\n" +
                        "\t\t\t\"toAddr2\":\"\",\n" +
                        "\t\t\t\"toPlace\":\""+ toCity +"\",\n" +
                        "\t\t\t\"toPincode\":" + toPin + ",\n" +
                        "\t\t\t\"toStateCode\":" + Int32.Parse(toGst.Substring(0, 2)) + ",\n" +
                        "\t\t\t\"actualToStateCode\":" + Int32.Parse(actualToGst.Substring(0, 2)) + ",\n" +
                        "\t\t\t\"totalValue\":" + netAmt + ",\n" +
                        "\t\t\t\"cgstValue\":" + cgstAmt + ",\n" +
                        "\t\t\t\"sgstValue\":" + sgstAmt + ",\n" +
                        "\t\t\t\"igstValue\":" + igstAmt + ",\n" +
                        "\t\t\t\"cessValue\":0,\n" +
                        "\t\t\t\"TotNonAdvolVal\":0,\n" +
                        "\t\t\t\"OthValue\":0,\n" +
                        "\t\t\t\"totInvValue\":" + billAmt + ",\n" +
                        "\t\t\t\"transMode\":1,\n" +
                        "\t\t\t\"transDistance\":" + distance + ",\n" +
                        "\t\t\t\"transporterName\":\"" + tName + "\",\n" +
                        "\t\t\t\"transporterId\":\"" + tId + "\",\n" +
                        "\t\t\t\"transDocNo\":\"\",\n" +
                        "\t\t\t\"transDocDate\":\"" + billDt + "\",\n" +
                        "\t\t\t\"vehicleNo\":\"\",\n" +
                        "\t\t\t\"vehicleType\":\"\",\n" +
                        "\t\t\t\"mainHsnCode\":" + hsn + ",\n" +
                        "\t\t\t\t\"itemList\":[{\n" +
                        "\t\t\t\t\t\"itemNo\":1,\n" +
                        "\t\t\t\t\t\"productName\":\"" + product + "\",\n" +
                        "\t\t\t\t\t\"productDesc\":\"" + pDesc + "\",\n" +
                        "\t\t\t\t\t\"hsnCode\":" + hsn + ",\n" +
                        "\t\t\t\t\t\"quantity\":" + quantity + ",\n" +
                        "\t\t\t\t\t\"qtyUnit\":\"" + unit + "\",\n" +
                        "\t\t\t\t\t\"taxableAmount\":" + netAmt + ",\n" +
                        "\t\t\t\t\t\"sgstRate\":" + sgst + ",\n" +
                        "\t\t\t\t\t\"cgstRate\":" + cgst + ",\n" +
                        "\t\t\t\t\t\"igstRate\":" + igst + ",\n" +
                        "\t\t\t\t\t\"cessRate\":0,\n" +
                        "\t\t\t\t\t\"cessNonAdvol\":0\n" +
                        "\t\t\t\t}]\n" +
                        "},\n";
                }
                output = output.Substring(0, output.Length - 2) + "]}";
            }

            con.Close();

            if(count > 0)
            {
                System.IO.File.WriteAllText(@"C:\Invoices\eWayBill.json", output);
                MessageBox.Show("JSON file generated on path C:\\Invoices\\eWayBill.json");
                Close();
            }
            else
            {
                MessageBox.Show("Invalid Bill ID(s)");
            }
        }

        private string formatBillIds(string data)
        {
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
                else if (data.Contains(","))
                {
                    input += "'" + data.Trim() + "', ";
                }
                else
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

            return output;
        }
    }
}
