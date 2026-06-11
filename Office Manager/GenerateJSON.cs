using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Office_Manager.GenerateEInvoice;

namespace Office_Manager
{
    public partial class GenerateJSON : Form
    {
        string firm;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        private static readonly HttpClient _httpClient = new HttpClient();

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

        public String parseBillIds(String data)
        {
            string output = "";
            string[] parts = data.Split(':');

            int num1;
            if (!Int32.TryParse(parts[0].Trim(), out num1))
            {
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

            for (int i = num1; i <= num2; i++)
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

        private string createJsonForApi()
        {
            String data = textBox1.Text.ToUpper();

            string input = formatBillIds(data);
            decimal mDiscAmount = 0;

            string output = "";
            string connectionStr = @"Data Source=(localdb)\VISHAL;AttachDbFilename=|DataDirectory|\Files\DBQuery.mdf;Integrated Security=True";

            System.Data.DataTable dt = new System.Data.DataTable();

            using (SqlConnection con = new SqlConnection(connectionStr))
            {
                string query = "SELECT DISTINCT C1.CID BILL_TO, S1.CID SHIP_TO, B.BILL_ID, B.BILL_DT, B.FREIGHT, CMP.GSTIN SELLER_GSTIN, CMP.CITY SELLER_CITY, CMP.PIN SELLER_PIN, C1.GSTIN CUST_GSTIN, C1.CNAME CUST_NAME, C1.ADDRESS CUST_ADDR1, C1.CITY CUST_CITY, C1.PINCODE CUST_PIN, S1.GSTIN SHIP_GSTIN, S1.CNAME SHIP_NAME, S1.ADDRESS SHIP_ADDR1, S1.CITY SHIP_CITY, S1.PINCODE SHIP_PIN, (SELECT SUM(BII.MTR*BII.RATE) FROM BILL_ITEM BII WHERE BII.BILL_ID = BI.BILL_ID) TOT_NET_AMT, (SELECT SUM(BII.MTR*BII.RATE) - BB.NET_AMT FROM BILL BB, BILL_ITEM BII WHERE BB.BILL_ID = BII.BILL_ID AND B.BILL_ID = BB.BILL_ID GROUP BY BB.NET_AMT) DISCOUNT, B.DISCOUNT DISC_PER, B.NET_AMT, B.ISGT IGST_RATE, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, B.CGST CGST_RATE, B.SGST SGST_RATE, (B.BILL_AMT - (B.NET_AMT + B.CGST + B.SGST_AMT + B.IGST_AMT)) ROUND_OFF, B.BILL_AMT, T1.TRANS_ID TRANS_GSTIN, T1.T_NAME TRANS_NAME, S1.DISTANCE, I.DESCR, I.HSN ITEM_HSN, LEFT(I.UNIT, CHARINDEX('-', I.UNIT) - 1) AS ITEM_UNIT, BI.ROLL_NO, BI.MTR ITEM_QTY, BI.RATE ITEM_RATE, BI.AMOUNT ITEM_AMOUNT FROM BILL B, CUSTOMER C1, CUSTOMER S1, COMPANY CMP, BILL_ITEM BI, ITEM I, TRANSPORT T1 WHERE I.ITEM_ID = BI.ITEM AND C1.CID = B.BILL_TO AND CMP.NAME = B.FIRM AND B.SHIP_TO = S1.CID AND B.TRANSPORTER = T1.TID AND B.BILL_ID = BI.BILL_ID AND B.FIRM = @FIRM AND B.BILL_ID IN " + input;
                using (SqlCommand oCmd = new SqlCommand(query, con))
                {
                    oCmd.Parameters.AddWithValue("@FIRM", firm);
                    con.Open();

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        dt.Load(oReader); // This cleanly loads all rows into memory at once
                    }
                }
            }

            if (dt.Rows.Count > 0)
            {
                var billsList = new List<object>();

                // Step 1: Group all rows by BILL_ID so we can process one complete invoice at a time
                var groupedBills = new Dictionary<string, List<DataRow>>();
                foreach (DataRow row in dt.Rows)
                {
                    string billId = row["BILL_ID"].ToString();
                    if (!groupedBills.ContainsKey(billId))
                    {
                        groupedBills[billId] = new List<DataRow>();
                    }
                    groupedBills[billId].Add(row);
                }

                // Step 2: Loop through each grouped invoice
                foreach (var kvp in groupedBills)
                {
                    string billId = kvp.Key;
                    List<DataRow> billRows = kvp.Value;
                    DataRow firstRow = billRows[0]; // Used for header details (Buyer, Seller, ValDtls)

                    // Extract header data
                    string sellerGstin = firstRow["SELLER_GSTIN"].ToString();
                    string custGstin = firstRow["CUST_GSTIN"].ToString();
                    string shipGstin = firstRow["SHIP_GSTIN"].ToString();

                    string sellerStateCode = sellerGstin.Length >= 2 ? sellerGstin.Substring(0, 2) : "";
                    string custStateCode = custGstin.Length >= 2 ? custGstin.Substring(0, 2) : "";
                    string shipStateCode = shipGstin.Length >= 2 ? shipGstin.Substring(0, 2) : "";

                    string billTo = firstRow["BILL_TO"].ToString();
                    string shipTo = firstRow["SHIP_TO"].ToString();

                    decimal netAmount = Convert.ToDecimal(firstRow["NET_AMT"]);
                    decimal totNetAmount = Convert.ToDecimal(firstRow["TOT_NET_AMT"]);
                    decimal freight = Convert.ToDecimal(firstRow["FREIGHT"]);

                    decimal discPer = Convert.ToDecimal(firstRow["DISC_PER"]);
                    decimal discAmount = Math.Round(totNetAmount * discPer / 100, 2);
                    netAmount = totNetAmount - discAmount + freight;

                    mDiscAmount = discAmount;

                    double freightGst = 0;
                    double freightCgst = 0;
                    double freightSgst = 0;
                    double freightIgst = 0;

                    if (freight > 0)
                    {
                        freightGst = Math.Round((double)freight * 0.05, 2);
                    }

                    DateTime parsedDate;
                    string billDt = DateTime.TryParse(firstRow["BILL_DT"].ToString(), out parsedDate)
                        ? parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : "";

                    decimal headerIgstVal = firstRow["IGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["IGST_AMT"]);
                    decimal headerCgstVal = firstRow["CGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["CGST_AMT"]);
                    decimal headerSgstVal = firstRow["SGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["SGST_AMT"]);

                    if (headerIgstVal > 0)
                    {
                        freightIgst = freightGst;
                    }
                    else
                    {
                        freightCgst = freightGst / 2;
                        freightSgst = freightGst / 2;
                    }

                    var itemList = new List<object>();

                    // Track running totals of GST applied to items
                    decimal sumPrevIgst = 0, sumPrevCgst = 0, sumPrevSgst = 0, sumPrevDiscount = 0;

                    int maxSl = 0;
                    // Loop through the items of this specific invoice
                    for (int i = 0; i < billRows.Count; i++)
                    {
                        decimal totalAmt = 0;
                        DataRow row = billRows[i];
                        bool isLastItem = (i == billRows.Count - 1);

                        string slNo;
                        string rollNoStr = row.Table.Columns.Contains("ROLL_NO") && row["ROLL_NO"] != DBNull.Value ? row["ROLL_NO"].ToString() : "";
                        if (long.TryParse(rollNoStr, out _))
                        {
                            slNo = rollNoStr;
                        }
                        else
                        {
                            slNo = (i + 1).ToString();
                        }
                        maxSl = Int32.Parse(slNo);

                        // Extract math variables safely
                        decimal assAmt = row["ITEM_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_AMOUNT"]);
                        decimal igstRate = row["IGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["IGST_RATE"]);
                        decimal cgstRate = row["CGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["CGST_RATE"]);
                        decimal sgstRate = row["SGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["SGST_RATE"]);

                        totalAmt = assAmt;
                        if (mDiscAmount > 0)
                        {
                            if (isLastItem)
                            {
                                discAmount = mDiscAmount - sumPrevDiscount;
                            }
                            else
                            {
                                discAmount = Math.Round(discPer * totalAmt / 100, 2);
                                sumPrevDiscount += discAmount;
                            }

                            assAmt = totalAmt - discAmount;
                        }

                        decimal itemIgstAmt = Math.Round((igstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemCgstAmt = Math.Round((cgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemSgstAmt = Math.Round((sgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);

                        if (isLastItem)
                        {
                            if (headerIgstVal > 0 || sumPrevIgst > 0) itemIgstAmt = headerIgstVal - (decimal)freightIgst - sumPrevIgst;
                            if (headerCgstVal > 0 || sumPrevCgst > 0) itemCgstAmt = headerCgstVal - (decimal)freightCgst - sumPrevCgst;
                            if (headerSgstVal > 0 || sumPrevSgst > 0) itemSgstAmt = headerSgstVal - (decimal)freightSgst - sumPrevSgst;
                        }
                        else
                        {
                            sumPrevIgst += itemIgstAmt;
                            sumPrevCgst += itemCgstAmt;
                            sumPrevSgst += itemSgstAmt;
                        }

                        decimal totItemVal = assAmt + itemIgstAmt + itemCgstAmt + itemSgstAmt;

                        // Built with corrected attributes
                        var newItem = new
                        {
                            serialNumber = slNo,
                            productDescription = row["DESCR"].ToString(),
                            isService = false, // Converted to boolean
                            hsnCode = row["ITEM_HSN"].ToString(),
                            barcode = (string)null,
                            quantity = row["ITEM_QTY"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_QTY"]),
                            freeQuantity = 0,
                            unit = row["ITEM_UNIT"].ToString(),
                            unitPrice = row["ITEM_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_RATE"]),
                            totalAmount = totalAmt,
                            discount = discAmount,
                            preTaxValue = 0,
                            assessableAmount = assAmt,
                            gstRate = 5,
                            igstAmount = itemIgstAmt,
                            cgstAmount = itemCgstAmt,
                            sgstAmount = itemSgstAmt,
                            cessRate = 0,
                            cessAmount = 0,
                            cessNonAdvolAmount = 0,
                            stateCessRate = 0,
                            stateCessAmount = 0,
                            stateCessNonAdvolAmount = 0,
                            otherCharges = 0,
                            totalItemValue = totItemVal,
                            orderLineReference = (string)null,
                            originCountry = (string)null,
                            productSerialNumber = (string)null,
                            batchDetails = (object)null,
                            attributeDetails = new[] { new { name = (string)null, value = (string)null } }
                        };

                        itemList.Add(newItem);
                    }

                    if (freight > 0)
                    {
                        var newItem = new
                        {
                            serialNumber = (maxSl + 1) + "",
                            productDescription = (string)null,
                            isService = true, // Converted to boolean
                            hsnCode = "9965",
                            barcode = (string)null,
                            quantity = 1m,
                            freeQuantity = 0,
                            unit = "OTH",
                            unitPrice = freight,
                            totalAmount = freight,
                            discount = 0,
                            preTaxValue = 0,
                            assessableAmount = freight,
                            gstRate = 5,
                            igstAmount = freightGst,
                            cgstAmount = freightCgst,
                            sgstAmount = freightSgst,
                            cessRate = 0,
                            cessAmount = 0,
                            cessNonAdvolAmount = 0,
                            stateCessRate = 0,
                            stateCessAmount = 0,
                            stateCessNonAdvolAmount = 0,
                            otherCharges = 0,
                            totalItemValue = ((double)freight + freightGst),
                            orderLineReference = (string)null,
                            originCountry = (string)null,
                            productSerialNumber = (string)null,
                            batchDetails = (object)null,
                            attributeDetails = new[] { new { name = (string)null, value = (string)null } }
                        };

                        itemList.Add(newItem);
                    }

                    decimal billAmt = netAmount + headerCgstVal + headerSgstVal + headerIgstVal;
                    decimal roundOff = Math.Round(billAmt) - billAmt;

                    // Build the main Invoice Object with corrected attributes
                    var newBill = new
                    {
                        version = "1.1",
                        transactionDetails = new { taxScheme = "GST", supplyType = "B2B", igstOnIntra = "N", reverseCharge = "N", ecmGstin = (string)null },
                        documentDetails = new { type = "INV", number = billId, date = billDt },
                        seller = new
                        {
                            gstin = sellerGstin,
                            legalName = firm,
                            tradeName = (string)null,
                            address1 = "276, Daudpura",
                            address2 = (string)null,
                            location = firstRow["SELLER_CITY"].ToString(),
                            pincode = firstRow["SELLER_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["SELLER_PIN"]),
                            stateCode = sellerStateCode,
                            phone = (string)null,
                            email = (string)null
                        },
                        buyer = new
                        {
                            gstin = custGstin,
                            legalName = firstRow["CUST_NAME"].ToString(),
                            tradeName = (string)null,
                            placeOfSupplyStateCode = custStateCode,
                            address1 = firstRow["CUST_ADDR1"].ToString(),
                            address2 = (string)null,
                            location = firstRow["CUST_CITY"].ToString(),
                            pincode = firstRow["CUST_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["CUST_PIN"]),
                            stateCode = custStateCode,
                            phone = (string)null,
                            email = (string)null
                        },
                        dispatchDetails = (object)null,
                        shippingDetails = string.Equals(billTo, shipTo, StringComparison.OrdinalIgnoreCase) ? (object)null : new
                        {
                            gstin = shipGstin,
                            legalName = firstRow["SHIP_NAME"].ToString(),
                            tradeName = (string)null,
                            address1 = firstRow["SHIP_ADDR1"].ToString(),
                            address2 = (string)null,
                            location = firstRow["SHIP_CITY"].ToString(),
                            pincode = firstRow["SHIP_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["SHIP_PIN"]),
                            stateCode = shipStateCode
                        },
                        valueDetails = new
                        {
                            assessableValue = Math.Round(netAmount, 2),
                            igstValue = headerIgstVal,
                            cgstValue = headerCgstVal,
                            sgstValue = headerSgstVal,
                            cessValue = 0,
                            stateCessValue = 0,
                            discount = 0,
                            otherCharges = 0,
                            roundOffAmount = Math.Round(roundOff, 2),
                            totalInvoiceValue = Math.Round(billAmt),
                            totalInvoiceValueFc = 0
                        },
                        exportDetails = (object)null,
                        ewayBillDetails = (headerCgstVal > 0) ? (object)null : new
                        {
                            transporterId = firstRow["TRANS_GSTIN"].ToString(),
                            transporterName = firstRow["TRANS_NAME"].ToString(),
                            transportMode = (string)null,
                            distance = firstRow["DISTANCE"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["DISTANCE"]),
                            transporterDocumentNumber = (string)null,
                            transporterDocumentDate = (string)null,
                            vehicleNumber = (string)null,
                            vehicleType = (string)null
                        },
                        paymentDetails = (object)null,
                        referenceDetails = (object)null,
                        additionalDocumentDetails = (object)null,
                        items = itemList
                    };

                    billsList.Add(newBill);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                output = JsonSerializer.Serialize(billsList, options);

                System.IO.File.WriteAllText(@"C:\Invoices\eInvoice.json", output);

                return "OK";
            }
            else
            {
                return "No records found for this query.";
            }
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

            string query = "SELECT DISTINCT C.CID BILL_TO, C2.CID SHIP_TO, F.GSTIN FROM_GSTIN, B.BILL_ID, CONVERT(VARCHAR(10), B.BILL_DT, 103) BILL_DT, F.CITY, F.PIN FROM_PIN, C.GSTIN TO_GSTIN, C2.GSTIN ACTUAL_TO_GSTIN, C2.CITY TO_CITY, C.CNAME, C2.PINCODE TO_PIN, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, B.BILL_AMT, C2.DISTANCE, T.T_NAME, T.TRANS_ID, I.ITEM_NAME, I.DESCR, I.HSN, (SELECT SUM(BI1.MTR) FROM BILL_ITEM BI1 WHERE BI1.BILL_ID = B.BILL_ID) QTY, (SELECT TOP 1 * FROM SPLIT((SELECT UNIT FROM ITEM I1 WHERE I1.ITEM_ID = I.ITEM_ID), '-')) UNIT, B.NET_AMT, B.CGST, B.SGST, B.ISGT FROM BILL B, CUSTOMER C, COMPANY F, TRANSPORT T, ITEM I, BILL_ITEM BI, CUSTOMER C2 WHERE C.CID = B.BILL_TO AND C2.CID = B.SHIP_TO AND F.NAME = C.FIRM AND B.TRANSPORTER = T.TID AND I.ITEM_ID = BI.ITEM AND BI.BILL_ID = B.BILL_ID AND F.NAME = @FIRM AND UPPER(B.BILL_ID) IN " + input;
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
                        string p1 = parseBillIds(s);
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
                    string p1 = parseBillIds(data);
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

        private async Task<AuthResult> AuthenticateAsync()
        {
            string authUrl = "https://login.onefinops.com/realms/onefinops/protocol/openid-connect/token";

            using (var request = new HttpRequestMessage(HttpMethod.Post, authUrl))
            {
                // FIX: Add all parameters to the dictionary for the request body
                var param = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", "ofin_live_ydkq2bwrn5rnxghdxh0zxa4294" },
            { "client_secret", "1l6S7dbovWw7qkqkt9YTD82pRu5fSIqn" }
        };

                // FormUrlEncodedContent automatically sets the 
                // "Content-Type: application/x-www-form-urlencoded" header for you.
                request.Content = new FormUrlEncodedContent(param);

                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    JsonNode jsonResponse = JsonNode.Parse(responseBody);

                    if (!response.IsSuccessStatusCode)
                    {
                        return new AuthResult
                        {
                            ErrorMessage = jsonResponse?["error_description"]?.ToString() ?? "Unknown error occurred."
                        };
                    }

                    return new AuthResult
                    {
                        Token = jsonResponse?["access_token"]?.ToString()
                    };
                }
                catch
                {
                    return new AuthResult
                    {
                        ErrorMessage = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    };
                }
            }
        }

        /// <summary>
        /// Submits a single invoice JSON payload to the API.
        /// </summary>
        private async Task<JsonNode> SubmitInvoiceAsync(string payload, string token)
        {
            string invoiceUrl = "https://api.in.onefinops.com/v1/einvoices/generate";

            using (var request = new HttpRequestMessage(HttpMethod.Post, invoiceUrl))
            {
                // Note: Content-Type: application/json is handled automatically by StringContent below.
                request.Headers.Add("Gstin", "23ABTPA4978M1Z2");

                // Set the Bearer token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Attach the JSON body
                request.Content = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                // Even on error statuses, APIs usually return a JSON explaining the error.
                // We read the string and parse it to return the exact JSON.
                string responseBody = await response.Content.ReadAsStringAsync();

                try
                {
                    return JsonNode.Parse(responseBody);
                }
                catch
                {
                    // Fallback in case the API returns non-JSON on a catastrophic failure (like a 502 Bad Gateway HTML page)
                    return JsonNode.Parse($"{{\"error\": \"Failed to parse response. Server returned: {response.StatusCode}\"}}");
                }
            }
        }

        public class AuthResult
        {
            public string Token { get; set; }
            public string ErrorMessage { get; set; }
        }

        private async void btnCreateEwb_Click(object sender, EventArgs e)
        {
            btnCreateEwb.Text = "Creating...";
            btnCreateEwb.Enabled = false;
            string msg = createJsonForApi();
            if (!msg.Equals("OK"))
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCreateEwb.Text = "Create EWB";
                btnCreateEwb.Enabled = true;
                return;
            }

            try
            {
                string filePath = @"C:\Invoices\eInvoice.json";

                // 1. Read the JSON array from the file (compatible with older .NET)
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("File not found at: " + filePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCreateEwb.Text = "Create EWB";
                    btnCreateEwb.Enabled = true;
                    return;
                }

                string fileContent;
                using (StreamReader reader = new StreamReader(filePath))
                {
                    fileContent = await reader.ReadToEndAsync();
                }

                JsonArray sourceArray = JsonNode.Parse(fileContent)?.AsArray();

                if (sourceArray == null || sourceArray.Count == 0)
                {
                    MessageBox.Show("The JSON file is empty or invalid.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    btnCreateEwb.Text = "Create EWB";
                    btnCreateEwb.Enabled = true;
                    return;
                }

                // 2. Authenticate and get the token
                AuthResult authResult = await AuthenticateAsync();
                if (string.IsNullOrEmpty(authResult.Token))
                {
                    MessageBox.Show($"Authorization error: {authResult.ErrorMessage}", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCreateEwb.Text = "Create EWB";
                    btnCreateEwb.Enabled = true;
                    return;
                }

                string accessToken = authResult.Token;

                // 3 & 4. Loop through the array, send requests, and collect responses
                JsonArray finalResponses = new JsonArray();

                for (int i = 0; i < sourceArray.Count; i++)
                {
                    var invoiceJsonString = sourceArray[i].ToJsonString();

                    //string requestId = $"{DateTime.UtcNow:yyyyMMddHHmmssffff}-{i}";

                    JsonNode apiResponse = await SubmitInvoiceAsync(invoiceJsonString, accessToken);

                    if (apiResponse != null)
                    {
                        // --- NEW API ERROR CHECKING ---
                        bool isApiError = false;
                        string apiErrorMessage = "Unknown API Error";

                        // Scenario A: API returns an Array for the error (as per your example)
                        if (apiResponse is JsonArray responseArray && responseArray.Count > 0)
                        {
                            var firstItem = responseArray[0];
                            var successNode = firstItem?["success"];

                            // Check if success is explicitly false
                            if (successNode != null && successNode.GetValue<bool>() == false)
                            {
                                isApiError = true;
                                apiErrorMessage = firstItem?["message"]?.ToString() ?? apiErrorMessage;
                            }
                        }
                        // Scenario B: API returns an Object (Handles IRN presence, DUPIRN, and validation codes)
                        else if (apiResponse is JsonObject responseObj)
                        {
                            var infoCodeNode = responseObj["infoCode"];
                            var codeNode = responseObj["code"];
                            var irnNode = responseObj["irn"];
                            var successNode = responseObj["success"]; // Kept for backward compatibility with other error formats

                            // 1. Check for Duplicate IRN using "infoCode" (Even though it has an IRN, we flag it as an error)
                            if (infoCodeNode != null && infoCodeNode.ToString() == "DUPIRN")
                            {
                                isApiError = true; // Use 'isError = true;' when updating Phase 1
                                apiErrorMessage = responseObj["message"]?.ToString() ?? "Duplicate IRN detected."; // Use 'statusMessage' in Phase 1
                            }
                            // 2. Check for explicit "success": false (Just in case the API still uses it for other business errors)
                            else if (successNode != null && successNode.GetValue<bool>() == false)
                            {
                                isApiError = true;
                                apiErrorMessage = responseObj["message"]?.ToString() ?? "Unknown API Error";
                            }
                            // 3. Check for Header/Validation errors using "code" (e.g., 'Gstin.Invalid')
                            else if (codeNode != null)
                            {
                                isApiError = true;
                                apiErrorMessage = responseObj["message"]?.ToString() ?? $"API Error Code: {codeNode}";
                            }
                            // 4. Missing IRN fallback: If we get here and there is no IRN, it is definitely a failure.
                            else if (irnNode == null)
                            {
                                isApiError = true;
                                apiErrorMessage = responseObj["message"]?.ToString() ?? "Failed to generate IRN. Unknown error.";
                            }
                        }

                        // Show the message box if an error was detected
                        if (isApiError)
                        {
                            //MessageBox.Show($"Error processing Invoice #{i + 1}:\n\n{apiErrorMessage}", "API Response Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        // ------------------------------

                        finalResponses.Add(apiResponse);
                    }
                }

                // update in db

                // Prepare variables for validation
                bool hasAnyErrors = false;
                StringBuilder summaryMessage = new StringBuilder();
                List<int> successfulIndices = new List<int>(); // Tracks which invoices to update in the DB

                // ==========================================
                // PHASE 1: Validate all responses
                // ==========================================
                for (int i = 0; i < finalResponses.Count; i++)
                {
                    var response = finalResponses[i];

                    // Extract Invoice Number from the original source payload to display in the message 
                    // and use as 'bill_id' later. Adjust the JSON path if your Document Number is stored elsewhere.
                    string invNo = sourceArray[i]?["documentDetails"]?["number"]?.ToString() ?? $"Invoice {i + 1}";

                    bool isError = false;
                    string statusMessage = "Success";

                    // Scenario A: Error returned as a JsonArray
                    if (response is JsonArray responseArray && responseArray.Count > 0)
                    {
                        var firstItem = responseArray[0];
                        var successNode = firstItem?["success"];
                        if (successNode != null && successNode.GetValue<bool>() == false)
                        {
                            isError = true;
                            statusMessage = firstItem?["message"]?.ToString() ?? "Unknown API Error";
                        }
                    }
                    // Scenario B: API returns an Object (Handles IRN presence, DUPIRN, and validation codes)
                    else if (response is JsonObject responseObj)
                    {
                        var infoCodeNode = responseObj["infoCode"];
                        var codeNode = responseObj["code"];
                        var irnNode = responseObj["irn"];
                        var successNode = responseObj["success"]; // Kept for backward compatibility with other error formats

                        // 1. Check for Duplicate IRN using "infoCode" (Even though it has an IRN, we flag it as an error)
                        if (infoCodeNode != null && infoCodeNode.ToString() == "DUPIRN")
                        {
                            isError = true; // Use 'isError = true;' when updating Phase 1
                            statusMessage = responseObj["message"]?.ToString() ?? "Duplicate IRN detected."; // Use 'statusMessage' in Phase 1
                        }
                        // 2. Check for explicit "success": false (Just in case the API still uses it for other business errors)
                        else if (successNode != null && successNode.GetValue<bool>() == false)
                        {
                            isError = true;
                            statusMessage = responseObj["message"]?.ToString() ?? "Unknown API Error";
                        }
                        // 3. Check for Header/Validation errors using "code" (e.g., 'Gstin.Invalid')
                        else if (codeNode != null)
                        {
                            isError = true;
                            statusMessage = responseObj["message"]?.ToString() ?? $"API Error Code: {codeNode}";
                        }
                        // 4. Missing IRN fallback: If we get here and there is no IRN, it is definitely a failure.
                        else if (irnNode == null)
                        {
                            isError = true;
                            statusMessage = responseObj["message"]?.ToString() ?? "Failed to generate IRN. Unknown error.";
                        }
                    }

                    // Record the result
                    if (isError)
                    {
                        hasAnyErrors = true;
                    }
                    else
                    {
                        successfulIndices.Add(i);
                    }

                    // Append to our summary list
                    summaryMessage.AppendLine($"{invNo} : {statusMessage}");
                }

                string finalOutputString = finalResponses.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                // Save to output file (compatible with older .NET)
                string outputPath = @"C:\Invoices\eInvoice_Responses.json";
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    await writer.WriteAsync(finalOutputString);
                }

                // ==========================================
                // PHASE 2: Update Database for Successes
                // ==========================================
                // Only run the DB logic if we have at least one successful invoice
                if (successfulIndices.Count > 0)
                {
                    con.Open();
                    try
                    {
                        string updateQuery = "UPDATE BILL SET EWAYBILL_NO = @EWAYBILL_NO, EWB_TIME = @EWB_TIME, IRN = @IRN, SIGNED_INVOICE = @SIGNED_INVOICE WHERE bill_id = @bill_id AND firm = @firm";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            foreach (int i in successfulIndices)
                            {
                                var response = finalResponses[i];

                                if (response != null)
                                {
                                    string irn = response["irn"]?.ToString();
                                    string ewayBillNo = response["ewbNumber"]?.ToString();
                                    string signedQRCode = response["signedQRCode"]?.ToString();
                                    string billId = sourceArray[i]?["documentDetails"]?["number"]?.ToString() ?? "";

                                    // The shortcut: AddWithValue infers the data type from the value you pass
                                    cmd.Parameters.AddWithValue("@EWAYBILL_NO", string.IsNullOrEmpty(ewayBillNo) ? DBNull.Value : (object)ewayBillNo);
                                    cmd.Parameters.AddWithValue("@IRN", string.IsNullOrEmpty(irn) ? DBNull.Value : (object)irn);
                                    cmd.Parameters.AddWithValue("@SIGNED_INVOICE", string.IsNullOrEmpty(signedQRCode) ? DBNull.Value : (object)signedQRCode);
                                    cmd.Parameters.AddWithValue("@bill_id", billId);
                                    cmd.Parameters.AddWithValue("@firm", firm);
                                    cmd.Parameters.AddWithValue("@EWB_TIME", DateTime.Now);

                                    cmd.ExecuteNonQuery();

                                    // CRITICAL: You must clear the parameters at the end of the loop 
                                    // if you use AddWithValue inside a loop, otherwise it will crash on the next iteration
                                    cmd.Parameters.Clear();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Database Update Error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        con.Close();
                    }
                }

                MessageBox.Show(summaryMessage.ToString(), "Invoice Generation Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //MessageBox.Show($"Processing complete! {finalResponses.Count} invoices processed.\nSaved to: {outputPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnCreateEwb.Text = "Create EWB";
            btnCreateEwb.Enabled = true;
        }
    }
}
