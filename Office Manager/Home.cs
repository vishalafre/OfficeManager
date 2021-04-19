using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using Office_Manager;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;

namespace Office_Manager
{
    public partial class Home : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public Home()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var newFirm = new NewFirm();
            newFirm.MdiParent = ParentForm;
            newFirm.Show();
            Close();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            int i = 0;

            string query = "Select * from company";
            SqlCommand oCmd = new SqlCommand(query, con);
            Boolean connected = false;

            if (!connected)
            {
                try
                {
                    con.Open();
                    connected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(i == 0)
                    {
                        label1.Visible = true;
                    }
                    String firm = oReader["NAME"].ToString();
                    String path = oReader["LOGO_IMG_PATH"].ToString();

                    byte[] photo_aray = (byte[]) oReader["LOGO_IMG"];
                    MemoryStream ms = new MemoryStream(photo_aray);

                    var picture = new PictureBox
                    {
                        Name = "firmLogo" + i,
                        Size = new Size(200, 200),
                        Location = new Point(125 + 458*(i%3), 140 + 300*(i/3)),
                        Image = Image.FromStream(ms),
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        BackColor = Color.White,
                    };

                    picture.Click += (s, evt) => 
                    {
                        var cHome = new CompanyHome(firm, photo_aray);
                        cHome.MdiParent = ParentForm;
                        cHome.Show();
                    };
                    Controls.Add(picture);

                    var label = new Label
                    {
                        Name = "firmLabel" + i,
                        Location = new Point(125 + 458 * (i % 3), 350 + 300 * (i / 3)),
                        Text = firm,
                        Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                        Size = new Size(200, 20)
                    };
                    Controls.Add(label);
                    i++;
                }
                con.Close();
            }

            string coneSuggestion = "Cone Supply Suggestor";

            var supplyLogo = new PictureBox
            {
                Name = "firmLogo" + i,
                Size = new Size(200, 200),
                Location = new Point(125 + 458 * (i % 3), 140 + 300 * (i / 3)),
                Image = new Bitmap(Properties.Resources.cone_supply_suggestion),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.White,
            };

            supplyLogo.Click += (s, evt) =>
            {
                var cHome = new ConeSuggestor();
                cHome.MdiParent = ParentForm;
                cHome.Show();
            };
            Controls.Add(supplyLogo);

