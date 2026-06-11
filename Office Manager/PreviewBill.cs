using DocumentFormat.OpenXml.Packaging;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using NPOI.XWPF.UserModel;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Word = Microsoft.Office.Interop.Word;
using System.Linq;

namespace Office_Manager
{
    public partial class PreviewBill : Form
    {
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetDefaultPrinter(string Name);
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string m_ExcelFileName;
        string company;
        byte[] lPath;
        string billNo;
        string firm;
        Boolean noTransport;

        // Contains a reference to the hosting application
        private Microsoft.Office.Interop.Excel.Application m_XlApplication = null;
        // Contains a reference to the active workbook
        private Workbook m_Workbook = null;
        private string defaultPrinter = "";

        public PreviewBill(string billNo, string firm, byte[] logoPath)
        {
            string fileName = "AE-CC";
            m_ExcelFileName = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\" + fileName + ".xlsx";
            this.billNo = billNo;
            company = firm;
            lPath = logoPath;
            InitializeComponent();
        }

        public PreviewBill(string billNo, string firm, byte[] logoPath, Boolean noTransport)
        {
            string fileName = "AE-CC";
            m_ExcelFileName = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\" + fileName + ".xlsx";
            this.billNo = billNo;
            this.firm = firm;
            company = firm;
            lPath = logoPath;
            this.noTransport = noTransport;
            InitializeComponent();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            //var addInvoice = new AddInvoice(company, lPath);
            //addInvoice.MdiParent = ParentForm;
            //addInvoice.Show();
            
        }

        private void LoadPrinters()
        {
            printersCb.Items.Clear();

            // 1. Loop through all installed printers and add them to the ComboBox
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printersCb.Items.Add(printer);
            }

            // 2. Get the current default system printer
            PrinterSettings settings = new PrinterSettings();
            defaultPrinter = settings.PrinterName;

            // 3. Select the default printer in the ComboBox if it exists in the list
            if (printersCb.Items.Contains(defaultPrinter))
            {
                printersCb.SelectedItem = defaultPrinter;
            }
            else if (printersCb.Items.Count > 0)
            {
                // Fallback: select the first item if the default isn't found
                printersCb.SelectedIndex = 0;
            }
        }

        public void OpenFile()
        {
            // Check the file exists
            if (!System.IO.File.Exists(m_ExcelFileName)) throw new Exception();
            // Load the workbook in the WebBrowser control
            //this.webBrowser1.Navigate(m_ExcelFileName, false);
        }



        private void PreviewBill_Load(object sender, EventArgs e)
        {
            invoiceNo.Text = billNo;
            label1.Text = firm;

            if(noTransport)
            {
                oc.Checked = false;
                cc.Checked = false;
            } else
            {
                tc.Checked = false;
            }
            LoadPrinters();
        }

