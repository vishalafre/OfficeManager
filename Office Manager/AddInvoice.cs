using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using Office_Manager;
using System.Net;


namespace Office_Manager
{
    public partial class AddInvoice : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> items = new Dictionary<string, string>();
        Dictionary<string, string> customers = new Dictionary<string, string>();
        Dictionary<string, string> godowns = new Dictionary<string, string>();

        string company;
        byte[] lPath;
        int rollCount = 1;
        double meters = 0;
        int totalQty = 0;
        double netTotal;
        Boolean isLoading = false;
        string invNoFromList;

        double netAmt = 0;
        double grossAmt = 0;
        double cgstAmt = 0;
        double sgstAmt = 0;
        double igstAmt = 0;
        double totalTax = 0;
        double billAmt = 0;
        double roundOff = 0;
        double disc = 0;

        //item , (rate, meters)
        Dictionary<string, Dictionary<double, double>> itemMeters = new Dictionary<string, Dictionary<double, double>>();

        string mCustomer;
        string mProduct;
        string mAgent;
        string mRate;
        string mPymtDeadline;
        string mDiscount;

        int txnFlag;        //1: update     //2: save

        public AddInvoice(String cName, byte[] logoPath)
        {
            InitializeComponent();
            company = cName;
            lPath = logoPath;
            label1.Text = cName;
        }

        public AddInvoice(String cName, byte[] logoPath, String intentInvNo)
        {
            InitializeComponent();
            company = cName;
            lPath = logoPath;
            invNoFromList = intentInvNo;
            invoiceNo.Text = invNoFromList;
        }

        public AddInvoice(string company, string customer, string product, string agent, string rate, string pymtDeadline, string disc)
        {
            InitializeComponent();
            this.company = company;
            mCustomer = customer;
            mProduct = product;
            mAgent = agent;
            mRate = rate;
            mPymtDeadline = pymtDeadline;
            mDiscount = disc;
        }

        class NumberToWords
        {
            private static String[] units = { "Zero", "One", "Two", "Three",
    "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven",
    "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
    "Seventeen", "Eighteen", "Nineteen" };
            private static String[] tens = { "", "", "Twenty", "Thirty", "Forty",
    "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            public static String ConvertAmount(double amount)
            {
                try
                {
                    Int64 amount_int = (Int64)amount;
                    Int64 amount_dec = (Int64)Math.Round((amount - (double)(amount_int)) * 100);
                    if (amount_dec == 0)
                    {
                        return Convert(amount_int) + " Only.";
                    }
                    else
                    {
                        return Convert(amount_int) + " Point " + Convert(amount_dec) + " Only.";
                    }
                }
                catch (Exception e)
                {
                    // TODO: handle exception  
                }
                return "";
            }

            public static String Convert(Int64 i)
            {
                if (i < 20)
                {
                    return units[i];
                }
                if (i < 100)
                {
                    return tens[i / 10] + ((i % 10 > 0) ? " " + Convert(i % 10) : "");
                }
                if (i < 1000)
                {
                    return units[i / 100] + " Hundred"
                            + ((i % 100 > 0) ? " And " + Convert(i % 100) : "");
                }
                if (i < 100000)
                {
                    return Convert(i / 1000) + " Thousand "
                    + ((i % 1000 > 0) ? " " + Convert(i % 1000) : "");
                }
                if (i < 10000000)
                {
                    return Convert(i / 100000) + " Lakh "
                            + ((i % 100000 > 0) ? " " + Convert(i % 100000) : "");
                }
                if (i < 1000000000)
                {
                    return Convert(i / 10000000) + " Crore "
                            + ((i % 10000000 > 0) ? " " + Convert(i % 10000000) : "");
                }
                return Convert(i / 1000000000) + " Arab "
                        + ((i % 1000000000 > 0) ? " " + Convert(i % 1000000000) : "");
            }
        }

        private void addRow_Click(object sender, EventArgs e)
        {
            var removeButton = new Button
            {
                Name = "removeRow" + rollCount,
                Location = new Point(addRow.Location.X - 25, addRow.Location.Y + 25 * rollCount),
                Text = "-",
                Font = addRow.Font,
                Size = addRow.Size,
                BackColor = addRow.BackColor
            };
            removeButton.Click += (s, evt) =>
            {
                rollCount--;
                copyCellsForDelete((Button)s);

                List<Control> controlsToRemove = new List<Control>();
                foreach (Control item in panel1.Controls.OfType<Control>())
                {
                    if (item.Name.EndsWith(rollCount + ""))
                    {
                        controlsToRemove.Add(item);
                    }
                }

                foreach (Control item in controlsToRemove)
                {
                    panel1.Controls.Remove(item);
                }
            };
            panel1.Controls.Add(removeButton);

            var addButton = new Button
            {
                Name = addRow.Name + rollCount,
                Location = new Point(addRow.Location.X, addRow.Location.Y + 25 * rollCount),
                Text = addRow.Text,
                Font = addRow.Font,
                Size = addRow.Size,
                BackColor = addRow.BackColor
            };
            addButton.Click += (s, evt) =>
            {
                addRow_Click(s, evt);
            };
            panel1.Controls.Add(addButton);

            Label[] labelNames = { rollNoLbl, itemLbl, qtyLbl, mtrLbl, rateLbl, weightLbl, widthLbl, godownLbl };
            TextBox[] textboxNames = { rollNo, qty, mtr, rate, weight, width };
            ComboBox[] comboBoxes = { item, godown };

            for (int i = 0; i < labelNames.Length; i++)
            {
                addLabelToPanel(labelNames[i]);
            }
            for (int i = 0; i < textboxNames.Length; i++)
            {
                addTextBoxToPanel(textboxNames[i]);
            }
            for (int i = 0; i < comboBoxes.Length; i++)
            {
                addCbToPanel(comboBoxes[i]);
            }

            copyCellsForAdd((Button)sender);

            string x = "";
            int n;
            if (rollCount > 1)
            {
                x = (rollCount - 1) + "";
            }
            TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + x, true)[0];
            if (Int32.TryParse(cRoll.Text, out n))
            {
                TextBox cRoll1 = (TextBox)panel1.Controls.Find("rollNo" + rollCount, true)[0];
                cRoll1.Text = (n + 1) + "";
                cRoll1.Focus();
            }

            TextBox cRate = (TextBox)panel1.Controls.Find("rate" + x, true)[0];
            TextBox cRate1 = (TextBox)panel1.Controls.Find("rate" + rollCount, true)[0];
            cRate1.Text = cRate.Text;

            rollCount++;
        }

        private void addLabelToPanel(Label srcLbl)
        {
            var label = new Label
            {
                Name = srcLbl.Name + rollCount,
                Location = new Point(srcLbl.Location.X, srcLbl.Location.Y + 25 * rollCount),
                Text = srcLbl.Text,
                Font = srcLbl.Font,
                Size = srcLbl.Size
            };
            panel1.Controls.Add(label);
        }

        private void addTextBoxToPanel(TextBox srcTb)
        {
            Boolean stop = true;
            if (srcTb.Name.Contains("rate"))
            {
                stop = false;
            }
            var tb = new TextBox
            {
                Name = srcTb.Name + rollCount,
                Location = new Point(srcTb.Location.X, srcTb.Location.Y + 25 * rollCount),
                Text = srcTb.Text,
                Font = srcTb.Font,
                Size = srcTb.Size,
                TabStop = stop
            };
            if (srcTb.Name.Equals("width"))
            {
                tb.KeyDown += new KeyEventHandler(mtr_KeyDown);
            }
            panel1.Controls.Add(tb);
        }

        private void addCbToPanel(ComboBox cb)
        {
            Boolean stop = true;
            if (cb.Name.Contains("item"))
            {
                stop = false;
            }
            var cBox = new ComboBox
            {
                Name = cb.Name + rollCount,
                Location = new Point(cb.Location.X, cb.Location.Y + 25 * rollCount),
                Text = cb.Text,
                Font = cb.Font,
                Size = cb.Size,
                TabStop = stop
            };
            panel1.Controls.Add(cBox);

            if (cb.Name.Contains("item"))
            {
                cBox.DataSource = new BindingSource(items, null);
                cBox.SelectedIndexChanged += (s, evt) => {
                    item_SelectedIndexChanged(s, evt);
                };
                cBox.SelectedIndex = item.SelectedIndex;
            }
            else if (cb.Name.Contains("godown"))
            {
                cBox.DataSource = new BindingSource(godowns, null);
                cBox.SelectedIndex = godown.SelectedIndex;
            }
            cBox.DisplayMember = "Value";
            cBox.ValueMember = "Key";

            cBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void copyCellsForAdd(Button s)
        {
            int index = Int32.Parse("0" + s.Name.Replace("addRow", ""));

            for (int i = rollCount - 1; i > index; i--)
            {
                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + i, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + i, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + i, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + i, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + i, true)[0];
                TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + i, true)[0];
                TextBox cWidth = (TextBox)panel1.Controls.Find("width" + i, true)[0];

                TextBox cRollPrev = (TextBox)panel1.Controls.Find("rollNo" + (i + 1), true)[0];
                TextBox cQtyPrev = (TextBox)panel1.Controls.Find("qty" + (i + 1), true)[0];
                TextBox cMtrPrev = (TextBox)panel1.Controls.Find("mtr" + (i + 1), true)[0];
                TextBox cRatePrev = (TextBox)panel1.Controls.Find("rate" + (i + 1), true)[0];
                ComboBox cItemPrev = (ComboBox)panel1.Controls.Find("item" + (i + 1), true)[0];
                TextBox cWeightPrev = (TextBox)panel1.Controls.Find("weight" + (i + 1), true)[0];
                TextBox cWidthPrev = (TextBox)panel1.Controls.Find("width" + (i + 1), true)[0];

                cRollPrev.Text = cRoll.Text;
                cQtyPrev.Text = cQty.Text;
                cMtrPrev.Text = cMtr.Text;
                cRatePrev.Text = cRate.Text;
                cItemPrev.SelectedIndex = cItem.SelectedIndex;
                cWeightPrev.Text = cWeight.Text;
                cWidthPrev.Text = cWidth.Text;
            }

            index++;
            TextBox cRollCurr = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
            TextBox cQtyCurr = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
            TextBox cMtrCurr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
            TextBox cRateCurr = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
            ComboBox cItemCurr = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
            TextBox cWeightCurr = (TextBox)panel1.Controls.Find("weight" + index, true)[0];
            TextBox cWidthCurr = (TextBox)panel1.Controls.Find("width" + index, true)[0];