            var text = new Label
            {
                Name = "firmLabel" + i,
                Location = new Point(125 + 458 * (i % 3), 350 + 300 * (i / 3)),
                Text = coneSuggestion,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                Size = new Size(200, 20)
            };
            Controls.Add(text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            string query = "SELECT B1.BILL_ID, CONCAT(( select (CASE WHEN b.BILL_DT >= '09-SEP-17' then '11' ELSE '00' END) ITEM_AMT from bill b where b.bill_id = b1.bill_id) , ( SELECT CASE WHEN BILL_DT < '07-SEP-17' AND B.BILL_ID <> 'AA-069/17-18' then '00' ELSE '11' END GST FROM BILL B WHERE B.BILL_ID = B1.BILL_ID), (SELECT '1')) VALUE FROM BILL B1";
            SqlCommand oCmd = new SqlCommand(query, con);

            con.Open();

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    SqlCommand cmd = new SqlCommand("UPDATE BILL SET ROUNDING_PREF = @ROUNDING_PREF WHERE BILL_ID = @BILL_ID", con1);
                    cmd.Parameters.AddWithValue("@ROUNDING_PREF", oReader["VALUE"].ToString());
                    cmd.Parameters.AddWithValue("@BILL_ID", oReader["BILL_ID"].ToString());
                    cmd.ExecuteNonQuery();
                }
            }

            con1.Close();
            con.Close();

            MessageBox.Show("Done");
        }

        static void InsertRows(ref ISheet sheet1, int fromRowIndex, int rowCount)
        {
            sheet1.ShiftRows(fromRowIndex, sheet1.LastRowNum, rowCount);

            for (int rowIndex = fromRowIndex; rowIndex < fromRowIndex + rowCount; rowIndex++)
            {
                IRow rowSource = sheet1.GetRow(rowIndex + rowCount);
                IRow rowInsert = sheet1.CreateRow(rowIndex);
                rowInsert.Height = rowSource.Height;
                for (int colIndex = 0; colIndex < rowSource.LastCellNum; colIndex++)
                {
                    ICell cellSource = rowSource.GetCell(colIndex);
                    ICell cellInsert = rowInsert.CreateCell(colIndex);
                    if (cellSource != null)
                    {
                        cellInsert.CellStyle = cellSource.CellStyle;
                    }
                }
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            IWorkbook templateWorkbook;
            using (FileStream fs = new FileStream(@"C:\Users\Vishal\OneDrive\Documents\bank.xls", FileMode.Open, FileAccess.ReadWrite))
            {
                templateWorkbook = new HSSFWorkbook(fs);
                fs.Close();
            }

            IWorkbook newWorkbook;
            using (FileStream fs = new FileStream(@"C:\Users\Vishal\OneDrive\Documents\old-format-stmt.xls", FileMode.Open, FileAccess.ReadWrite))
            {
                newWorkbook = new HSSFWorkbook(fs);
                fs.Close();
            }

            try
            {
                ISheet sheet = templateWorkbook.GetSheet("OpTransactionHistoryUX5");
                ISheet sheetNew = newWorkbook.GetSheet("afre july");

                int row = 12;
                int count = 0;

                bool rowsInserted = false;

                int totalRows = 12;
                while (true)
                {
                    if (sheet.GetRow(totalRows) == null)
                    {
                        break;
                    }
                    totalRows++;
                }

                row = totalRows - 1;
                while (true)
                {
                    if (row == 11)
                    {
                        break;
                    }

                    if (sheet.GetRow(row).GetCell(3) == null ||
                        sheet.GetRow(row).GetCell(3).StringCellValue == null ||
                        sheet.GetRow(row).GetCell(3).StringCellValue.Trim().Equals(""))
                    {
                        row--;
                        continue;
                    }


                    if (sheetNew.GetRow(13 + count).GetCell(0) == null || 
                    sheetNew.GetRow(13 + count).GetCell(0).CellType.Equals(CellType.Blank) || 
                    (sheetNew.GetRow(13 + count).GetCell(0).CellType.Equals(CellType.String) 
                    && sheetNew.GetRow(13 + count).GetCell(0).StringCellValue.Trim().Equals("")))
                    {
                        InsertRows(ref sheetNew, 13 + count, 1);
                        sheetNew.GetRow(13 + count).CreateCell(0);

                        sheetNew.GetRow(13 + count).CreateCell(1);
                        sheetNew.GetRow(13 + count).CreateCell(2);
                        sheetNew.GetRow(13 + count).CreateCell(3);
                        sheetNew.GetRow(13 + count).CreateCell(4);
                        sheetNew.GetRow(13 + count).CreateCell(5);
                        sheetNew.GetRow(13 + count).CreateCell(6);
                        sheetNew.GetRow(13 + count).CreateCell(7);
                        sheetNew.GetRow(13 + count).CreateCell(8);

                    rowsInserted = true;
                    }

                    sheetNew.GetRow(13 + count).GetCell(0).SetCellValue(count + 1);

                    string date = sheet.GetRow(row).GetCell(1).StringCellValue.Replace("/", "-");
                    sheetNew.GetRow(13 + count).GetCell(1).SetCellValue(date);

                    string narration = sheet.GetRow(row).GetCell(3).StringCellValue;
                    sheetNew.GetRow(13 + count).GetCell(2).SetCellValue(narration);

                    string chqNo = "";
                    if (sheet.GetRow(row).GetCell(9) != null)
                    {
                        chqNo = sheet.GetRow(row).GetCell(9).StringCellValue;
                        sheetNew.GetRow(13 + count).GetCell(4).SetCellValue(chqNo);
                    }
                    string debit = "";
                    if (sheet.GetRow(row).GetCell(9) != null)
                    {
                        debit = sheet.GetRow(row).GetCell(11).StringCellValue;
                        sheetNew.GetRow(13 + count).GetCell(5).SetCellValue(debit);
                    }
                    string credit = "";
                    if (sheet.GetRow(row).GetCell(9) != null)
                    {
                        credit = sheet.GetRow(row).GetCell(17).StringCellValue;
                        sheetNew.GetRow(13 + count).GetCell(6).SetCellValue(credit);
                    }
                    string balance = sheet.GetRow(row).GetCell(20).StringCellValue;
                    sheetNew.GetRow(13 + count).GetCell(7).SetCellValue(balance);

                    sheetNew.GetRow(13 + count).GetCell(8).SetCellValue(date);

                    row--;
                    count++;
                }
            
                if(rowsInserted)
            {
                sheetNew.GetRow(15 + count).GetCell(0).SetCellValue("Total no. of Transactions : " + count);
            }

                while(true && !rowsInserted)
                {
                int records = count;

                IRow r = sheetNew.GetRow(13 + count);
                    if(r == null)
                    {
                        count++;
                        continue;
                    }

                    ICell c = r.GetCell(0);
                    if(c != null)
                    {
                        if (c.CellType.Equals(CellType.String) && c.StringCellValue.Contains("Total no."))
                        {
                            c.SetCellValue("Total no. of Transactions : " + records);
                            break;
                        } else
                        {
                            sheetNew.RemoveRow(r);
                        }
                    }
                    count++;
                }

                templateWorkbook.Close();
                if (File.Exists(@"C:\Users\Vishal\OneDrive\Documents\Tally Statement.xls"))
                {
                    File.Delete(@"C:\Users\Vishal\OneDrive\Documents\Tally Statement.xls");
                }

                using (FileStream file = new FileStream(@"C:\Users\Vishal\OneDrive\Documents\Tally Statement.xls", FileMode.CreateNew, FileAccess.Write))
                {
                    newWorkbook.Write(file);
                    file.Close();
                }
                MessageBox.Show("Workbook with new format created");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                if (templateWorkbook != null)
                {
                    templateWorkbook.Close();
                }
                if (newWorkbook != null)
                {
                    newWorkbook.Close();
                }
            }
        }
    }
}
