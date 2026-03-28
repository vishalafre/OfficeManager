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

using System.Text.Json;
using System.Collections.Generic;
using Microsoft.Office.Interop.Excel;
using NPOI.SS.Formula.Functions;
using OpenQA.Selenium.Remote;
using static NPOI.HSSF.Util.HSSFColor;
using System.Globalization;
// (Alongside your existing using statements like System.Data.SqlClient)

namespace Office_Manager
{
    public partial class GenerateEInvoice : Form
    {
        string firm;

        public GenerateEInvoice(string firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void GenerateEInvoice_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            AcceptButton = button1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            String data = textBox1.Text.ToUpper();

            string input = formatBillIds(data);
            decimal mDiscAmount = 0;

            string output = "";
            string connectionStr = @"Data Source=(localdb)\VISHAL;AttachDbFilename=|DataDirectory|\Files\DBQuery.mdf;Integrated Security=True";

            System.Data.DataTable dt = new System.Data.DataTable();

            using (SqlConnection con = new SqlConnection(connectionStr))
            {
                string query = "SELECT DISTINCT C1.CID BILL_TO, S1.CID SHIP_TO, B.BILL_ID, B.BILL_DT, CMP.GSTIN SELLER_GSTIN, CMP.CITY SELLER_CITY, CMP.PIN SELLER_PIN, C1.GSTIN CUST_GSTIN, C1.CNAME CUST_NAME, C1.ADDRESS CUST_ADDR1, C1.CITY CUST_CITY, C1.PINCODE CUST_PIN, S1.GSTIN SHIP_GSTIN, S1.CNAME SHIP_NAME, S1.ADDRESS SHIP_ADDR1, S1.CITY SHIP_CITY, S1.PINCODE SHIP_PIN, (SELECT SUM(BII.MTR*BII.RATE) - BB.NET_AMT FROM BILL BB, BILL_ITEM BII WHERE BB.BILL_ID = BII.BILL_ID AND B.BILL_ID = BB.BILL_ID GROUP BY BB.NET_AMT) DISCOUNT, B.NET_AMT, B.ISGT IGST_RATE, B.CGST_AMT, B.SGST_AMT, B.IGST_AMT, B.CGST CGST_RATE, B.SGST SGST_RATE, (B.BILL_AMT - (B.NET_AMT + B.CGST + B.SGST_AMT + B.IGST_AMT)) ROUND_OFF, B.BILL_AMT, T1.TRANS_ID TRANS_GSTIN, T1.T_NAME TRANS_NAME, S1.DISTANCE, I.HSN ITEM_HSN, LEFT(I.UNIT, CHARINDEX('-', I.UNIT) - 1) AS ITEM_UNIT, BI.ROLL_NO, BI.MTR ITEM_QTY, BI.RATE ITEM_RATE, BI.AMOUNT ITEM_AMOUNT FROM BILL B, CUSTOMER C1, CUSTOMER S1, COMPANY CMP, BILL_ITEM BI, ITEM I, TRANSPORT T1 WHERE I.ITEM_ID = BI.ITEM AND C1.CID = B.BILL_TO AND CMP.NAME = B.FIRM AND B.SHIP_TO = S1.CID AND B.TRANSPORTER = T1.TID AND B.BILL_ID = BI.BILL_ID AND B.FIRM = @FIRM AND B.BILL_ID IN " + input;
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
                    decimal discAmount = Convert.ToDecimal(firstRow["DISCOUNT"]);
                    mDiscAmount = discAmount;

                    DateTime parsedDate;
                    string billDt = DateTime.TryParse(firstRow["BILL_DT"].ToString(), out parsedDate)
                        ? parsedDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                        : "";

                    // ⚠️ NOTE: If your database uses a different column name for the TOTAL GST amounts 
                    // in the header vs the percentage rates in the items, change these three variables accordingly.
                    decimal headerIgstVal = firstRow["IGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["IGST_AMT"]);
                    decimal headerCgstVal = firstRow["CGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["CGST_AMT"]);
                    decimal headerSgstVal = firstRow["SGST_AMT"] == DBNull.Value ? 0m : Convert.ToDecimal(firstRow["SGST_AMT"]);

                    var itemList = new List<object>();

                    // Track running totals of GST applied to items
                    decimal sumPrevIgst = 0, sumPrevCgst = 0, sumPrevSgst = 0;

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

                        // Extract math variables safely
                        decimal assAmt = row["ITEM_AMOUNT"] == DBNull.Value ? 0m : Convert.ToDecimal(row["ITEM_AMOUNT"]);
                        decimal igstRate = row["IGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["IGST_RATE"]);
                        decimal cgstRate = row["CGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["CGST_RATE"]);
                        decimal sgstRate = row["SGST_RATE"] == DBNull.Value ? 0m : Convert.ToDecimal(row["SGST_RATE"]);

                        // --- CHANGES 2, 3 & 4: Calculate Item GST (Rounded to 2 decimals) ---
                        decimal itemIgstAmt = Math.Round((igstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemCgstAmt = Math.Round((cgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);
                        decimal itemSgstAmt = Math.Round((sgstRate * assAmt) / 100m, 2, MidpointRounding.AwayFromZero);

                        // --- CHANGE 6: Rounding Reconciliation on the Last Item ---
                        if (isLastItem)
                        {
                            // Force the last item to absorb any penny differences
                            if (headerIgstVal > 0 || sumPrevIgst > 0) itemIgstAmt = headerIgstVal - sumPrevIgst;
                            if (headerCgstVal > 0 || sumPrevCgst > 0) itemCgstAmt = headerCgstVal - sumPrevCgst;
                            if (headerSgstVal > 0 || sumPrevSgst > 0) itemSgstAmt = headerSgstVal - sumPrevSgst;

                            totalAmt = assAmt;
                            if (mDiscAmount > 0)
                            {
                                assAmt -= mDiscAmount;
                            }
                            discAmount = mDiscAmount;
                        }
                        else
                        {
                            discAmount = 0;
                            totalAmt = assAmt;
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
                            AssVal = netAmount,
                            IgstVal = headerIgstVal,
                            CgstVal = headerCgstVal,
                            SgstVal = headerSgstVal,
                            CesVal = 0,
                            StCesVal = 0,
                            Discount = mDiscAmount,
                            OthChrg = 0,
                            RndOffAmt = roundOff,
                            TotInvVal = Math.Round(billAmt),
                            TotInvValFc = 0
                        },
                        ExpDtls = (object)null,
                        EwbDtls = new
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
                MessageBox.Show(@"JSON file generated on path C:\Invoices\eInvoice.json");
                Close();
            }
            else
            {
                MessageBox.Show("No records found for this query.");
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