            cRollCurr.Text = "";
            cQtyCurr.Text = "";
            cMtrCurr.Text = "";
            cWeightCurr.Text = "";
            cWidthCurr.Text = "";
        }

        private void copyCellsForDelete(Button s)
        {
            int index = Int32.Parse(s.Name.Replace("removeRow", ""));

            for (int i = index + 1; i <= rollCount; i++)
            {
                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + i, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + i, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + i, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + i, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + i, true)[0];
                TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + i, true)[0];
                TextBox cWidth = (TextBox)panel1.Controls.Find("width" + i, true)[0];

                TextBox cRollPrev = (TextBox)panel1.Controls.Find("rollNo" + (i - 1), true)[0];
                TextBox cQtyPrev = (TextBox)panel1.Controls.Find("qty" + (i - 1), true)[0];
                TextBox cMtrPrev = (TextBox)panel1.Controls.Find("mtr" + (i - 1), true)[0];
                TextBox cRatePrev = (TextBox)panel1.Controls.Find("rate" + (i - 1), true)[0];
                ComboBox cItemPrev = (ComboBox)panel1.Controls.Find("item" + (i - 1), true)[0];
                TextBox cWeightPrev = (TextBox)panel1.Controls.Find("weight" + (i - 1), true)[0];
                TextBox cWidthPrev = (TextBox)panel1.Controls.Find("width" + (i - 1), true)[0];

                cRollPrev.Text = cRoll.Text;
                cQtyPrev.Text = cQty.Text;
                cMtrPrev.Text = cMtr.Text;
                cRatePrev.Text = cRate.Text;
                cItemPrev.SelectedIndex = cItem.SelectedIndex;
                cWeightPrev.Text = cWeight.Text;
                cWidthPrev.Text = cWidth.Text;
            }
        }

        private void fillExcelNew(string type)
        {
            meters = 0;
            String cGstin = "";
            String cAddress = "";
            String cMobile = "";
            String cOffice = "";
            String cEmail = "";
            String bName = "";
            String bkAddress = "";
            String ifsc = "";
            String acNo = "";
            byte[] logo = null;

            String bGstin = "";
            String bAddress = "";
            String sGstin = "";
            String sAddress = "";
            String unit = "";

            string query = "SELECT * from company where NAME = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    cGstin = oReader["GSTIN"].ToString();
                    cAddress = oReader["C_ADDRESS"].ToString();
                    cMobile = oReader["MOBILE"].ToString();
                    cOffice = oReader["OFFICE"].ToString();
                    cEmail = oReader["EMAIL"].ToString();
                    bName = oReader["BANK_NAME"].ToString();
                    bkAddress = oReader["B_ADDRESS"].ToString();
                    ifsc = oReader["IFSC"].ToString();
                    acNo = oReader["AC_NO"].ToString();
                    logo = (byte[])oReader["LOGO_IMG"];
                }
            }

            query = "SELECT * from CUSTOMER where FIRM = @FIRM and CNAME = @CNAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@CNAME", ((KeyValuePair<string, string>)billTo.SelectedItem).Value);
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    bGstin = oReader["GSTIN"].ToString();
                    bAddress = oReader["ADDRESS"].ToString();
                }
            }

            query = "SELECT * from CUSTOMER where FIRM = @FIRM and CNAME = @CNAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@CNAME", ((KeyValuePair<string, string>)shipTo.SelectedItem).Value);
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    sGstin = oReader["GSTIN"].ToString();
                    sAddress = oReader["ADDRESS"].ToString();
                }
            }

            IWorkbook templateWorkbook;
            using (FileStream fs = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\Invoice" + type + " - 2.xlsx", FileMode.Open, FileAccess.ReadWrite))
            {
                templateWorkbook = new XSSFWorkbook(fs);
                fs.Close();
            }

            String dueDtTxt = "NA";
            if (!dueDt.Text.Equals(""))
            {
                dueDtTxt = dueDt.Text;
            }

            string amountInWords = NumberToWords.ConvertAmount(billAmt);

            ISheet sheet = templateWorkbook.GetSheet("AFRE");
            sheet.GetRow(0).GetCell(1).SetCellValue(company);
            sheet.GetRow(1).GetCell(1).SetCellValue("GSTIN : " + cGstin);
            sheet.GetRow(2).GetCell(1).SetCellValue(new Regex("\\n+").Replace(cAddress, "\n"));
            sheet.GetRow(4).GetCell(1).SetCellValue("P: " + cMobile);
            sheet.GetRow(5).GetCell(1).SetCellValue("O: " + cOffice);
            sheet.GetRow(6).GetCell(1).SetCellValue("Email: " + cEmail);
            sheet.GetRow(2).GetCell(6).SetCellValue(invoiceNo.Text);
            sheet.GetRow(4).GetCell(6).SetCellValue(invoiceDt.Value.ToString("dd-MMM-yy"));
            sheet.GetRow(5).GetCell(6).SetCellValue(dueDtTxt);
            sheet.GetRow(3).GetCell(6).SetCellValue(eWayBill.Text);
            sheet.GetRow(8).GetCell(1).SetCellValue(((KeyValuePair<string, string>)billTo.SelectedItem).Value);
            sheet.GetRow(8).GetCell(3).SetCellValue(((KeyValuePair<string, string>)shipTo.SelectedItem).Value);
            sheet.GetRow(8).GetCell(6).SetCellValue(((KeyValuePair<string, string>)agt.SelectedItem).Value);
            sheet.GetRow(9).GetCell(1).SetCellValue(bGstin);
            sheet.GetRow(9).GetCell(3).SetCellValue(sGstin);
            sheet.GetRow(9).GetCell(7).SetCellValue(((KeyValuePair<string, string>)transporter.SelectedItem).Value);
            sheet.GetRow(10).GetCell(1).SetCellValue(bAddress);
            sheet.GetRow(10).GetCell(3).SetCellValue(sAddress);
            sheet.GetRow(10).GetCell(7).SetCellValue(lrNo.Text);
            sheet.GetRow(11).GetCell(7).SetCellValue(lotNo.Text);
            sheet.GetRow(26).GetCell(7).SetCellValue(netAmt);
            sheet.GetRow(28).GetCell(7).SetCellValue(cgstAmt);
            sheet.GetRow(29).GetCell(7).SetCellValue(sgstAmt);
            sheet.GetRow(30).GetCell(7).SetCellValue(igstAmt);
            sheet.GetRow(31).GetCell(7).SetCellValue(totalTax);
            sheet.GetRow(32).GetCell(6).SetCellValue(roundOff);
            sheet.GetRow(33).GetCell(6).SetCellValue(billAmt);
            sheet.GetRow(34).GetCell(0).SetCellValue(amountInWords);

            totalQty = 0;
            int noOfRolls = 0;
            double totalAmt = 0;
            for (int x = 0; x < rollCount; x++)
            {
                string hsn = "";
                string index = "";
                if (x > 0)
                {
                    index = x + "";
                }
                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];

                string roll = cRoll.Text;
                string item = ((KeyValuePair<string, string>)cItem.SelectedItem).Value;
                double rate = Double.Parse(cRate.Text);
                int qty = Int32.Parse(cQty.Text);
                totalQty += qty;
                double mtr = Double.Parse(cMtr.Text);
                meters += mtr;

                int n;
                if (int.TryParse(roll, out n))
                {
                    noOfRolls++;
                }

                query = "SELECT * from item where FIRM = @FIRM and ITEM_NAME = @ITEM_NAME";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", company);
                oCmd.Parameters.AddWithValue("@ITEM_NAME", item);
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        hsn = oReader["HSN"].ToString();
                        unit = oReader["UNIT"].ToString();
                    }
                }

                sheet.GetRow(13 + x).GetCell(0).SetCellValue(roll);
                sheet.GetRow(13 + x).GetCell(1).SetCellValue(item);
                sheet.GetRow(13 + x).GetCell(3).SetCellValue(hsn);
                sheet.GetRow(13 + x).GetCell(4).SetCellValue(qty);
                sheet.GetRow(13 + x).GetCell(5).SetCellValue(mtr);
                sheet.GetRow(13 + x).GetCell(6).SetCellValue(rate);

                double amount = round(mtr * rate, 2);
                if (checkBox1.Checked)
                {
                    amount = round(mtr * rate);
                }
                totalAmt += amount;

                sheet.GetRow(13 + x).GetCell(7).SetCellValue(amount);
            }

            disc = round(Double.Parse(disount.Text) * totalAmt / 100, 2);
            sheet.GetRow(24).GetCell(7).SetCellValue(disc);

            ICellStyle bottomBorderStyle = sheet.GetRow(24).GetCell(3).CellStyle;

            String product = "Roll";
            if (totalQty / rollCount > 1)
            {
                product = "Bale";
            }

            if (unit.Equals("PRS-PAIRS"))
            {
                sheet.GetRow(12).GetCell(5).SetCellValue("PAIRS");
            }

            sheet.GetRow(12).GetCell(0).SetCellValue(product.ToUpper() + " NO");

            if (rollCount > 1)
            {
                sheet.GetRow(13 + rollCount).GetCell(4).SetCellValue(totalQty);
                sheet.GetRow(13 + rollCount).GetCell(5).SetCellValue(meters);
                sheet.GetRow(13 + rollCount).GetCell(7).SetCellValue(totalAmt);

                if (noOfRolls > 0)
                {
                    sheet.GetRow(13 + rollCount).GetCell(0).SetCellValue(noOfRolls + " " + product);
                }

                if (rollCount % 2 == 0)
                {
                    sheet.GetRow(13 + rollCount).GetCell(4).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(5).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(7).CellStyle = sheet.GetRow(5).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(8).CellStyle = sheet.GetRow(5).GetCell(0).CellStyle;

                    if (noOfRolls > 0)
                    {
                        sheet.GetRow(13 + rollCount).GetCell(0).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    }
                }
                else
                {
                    sheet.GetRow(13 + rollCount).GetCell(4).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(5).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(7).CellStyle = sheet.GetRow(4).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(8).CellStyle = sheet.GetRow(4).GetCell(0).CellStyle;

                    if (noOfRolls > 0)
                    {
                        sheet.GetRow(13 + rollCount).GetCell(0).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    }
                }

                if (rollCount == 10)
                {
                    sheet.GetRow(24).GetCell(0).CellStyle = bottomBorderStyle;
                    sheet.GetRow(24).GetCell(4).CellStyle = bottomBorderStyle;
                }
            }
            sheet.GetRow(1).CreateCell(0);
            sheet.GetRow(2).CreateCell(0);
            sheet.GetRow(4).CreateCell(0);
            sheet.GetRow(5).CreateCell(0);

            con.Close();

            if (disount.Text == "0")
            {
                sheet.GetRow(24).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(24).GetCell(6).SetCellValue(Double.Parse(disount.Text) / 100);
            }

            if (cgst.Text == "0")
            {
                sheet.GetRow(28).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(28).GetCell(6).SetCellValue(Double.Parse(cgst.Text) / 100);
            }

            if (sgst.Text == "0")
            {
                sheet.GetRow(29).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(29).GetCell(6).SetCellValue(Double.Parse(sgst.Text) / 100);
            }

            if (igst.Text == "0")
            {
                sheet.GetRow(30).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(30).GetCell(6).SetCellValue(Double.Parse(igst.Text) / 100);
            }

            sheet.GetRow(25).GetCell(7).SetCellValue(Double.Parse(freight.Text));
            sheet.GetRow(27).GetCell(2).SetCellValue(company);
            sheet.GetRow(28).GetCell(2).SetCellValue(bName);
            sheet.GetRow(29).GetCell(2).SetCellValue(bkAddress);
            sheet.GetRow(30).GetCell(2).SetCellValue(ifsc);
            sheet.GetRow(31).GetCell(2).SetCellValue(acNo);
            sheet.GetRow(35).GetCell(5).SetCellValue("(For : " + company + ")");

            int pictureIndex = templateWorkbook.AddPicture(logo, PictureType.PNG);
            ICreationHelper helper = templateWorkbook.GetCreationHelper();
            IDrawing drawing = sheet.CreateDrawingPatriarch();
            IClientAnchor anchor = helper.CreateClientAnchor();
            anchor.Col1 = 0;//0 index based column
            anchor.Row1 = 0;//0 index based row
            IPicture picture = drawing.CreatePicture(anchor, pictureIndex);
            picture.Resize(1.01, 4.3);

            sheet.ForceFormulaRecalculation = true;

            IHeader header = sheet.Header;
            header.Right = HSSFHeader.Font("Arial Unicode MS", "regular") + HSSFHeader.FontSize((short)16) +
                 HSSFHeader.StartBold + "Transport Copy" + HSSFHeader.EndBold;

            File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\AE-" + type + ".xlsx");

            using (FileStream file = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\AE-" + type + ".xlsx", FileMode.CreateNew, FileAccess.Write))
            {
                templateWorkbook.Write(file);
                file.Close();
            }
        }


        private void fillExcel(string type)
        {
            meters = 0;
            String cGstin = "";
            String cAddress = "";
            String cMobile = "";
            String cOffice = "";
            String cEmail = "";
            String bName = "";
            String bkAddress = "";
            String ifsc = "";
            String acNo = "";
            byte[] logo = null;

            String bGstin = "";
            String bAddress = "";
            String sGstin = "";
            String sAddress = "";
            String unit = "";

            string query = "SELECT * from company where NAME = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    cGstin = oReader["GSTIN"].ToString();
                    cAddress = oReader["C_ADDRESS"].ToString();
                    cMobile = oReader["MOBILE"].ToString();
                    cOffice = oReader["OFFICE"].ToString();
                    cEmail = oReader["EMAIL"].ToString();
                    bName = oReader["BANK_NAME"].ToString();
                    bkAddress = oReader["B_ADDRESS"].ToString();
                    ifsc = oReader["IFSC"].ToString();
                    acNo = oReader["AC_NO"].ToString();
                    logo = (byte[])oReader["LOGO_IMG"];
                }
            }

            query = "SELECT * from CUSTOMER where FIRM = @FIRM and CNAME = @CNAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@CNAME", ((KeyValuePair<string, string>)billTo.SelectedItem).Value);
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    bGstin = oReader["GSTIN"].ToString();
                    bAddress = oReader["ADDRESS"].ToString();
                }
            }

            query = "SELECT * from CUSTOMER where FIRM = @FIRM and CNAME = @CNAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@CNAME", ((KeyValuePair<string, string>)shipTo.SelectedItem).Value);
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    sGstin = oReader["GSTIN"].ToString();
                    sAddress = oReader["ADDRESS"].ToString();
                }
            }

            IWorkbook templateWorkbook;
            using (FileStream fs = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\Invoice" + type + ".xlsx", FileMode.Open, FileAccess.ReadWrite))
            {
                templateWorkbook = new XSSFWorkbook(fs);
                fs.Close();
            }

            String dueDtTxt = "NA";
            if (!dueDt.Text.Equals(""))
            {
                dueDtTxt = dueDt.Text;
            }

            //MessageBox.Show("-" + cAddress + "-");

            ISheet sheet = templateWorkbook.GetSheet("AFRE");
            sheet.GetRow(0).GetCell(1).SetCellValue(company);
            sheet.GetRow(1).GetCell(1).SetCellValue("GSTIN : " + cGstin);
            sheet.GetRow(2).GetCell(1).SetCellValue(new Regex("\\n+").Replace(cAddress, "\n"));
            sheet.GetRow(4).GetCell(1).SetCellValue("P: " + cMobile);
            sheet.GetRow(5).GetCell(1).SetCellValue("O: " + cOffice);
            sheet.GetRow(6).GetCell(1).SetCellValue("Email: " + cEmail);
            sheet.GetRow(2).GetCell(6).SetCellValue(invoiceNo.Text);
            sheet.GetRow(4).GetCell(6).SetCellValue(invoiceDt.Value.ToString("dd-MMM-yy"));
            sheet.GetRow(5).GetCell(6).SetCellValue(dueDtTxt);
            sheet.GetRow(3).GetCell(6).SetCellValue(eWayBill.Text);
            sheet.GetRow(8).GetCell(1).SetCellValue(((KeyValuePair<string, string>)billTo.SelectedItem).Value);
            sheet.GetRow(8).GetCell(3).SetCellValue(((KeyValuePair<string, string>)shipTo.SelectedItem).Value);
            sheet.GetRow(8).GetCell(6).SetCellValue(((KeyValuePair<string, string>)agt.SelectedItem).Value);
            sheet.GetRow(9).GetCell(1).SetCellValue(bGstin);
            sheet.GetRow(9).GetCell(3).SetCellValue(sGstin);
            sheet.GetRow(9).GetCell(7).SetCellValue(((KeyValuePair<string, string>)transporter.SelectedItem).Value);
            sheet.GetRow(10).GetCell(1).SetCellValue(bAddress);
            sheet.GetRow(10).GetCell(3).SetCellValue(sAddress);
            sheet.GetRow(10).GetCell(7).SetCellValue(lrNo.Text);
            sheet.GetRow(11).GetCell(7).SetCellValue(lotNo.Text);
            sheet.GetRow(25).GetCell(7).SetCellValue(netAmt);
            sheet.GetRow(27).GetCell(7).SetCellValue(cgstAmt);
            sheet.GetRow(28).GetCell(7).SetCellValue(sgstAmt);
            sheet.GetRow(29).GetCell(7).SetCellValue(igstAmt);
            sheet.GetRow(30).GetCell(7).SetCellValue(totalTax);
            sheet.GetRow(32).GetCell(6).SetCellValue(roundOff);
            sheet.GetRow(33).GetCell(6).SetCellValue(billAmt);

            totalQty = 0;
            int noOfRolls = 0;
            double totalAmt = 0;
            for (int x = 0; x < rollCount; x++)
            {
                string hsn = "";
                string index = "";
                if (x > 0)
                {
                    index = x + "";
                }
                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];

                string roll = cRoll.Text;
                string item = ((KeyValuePair<string, string>)cItem.SelectedItem).Value;
                double rate = Double.Parse(cRate.Text);
                int qty = Int32.Parse(cQty.Text);
                totalQty += qty;
                double mtr = Double.Parse(cMtr.Text);
                meters += mtr;

                int n;
                if (int.TryParse(roll, out n))
                {
                    noOfRolls++;
                }

                query = "SELECT * from item where FIRM = @FIRM and ITEM_NAME = @ITEM_NAME";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", company);
                oCmd.Parameters.AddWithValue("@ITEM_NAME", item);
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        hsn = oReader["HSN"].ToString();
                        unit = oReader["UNIT"].ToString();
                    }
                }

                sheet.GetRow(13 + x).GetCell(0).SetCellValue(roll);
                sheet.GetRow(13 + x).GetCell(1).SetCellValue(item);
                sheet.GetRow(13 + x).GetCell(3).SetCellValue(hsn);
                sheet.GetRow(13 + x).GetCell(4).SetCellValue(qty);
                sheet.GetRow(13 + x).GetCell(5).SetCellValue(mtr);
                sheet.GetRow(13 + x).GetCell(6).SetCellValue(rate);

                double amount = round(mtr * rate, 2);
                if (checkBox1.Checked)
                {
                    amount = round(mtr * rate);
                }
                totalAmt += amount;

                sheet.GetRow(13 + x).GetCell(7).SetCellValue(amount);
            }
            disc = round(Double.Parse(disount.Text) * totalAmt / 100, 2);
            sheet.GetRow(24).GetCell(7).SetCellValue(disc);

            ICellStyle bottomBorderStyle = sheet.GetRow(24).GetCell(3).CellStyle;

            String product = "Roll";
            if (totalQty / rollCount > 1)
            {
                product = "Bale";
            }

            if (unit.Equals("PRS-PAIRS"))
            {
                sheet.GetRow(12).GetCell(5).SetCellValue("PAIRS");
            }

            sheet.GetRow(12).GetCell(0).SetCellValue(product.ToUpper() + " NO");

            if (rollCount > 1)
            {
                /*ICellStyle style1 = templateWorkbook.CreateCellStyle();
                style1.FillPattern = FillPattern.SolidForeground;
                style1.FillForegroundColor = sheet.GetRow(12 + rollCount).GetCell(2).CellStyle.FillBackgroundColor;
                style1.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                style1.BorderTop = NPOI.SS.UserModel.BorderStyle.Double;
                style1.TopBorderColor = sheet.GetRow(12 + rollCount).GetCell(2).CellStyle.FillForegroundColor;
                MessageBox.Show("Color : " + sheet.GetRow(12 + rollCount).GetCell(2).CellStyle.FillBackgroundColor);
                IFont font1 = templateWorkbook.CreateFont();
                font1.FontHeight = 16;
                font1.FontName = "Adobe Gothic Std B";
                style1.SetFont(font1);*/

                sheet.GetRow(13 + rollCount).GetCell(4).SetCellValue(totalQty);
                sheet.GetRow(13 + rollCount).GetCell(5).SetCellValue(meters);
                sheet.GetRow(13 + rollCount).GetCell(7).SetCellValue(totalAmt);

                if (noOfRolls > 0)
                {
                    sheet.GetRow(13 + rollCount).GetCell(0).SetCellValue(noOfRolls + " " + product);
                }

                if (rollCount % 2 == 0)
                {
                    sheet.GetRow(13 + rollCount).GetCell(4).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(5).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(7).CellStyle = sheet.GetRow(5).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(8).CellStyle = sheet.GetRow(5).GetCell(0).CellStyle;

                    if (noOfRolls > 0)
                    {
                        sheet.GetRow(13 + rollCount).GetCell(0).CellStyle = sheet.GetRow(2).GetCell(0).CellStyle;
                    }
                }
                else
                {
                    sheet.GetRow(13 + rollCount).GetCell(4).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(5).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(7).CellStyle = sheet.GetRow(4).GetCell(0).CellStyle;
                    sheet.GetRow(13 + rollCount).GetCell(8).CellStyle = sheet.GetRow(4).GetCell(0).CellStyle;

                    if (noOfRolls > 0)
                    {
                        sheet.GetRow(13 + rollCount).GetCell(0).CellStyle = sheet.GetRow(1).GetCell(0).CellStyle;
                    }
                }

                if (rollCount == 10)
                {
                    sheet.GetRow(24).GetCell(0).CellStyle = bottomBorderStyle;
                    sheet.GetRow(24).GetCell(4).CellStyle = bottomBorderStyle;
                }
            }
            sheet.GetRow(1).CreateCell(0);
            sheet.GetRow(2).CreateCell(0);
            sheet.GetRow(4).CreateCell(0);
            sheet.GetRow(5).CreateCell(0);

            con.Close();

            if (disount.Text == "0")
            {
                sheet.GetRow(24).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(24).GetCell(6).SetCellValue(Double.Parse(disount.Text) / 100);
            }

            if (cgst.Text == "0")
            {
                sheet.GetRow(27).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(27).GetCell(6).SetCellValue(Double.Parse(cgst.Text) / 100);
            }

            if (sgst.Text == "0")
            {
                sheet.GetRow(28).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(28).GetCell(6).SetCellValue(Double.Parse(sgst.Text) / 100);
            }

            if (igst.Text == "0")
            {
                sheet.GetRow(29).GetCell(6).SetCellValue("Nil");
            }
            else
            {
                sheet.GetRow(29).GetCell(6).SetCellValue(Double.Parse(igst.Text) / 100);
            }

            sheet.GetRow(31).GetCell(7).SetCellValue(Double.Parse(freight.Text));
            sheet.GetRow(26).GetCell(2).SetCellValue(company);
            sheet.GetRow(27).GetCell(2).SetCellValue(bName);
            sheet.GetRow(28).GetCell(2).SetCellValue(bkAddress);
            sheet.GetRow(29).GetCell(2).SetCellValue(ifsc);
            sheet.GetRow(30).GetCell(2).SetCellValue(acNo);
            sheet.GetRow(35).GetCell(5).SetCellValue("(For : " + company + ")");

            int pictureIndex = templateWorkbook.AddPicture(logo, PictureType.PNG);
            ICreationHelper helper = templateWorkbook.GetCreationHelper();
            IDrawing drawing = sheet.CreateDrawingPatriarch();
            IClientAnchor anchor = helper.CreateClientAnchor();
            anchor.Col1 = 0;//0 index based column
            anchor.Row1 = 0;//0 index based row
            IPicture picture = drawing.CreatePicture(anchor, pictureIndex);
            picture.Resize(1.01, 4.3);

            sheet.ForceFormulaRecalculation = true;

            IHeader header = sheet.Header;
            header.Right = HSSFHeader.Font("Arial Unicode MS", "regular") + HSSFHeader.FontSize((short)16) +
                 HSSFHeader.StartBold + "Transport Copy" + HSSFHeader.EndBold;

            File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\AE-" + type + ".xlsx");

            using (FileStream file = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\AE-" + type + ".xlsx", FileMode.CreateNew, FileAccess.Write))
            {
                templateWorkbook.Write(file);
                file.Close();
            }
        }

        public void showExcel(string m_ExcelFileName)
        {
            Microsoft.Office.Interop.Excel.Application excelApp = new Microsoft.Office.Interop.Excel.Application();
            excelApp.Visible = true;
            Microsoft.Office.Interop.Excel.Workbook wb = excelApp.Workbooks.Open(m_ExcelFileName,
                            Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                            Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                            Type.Missing, Type.Missing, Type.Missing, Type.Missing);
            Microsoft.Office.Interop.Excel.Worksheet ws = (Microsoft.Office.Interop.Excel.Worksheet)wb.Worksheets[1];
            bool userDidntCancel = excelApp.Dialogs[Microsoft.Office.Interop.Excel.XlBuiltInDialog.xlDialogPrintPreview].Show(
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing,
                                        Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            wb.Close(false, Type.Missing, Type.Missing);
            excelApp.Quit();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            DateTime date1 = new DateTime(2018, 6, 6, 0, 0, 0);
            DateTime date2 = invoiceDt.Value;
            if (button6.Text.Equals("Preview"))
            {
                if (DateTime.Compare(date2, date1) < 0)
                {
                    fillExcel("CC");
                    fillExcel("SC");
                    fillExcel("TC");
                }
                else
                {
                    fillExcelNew("CC");
                    fillExcelNew("SC");
                    fillExcelNew("TC");
                }

                MessageBox.Show("Excel Print Preview will be shown. Close the Preview to proceed printing of multiple copies.");

                string fileName = "AE-CC";
                string m_ExcelFileName = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\" + fileName + ".xlsx";
                showExcel(m_ExcelFileName);

                var addCustomer = new PreviewBill(invoiceNo.Text, company, lPath, (lrNo.Text.Trim().Equals("") && Double.Parse(igst.Text) != 0));
                addCustomer.MdiParent = ParentForm;
                addCustomer.Show();
            }
            else
            {
                if (!validateFirmData())
                {
                    MessageBox.Show("Please enter all the fields correctly and try again");
                    return;
                }

                button6.Text = "Saving";
                button6.Enabled = false;
                updateBtn.Enabled = false;
                deleteBtn.Enabled = false;

                if (!updateOrder())
                {
                    return;
                }

                Boolean flag = false;
                txnFlag = 2;
                if (DateTime.Compare(date2, date1) < 0)
                {
                    saveInvoice();
                    flag = true;
                }
                else
                {
                    saveInvoiceNew();
                    flag = true;
                }

                if (flag)
                {
                    con.Open();
                    SqlCommand cmd = new SqlCommand("update item set rate = (select top 1 rate from bill_item bi, bill b where b.bill_id = bi.BILL_ID and item = item_id order by bill_dt desc)", con);
                    cmd.ExecuteNonQuery();

                    ComboBox cItem = (ComboBox)panel1.Controls.Find("item", true)[0];

                    String query = "select taka from product where pid = (select pid_pk from item where item_id = @ITEM_ID)";
                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@ITEM_ID", ((KeyValuePair<string, string>)cItem.SelectedItem).Key);

                    Boolean taka = false;
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            if (oReader["TAKA"].ToString().Equals("Y"))
                            {
                                taka = true;
                            }
                        }
                    }

                    if (taka)
                    {
                        cmd = new SqlCommand("insert into taka_despatch (FIRM, TAKA_CNT, MTR, QUALITY, DESPATCH_DATE, GODOWN, BILL_ID) values (@FIRM, @TAKA_CNT, @MTR, (select pid_pk from item where item_id = @ITEM_ID), @DESPATCH_DATE, @GODOWN, @BILL_ID)", con);
                        cmd.Parameters.AddWithValue("@firm", company);
                        cmd.Parameters.AddWithValue("@TAKA_CNT", totalQty);
                        cmd.Parameters.AddWithValue("@MTR", meters);
                        cmd.Parameters.AddWithValue("@ITEM_ID", ((KeyValuePair<string, string>)cItem.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@DESPATCH_DATE", invoiceDt.Value.ToString("dd-MMM-yy"));
                        cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)godown.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                        cmd.ExecuteNonQuery();
                    }

                    con.Close();

                    uploadRollNo();
                    insertRolls();
                }

                button6.Text = "Preview";
                updateBtn.Enabled = true;
                button6.Enabled = true;
                deleteBtn.Enabled = true;

                updateBtn.Visible = true;
                deleteBtn.Visible = true;
            }
        }

        public static double round(double d, int precision)
        {
            return (double)round(d * Math.Pow(10, precision)) / Math.Pow(10, precision);
        }

        public static int round(double d)
        {
            int i = (int)d;
            double j = d - i;
            return ((j < 0.5) ? i : (i + 1));
        }

        private void saveInvoiceNew()
        {
            netAmt = 0;
            cgstAmt = 0;
            sgstAmt = 0;
            igstAmt = 0;
            totalTax = 0;
            billAmt = 0;
            roundOff = 0;
            disc = 0;

            // Get Roll Nos
            meters = 0;
            Boolean itemAmtRounding = checkBox1.Checked;
            Boolean netAmtRounding = checkBox2.Checked;
            Boolean gstRounding = checkBox3.Checked;
            Boolean taxRounding = checkBox4.Checked;
            Boolean billAmtRounding = checkBox5.Checked;

            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];

                if (itemAmtRounding)
                {
                    netAmt += round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text));
                }
                else
                {
                    netAmt += round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text), 2);
                }
            }

            disc = round(Double.Parse(disount.Text) * netAmt / 100, 2);
            grossAmt = netAmt;
            netAmt -= disc;
            netAmt += round(Double.Parse(freight.Text), 2);
            if (netAmtRounding)
            {
                netAmt = round(netAmt);
            }
            cgstAmt = round(netAmt * Double.Parse(cgst.Text) / 100, 2);
            sgstAmt = round(netAmt * Double.Parse(sgst.Text) / 100, 2);
            igstAmt = round(netAmt * Double.Parse(igst.Text) / 100, 2);

            if (gstRounding)
            {
                cgstAmt = round(cgstAmt);
                sgstAmt = round(sgstAmt);
                igstAmt = round(igstAmt);
            }

            totalTax = cgstAmt + sgstAmt + igstAmt;
            if (taxRounding)
            {
                totalTax = round(totalTax);
            }

            billAmt = netAmt + totalTax;
            if (billAmtRounding)
            {
                roundOff = round(billAmt) - billAmt;
                billAmt = round(billAmt);
            }

            string dueDateTxt = "null";
            if (!dueDt.Text.Equals("") && !dueDt.Text.Equals("NA"))
            {
                dueDateTxt = "'" + dueDt.Text + "'";
            }

            con.Open();

            SqlCommand cmd = new SqlCommand("insert into bill values(@FIRM, " +
                "@BILL_ID, @BILL_DT, " + dueDateTxt + ", @BILL_TO, @SHIP_TO, @TRANSPORTER, " +
                "@CGST, @SGST, @IGST, @DISCOUNT, @FREIGHT, " +
                "@AGENT, @LOT_NO, @LR_NO, @EWAYBILL_NO, @CGST_AMT, @SGST_AMT, @IGST_AMT, @NET_AMT, @BILL_AMT, " +
                "@ROUNDING_PREF)", con);

            string roundingPref = ((checkBox1.Checked) ? "1" : "0") +
                ((checkBox2.Checked) ? "1" : "0") +
                ((checkBox3.Checked) ? "1" : "0") +
                ((checkBox4.Checked) ? "1" : "0") +
                ((checkBox5.Checked) ? "1" : "0");

            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
            cmd.Parameters.AddWithValue("@BILL_DT", invoiceDt.Value.ToString("dd-MMM-yy"));
            cmd.Parameters.AddWithValue("@DUE_DT", dueDateTxt);
            cmd.Parameters.AddWithValue("@BILL_TO", ((KeyValuePair<string, string>)billTo.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@SHIP_TO", ((KeyValuePair<string, string>)shipTo.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@TRANSPORTER", ((KeyValuePair<string, string>)transporter.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@ITEM_NAME", ((KeyValuePair<string, string>)item.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@CGST", Double.Parse(cgst.Text));
            cmd.Parameters.AddWithValue("@SGST", Double.Parse(sgst.Text));
            cmd.Parameters.AddWithValue("@IGST", Double.Parse(igst.Text));
            cmd.Parameters.AddWithValue("@DISCOUNT", Double.Parse(disount.Text));
            cmd.Parameters.AddWithValue("@FREIGHT", Double.Parse(freight.Text));
            cmd.Parameters.AddWithValue("@AGENT", ((KeyValuePair<string, string>)agt.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@LOT_NO", lotNo.Text);
            cmd.Parameters.AddWithValue("@LR_NO", lrNo.Text);
            cmd.Parameters.AddWithValue("@EWAYBILL_NO", eWayBill.Text);
            cmd.Parameters.AddWithValue("@CGST_AMT", cgstAmt);
            cmd.Parameters.AddWithValue("@SGST_AMT", sgstAmt);
            cmd.Parameters.AddWithValue("@IGST_AMT", igstAmt);
            cmd.Parameters.AddWithValue("@NET_AMT", netAmt);
            cmd.Parameters.AddWithValue("@BILL_AMT", billAmt);
            cmd.Parameters.AddWithValue("@ROUNDING_PREF", roundingPref);
            int i = cmd.ExecuteNonQuery();

            //Dictionary<double, double> rateMtr = new Dictionary<double, double>();

            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }

                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + index, true)[0];
                TextBox cWidth = (TextBox)panel1.Controls.Find("width" + index, true)[0];
                ComboBox cGodown = (ComboBox)panel1.Controls.Find("godown" + index, true)[0];

                string cWt = cWeight.Text;
                if (cWt.Equals(""))
                {
                    cWt = "null";
                }

                string cWd = cWidth.Text;
                if (cWd.Equals(""))
                {
                    cWd = "null";
                }

                double meter = Double.Parse(cMtr.Text);
                meters += meter;//
                netTotal += Double.Parse(cMtr.Text) * Double.Parse(cRate.Text);

                int year = invoiceDt.Value.Year;
                int month = invoiceDt.Value.Month;
                string fy = "";

                if (month >= 4)
                {
                    fy = year + "-" + (year + 1).ToString().Substring(year.ToString().Length - 2);
                }
                else
                {
                    fy = (year - 1) + "-" + year.ToString().Substring(year.ToString().Length - 2);
                }

                string g = ((KeyValuePair<string, string>)cGodown.SelectedItem).Key;
                if (g.Equals(""))
                {
                    g = "null";
                }

                SqlCommand cmdBI = new SqlCommand("insert into BILL_ITEM values(@FIRM, " +
                    "@BILL_ID, @ROLL_NO, @ITEM, @RATE, @QTY, @MTR, @AMOUNT, " + cWt + ", " + cWd + ", @FY, @ORDER_ID, " + g + ")", con);

                string mItem = ((KeyValuePair<string, string>)cItem.SelectedItem).Key;

                cmdBI.Parameters.AddWithValue("@FIRM", company);
                cmdBI.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                cmdBI.Parameters.AddWithValue("@ROLL_NO", cRoll.Text);
                cmdBI.Parameters.AddWithValue("@ITEM", mItem);
                cmdBI.Parameters.AddWithValue("@RATE", cRate.Text);
                cmdBI.Parameters.AddWithValue("@QTY", cQty.Text);
                cmdBI.Parameters.AddWithValue("@MTR", cMtr.Text);
                cmdBI.Parameters.AddWithValue("@FY", fy);
                cmdBI.Parameters.AddWithValue("@ORDER_ID", 0);

                /*if (rateMtr.ContainsKey(Double.Parse(cRate.Text)))
                {
                    rateMtr[Double.Parse(cRate.Text)] += Double.Parse(cMtr.Text);
                }
                else
                {
                    rateMtr.Add(Double.Parse(cRate.Text), Double.Parse(cMtr.Text));
                }

                if (itemMeters.ContainsKey(mItem))
                {
                    itemMeters[mItem] = rateMtr;
                }
                else
                {
                    itemMeters.Add(mItem, rateMtr);
                }*/

                double amount = 0;
                if (itemAmtRounding)
                {
                    amount = round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text));
                }
                else
                {
                    amount = round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text), 2);
                }

                cmdBI.Parameters.AddWithValue("@AMOUNT", amount);
                cmdBI.ExecuteNonQuery();
            }
            con.Close();
        }

        private void saveInvoice()
        {
            netAmt = 0;
            cgstAmt = 0;
            sgstAmt = 0;
            igstAmt = 0;
            totalTax = 0;
            billAmt = 0;
            roundOff = 0;
            disc = 0;

            // Get Roll Nos
            meters = 0;
            Boolean itemAmtRounding = checkBox1.Checked;
            Boolean netAmtRounding = checkBox2.Checked;
            Boolean gstRounding = checkBox3.Checked;
            Boolean taxRounding = checkBox4.Checked;
            Boolean billAmtRounding = checkBox5.Checked;

            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];

                if (itemAmtRounding)
                {
                    netAmt += round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text));
                }
                else
                {
                    netAmt += round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text), 2);
                }
            }

            disc = round(Double.Parse(disount.Text) * netAmt / 100, 2);
            grossAmt = netAmt;
            netAmt -= disc;
            if (netAmtRounding)
            {
                netAmt = round(netAmt);
            }
            cgstAmt = round(netAmt * Double.Parse(cgst.Text) / 100, 2);
            sgstAmt = round(netAmt * Double.Parse(sgst.Text) / 100, 2);
            igstAmt = round(netAmt * Double.Parse(igst.Text) / 100, 2);

            if (gstRounding)
            {
                cgstAmt = round(cgstAmt);
                sgstAmt = round(sgstAmt);
                igstAmt = round(igstAmt);
            }

            totalTax = cgstAmt + sgstAmt + igstAmt;
            if (taxRounding)
            {
                totalTax = round(totalTax);
            }

            billAmt = netAmt + totalTax + round(Double.Parse(freight.Text), 2);
            if (billAmtRounding)
            {
                roundOff = round(billAmt) - billAmt;
                billAmt = round(billAmt);
            }

            string dueDateTxt = "null";
            if (!dueDt.Text.Equals("") && !dueDt.Text.Equals("NA"))
            {
                dueDateTxt = "'" + dueDt.Text + "'";
            }

            con.Open();

            SqlCommand cmd = new SqlCommand("insert into bill values(@FIRM, " +
                "@BILL_ID, @BILL_DT, " + dueDateTxt + ", @BILL_TO, @SHIP_TO, @TRANSPORTER, " +
                "@CGST, @SGST, @IGST, @DISCOUNT, @FREIGHT, " +
                "@AGENT, @LOT_NO, @LR_NO, @EWAYBILL_NO, @CGST_AMT, @SGST_AMT, @IGST_AMT, @NET_AMT, @BILL_AMT, " +
                "@ROUNDING_PREF)", con);

            string roundingPref = ((checkBox1.Checked) ? "1" : "0") +
                ((checkBox2.Checked) ? "1" : "0") +
                ((checkBox3.Checked) ? "1" : "0") +
                ((checkBox4.Checked) ? "1" : "0") +
                ((checkBox5.Checked) ? "1" : "0");

            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
            cmd.Parameters.AddWithValue("@BILL_DT", invoiceDt.Value.ToString("dd-MMM-yy"));
            cmd.Parameters.AddWithValue("@DUE_DT", dueDateTxt);
            cmd.Parameters.AddWithValue("@BILL_TO", ((KeyValuePair<string, string>)billTo.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@SHIP_TO", ((KeyValuePair<string, string>)shipTo.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@TRANSPORTER", ((KeyValuePair<string, string>)transporter.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@ITEM_NAME", ((KeyValuePair<string, string>)item.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@CGST", Double.Parse(cgst.Text));
            cmd.Parameters.AddWithValue("@SGST", Double.Parse(sgst.Text));
            cmd.Parameters.AddWithValue("@IGST", Double.Parse(igst.Text));
            cmd.Parameters.AddWithValue("@DISCOUNT", Double.Parse(disount.Text));
            cmd.Parameters.AddWithValue("@FREIGHT", Double.Parse(freight.Text));
            cmd.Parameters.AddWithValue("@AGENT", ((KeyValuePair<string, string>)agt.SelectedItem).Key);
            cmd.Parameters.AddWithValue("@LOT_NO", lotNo.Text);
            cmd.Parameters.AddWithValue("@LR_NO", lrNo.Text);
            cmd.Parameters.AddWithValue("@EWAYBILL_NO", eWayBill.Text);
            cmd.Parameters.AddWithValue("@CGST_AMT", cgstAmt);
            cmd.Parameters.AddWithValue("@SGST_AMT", sgstAmt);
            cmd.Parameters.AddWithValue("@IGST_AMT", igstAmt);
            cmd.Parameters.AddWithValue("@NET_AMT", netAmt);
            cmd.Parameters.AddWithValue("@BILL_AMT", billAmt);
            cmd.Parameters.AddWithValue("@ROUNDING_PREF", roundingPref);
            int i = cmd.ExecuteNonQuery();

            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }

                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + index, true)[0];
                TextBox cWidth = (TextBox)panel1.Controls.Find("width" + index, true)[0];
                ComboBox cGodown = (ComboBox)panel1.Controls.Find("godown" + index, true)[0];

                string cWt = cWeight.Text;
                if (cWt.Equals(""))
                {
                    cWt = "null";
                }

                string cWd = cWidth.Text;
                if (cWd.Equals(""))
                {
                    cWd = "null";
                }

                double meter = Double.Parse(cMtr.Text);
                meters += meter;//
                netTotal += Double.Parse(cMtr.Text) * Double.Parse(cRate.Text);

                int year = invoiceDt.Value.Year;
                int month = invoiceDt.Value.Month;
                string fy = "";

                if (month >= 4)
                {
                    fy = year + "-" + (year + 1).ToString().Substring(year.ToString().Length - 2);
                }
                else
                {
                    fy = (year - 1) + "-" + year.ToString().Substring(year.ToString().Length - 2);
                }

                string g = ((KeyValuePair<string, string>)cGodown.SelectedItem).Key;
                if (g.Equals(""))
                {
                    g = "null";
                }

                SqlCommand cmdBI = new SqlCommand("insert into BILL_ITEM values(@FIRM, " +
                    "@BILL_ID, @ROLL_NO, @ITEM, @RATE, @QTY, @MTR, @AMOUNT, " + cWt + ", " + cWd + ", " + fy + ", @ORDER_ID)", con);

                string mItem = ((KeyValuePair<string, string>)cItem.SelectedItem).Key;

                cmdBI.Parameters.AddWithValue("@FIRM", company);
                cmdBI.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                cmdBI.Parameters.AddWithValue("@ROLL_NO", cRoll.Text);
                cmdBI.Parameters.AddWithValue("@ITEM", mItem);
                cmdBI.Parameters.AddWithValue("@RATE", cRate.Text);
                cmdBI.Parameters.AddWithValue("@QTY", cQty.Text);
                cmdBI.Parameters.AddWithValue("@MTR", cMtr.Text);
                cmdBI.Parameters.AddWithValue("@ORDER_ID", 0);

                double amount = 0;
                if (itemAmtRounding)
                {
                    amount = round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text));
                }
                else
                {
                    amount = round(Double.Parse(cRate.Text) * Double.Parse(cMtr.Text), 2);
                }

                cmdBI.Parameters.AddWithValue("@AMOUNT", amount);
                cmdBI.ExecuteNonQuery();
            }
            con.Close();
        }

        private void AddInvoice_Load(object sender, EventArgs e)
        {
            isLoading = true;
            lotNo.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            lotNo.AutoCompleteSource = AutoCompleteSource.CustomSource;
            label1.Text = company;
            width.KeyDown += new KeyEventHandler(mtr_KeyDown);

            // Set bill to

            String query = "select CID, CNAME from customer where firm = @FIRM order by CNAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    customers.Add(oReader["CID"].ToString(), oReader["CNAME"].ToString());
                }
            }

            if (customers.Count() > 0)
            {
                billTo.DataSource = new BindingSource(customers, null);
                billTo.DisplayMember = "Value";
                billTo.ValueMember = "Key";

                shipTo.DataSource = new BindingSource(customers, null);
                shipTo.DisplayMember = "Value";
                shipTo.ValueMember = "Key";
            }

            // set godown

            query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godowns.Count() > 0)
            {
                godown.DataSource = new BindingSource(godowns, null);
                godown.DisplayMember = "Value";
                godown.ValueMember = "Key";
            }

            // Get transport

            query = "select TID, T_NAME from TRANSPORT where firm = @FIRM order by T_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            Dictionary<string, string> transporters = new Dictionary<string, string>();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    transporters.Add(oReader["TID"].ToString(), oReader["T_NAME"].ToString());
                }
            }

            if (transporters.Count() > 0)
            {
                transporter.DataSource = new BindingSource(transporters, null);
                transporter.DisplayMember = "Value";
                transporter.ValueMember = "Key";
            }

            // Set item

            query = "select ITEM_ID, ITEM_NAME from item where firm = @FIRM order by ITEM_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    items.Add(oReader["ITEM_ID"].ToString(), oReader["ITEM_NAME"].ToString());
                }
            }

            if (items.Count() > 0)
            {
                item.DataSource = new BindingSource(items, null);
                item.DisplayMember = "Value";
                item.ValueMember = "Key";
            }

            // Get agent

            query = "select AID, A_NAME from AGENT where firm = @FIRM";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            try
            {
                con.Open();
            }
            catch
            {

            }

            Dictionary<string, string> agents = new Dictionary<string, string>();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    agents.Add(oReader["AID"].ToString(), oReader["A_NAME"].ToString());
                }
            }

            if (agents.Count() > 0)
            {
                agt.DataSource = new BindingSource(agents, null);
                agt.DisplayMember = "Value";
                agt.ValueMember = "Key";
            }

            // Get LAST ROUNDING PREF

            query = "select top 1 rounding_pref from bill where firm = @FIRM order by bill_dt desc";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    string roundingPref = oReader["ROUNDING_PREF"].ToString();
                    checkBox1.Checked = (roundingPref.ToCharArray()[0] == '1');
                    checkBox2.Checked = (roundingPref.ToCharArray()[1] == '1');
                    checkBox3.Checked = (roundingPref.ToCharArray()[2] == '1');
                    checkBox4.Checked = (roundingPref.ToCharArray()[3] == '1');
                    checkBox5.Checked = (roundingPref.ToCharArray()[4] == '1');
                }
            }
            con.Close();

            if (agents.Count() > 0)
            {
                agt.DataSource = new BindingSource(agents, null);
                agt.DisplayMember = "Value";
                agt.ValueMember = "Key";
            }

            // display invoice info from intent bill id

            if (invNoFromList != null)
            {
                con.Open();

                query = "select * from bill where firm = @FIRM and BILL_ID = @BILL_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", company);
                oCmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        invoiceNo.Text = invNoFromList.ToString();
                        CultureInfo ci = CultureInfo.InvariantCulture;
                        string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                        invoiceDt.Value = DateTime.ParseExact(oReader["BILL_DT"].ToString().Split(' ')[0], sysFormat, ci);
                        string due = oReader["DUE_DT"].ToString();
                        if (due.Contains(" "))
                        {
                            due = due.Split(' ')[0];
                            dueDt.Text = due;
                            dueDtLmt.Text = (DateTime.ParseExact(due, sysFormat, ci) - invoiceDt.Value).TotalDays + "";
                        }
                        else
                        {
                            dueDtLmt.Text = "NA";
                        }
                        eWayBill.Text = oReader["EWAYBILL_NO"].ToString();
                        //oReader["DUE_DT"].ToString().Split(' ')[0];

                        billTo.SelectedIndex = billTo.FindString(customers[oReader["BILL_TO"].ToString()]);
                        shipTo.SelectedIndex = shipTo.FindString(customers[oReader["SHIP_TO"].ToString()]);
                        transporter.SelectedIndex = transporter.FindString(transporters[oReader["TRANSPORTER"].ToString()]);
                        agt.SelectedIndex = agt.FindString(agents[oReader["AGENT"].ToString()]);
                        lotNo.Text = oReader["LOT_NO"].ToString();
                        lrNo.Text = oReader["LR_NO"].ToString();

                        cgst.Text = oReader["CGST"].ToString();
                        sgst.Text = oReader["SGST"].ToString();
                        igst.Text = oReader["ISGT"].ToString();
                        disount.Text = oReader["DISCOUNT"].ToString();
                        freight.Text = oReader["FREIGHT"].ToString();

                        string roundingPref = oReader["ROUNDING_PREF"].ToString();
                        checkBox1.Checked = (roundingPref.ToCharArray()[0] == '1');
                        checkBox2.Checked = (roundingPref.ToCharArray()[1] == '1');
                        checkBox3.Checked = (roundingPref.ToCharArray()[2] == '1');
                        checkBox4.Checked = (roundingPref.ToCharArray()[3] == '1');
                        checkBox5.Checked = (roundingPref.ToCharArray()[4] == '1');

                        netAmt = Double.Parse(oReader["NET_AMT"].ToString());
                        cgstAmt = Double.Parse(oReader["CGST_AMT"].ToString());
                        sgstAmt = Double.Parse(oReader["SGST_AMT"].ToString());
                        igstAmt = Double.Parse(oReader["IGST_AMT"].ToString());
                        totalTax = cgstAmt + sgstAmt + igstAmt;

                        if (checkBox4.Checked)
                        {
                            totalTax = round(totalTax);
                        }
                        billAmt = Double.Parse(oReader["BILL_AMT"].ToString());

                        if (checkBox5.Checked)
                        {
                            roundOff = round(billAmt) - (netAmt + totalTax);
                        }
                        else
                        {
                            roundOff = 0;
                        }
                        disc = round(Double.Parse(disount.Text) * netAmt / 100, 2);
                    }
                }

                int count = 0;

                SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con1.Open();

                query = "select * from bill_item where firm = @FIRM and BILL_ID = @BILL_ID ORDER BY cast(ROLL_NO as int)";
                oCmd = new SqlCommand(query, con1);
                oCmd.Parameters.AddWithValue("@FIRM", company);
                oCmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);

                string index = "";
                try
                {
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (count > 0)
                            {
                                addRow_Click(addRow, e);
                            }
                            count++;
                        }
                    }
                }
                catch
                {
                    query = "select * from bill_item where firm = @FIRM and BILL_ID = @BILL_ID ORDER BY ROLL_NO";
                    oCmd = new SqlCommand(query, con1);
                    oCmd.Parameters.AddWithValue("@FIRM", company);
                    oCmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if (count > 0)
                            {
                                addRow_Click(addRow, e);
                            }
                            count++;
                        }
                    }
                }

                rollCount = count;
                count = 0;
                using (SqlDataReader oReader1 = oCmd.ExecuteReader())
                {
                    while (oReader1.Read())
                    {
                        if (count > 0)
                        {
                            index = count + "";
                        }

                        TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                        ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                        TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                        TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                        TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
                        TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + index, true)[0];
                        TextBox cWidth = (TextBox)panel1.Controls.Find("width" + index, true)[0];
                        ComboBox cGodown = (ComboBox)panel1.Controls.Find("godown" + index, true)[0];

                        cItem.DataSource = new BindingSource(items, null);
                        cItem.DisplayMember = "Value";
                        cItem.ValueMember = "Key";

                        cRoll.Text = oReader1["ROLL_NO"].ToString();
                        cItem.SelectedIndex = cItem.FindString(items[oReader1["ITEM"].ToString()]);
                        cRate.Text = oReader1["RATE"].ToString();
                        cQty.Text = oReader1["QTY"].ToString();
                        cMtr.Text = oReader1["MTR"].ToString();
                        cWeight.Text = oReader1["WEIGHT"].ToString();
                        cWidth.Text = oReader1["WIDTH"].ToString();
                        try
                        {
                            cGodown.SelectedIndex = godown.FindString(godowns[oReader1["GODOWN"].ToString()]);
                        }
                        catch
                        {
                            cGodown.SelectedIndex = 0;
                        }
                        count++;
                    }
                }

                con1.Close();
                con.Close();

                button6.Text = "Preview";
                updateBtn.Visible = true;
                deleteBtn.Visible = true;
            }
            else
            {
                // Set invoice no
                setBillNo(DateTime.Now.ToString("dd-MMM-yy"));

                if (mCustomer != null)
                {
                    billTo.SelectedIndex = billTo.FindString(customers[mCustomer]);
                    //shipTo.SelectedIndex = shipTo.FindString(customers[mCustomer]);
                    if (mAgent.Equals(""))
                    {
                        agt.SelectedIndex = agt.FindString("NA");
                    }
                    else
                    {
                        agt.SelectedIndex = agt.FindString(agents[mAgent]);
                    }

                    ComboBox cItem = (ComboBox)panel1.Controls.Find("item", true)[0];
                    cItem.SelectedIndex = cItem.FindString(items[mProduct]);

                    TextBox cRate = (TextBox)panel1.Controls.Find("rate", true)[0];
                    cRate.Text = mRate;

                    dueDtLmt.Text = mPymtDeadline;
                    disount.Text = mDiscount;
                }
            }
            isLoading = false;
        }

        private void setBillNo(string billDt)
        {
            int billNo = 1;
            string query = "SELECT TOP 1 CAST(REVERSE(LEFT(REVERSE(BILL_ID), CHARINDEX('/', REVERSE(BILL_ID)) - 1)) AS INT) + 1 AS MAX_VALUE FROM BILL where firm = @FIRM and bill_dt between (cast(concat('01-apr-', (select case sign(month(@BILL_DT) - 3) when 1 then cast(SUBSTRING(cast(YEAR(@BILL_DT) as varchar), LEN(cast(YEAR(@BILL_DT) as varchar)) - 1, 2) as int) else cast(SUBSTRING(cast(YEAR(@BILL_DT) as varchar), LEN(cast(YEAR(@BILL_DT) as varchar)) - 1, 2) - 1 as int) end)) as date)) and (cast(concat('31-mar-', (select case sign(month(@BILL_DT) - 3) when 1 then cast(SUBSTRING(cast(YEAR(@BILL_DT) as varchar), LEN(cast(YEAR(@BILL_DT) as varchar)) - 1, 2) + 1 as int) else cast(SUBSTRING(cast(YEAR(@BILL_DT) as varchar), LEN(cast(YEAR(@BILL_DT) as varchar)) - 1, 2) as int) end)) as date)) ORDER BY 1 DESC";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@BILL_DT", billDt);
            con.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    billNo = Int32.Parse(oReader["MAX_VALUE"].ToString());
                }
            }
            con.Close();

            string sDate = invoiceDt.Value.ToString();
            DateTime datevalue = (Convert.ToDateTime(sDate.ToString()));
            int month = Int32.Parse(datevalue.Month.ToString());

            int year = Int32.Parse(datevalue.Year.ToString().Substring(datevalue.Year.ToString().Length - 2)) - 1;
            if (month > 3)
            {
                year++;
            }

            string yearInit = year + "-" + (year + 1);

            String compInit;
            switch (company.Substring(0, 1))
            {
                case "A":
                    compInit = "AE";
                    break;

                case "E":
                    compInit = "ET";
                    break;

                default:
                    compInit = "GST";
                    break;
            }

            /*
            do
            {
                int offset = 0;
                if (initials.Length == 1)
                {
                    int index = company.ToUpper().ToCharArray()[0];
                    offset = index - 65;
                }
                char c = (char)((prefix + offset) % 26 + 65);
                prefix = prefix / 26;
                initials = c + initials;
            } while (prefix > 0);

            if (initials.Length == 1)
            {
                initials = company.Substring(0, 1) + initials;
            }*/

            String billId = "" + billNo;
            if ((billNo + "").Length == 1)
            {
                billId = "00" + billNo;
            }
            else if ((billNo + "").Length == 2)
            {
                billId = "0" + billNo;
            }

            String invNo = compInit + "/" + yearInit + "/" + billId;
            invoiceNo.Text = invNo;
        }

        private void dueDtLmt_TextChanged(object sender, EventArgs e)
        {
            int n;
            if (int.TryParse(dueDtLmt.Text, out n))
            {
                DateTime billDt = invoiceDt.Value.AddDays(Int32.Parse(dueDtLmt.Text));
                dueDt.Text = billDt.ToString("dd-MMM-yy");
            }
            else
            {
                dueDt.Text = "NA";
            }
        }

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private void invoiceDt_ValueChanged(object sender, EventArgs e)
        {
            if (!dueDtLmt.Text.Equals(""))
            {
                int n;
                if (Int32.TryParse(dueDtLmt.Text, out n))
                {
                    DateTime billDt = invoiceDt.Value.AddDays(n);
                    dueDt.Text = billDt.ToString(CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern);
                }
            }
            else
            {
                dueDt.Text = "";
            }

            if (!isLoading && !updateBtn.Visible)
            {
                setBillNo(invoiceDt.Value.ToString("dd-MMM-yy"));
            }
        }

        private void item_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (item.Items.Count > 0)
            {
                string query = "SELECT TOP 1 RATE FROM (SELECT B.BILL_DT, CAST(BI.RATE AS VARCHAR(100)) RATE FROM BILL B, BILL_ITEM BI WHERE B.BILL_ID = BI.BILL_ID AND B.BILL_TO = @BILL_TO AND BI.ITEM = @ITEM AND B.FIRM = @FIRM union select '01-01-1990', '' union select '02-jan-1990', rate from item where item_id = @ITEM AND FIRM = @FIRM) T ORDER BY BILL_DT DESC";
                SqlCommand oCmd = new SqlCommand(query, con);
                ComboBox cb = (ComboBox)sender;
                oCmd.Parameters.AddWithValue("@BILL_TO", ((KeyValuePair<string, string>)billTo.SelectedItem).Key);
                oCmd.Parameters.AddWithValue("@ITEM", ((KeyValuePair<string, string>)cb.SelectedItem).Key);
                oCmd.Parameters.AddWithValue("@FIRM", company);

                try
                {
                    con.Open();
                }
                catch
                {

                }
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        String index = cb.Name.Replace("item", "");
                        TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                        cRate.Text = oReader["RATE"].ToString();
                    }
                }
                con.Close();
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            var home = new Home();
            home.MdiParent = ParentForm;
            home.Show();

        }

        private void deleteBtn_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete this invoice?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                deleteBtn.Text = "Deleting";
                deleteBtn.Enabled = false;
                updateBtn.Enabled = false;
                button6.Enabled = false;

                string URI = "http://www.afrestudios.com/office-manager/mark_order_pending.php";

                string response = "";
                using (WebClient client = new WebClient())
                {
                    var reqparm = new System.Collections.Specialized.NameValueCollection();
                    reqparm.Add("billId", invoiceNo.Text);

                    try
                    {
                        byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                        response = Encoding.UTF8.GetString(responsebytes);
                    }
                    catch
                    {
                        button6.Enabled = true;
                        updateBtn.Enabled = true;
                        deleteBtn.Enabled = true;
                        deleteBtn.Text = "Delete";

                        MessageBox.Show("No connection to network");
                        return;
                    }
                }

                if (true)   //response.Equals("SUCCESS")
                {
                    deleteInvoice();
                    MessageBox.Show("Invoice Deleted");
                    var addInvoice = new AddInvoice(company, lPath);
                    addInvoice.MdiParent = ParentForm;
                    addInvoice.Show();
                }
                else
                {
                    MessageBox.Show("Error connecting to network");
                }

                deleteBtn.Text = "Delete";
                deleteBtn.Enabled = true;
                updateBtn.Enabled = true;
                button6.Enabled = true;
            }
        }

        private void deleteInvoice()
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE FIRM = @FIRM AND BILL_ID = @BILL_ID", con);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand("DELETE FROM BILL WHERE FIRM = @FIRM AND BILL_ID = @BILL_ID", con);
            cmd.Parameters.AddWithValue("@FIRM", company);
            cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
            cmd.ExecuteNonQuery();

            con.Close();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete " + company + "?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                con.Open();

                SqlCommand cmd = new SqlCommand("DELETE FROM CUSTOMER WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                int i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TRANSPORT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM AGENT WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL_ITEM WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM BILL WHERE FIRM = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM COMPANY WHERE NAME = @FIRM", con);
                cmd.Parameters.AddWithValue("@FIRM", company);
                i = cmd.ExecuteNonQuery();
                con.Close();

                MessageBox.Show("Firm Deleted Successfully!!");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();

            }
        }

        private Boolean validateFirmData()
        {
            double n;
            int i;
            Boolean isValid = Double.TryParse(cgst.Text, out n) && Double.TryParse(sgst.Text, out n) &&
                Double.TryParse(igst.Text, out n) && Double.TryParse(disount.Text, out n) &&
                Double.TryParse(freight.Text, out n) && Double.Parse(cgst.Text) >= 0 && Double.Parse(cgst.Text) <= 100
                && Double.Parse(sgst.Text) >= 0 && Double.Parse(sgst.Text) <= 100 && Double.Parse(igst.Text) >= 0
                && Double.Parse(igst.Text) <= 100 && Double.Parse(disount.Text) >= 0 && Double.Parse(disount.Text) <= 100;

            int it1 = billTo.Items.Count;
            int it2 = shipTo.Items.Count;
            int it3 = transporter.Items.Count;
            int it4 = item.Items.Count;

            isValid = isValid && (it1 != 0) && (it2 != 0) && (it3 != 0) && (it4 != 0);

            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }

                TextBox cRoll = (TextBox)panel1.Controls.Find("rollNo" + index, true)[0];
                TextBox cQty = (TextBox)panel1.Controls.Find("qty" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];
                TextBox cWeight = (TextBox)panel1.Controls.Find("weight" + index, true)[0];
                TextBox cWidth = (TextBox)panel1.Controls.Find("width" + index, true)[0];

                isValid = isValid && (!cRoll.Equals("")) && Int32.TryParse(cQty.Text, out i) &&
                    Double.TryParse(cMtr.Text, out n) && Double.TryParse(cRate.Text, out n) &&
                    (Double.TryParse(cWeight.Text, out n) || (cWeight.Text.Equals("") && !cQty.Text.Equals("1"))) &&
                    (Double.TryParse(cWidth.Text, out n) || (cWidth.Text.Equals("") && !cQty.Text.Equals("1")));
            }

            return isValid;
        }

        public static async Task uploadRollNo()
        {
            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            int year;
            if (DateTime.Now.Month > 3)
            {
                year = DateTime.Now.Year;
            }
            else
            {
                year = (DateTime.Now.Year - 1);
            }

            String fromDt = "01-APR-" + year;
            String toDt = "31-MAR-" + (year + 1);

            string query1 = "select top 1 b1.firm, b1.bill_id, min(bi1.roll_no) roll_no1, max(cast(roll_no as int)) roll_no2 from bill_item bi1, bill b1 where bi1.bill_id = b1.bill_id and b1.bill_id in (select bill_id from (select b.firm, max(b.bill_id) bill_id from bill_item bi, bill b where b.bill_id = bi.bill_id and BILL_DT BETWEEN @FROM_DT AND @TO_DT and qty = 1 and ISNUMERIC(roll_no) = 1 group by b.firm) t) group by b1.firm, b1.bill_id order by 4 desc";
            SqlCommand oCmd1 = new SqlCommand(query1, con1);
            oCmd1.Parameters.AddWithValue("@FROM_DT", fromDt);
            oCmd1.Parameters.AddWithValue("@TO_DT", toDt);

            string output = "";

            using (SqlDataReader oReader = oCmd1.ExecuteReader())
            {
                if (oReader.Read())
                {
                    output = oReader["FIRM"].ToString() + "|" + oReader["BILL_ID"].ToString() + "|" + oReader["ROLL_NO1"].ToString() + "|" + oReader["ROLL_NO2"].ToString();
                }
            }

            // Bale No

            output += "<-->";

            query1 = "select top 1 b1.firm, b1.bill_id, min(bi1.roll_no) roll_no1, max(roll_no) roll_no2 from bill_item bi1, bill b1 where bi1.bill_id = b1.bill_id and b1.bill_id in (select bill_id from (select b.firm, max(b.bill_id) bill_id from bill_item bi, bill b where b.bill_id = bi.bill_id and BILL_DT BETWEEN @FROM_DT AND @TO_DT and qty > 1 and ISNUMERIC(roll_no) = 1 group by b.firm) t) group by b1.firm, b1.bill_id order by 4 desc";
            oCmd1 = new SqlCommand(query1, con1);
            oCmd1.Parameters.AddWithValue("@FROM_DT", fromDt);
            oCmd1.Parameters.AddWithValue("@TO_DT", toDt);

            using (SqlDataReader oReader = oCmd1.ExecuteReader())
            {
                if (oReader.Read())
                {
                    output += oReader["FIRM"].ToString() + "|" + oReader["BILL_ID"].ToString() + "|" + oReader["ROLL_NO1"].ToString() + "|" + oReader["ROLL_NO2"].ToString();
                }
            }

            string time = "<-->" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString();
            output += time;

            con1.Close();

            File.WriteAllText(@"C:\Invoices\rollNo.txt", output);

            await Task.Run(() =>
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("u220970540", "Mycomputer12@");
                    client.UploadFile("ftp://files141.hostinger.in/office-manager/roll_no.txt", WebRequestMethods.Ftp.UploadFile, @"C:\Invoices\rollNo.txt");
                }
            });

            File.Delete(@"C:\Invoices\rollNo.txt");
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            txnFlag = 1;
            DateTime date1 = new DateTime(2018, 6, 6, 0, 0, 0);
            DateTime date2 = invoiceDt.Value;
            if (!validateFirmData())
            {
                MessageBox.Show("Please enter all the fields correctly and try again");
                return;
            }

            updateBtn.Text = "Updating";
            updateBtn.Enabled = false;
            button6.Enabled = false;
            deleteBtn.Enabled = false;

            if (!updateOrder())
            {
                return;
            }
            deleteInvoice();
            Boolean flag = false;
            if (DateTime.Compare(date2, date1) < 0)
            {
                saveInvoice();
                flag = true;
            }
            else
            {
                saveInvoiceNew();
                flag = true;
            }

            if (flag)
            {
                con.Open();
                SqlCommand cmd = new SqlCommand("update item set rate = (select top 1 rate from bill_item bi, bill b where b.bill_id = bi.BILL_ID and item = item_id order by bill_dt desc)", con);
                cmd.ExecuteNonQuery();

                ComboBox cItem = (ComboBox)panel1.Controls.Find("item", true)[0];

                String query = "select taka from product where pid = (select pid_pk from item where item_id = @ITEM_ID)";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@ITEM_ID", ((KeyValuePair<string, string>)cItem.SelectedItem).Key);

                Boolean taka = false;
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        if (oReader["TAKA"].ToString().Equals("Y"))
                        {
                            taka = true;
                        }
                    }
                }

                if (taka)
                {
                    cmd = new SqlCommand("delete from taka_despatch where bill_id = @BILL_ID", con);
                    cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                    int i = cmd.ExecuteNonQuery();

                    if (i > 0)
                    {
                        cmd = new SqlCommand("insert into taka_despatch (FIRM, TAKA_CNT, MTR, QUALITY, DESPATCH_DATE, GODOWN, BILL_ID) values (@FIRM, @TAKA_CNT, @MTR, (select pid_pk from item where item_id = @ITEM_ID), @DESPATCH_DATE, @GODOWN, @BILL_ID)", con);
                        cmd.Parameters.AddWithValue("@firm", company);
                        cmd.Parameters.AddWithValue("@TAKA_CNT", totalQty);
                        cmd.Parameters.AddWithValue("@MTR", meters);
                        cmd.Parameters.AddWithValue("@ITEM_ID", ((KeyValuePair<string, string>)cItem.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@DESPATCH_DATE", invoiceDt.Value.ToString("dd-MMM-yy"));
                        cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)godown.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                        cmd.ExecuteNonQuery();
                    }
                }
                con.Close();

                uploadRollNo();
                insertRolls();
            }

            updateBtn.Text = "Update";
            updateBtn.Enabled = true;
            button6.Enabled = true;
            deleteBtn.Enabled = true;
        }

        private async void insertRolls()
        {
            await Task.Run(() =>
            {
                SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con1.Open();

                string query1 = "SELECT (SELECT TECH_NAME FROM PRODUCT WHERE PID = RE.QUALITY) QUALITY, TXN_DATE, (SELECT W_NAME FROM WEAVER WHERE WID = RE.WEAVER) WEAVER, ROLL_NO, MTR, " +
                "(SELECT G_NAME FROM GODOWN WHERE GID = RE.godown) GODOWN FROM ROLL_ENTRY RE WHERE DESPATCHED = 'N' ORDER BY 1, 2";
                SqlCommand oCmd1 = new SqlCommand(query1, con1);

                string data = "";

                using (SqlDataReader oReader = oCmd1.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        if(!data.Equals(""))
                        {
                            data += "♣";
                        }
                        string quality = oReader["QUALITY"].ToString();
                        int mtr = (int) Double.Parse(oReader["MTR"].ToString());
                        DateTime date = (DateTime)oReader["TXN_DATE"];
                        string txnDate = date.ToString("yyyy-MM-dd");
                        string rollNo = oReader["ROLL_NO"].ToString();
                        string weaver = oReader["WEAVER"].ToString();
                        string godown = oReader["GODOWN"].ToString();

                        data += (quality + "•" + rollNo + "•" + mtr + "•" + txnDate + "•" + weaver + "•" + godown);
                    }
                }

                if (!data.Equals(""))
                {
                    string URI = "https://www.afrestudios.com/office-manager/insert_rolls.php";

                    using (WebClient client = new WebClient())
                    {
                        var reqparm = new System.Collections.Specialized.NameValueCollection();
                        reqparm.Add("data", data);

                        try
                        {
                            byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                            string resp = Encoding.UTF8.GetString(responsebytes);
                            resp += " ";
                        }
                        catch
                        {

                        }
                    }
                }
                con1.Close();
            });
        }

        private Boolean updateOrder()
        {
            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            string URI = "http://www.afrestudios.com/office-manager/mark_order_pending.php";

            string response = "";
            using (WebClient client = new WebClient())
            {
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                reqparm.Add("billId", invoiceNo.Text);

                try
                {
                    byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                    response = Encoding.UTF8.GetString(responsebytes);
                }
                catch
                {
                    con1.Close();

                    button6.Enabled = true;
                    updateBtn.Enabled = true;
                    deleteBtn.Enabled = true;

                    button6.Text = "Save";
                    updateBtn.Text = "Update";
                    deleteBtn.Text = "Delete";

                    MessageBox.Show("No connection to network");
                    return false;
                }
            }

            if (true)   //response.Equals("SUCCESS")
            {
                SqlCommand cmd = new SqlCommand("update orders set status = 'P' where order_id in (select order_id from order_supply where bill_id = @BILL_ID)", con1);
                cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from ORDER_SUPPLY WHERE BILL_ID = @BILL_ID", con1);
                cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                cmd.ExecuteNonQuery();
            }
            else
            {
                MessageBox.Show(response);
                con1.Close();
                return false;
            }

            string agentFilter = "AND AGENT IS NULL";
            string mAgent = ((KeyValuePair<string, string>)agt.SelectedItem).Value;

            if (!mAgent.Equals("NA"))
            {
                agentFilter = "AND AGENT = " + ((KeyValuePair<string, string>)agt.SelectedItem).Key;
            }

            // init itemmeters

            Dictionary<double, double> rateMtr = new Dictionary<double, double>();
            itemMeters = new Dictionary<string, Dictionary<double, double>>();
            for (int j = 0; j < rollCount; j++)
            {
                string index = "";
                if (j > 0)
                {
                    index = j + "";
                }

                ComboBox cItem = (ComboBox)panel1.Controls.Find("item" + index, true)[0];
                TextBox cMtr = (TextBox)panel1.Controls.Find("mtr" + index, true)[0];
                TextBox cRate = (TextBox)panel1.Controls.Find("rate" + index, true)[0];

                string mItem = ((KeyValuePair<string, string>)cItem.SelectedItem).Key;

                if (rateMtr.ContainsKey(Double.Parse(cRate.Text)))
                {
                    rateMtr[Double.Parse(cRate.Text)] += Double.Parse(cMtr.Text);
                }
                else
                {
                    rateMtr.Add(Double.Parse(cRate.Text), Double.Parse(cMtr.Text));
                }

                if (itemMeters.ContainsKey(mItem))
                {
                    itemMeters[mItem] = rateMtr;
                }
                else
                {
                    itemMeters.Add(mItem, rateMtr);
                }
            }

            foreach (string product in itemMeters.Keys)
            {
                // select latest pending order with bill to, product, rate and agent filter

                foreach (double rate in itemMeters[product].Keys)
                {
                    string query1 = "select top 1 ORDER_ID, QTY from ORDERS where CUSTOMER = @CUSTOMER and PRODUCT = @PRODUCT " + agentFilter + " and rate = @RATE and status = 'P' AND ORDER_DATE <= @BILL_DT order by ORDER_DATE, ORDER_ID";
                    SqlCommand oCmd1 = new SqlCommand(query1, con1);
                    oCmd1.Parameters.AddWithValue("@CUSTOMER", ((KeyValuePair<string, string>)billTo.SelectedItem).Key);
                    oCmd1.Parameters.AddWithValue("@PRODUCT", product);
                    oCmd1.Parameters.AddWithValue("@RATE", rate);
                    oCmd1.Parameters.AddWithValue("@BILL_DT", invoiceDt.Value.ToString("dd-MMM-yyyy"));

                    string orderId = "";
                    double orderQty = 0;

                    using (SqlDataReader oReader = oCmd1.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            orderId = oReader["ORDER_ID"].ToString();
                            orderQty = Double.Parse(oReader["QTY"].ToString());
                        }
                    }

                    if (!orderId.Equals(""))
                    {
                        if (true)   // response.Equals("SUCCESS")
                        {
                            SqlCommand cmd = new SqlCommand("insert into ORDER_SUPPLY (ORDER_ID, TXN_DATE, DEL_QTY, BILL_ID) values (@ORDER_ID, @TXN_DATE, @DEL_QTY, @BILL_ID)", con1);
                            cmd.Parameters.AddWithValue("@ORDER_ID", orderId);
                            cmd.Parameters.AddWithValue("@TXN_DATE", invoiceDt.Value.ToString("dd-MMM-yyyy"));
                            cmd.Parameters.AddWithValue("@DEL_QTY", itemMeters[product][rate]);
                            cmd.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                            cmd.ExecuteNonQuery();
                        }
                        else
                        {
                            MessageBox.Show("No connection to network");
                            con1.Close();
                            return false;
                        }

                        query1 = "select MAX(OS_ID) OS_ID FROM ORDER_SUPPLY";
                        oCmd1 = new SqlCommand(query1, con1);

                        int osId = 0;

                        using (SqlDataReader oReader = oCmd1.ExecuteReader())
                        {
                            if (oReader.Read())
                            {
                                osId = Int32.Parse(oReader["OS_ID"].ToString()) + 1;
                            }
                        }

                        // insert into order supply

                        URI = "http://www.afrestudios.com/office-manager/insert_order_supply.php";

                        response = "";
                        using (WebClient client = new WebClient())
                        {
                            var reqparm = new System.Collections.Specialized.NameValueCollection();
                            reqparm.Add("orderId", orderId);
                            reqparm.Add("osId", osId + "");
                            reqparm.Add("txnDt", invoiceDt.Value.ToString("yyyy-MM-dd"));
                            reqparm.Add("delQty", itemMeters[product][rate] + "");
                            reqparm.Add("billId", invoiceNo.Text);

                            try
                            {
                                byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                                response = Encoding.UTF8.GetString(responsebytes);
                            }
                            catch
                            {
                                con1.Close();

                                button6.Enabled = true;
                                updateBtn.Enabled = true;
                                deleteBtn.Enabled = true;

                                button6.Text = "Save";
                                updateBtn.Text = "Update";
                                deleteBtn.Text = "Delete";
                                MessageBox.Show("No connection to network");
                                return false;
                            }
                        }

                        // check if order is completed

                        Boolean completed = false;

                        query1 = "SELECT sum(DEL_QTY) DEL_QTY FROM ORDER_SUPPLY WHERE ORDER_ID = @ORDER_ID";
                        oCmd1 = new SqlCommand(query1, con1);
                        oCmd1.Parameters.AddWithValue("@ORDER_ID", orderId);

                        double delQty = 0;

                        using (SqlDataReader oReader = oCmd1.ExecuteReader())
                        {
                            if (oReader.Read())
                            {
                                delQty = Double.Parse(oReader["DEL_QTY"].ToString());
                                if (delQty >= orderQty)
                                {
                                    completed = true;
                                }
                            }
                        }

                        if (completed)
                        {
                            URI = "http://www.afrestudios.com/office-manager/mark_order_confirm.php";

                            response = "";
                            using (WebClient client = new WebClient())
                            {
                                var reqparm = new System.Collections.Specialized.NameValueCollection();
                                reqparm.Add("orderId", orderId);

                                try
                                {
                                    byte[] responsebytes = client.UploadValues(URI, "POST", reqparm);
                                    response = Encoding.UTF8.GetString(responsebytes);
                                }
                                catch
                                {
                                    con1.Close();

                                    button6.Enabled = true;
                                    updateBtn.Enabled = true;
                                    deleteBtn.Enabled = true;

                                    button6.Text = "Save";
                                    updateBtn.Text = "Update";
                                    deleteBtn.Text = "Delete";
                                    MessageBox.Show("No connection to network");
                                    return false;
                                }
                            }

                            if (true)   //response.Equals("SUCCESS")
                            {
                                SqlCommand cmd = new SqlCommand("update orders set status = 'C' where order_id = @ORDER_ID", con1);
                                cmd.Parameters.AddWithValue("@ORDER_ID", orderId);
                                cmd.ExecuteNonQuery();
                            }
                            else
                            {
                                MessageBox.Show("No connection to network");
                                con1.Close();
                                return false;
                            }
                        }

                        // update bill item with order

                        int oId = 0;
                        if (!orderId.Equals(""))
                        {
                            oId = Int32.Parse(orderId);
                        }

                        SqlCommand cmd1 = new SqlCommand("UPDATE BILL_ITEM SET ORDER_ID = @ORDER_ID WHERE BILL_ID = @BILL_ID AND ITEM = @PRODUCT AND RATE = @RATE", con1);
                        cmd1.Parameters.AddWithValue("@ORDER_ID", oId);
                        cmd1.Parameters.AddWithValue("@BILL_ID", invoiceNo.Text);
                        cmd1.Parameters.AddWithValue("@RATE", rate);
                        cmd1.Parameters.AddWithValue("@PRODUCT", product);
                        cmd1.ExecuteNonQuery();
                    }
                }
            }

            con1.Close();

            if (txnFlag == 1)
            {
                MessageBox.Show("Invoice Updated");
            }
            else if (txnFlag == 2)
            {
                MessageBox.Show("Invoice Created Successfully");
            }

            return true;
        }

        private void mtr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string index = "";
                if (rollCount > 1)
                {
                    index = (rollCount - 1) + "";
                }
                Button add = (Button)panel1.Controls.Find("addRow" + index, true)[0];
                addRow_Click(add, new EventArgs());
                TextBox cRoll1 = (TextBox)panel1.Controls.Find("rollNo" + (rollCount - 1), true)[0];
                cRoll1.Focus();
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            var addCustomer = new AddCustomer(company, lPath);
            addCustomer.MdiParent = ParentForm;
            addCustomer.Show();

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            var addItem = new AddItem(company, lPath);
            addItem.MdiParent = ParentForm;
            addItem.Show();

        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var addInvoice = new AddInvoice(company, lPath);
            addInvoice.MdiParent = ParentForm;
            addInvoice.Show();

        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            var invList = new InvList(company, lPath);
            invList.MdiParent = ParentForm;
            invList.Show();

        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();

        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();

        }

        private void button8_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();

        }

        private void billTo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (billTo.SelectedIndex != 0)
            {
                shipTo.SelectedIndex = billTo.SelectedIndex;
            }
            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

            con1.Open();

            String cGstin = "";
            String bGstin = "";

            string query = "SELECT * from company where NAME = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con1);
            oCmd.Parameters.AddWithValue("@FIRM", company);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    cGstin = oReader["GSTIN"].ToString();
                }
            }

            query = "SELECT * from CUSTOMER where FIRM = @FIRM and CNAME = @CNAME";
            oCmd = new SqlCommand(query, con1);
            oCmd.Parameters.AddWithValue("@FIRM", company);
            oCmd.Parameters.AddWithValue("@CNAME", ((KeyValuePair<string, string>)billTo.SelectedItem).Value);
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    bGstin = oReader["GSTIN"].ToString();
                }
            }

            DateTime today = invoiceDt.Value;
            DateTime newGSTDate = new DateTime(2022, 1, 1);

            Boolean isNewDate = today.CompareTo(newGSTDate) >= 0;

            if (cGstin.Substring(0, 2).Equals(bGstin.Substring(0, 2)))
            {
                if (!isNewDate)
                {
                    cgst.Text = "2.5";
                    sgst.Text = "2.5";
                    igst.Text = "0";
                }
                else
                {
                    cgst.Text = "2.5";
                    sgst.Text = "2.5";
                    igst.Text = "0";
                }
            }
            else
            {
                if (!isNewDate)
                {
                    cgst.Text = "0";
                    sgst.Text = "0";
                    igst.Text = "5";
                }
                else
                {
                    cgst.Text = "0";
                    sgst.Text = "0";
                    igst.Text = "5";
                }
            }

            con1.Close();
        }

        private void mtr_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            var invList = new OrderManagement(company);
            invList.MdiParent = ParentForm;
            invList.Show();
        }
    }
}