        private void webBrowser1_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            // Creation of the workbook object
            if ((m_Workbook = RetrieveWorkbook(m_ExcelFileName)) == null) return;
            // Create the Excel.Application
            m_XlApplication = (Microsoft.Office.Interop.Excel.Application)m_Workbook.Application;
        }

        [DllImport("ole32.dll")]
        static extern int GetRunningObjectTable
                (uint reserved, out IRunningObjectTable pprot);
        [DllImport("ole32.dll")] static extern int CreateBindCtx(uint reserved, out IBindCtx pctx);

        public Workbook RetrieveWorkbook(string xlfile)
        {
            IRunningObjectTable prot = null;
            IEnumMoniker pmonkenum = null;
            try
            {
                IntPtr pfetched = IntPtr.Zero;
                // Query the running object table (ROT)
                if (GetRunningObjectTable(0, out prot) != 0 || prot == null) return null;
                prot.EnumRunning(out pmonkenum); pmonkenum.Reset();
                IMoniker[] monikers = new IMoniker[1];
                while (pmonkenum.Next(1, monikers, pfetched) == 0)
                {
                    IBindCtx pctx; string filepathname;
                    CreateBindCtx(0, out pctx);
                    // Get the name of the file
                    monikers[0].GetDisplayName(pctx, null, out filepathname);
                    // Clean up
                    Marshal.ReleaseComObject(pctx);
                    // Search for the workbook
                    if (filepathname.IndexOf(xlfile) != -1)
                    {
                        object roval;
                        // Get a handle on the workbook
                        prot.GetObject(monikers[0], out roval);
                        return roval as Workbook;
                    }
                }
            }
            catch
            {
                return null;
            }
            finally
            {
                // Clean up
                if (prot != null) Marshal.ReleaseComObject(prot);
                if (pmonkenum != null) Marshal.ReleaseComObject(pmonkenum);
            }
            return null;
        }

        private bool SetDefaultPrinterWMI(string printerName)
        {
            try
            {
                // Query the OS for all installed printers
                string query = "SELECT * FROM Win32_Printer";

                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection printers = searcher.Get())
                {
                    foreach (ManagementObject printer in printers)
                    {
                        // Check if the current printer matches the target name (ignoring case)
                        string currentName = printer["Name"]?.ToString();

                        if (string.Equals(currentName, printerName, StringComparison.OrdinalIgnoreCase))
                        {
                            // Invoke the hardware-level SetDefaultPrinter method
                            ManagementBaseObject outParams = printer.InvokeMethod("SetDefaultPrinter", null, null);

                            // A return value of 0 means absolute success at the WMI level
                            if (outParams != null && (uint)outParams["ReturnValue"] == 0)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false; // Printer name was not found in WMI
            }
            catch (Exception ex)
            {
                // Log the error if necessary
                return false;
            }
        }

        private void SendToPrinter(String fileName)
        {
            ProcessStartInfo info = new ProcessStartInfo(fileName);
            info.Verb = "Print";
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;

            Process p = new Process();
            p.StartInfo = info;
            p.Start();

            try
            {
                p.WaitForInputIdle();
                System.Threading.Thread.Sleep(3000);
                if (false == p.CloseMainWindow())
                    p.Kill();
            }
            catch
            {

            }
        }

        private void SendToPrinter(string fileName, string targetPrinterName)
        {
            PrinterSettings settings = new PrinterSettings();
            string originalDefaultPrinter = settings.PrinterName;

            try
            {
                // 1. Force the spooler to change the default printer
                bool success = SetDefaultPrinterWMI(targetPrinterName);

                if (!success)
                {
                    MessageBox.Show("Could not change the default printer.");
                    return;
                }
                defaultPrinter = targetPrinterName;

                // 2. Print using the default verb
                ProcessStartInfo info = new ProcessStartInfo(fileName);
                info.Verb = "Print";
                info.UseShellExecute = true;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;

                Process p = new Process();
                p.StartInfo = info;
                p.Start();

                p.WaitForInputIdle();
                bool exitedCleanly = p.WaitForExit(60000);

                if (!exitedCleanly)
                {
                    if (!p.CloseMainWindow())
                    {
                        p.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}");
            }
            finally
            {
                // 3. Force the spooler to restore the original printer
                //ForceSetDefaultPrinter(originalDefaultPrinter);
            }
        }

        private void PrintPDF(string fileName, string targetPrinterName)
        {
            try
            {
                ProcessStartInfo info = new ProcessStartInfo(fileName);
                // "PrintTo" allows you to specify the printer, avoiding WMI changes
                info.Verb = "PrintTo";
                info.Arguments = $"\"{targetPrinterName}\"";
                info.UseShellExecute = true;
                info.CreateNoWindow = true;
                info.WindowStyle = ProcessWindowStyle.Hidden;

                using (Process p = new Process())
                {
                    p.StartInfo = info;
                    p.Start();

                    // Wait up to 10 seconds for the application to spool the job
                    p.WaitForExit(10000);

                    // Force kill it if it's stubbornly staying open in the background
                    if (!p.HasExited)
                    {
                        p.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Quit Excel and clean up.
                if (m_Workbook != null)
                {
                    m_Workbook.Close(true, Missing.Value, Missing.Value);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject
                                            (m_Workbook);
                    m_Workbook = null;
                }
                if (m_XlApplication != null)
                {
                    m_XlApplication.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject
                                        (m_XlApplication);
                    m_XlApplication = null;
                    System.GC.Collect();
                }
            }
            catch
            {
                MessageBox.Show("Failed to close the application");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if(printersCb.SelectedItem.ToString().Equals(defaultPrinter))
            {
                if (oc.Checked)
                {
                    SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-SC.xlsx");
                }
                if (tc.Checked)
                {
                    SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-TC.xlsx");
                }
                if (cc.Checked)
                {
                    SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-CC.xlsx");
                }
            } 
            else
            {
                bool printed = false;
                if (oc.Checked)
                {
                    SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-SC.xlsx", printersCb.SelectedItem.ToString());
                    printed = true;
                }
                if (tc.Checked)
                {
                    if (printed)
                    {
                        SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-TC.xlsx");
                    }
                    else
                    {
                        SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-TC.xlsx", printersCb.SelectedItem.ToString());
                    }
                    printed = true;
                }
                if (cc.Checked)
                {
                    if (printed)
                    {
                        SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-CC.xlsx");
                    } 
                    else
                    {
                        SendToPrinter(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\AE-CC.xlsx", printersCb.SelectedItem.ToString());
                    }
                }
            }
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

        private void button3_Click_1(object sender, EventArgs e)
        {
            var addTransporter = new AddTransporter(company, lPath);
            addTransporter.MdiParent = ParentForm;
            addTransporter.Show();
            
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

        private void button8_Click_1(object sender, EventArgs e)
        {
            Close();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            var addAgent = new AddAgent(company, lPath);
            addAgent.MdiParent = ParentForm;
            addAgent.Show();
            
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var home = new CompanyHome(company, lPath);
            home.MdiParent = ParentForm;
            home.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            btnPrintEwb.Enabled = false;

            string docxPath = @"Files\EWB_Format.docx";
            string tempDocxPath = @"Files\EWB_Temp.docx";
            string finalPdfPath = @"Files\EWB.pdf";

            con.Open();

            string query = @"
            SELECT b.BILL_ID, ewaybill_no, EWB_TIME, bill_dt, c.gstin AS FROM_GSTIN, b.firm, 
                   cs.DISTANCE, irn, cb.GSTIN AS CB_GSTIN, cb.CNAME, cs.CITY, 
                   cs.PINCODE, BILL_AMT, MIN(hsn) AS hsn, t.TRANS_ID, t.T_NAME
            FROM bill b
            JOIN bill_item bi ON b.BILL_ID = bi.BILL_ID
            JOIN item i ON i.ITEM_ID = bi.ITEM
            JOIN company c ON b.FIRM = c.NAME
            JOIN CUSTOMER cs ON cs.CID = b.SHIP_TO
            JOIN CUSTOMER cb ON cb.CID = b.BILL_TO
            JOIN TRANSPORT t ON b.TRANSPORTER = t.TID
            WHERE b.BILL_ID = @bill_id
            GROUP BY b.BILL_ID, ewaybill_no, EWB_TIME, bill_dt, c.gstin, b.firm, cs.DISTANCE, 
                     irn, cb.GSTIN, cb.CNAME, cs.CITY, cs.PINCODE, BILL_AMT, t.TRANS_ID, t.T_NAME";

            Dictionary<string, string> mappings = new Dictionary<string, string>();
            string qrEwbNo = "", qrFromGstin = "", qrEwbDt = "", billDt = "";

            // 1. Fetch Data from SQL
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.AddWithValue("@bill_id", billNo);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        qrEwbNo = reader["ewaybill_no"]?.ToString() ?? "";
                        qrFromGstin = reader["FROM_GSTIN"]?.ToString() ?? "";
                        qrEwbDt = reader["EWB_TIME"] != DBNull.Value ? Convert.ToDateTime(reader["EWB_TIME"]).ToString("dd/MM/yyyy hh:mm:ss tt") : "";
                        billDt = reader["BILL_DT"] != DBNull.Value ? Convert.ToDateTime(reader["BILL_DT"]).ToString("dd/MM/yyyy") : "";

                        mappings.Add("#EWB_NO", qrEwbNo);
                        mappings.Add("#EWB_DT", qrEwbDt);
                        mappings.Add("#BILL_DT", billDt);
                        mappings.Add("#GSTIN", qrFromGstin);
                        mappings.Add("#COMPANY", reader["firm"]?.ToString() ?? "");
                        mappings.Add("#DIST", reader["DISTANCE"]?.ToString() ?? "");
                        mappings.Add("#IRN", reader["irn"]?.ToString() ?? "");
                        mappings.Add("#TO_GSTIN", reader["CB_GSTIN"]?.ToString() ?? "");
                        mappings.Add("#PARTY", reader["CNAME"]?.ToString() ?? "");
                        mappings.Add("#CITY", reader["CITY"]?.ToString() ?? "");
                        mappings.Add("#PINCODE", reader["PINCODE"]?.ToString() ?? "");
                        mappings.Add("#BILL_NO", reader["BILL_ID"]?.ToString() ?? "");
                        mappings.Add("#BILL_AMT", reader["BILL_AMT"]?.ToString() ?? "");
                        mappings.Add("#HSN", reader["hsn"]?.ToString() ?? "");
                        mappings.Add("#TRANS_GSTIN", reader["TRANS_ID"]?.ToString() ?? "");
                        mappings.Add("#TRANS_NAME", reader["T_NAME"]?.ToString() ?? "");
                    }
                    else
                    {
                        con.Close();
                        throw new Exception("No data found for the provided BILL_ID.");
                    }
                }
            }
            con.Close();

            if (qrEwbNo.Equals(""))
            {
                MessageBox.Show("No EWB Number found for this bill. Please generate EWB first.");
                btnPrintEwb.Enabled = true;
                return;
            }

            if(qrEwbDt.Equals(""))
            {
                con.Close();
                MessageBox.Show("EWB is not generated using API. Please print the eWayBill directly from the portal.");
                btnPrintEwb.Enabled = true;
                return;
            }

            // 2. Generate new QR Code
            string qrText = $"{qrEwbNo}/{qrFromGstin}/{qrEwbDt}";
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrBytes = qrCode.GetGraphic(10);

            // 3. Edit the Word Document using OpenXML
            // First, create a copy of the template to work on
            File.Copy(docxPath, tempDocxPath, true);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(tempDocxPath, true))
            {
                // --- TEXT REPLACEMENT ---
                // The most robust way to replace text in OpenXML (avoiding the "split run" issue) 
                // is to read the entire document XML, perform string replacements, and write it back.
                string docText = null;
                using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
                {
                    docText = sr.ReadToEnd();
                }

                // Execute text replacements
                foreach (var kvp in mappings)
                {
                    // Escape XML characters just in case your DB data contains '<', '>', or '&'
                    string safeValue = System.Security.SecurityElement.Escape(kvp.Value);
                    docText = docText.Replace(kvp.Key, safeValue);
                }

                // Write the modified XML back to the document
                using (StreamWriter sw = new StreamWriter(wordDoc.MainDocumentPart.GetStream(FileMode.Create)))
                {
                    sw.Write(docText);
                }

                // --- IMAGE REPLACEMENT ---
                // Find the first image part in the document and overwrite its data stream
                ImagePart imagePart = wordDoc.MainDocumentPart.ImageParts.FirstOrDefault();
                if (imagePart != null)
                {
                    using (MemoryStream ms = new MemoryStream(qrBytes))
                    {
                        imagePart.FeedData(ms);
                    }
                }

                // Save changes to the OpenXML document
                wordDoc.MainDocumentPart.Document.Save();
            }

            // 4. Convert DOCX to PDF using Word Interop
            ConvertDocxToPdf(Path.GetFullPath(tempDocxPath), Path.GetFullPath(finalPdfPath));

            // Clean up temporary docx
            if (File.Exists(tempDocxPath))
            {
                //File.Delete(tempDocxPath);
            }

            btnPrintEwb.Enabled = true;

            // Print docx file

            if (printersCb.SelectedItem.ToString().Equals(defaultPrinter))
            {
                SendToPrinter(tempDocxPath);
            }
            else
            {
                bool printed = false;
                SendToPrinter(tempDocxPath, printersCb.SelectedItem.ToString());
                printed = true;
            }
        }

        private void ConvertDocxToPdf(string wordFile, string pdfFile)
        {
            Word.Application appWord = new Word.Application();
            appWord.Visible = false;
            Word.Document wordDocument = null;

            try
            {
                wordDocument = appWord.Documents.Open(wordFile);
                wordDocument.ExportAsFixedFormat(pdfFile, Word.WdExportFormat.wdExportFormatPDF);
            }
            finally
            {
                if (wordDocument != null)
                {
                    wordDocument.Close(Word.WdSaveOptions.wdDoNotSaveChanges);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(wordDocument);
                }
                if (appWord != null)
                {
                    appWord.Quit();
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(appWord);
                }
            }
        }
    }
}
