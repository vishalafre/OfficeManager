using Microsoft.Office.Interop.Excel;
using NPOI.SS.Formula.Functions;
using OpenQA.Selenium.Remote;
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
using static NPOI.HSSF.Util.HSSFColor;
// (Alongside your existing using statements like System.Data.SqlClient)

namespace Office_Manager
{
    public partial class GenerateEInvoice : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        string firm;
        private static readonly HttpClient _httpClient = new HttpClient();

        public GenerateEInvoice(string firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void GenerateEInvoice_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            AcceptButton = btnCreateEinv;
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
                            transporterMode = (string)null,
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
                Close();

                return "OK";
            }
            else
            {
                return "No records found for this query.";
            }
        }

        private string createJson()
        {
            String data = textBox1.Text.ToUpper();

            string input = formatBillIds(data);
            decimal mDiscAmount = 0;

            string output = "";
            string connectionStr = @"Data Source=(localdb)\VISHAL;AttachDbFilename=|DataDirectory|\Files\DBQuery.mdf;Integrated Security=True";

            System.Data.DataTable dt = new System.Data.DataTable();

            using (SqlConnection con = new SqlConnection(connectionStr))
            {
                string query = "SELECT DISTINCT C1.CID BILL_TO, S1.CID SHIP_TO, B.BILL_ID, B.BILL_DT, B.FREIGHT, CMP.GSTIN SELLER_GSTIN, CMP.CITY SELLER_CITY, CMP.PIN SELLER_PIN, C1.GSTIN CUST_GSTIN, C1.CNAME CUST_NAME, C1.ADDRESS CUST_ADDR1, C1.CITY CUST_CITY, C1.PINCODE CUST_PIN, S1.GSTIN SHIP_GSTIN, S1.CNAME SHIP_NAME, S1.ADDRESS SHIP_ADDR1, S1.CITY SHIP_CITY, S1.PINCODE SHIP_PIN, (SELECT SUM(BII.MTR*BII.RATE) FROM BILL_ITEM BII WHERE BII.BILL_ID = BI.BILL_ID) TOT_NET_AMT, (SELECT SUM(BII.MTR*BII.RATE) - BB.NET_AMT FROM BILL BB, BILL_ITEM BII WHERE BB.BILL_ID = BII.BILL_ID AND B.BILL_ID = BB.BILL_ID GROUP BY BB.NET_AMT) DISCOUNT, B.DISCOUNT DISC_PER, B.NET_AMT, B.ISGT IGST_RATE, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, B.CGST CGST_RATE, B.SGST SGST_RATE, (B.BILL_AMT - (B.NET_AMT + B.CGST + B.SGST_AMT + B.IGST_AMT)) ROUND_OFF, B.BILL_AMT, T1.TRANS_ID TRANS_GSTIN, T1.T_NAME TRANS_NAME, S1.DISTANCE, I.HSN ITEM_HSN, LEFT(I.UNIT, CHARINDEX('-', I.UNIT) - 1) AS ITEM_UNIT, BI.ROLL_NO, BI.MTR ITEM_QTY, BI.RATE ITEM_RATE, BI.AMOUNT ITEM_AMOUNT FROM BILL B, CUSTOMER C1, CUSTOMER S1, COMPANY CMP, BILL_ITEM BI, ITEM I, TRANSPORT T1 WHERE I.ITEM_ID = BI.ITEM AND C1.CID = B.BILL_TO AND CMP.NAME = B.FIRM AND B.SHIP_TO = S1.CID AND B.TRANSPORTER = T1.TID AND B.BILL_ID = BI.BILL_ID AND B.FIRM = @FIRM AND B.BILL_ID IN " + input;
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
                        ? parsedDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        : "";

                    // ⚠️ NOTE: If your database uses a different column name for the TOTAL GST amounts 
                    // in the header vs the percentage rates in the items, change these three variables accordingly.
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
                        bool isLastItem = (i == billRows.Count - 1); // True if this is the final item in the loop

                        // --- CHANGE 1: ROLL_NO Logic ---
                        string slNo;
                        string rollNoStr = row.Table.Columns.Contains("ROLL_NO") && row["ROLL_NO"] != DBNull.Value ? row["ROLL_NO"].ToString() : "";
                        if (long.TryParse(rollNoStr, out _))
                        {
                            slNo = rollNoStr; // It's purely numeric
                        }
                        else
                        {
                            slNo = (i + 1).ToString(); // Fallback to 1, 2, 3...
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

                        // --- CHANGES 2, 3 & 4: Calculate Item GST (Rounded to 2 decimals) ---
                        decimal itemIgstAmt = Math.Round((igstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemCgstAmt = Math.Round((cgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemSgstAmt = Math.Round((sgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);

                        // --- CHANGE 6: Rounding Reconciliation on the Last Item ---

                        if (isLastItem)
                        {
                            // Force the last item to absorb any penny differences
                            if (headerIgstVal > 0 || sumPrevIgst > 0) itemIgstAmt = headerIgstVal - (decimal)freightIgst - sumPrevIgst;
                            if (headerCgstVal > 0 || sumPrevCgst > 0) itemCgstAmt = headerCgstVal - (decimal)freightCgst - sumPrevCgst;
                            if (headerSgstVal > 0 || sumPrevSgst > 0) itemSgstAmt = headerSgstVal - (decimal)freightSgst - sumPrevSgst;

                            /*totalAmt = assAmt;
                            if (mDiscAmount > 0)
                            {
                                assAmt -= mDiscAmount;
                            }
                            discAmount = mDiscAmount;*/
                        }
                        else
                        {
                            /*discAmount = 0;
                            totalAmt = assAmt;*/

                            // Keep summing up the amounts for previous items
                            sumPrevIgst += itemIgstAmt;
                            sumPrevCgst += itemCgstAmt;
                            sumPrevSgst += itemSgstAmt;
                        }

                        // --- CHANGE 5: TotItemVal Calculation ---
                        decimal totItemVal = assAmt + itemIgstAmt + itemCgstAmt + itemSgstAmt;

                        // Build the JSON object for the item
                        var newItem = new
                        {
                            SlNo = slNo,
                            PrdDesc = (string)null,
                            IsServc = "N",
                            HsnCd = row["ITEM_HSN"].ToString(),
                            Barcde = (string)null,
                            Qty = row["ITEM_QTY"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_QTY"]),
                            FreeQty = 0,
                            Unit = row["ITEM_UNIT"].ToString(),
                            UnitPrice = row["ITEM_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_RATE"]),
                            TotAmt = totalAmt,
                            Discount = discAmount,
                            PreTaxVal = 0,
                            AssAmt = assAmt,
                            GstRt = 5, // (Assuming fixed as per original code, update if DB holds total item GST rate)
                            IgstAmt = itemIgstAmt,
                            CgstAmt = itemCgstAmt,
                            SgstAmt = itemSgstAmt,
                            CesRt = 0,
                            CesAmt = 0,
                            CesNonAdvlAmt = 0,
                            StateCesRt = 0,
                            StateCesAmt = 0,
                            StateCesNonAdvlAmt = 0,
                            OthChrg = 0,
                            TotItemVal = totItemVal,
                            OrdLineRef = (string)null,
                            OrgCntry = (string)null,
                            PrdSlNo = (string)null,
                            BchDtls = (object)null,
                            AttribDtls = new[] { new { Nm = (string)null, Val = (string)null } }
                        };

                        itemList.Add(newItem);
                    } // End of Items loop

                    if (freight > 0)
                    {
                        var newItem = new
                        {
                            SlNo = (maxSl + 1) + "",
                            PrdDesc = (string)null,
                            IsServc = "Y",
                            HsnCd = "9965",
                            Barcde = (string)null,
                            Qty = 1m,
                            FreeQty = 0,
                            Unit = "OTH",
                            UnitPrice = freight,
                            TotAmt = freight,
                            Discount = 0,
                            PreTaxVal = 0,
                            AssAmt = freight,
                            GstRt = 5, // (Assuming fixed as per original code, update if DB holds total item GST rate)
                            IgstAmt = freightGst,
                            CgstAmt = freightCgst,
                            SgstAmt = freightSgst,
                            CesRt = 0,
                            CesAmt = 0,
                            CesNonAdvlAmt = 0,
                            StateCesRt = 0,
                            StateCesAmt = 0,
                            StateCesNonAdvlAmt = 0,
                            OthChrg = 0,
                            TotItemVal = ((double)freight + freightGst),
                            OrdLineRef = (string)null,
                            OrgCntry = (string)null,
                            PrdSlNo = (string)null,
                            BchDtls = (object)null,
                            AttribDtls = new[] { new { Nm = (string)null, Val = (string)null } }
                        };

                        itemList.Add(newItem);
                    }

                    decimal billAmt = netAmount + headerCgstVal + headerSgstVal + headerIgstVal;
                    decimal roundOff = Math.Round(billAmt) - billAmt;
                    // Build the main Invoice Object
                    var newBill = new
                    {
                        Version = "1.1",
                        TranDtls = new { TaxSch = "GST", SupTyp = "B2B", IgstOnIntra = "N", RegRev = (string)null, EcmGstin = (string)null },
                        DocDtls = new { Typ = "INV", No = billId, Dt = billDt },
                        SellerDtls = new
                        {
                            Gstin = sellerGstin,
                            LglNm = firm,
                            TrdNm = (string)null,
                            Addr1 = "276, Daudpura",
                            Addr2 = (string)null,
                            Loc = firstRow["SELLER_CITY"].ToString(),
                            Pin = firstRow["SELLER_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["SELLER_PIN"]),
                            Stcd = sellerStateCode,
                            Ph = (string)null,
                            Em = (string)null
                        },
                        BuyerDtls = new
                        {
                            Gstin = custGstin,
                            LglNm = firstRow["CUST_NAME"].ToString(),
                            TrdNm = (string)null,
                            Pos = custStateCode,
                            Addr1 = firstRow["CUST_ADDR1"].ToString(),
                            Addr2 = (string)null,
                            Loc = firstRow["CUST_CITY"].ToString(),
                            Pin = firstRow["CUST_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["CUST_PIN"]),
                            Stcd = custStateCode,
                            Ph = (string)null,
                            Em = (string)null
                        },
                        DispDtls = (object)null,
                        ShipDtls = string.Equals(billTo, shipTo, StringComparison.OrdinalIgnoreCase) ? (object)null : new
                        {
                            Gstin = shipGstin,
                            LglNm = firstRow["SHIP_NAME"].ToString(),
                            TrdNm = (string)null,
                            Addr1 = firstRow["SHIP_ADDR1"].ToString(),
                            Addr2 = (string)null,
                            Loc = firstRow["SHIP_CITY"].ToString(),
                            Pin = firstRow["SHIP_PIN"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["SHIP_PIN"]),
                            Stcd = shipStateCode
                        },
                        ValDtls = new
                        {
                            AssVal = Math.Round(netAmount, 2),
                            IgstVal = headerIgstVal,
                            CgstVal = headerCgstVal,
                            SgstVal = headerSgstVal,
                            CesVal = 0,
                            StCesVal = 0,
                            Discount = 0,
                            OthChrg = 0,
                            RndOffAmt = Math.Round(roundOff, 2),
                            TotInvVal = Math.Round(billAmt),
                            TotInvValFc = 0
                        },
                        ExpDtls = (object)null,
                        EwbDtls = (headerCgstVal > 0) ? (object)null : new
                        {
                            TransId = firstRow["TRANS_GSTIN"].ToString(),
                            TransName = firstRow["TRANS_NAME"].ToString(),
                            TransMode = (string)null,
                            Distance = firstRow["DISTANCE"] == DBNull.Value ? 0 : Convert.ToInt32(firstRow["DISTANCE"]),
                            TransDocNo = (string)null,
                            TransDocDt = (string)null,
                            VehNo = (string)null,
                            VehType = (string)null
                        },
                        PayDtls = (object)null,
                        RefDtls = (object)null,
                        AddlDocDtls = (object)null,
                        ItemList = itemList
                    };

                    billsList.Add(newBill);
                } // End of Invoice Loop

                // Final Serialization
                var options = new JsonSerializerOptions { WriteIndented = true };
                output = JsonSerializer.Serialize(billsList, options);

                System.IO.File.WriteAllText(@"C:\Invoices\eInvoice.json", output);
                //.Show(@"JSON file generated on path C:\Invoices\eInvoice.json");
                Close();

                return "OK";
            }
            else
            {
                return "No records found for this query.";
                //MessageBox.Show("No records found for this query.");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string msg = createJson();
            if(msg.Equals("OK"))
            {
                MessageBox.Show("JSON file generated successfully at C:\\Invoices\\eInvoice.json");
            } else
            {
                MessageBox.Show(msg);
            }
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

        private async void btnCreateEinv_Click(object sender, EventArgs e)
        {
            btnCreateEinv.Text = "Creating...";
            btnCreateEinv.Enabled = false;
            string msg = createJsonForApi();
            if(!msg.Equals("OK"))
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnCreateEinv.Text = "Create e-Invoice";
                btnCreateEinv.Enabled = true;
                return;
            }

            try
            {
                string filePath = @"C:\Invoices\eInvoice.json";

                // 1. Read the JSON array from the file (compatible with older .NET)
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("File not found at: " + filePath, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCreateEinv.Text = "Create e-Invoice";
                    btnCreateEinv.Enabled = true;
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
                    btnCreateEinv.Text = "Create e-Invoice";
                    btnCreateEinv.Enabled = true;
                    return;
                }

                // ---------------------------------------------------------
                // NEW CODE: Update SellerDtls in every object in the array
                // ---------------------------------------------------------
                /*foreach (var invoice in sourceArray)
                {
                    var sellerDtls = invoice["SellerDtls"];
                    if (sellerDtls != null)
                    {
                        // Update only the fields that changed
                        sellerDtls["Gstin"] = "02AMBPG7773M002";
                        sellerDtls["LglNm"] = "NIC company pvt ltd";
                        sellerDtls["Loc"] = "GANDHINAGAR";
                        sellerDtls["Pin"] = 175032;
                        sellerDtls["Stcd"] = "02";
                    }
                }*/

                // 2. Authenticate and get the token
                AuthResult authResult = await AuthenticateAsync();
                if (string.IsNullOrEmpty(authResult.Token))
                {
                    MessageBox.Show($"Authorization error: {authResult.ErrorMessage}", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnCreateEinv.Text = "Create e-Invoice";
                    btnCreateEinv.Enabled = true;
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

                // Show the summary MessageBox if AT LEAST ONE error was found
                if (hasAnyErrors)
                {
                    MessageBox.Show(summaryMessage.ToString(), "Invoice Generation Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                        string updateQuery = "UPDATE BILL SET EWAYBILL_NO = @EWAYBILL_NO, IRN = @IRN, SIGNED_INVOICE = @SIGNED_INVOICE WHERE bill_id = @bill_id AND firm = @firm";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                        {
                            // Best Practice: Define parameters once outside the loop to improve performance
                            cmd.Parameters.Add("@EWAYBILL_NO", System.Data.SqlDbType.VarChar);
                            cmd.Parameters.Add("@IRN", System.Data.SqlDbType.VarChar);
                            cmd.Parameters.Add("@SIGNED_INVOICE", System.Data.SqlDbType.VarChar); // Change to VarChar(MAX) if necessary
                            cmd.Parameters.Add("@bill_id", System.Data.SqlDbType.VarChar);
                            cmd.Parameters.Add("@firm", System.Data.SqlDbType.VarChar);

                            foreach (int i in successfulIndices)
                            {
                                var response = finalResponses[i];
                                var resultNode = response["result"];

                                if (resultNode != null)
                                {
                                    // Extract data from the successful API response
                                    string irn = resultNode["Irn"]?.ToString();
                                    string ewayBillNo = resultNode["EwbNo"]?.ToString();
                                    string signedQRCode = resultNode["SignedQRCode"]?.ToString(); // Taking SignedQRCode as per your snippet

                                    // Map bill_id using the original array
                                    string billId = sourceArray[i]?["DocDtls"]?["No"]?.ToString() ?? "";

                                    // Assign values to parameters (handle potential nulls like EwbNo gracefully)
                                    cmd.Parameters["@EWAYBILL_NO"].Value = string.IsNullOrEmpty(ewayBillNo) ? DBNull.Value : (object)ewayBillNo;
                                    cmd.Parameters["@IRN"].Value = string.IsNullOrEmpty(irn) ? DBNull.Value : (object)irn;
                                    cmd.Parameters["@SIGNED_INVOICE"].Value = string.IsNullOrEmpty(signedQRCode) ? DBNull.Value : (object)signedQRCode;
                                    cmd.Parameters["@bill_id"].Value = billId;
                                    cmd.Parameters["@firm"].Value = firm; // Assuming targetCompany is available in your form's scope

                                    cmd.ExecuteNonQuery();
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







                /*
                string finalOutputString = finalResponses.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

                // Save to output file (compatible with older .NET)
                string outputPath = @"C:\Invoices\eInvoice_Responses.json";
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    await writer.WriteAsync(finalOutputString);
                }

                MessageBox.Show($"Processing complete! {finalResponses.Count} invoices processed.\nSaved to: {outputPath}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);*/
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Exception", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            btnCreateEinv.Text = "Create e-Invoice";
            btnCreateEinv.Enabled = true;
        }

        /// <summary>
        /// Authenticates with the GSP server and returns the Bearer access token.
        /// </summary>
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
    }
}
