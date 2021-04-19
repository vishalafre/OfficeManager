using System;
using System.Windows.Forms;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.Excel;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Diagnostics;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace Office_Manager
{
    public partial class PreviewBill : Form
    {
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
            } catch
            {

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
