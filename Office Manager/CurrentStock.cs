using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class CurrentStock : Form
    {
        string firm;
        string godown;
        string gName;
        Dictionary<string, string> rolls = new Dictionary<string, string>();
        Dictionary<string, string> takas = new Dictionary<string, string>();
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        double meterR;
        double totalR;
        double meterT;
        double totalT;

        public CurrentStock(string firm, string godown, string gName)
        {
            InitializeComponent();
            this.firm = firm;
            this.godown = godown;
            this.gName = gName;
        }

        private void CurrentStock_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            label6.Text = gName;

            con.Open();

            // initialize all rolls and takas

            string query = "select PID, TECH_NAME, TAKA FROM PRODUCT WHERE FIRM = @FIRM AND CATEGORY = 'Cloth'";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string taka = oReader["TAKA"].ToString();
                    if (taka.Equals("Y"))
                    {
                        takas.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                    }
                    else
                    {
                        rolls.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                    }
                }
            }

            string godownFilter = "";
            if(!gName.Equals("All"))
            {
                godownFilter = "AND GODOWN = " + godown;
            }

            // display rolls stock
            int index = 0;
            int maxHeight = 0;

            foreach(string r in rolls.Keys)
            {
                string sql = "SELECT (select ( (select isnull(sum(mtr), 0.00) from roll_entry WHERE FIRM = @FIRM "+ godownFilter + " AND QUALITY = @QUALITY AND DESPATCHED = 'N') - ( SELECT ISNULL(SUM(MTR), 0) FROM BILL_ITEM BI, BILL B, ITEM I WHERE B.FIRM = @FIRM AND BI.ITEM = I.ITEM_ID "+ godownFilter +" AND I.PID_PK = @QUALITY AND B.BILL_ID = BI.BILL_ID AND B.BILL_DT > '30-SEP-19' AND ISNUMERIC(ROLL_NO) = 1 AND QTY = 1 AND ROLL_NO NOT IN (SELECT ROLL_NO FROM ROLL R WHERE R.FY = BI.FY) ))) MTR, ISNULL((SELECT COUNT(*) FROM ( SELECT QUALITY, ROLL_NO, GODOWN FROM ROLL_ENTRY WHERE FIRM = @FIRM AND QUALITY = @QUALITY " + godownFilter + " AND DESPATCHED = 'N' GROUP BY QUALITY, ROLL_NO, GODOWN) T GROUP BY QUALITY) - (SELECT count(*) FROM BILL_ITEM BI, BILL B, ITEM I WHERE B.FIRM = @FIRM AND BI.ITEM = I.ITEM_ID "+ godownFilter +" AND I.PID_PK = @QUALITY AND GODOWN <> 2 AND B.BILL_ID = BI.BILL_ID AND B.BILL_DT > '30-SEP-19' AND ISNUMERIC(ROLL_NO) = 1 AND QTY = 1 AND ROLL_NO NOT IN (SELECT ROLL_NO FROM ROLL R WHERE R.FY = BI.FY)), 0) ROLLS";

                oCmd = new SqlCommand(sql, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@QUALITY", r);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read() && !oReader["ROLLS"].ToString().Equals("0"))
                    {
                        var q = new Label()
                        {
                            Name = "qualityR" + index,
                            Location = new Point(qualityR.Location.X, qualityR.Location.Y + 20 * index),
                            Size = qualityR.Size,
                            Text = rolls[r],
                            Font = qualityR.Font,
                            ForeColor = qualityR.ForeColor
                        };

                        var qMtr = new Label()
                        {
                            Name = "mtrR" + index,
                            Location = new Point(mtrR.Location.X, mtrR.Location.Y + 20 * index),
                            Size = mtrR.Size,
                            Text = oReader["MTR"].ToString(),
                            Font = mtrR.Font,
                            ForeColor = mtrR.ForeColor
                        };

                        var qRolls = new Label()
                        {
                            Name = "rollsR" + index,
                            Location = new Point(rollsR.Location.X, rollsR.Location.Y + 20 * index),
                            Size = rollsR.Size,
                            Text = oReader["ROLLS"].ToString(),
                            Font = rollsR.Font,
                            ForeColor = rollsR.ForeColor
                        };

                        panel1.Controls.Add(q);
                        panel1.Controls.Add(qMtr);
                        panel1.Controls.Add(qRolls);

                        index++;
                        meterR += Double.Parse(qMtr.Text);
                        totalR += Double.Parse(qRolls.Text);
                        maxHeight = qRolls.Location.Y;
                    }
                }
            }

            totalMtrR.Text = meterR.ToString();
            totalRolls.Text = totalR.ToString();

            totalMtrR.Location = new Point(totalMtrR.Location.X, maxHeight + 25);
            totalRolls.Location = new Point(totalRolls.Location.X, maxHeight + 25);
            totalLblR.Location = new Point(totalLblR.Location.X, maxHeight + 25);

            totalMtrR.Visible = true;
            totalRolls.Visible = true;
            totalLblR.Visible = true;

            // display taka stock
            index = 0;
            maxHeight = 0;

            maxHeight = takasT.Location.Y;

            foreach (string t in takas.Keys)
            {
                string sql = "SELECT (select ((select isnull(sum(mtr), 0.00) from taka_entry WHERE FIRM = @FIRM "+ godownFilter + " AND QUALITY = @QUALITY ) - (select isnull(sum(mtr), 0.00) from taka_despatch WHERE FIRM = @FIRM " + godownFilter + " AND QUALITY = @QUALITY ))) MTR, (select ((select isnull(sum(taka_cnt), 0.00) from taka_entry WHERE FIRM = @FIRM " + godownFilter + " AND QUALITY = @QUALITY ) - (select isnull(sum(taka_CNT), 0.00) from taka_despatch WHERE FIRM = @FIRM " + godownFilter + " AND QUALITY = @QUALITY ))) TAKA";

                oCmd = new SqlCommand(sql, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@QUALITY", t);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read() && Double.Parse(oReader["MTR"].ToString()) != 0)
                    {
                        var q = new Label()
                        {
                            Name = "qualityT" + index,
                            Location = new Point(qualityT.Location.X, qualityT.Location.Y + 25 * index),
                            Size = qualityT.Size,
                            Text = takas[t],
                            Font = qualityT.Font,
                            ForeColor = qualityT.ForeColor
                        };

                        var qMtr = new Label()
                        {
                            Name = "mtrT" + index,
                            Location = new Point(mtrT.Location.X, mtrT.Location.Y + 25 * index),
                            Size = mtrT.Size,
                            Text = oReader["MTR"].ToString(),
                            Font = mtrT.Font,
                            ForeColor = mtrT.ForeColor
                        };

                        var qTakas = new Label()
                        {
                            Name = "takasT" + index,
                            Location = new Point(takasT.Location.X, takasT.Location.Y + 25 * index),
                            Size = takasT.Size,
                            Text = oReader["TAKA"].ToString(),
                            Font = takasT.Font,
                            ForeColor = takasT.ForeColor
                        };

                        panel2.Controls.Add(q);
                        panel2.Controls.Add(qMtr);
                        panel2.Controls.Add(qTakas);

                        index++;
                        meterT += Double.Parse(qMtr.Text);
                        totalT += Double.Parse(qTakas.Text);
                        maxHeight = qTakas.Location.Y;
                    }
                }
            }

            totalMtrT.Text = meterT.ToString();
            totalTaka.Text = totalT.ToString();

            totalMtrT.Location = new Point(totalMtrT.Location.X, maxHeight + 25);
            totalTaka.Location = new Point(totalTaka.Location.X, maxHeight + 25);
            totalLblT.Location = new Point(totalLblR.Location.X, maxHeight + 25);

            totalMtrT.Visible = true;
            totalTaka.Visible = true;
            totalLblT.Visible = true;

            con.Close();
        }
		
		private void button1_Click(object sender, EventArgs e)
        {
            //A4 - H50
            try
            {
                generateExcel();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void generateExcel()
        {
            int officeRowIndex = 3;
            int officeColIndex = 0;

            int patondaRowIndex = 3;
            int patondaColIndex = 0;

            string previousOfficeQuality = "";
            string previousPatondaQuality = "";

            // init workbook

            IWorkbook templateWorkbook;
            using (FileStream fs = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\Roll Stock Template.xlsx", FileMode.Open, FileAccess.ReadWrite))
            {
                templateWorkbook = new XSSFWorkbook(fs);
                fs.Close();
            }

            ISheet officeSheet = templateWorkbook.GetSheet("OFFICE");
            ISheet patondaSheet = templateWorkbook.GetSheet("PATONDA");

            // fetch current roll stock

            con.Open();
            string query = "SELECT TECH_NAME, MTR, G_NAME, (SELECT COUNT(*) FROM CURRENT_ROLL_STOCK C2 WHERE C2.TECH_NAME = C1.TECH_NAME AND C2.G_NAME = C1.G_NAME) CNT FROM CURRENT_ROLL_STOCK C1 ORDER BY TECH_NAME, G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    string quality = oReader["TECH_NAME"].ToString();
                    int mtr = (int) Double.Parse(oReader["MTR"].ToString());
                    string godown = oReader["G_NAME"].ToString().ToUpper();
                    int count = Int32.Parse(oReader["CNT"].ToString());

                    if(godown.Equals("OFFICE"))
                    {
                        if (quality.Equals(previousOfficeQuality) || officeRowIndex == 3)
                        {
                            if(officeRowIndex == 3)
                            {
                                previousOfficeQuality = quality;
                            }
                            officeSheet.GetRow(officeRowIndex).GetCell(officeColIndex).SetCellValue(quality);
                            officeSheet.GetRow(officeRowIndex++).GetCell(officeColIndex + 1).SetCellValue(mtr);
                        }
                        else
                        {
                            officeRowIndex++;
                            previousOfficeQuality = quality;

                            if(officeRowIndex + count > 50)
                            {
                                officeRowIndex = 3;
                                officeColIndex+= 3;
                            }

                            officeSheet.GetRow(officeRowIndex).GetCell(officeColIndex).SetCellValue(quality);
                            officeSheet.GetRow(officeRowIndex++).GetCell(officeColIndex + 1).SetCellValue(mtr);
                        }
                    } 
                    else if(godown.Equals("PATONDA"))
                    {
                        if(quality.Equals(""))
                        {

                        }
                        if (quality.Equals(previousPatondaQuality) || patondaRowIndex == 3)
                        {
                            if (patondaRowIndex == 3)
                            {
                                previousPatondaQuality = quality;
                            }

                            if (patondaRowIndex > 49)
                            {
                                patondaRowIndex = 3;
                                patondaColIndex += 3;
                            }

                            ICell cell = patondaSheet.GetRow(patondaRowIndex).GetCell(patondaColIndex);
                            if (cell == null)
                            {
                                patondaSheet.GetRow(patondaRowIndex).CreateCell(patondaColIndex);
                            }
                            patondaSheet.GetRow(patondaRowIndex).GetCell(patondaColIndex).SetCellValue(quality);

                            cell = patondaSheet.GetRow(patondaRowIndex).GetCell(patondaColIndex + 1);
                            if (cell == null)
                            {
                                patondaSheet.GetRow(patondaRowIndex).CreateCell(patondaColIndex + 1);
                            }

                            patondaSheet.GetRow(patondaRowIndex++).GetCell(patondaColIndex + 1).SetCellValue(mtr);
                        }
                        else
                        {
                            patondaRowIndex++;
                            previousPatondaQuality = quality;

                            if (patondaRowIndex + count > 50)
                            {
                                patondaRowIndex = 3;
                                patondaColIndex += 3;
                            }

                            patondaSheet.GetRow(patondaRowIndex).GetCell(patondaColIndex).SetCellValue(quality);
                            patondaSheet.GetRow(patondaRowIndex++).GetCell(patondaColIndex + 1).SetCellValue(mtr);
                        }
                    }
                }
            }

            con.Close();

            File.Delete(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\Roll Stock.xlsx");

            using (FileStream file = new FileStream(Path.GetDirectoryName(Application.ExecutablePath) + @"\Files\Roll Stock.xlsx", FileMode.CreateNew, FileAccess.Write))
            {
                templateWorkbook.Write(file);
                file.Close();
            }

            MessageBox.Show("Report generated. Click OK to view");

            string fileName = "Roll Stock";
            string m_ExcelFileName = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath) + @"\Files\" + fileName + ".xlsx";
            //showExcel(m_ExcelFileName);
            Process.Start(m_ExcelFileName);
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
    }
}
