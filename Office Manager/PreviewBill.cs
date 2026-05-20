using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Management;

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
    }
}
