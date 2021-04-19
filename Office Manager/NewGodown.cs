using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class NewGodown : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;
        Dictionary<string, string> products = new Dictionary<string, string>();
        Dictionary<string, string> productsMfgType = new Dictionary<string, string>();
        Dictionary<string, string> productType = new Dictionary<string, string>();
        Dictionary<string, bool> waterMarkActive = new Dictionary<string, bool>();
        Dictionary<string, string> godownBalanceInfo = new Dictionary<string, string>();

        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        string gName;
        int gId = -1;

        public NewGodown()
        {
            InitializeComponent();
        }

        public NewGodown(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public NewGodown(string firm, byte[] logo, string gName)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.gName = gName;
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void NewGodown_Load(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream(logo);
            pictureBox17.Image = Image.FromStream(ms);

            String query = "select PID, TECH_NAME, UNIT_NAME, CATEGORY, TAKA from PRODUCT P, UNIT U where P.FIRM = @FIRM AND P.UNIT = U.UID and (p.category <> 'Cloth' or p.taka = 'Y') ORDER BY TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            int i = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    products.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                    productType.Add(oReader["PID"].ToString(), oReader["CATEGORY"].ToString());
                    productsMfgType.Add(oReader["PID"].ToString(), oReader["TAKA"].ToString());

                    var product = new Label()
                    {
                        Name = "pName" + i,
                        Location = new Point(pName.Location.X, pName.Location.Y + 25 * i),
                        Font = pName.Font,
                        Text = oReader["TECH_NAME"].ToString(),
                        Size = pName.Size
                    };

                    var unit = new Label()
                    {
                        Name = "unitLbl" + i,
                        Location = new Point(unitLbl.Location.X, unitLbl.Location.Y + 25 * i),
                        Font = unitLbl.Font,
                        ForeColor = unitLbl.ForeColor,
                        Text = oReader["UNIT_NAME"].ToString(),
                        Size = unitLbl.Size
                    };

                    var qty = new TextBox()
                    {
                        Name = "qty" + i,
                        Location = new Point(this.qty.Location.X, this.qty.Location.Y + 25 * i),
                        Size = this.qty.Size,
                        Text = "0",
                    };

                    var asOn = new Label()
                    {
                        Name = "asOn" + i,
                        Location = new Point(this.asOn.Location.X, this.asOn.Location.Y + 25 * i),
                        Font = this.asOn.Font,
                        ForeColor = this.asOn.ForeColor,
                        Text = this.asOn.Text,
                        Size = this.asOn.Size
                    };

                    var obDate = new TextBox()
                    {
                        Name = "obDate" + i,
                        Location = new Point(this.obDate.Location.X, this.obDate.Location.Y + 25 * i),
                        Size = this.obDate.Size
                    };

                    setTextboxWatermark(obDate);

                    panel3.Controls.Add(product);
                    panel3.Controls.Add(qty);
                    panel3.Controls.Add(unit);
                    panel3.Controls.Add(asOn);
                    panel3.Controls.Add(obDate);

                    if(oReader["CATEGORY"].Equals("Cloth") || oReader["CATEGORY"].Equals("Yarn"))
                    {
                        var tm = new TextBox()
                        {
                            Name = "takaMtr" + i,
                            Location = new Point(this.takaMtr.Location.X, this.takaMtr.Location.Y + 25 * i),
                            Size = this.takaMtr.Size,
                            Text = "0"
                        };

                        var tml = new Label()
                        {
                            Name = "takaMtrLbl" + i,
                            Location = new Point(this.takaMtrLbl.Location.X, this.takaMtrLbl.Location.Y + 25 * i),
                            Font = this.takaMtrLbl.Font,
                            ForeColor = this.takaMtrLbl.ForeColor,
                            Text = "Pieces",
                            Size = this.takaMtrLbl.Size
                        };

                        if(oReader["CATEGORY"].ToString().Equals("Yarn"))
                        {
                            tml.Text = "Cartons";
                        }
                        else if(oReader["TAKA"].ToString().Equals("N"))
                        {
                            tml.Text = "Rolls";
                        }

                        panel3.Controls.Add(tm);
                        panel3.Controls.Add(tml);
                    }

                    i++;
                }
            }

            if(gName != null)
            {
                button1.Text = "Update";
                button2.Visible = true;

                query = "select * from godown where g_name = @G_NAME AND FIRM = @FIRM";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@G_NAME", gName);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        gId = Int32.Parse(oReader["GID"].ToString());
                        textBox1.Text = gName;
                    }
                }

                query = "select TECH_NAME, CUTS QTY, 0 CNT, txn_date from supply_beam SB, PRODUCT P where supply_from_type = 'O' AND P.PID = SB.BEAM and supply_to_type = 'G' and supply_to = @GID and P.firm = @FIRM UNION select TECH_NAME, QTY, boxes, txn_date from supply_CONE SC, PRODUCT P WHERE P.PID = SC.YARN and supply_to_type = 'G' AND supply_from_type = 'O' and supply_to = @GID and P.firm = @FIRM UNION SELECT TECH_NAME, MTR, TAKA_CNT, TXN_DATE FROM TAKA_ENTRY TE, PRODUCT P WHERE P.PID = TE.QUALITY AND GODOWN = @GID AND WEAVER IS NULL AND P.firm = @FIRM UNION SELECT TECH_NAME, SUM(MTR), COUNT(*), TXN_DATE FROM ROLL_ENTRY RE, PRODUCT P WHERE P.PID = RE.QUALITY AND GODOWN = @GID AND ROLL_NO < 0 AND P.firm = @FIRM GROUP BY TECH_NAME, TXN_DATE";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@GID", gId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        godownBalanceInfo.Add(oReader["TECH_NAME"].ToString(), oReader["QTY"].ToString() + "|" + ((DateTime)oReader["TXN_DATE"]).ToString("dd-MM-yy") + "|" + oReader["CNT"].ToString());
                    }
                }

                for (int j = 0; j < i; j++)
                {
                    string product = ((Label)panel3.Controls.Find("pName" + j, true)[0]).Text;
                    if (godownBalanceInfo.ContainsKey(product))
                    {
                        string[] parts = godownBalanceInfo[product].Split('|');
                        ((TextBox)panel3.Controls.Find("qty" + j, true)[0]).Text = parts[0];
                        if (panel3.Controls.Find("takaMtr" + j, true).Length > 0)
                        {
                            string takaMtrStr = parts[2];
                            if (((Label)panel3.Controls.Find("takaMtrLbl" + j, true)[0]).Text.Equals("Rolls"))
                            {
                                int tMtr = (int)Double.Parse(takaMtrStr);
                                ((TextBox)panel3.Controls.Find("takaMtr" + j, true)[0]).Text = tMtr + "";
                            }
                            else
                            {
                                ((TextBox)panel3.Controls.Find("takaMtr" + j, true)[0]).Text = parts[2];
                            }
                        }
                        ((TextBox)panel3.Controls.Find("obDate" + j, true)[0]).Text = parts[1];
                    }
                }
            }

            con.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd;
            if (gId == -1)
            {
                cmd = new SqlCommand("insert into GODOWN (FIRM, G_NAME) " +
                "values(@FIRM, @G_NAME)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@G_NAME", textBox1.Text);
            }
            else
            {
                cmd = new SqlCommand("UPDATE GODOWN SET G_NAME = @G_NAME WHERE GID = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.Parameters.AddWithValue("@G_NAME", textBox1.Text);
            }
            cmd.ExecuteNonQuery();
            
            if(gId != -1)
            {
                cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_FROM_TYPE = 'O' AND SUPPLY_TO_TYPE = 'G' AND SUPPLY_TO = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM SUPPLY_BEAM WHERE SUPPLY_FROM_TYPE = 'O' AND SUPPLY_TO_TYPE = 'G' AND SUPPLY_TO = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM TAKA_ENTRY WHERE WEAVER IS NULL AND GODOWN = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();
            }

            int i = 0;
            foreach (KeyValuePair<string, string> entry in products)
            {
                string balance = ((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text;

                if(!balance.Equals("0"))
                {
                    // MAKE TXN ENTRY

                    string date = ((TextBox)panel3.Controls.Find("obDate" + i, true)[0]).Text;
                    string techName = ((Label)panel3.Controls.Find("pName" + i, true)[0]).Text;

                    if (!String.IsNullOrEmpty(date))
                    {
                        string monthStr = date.Split('-')[1].Split('-')[0];
                        int month = Int32.Parse(date.Split('-')[1].Split('-')[0]);
                        string year = DateTime.Now.Year.ToString();
                        string century = year.Substring(0, year.Length - 2);
                        date = date.Replace("-" + monthStr + "-", "-" + months[month - 1] + "-" + century);
                    }

                    Boolean flag = false;
                    if (productType[entry.Key].Equals("Yarn"))
                    {
                        string godown;
                        if (gId == -1)
                        {
                            godown = "(SELECT MAX(GID) FROM godown)";
                        }
                        else
                        {
                            godown = gId.ToString();
                        }
                        cmd = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, BOXES, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM_TYPE) " +
                    "values(@FIRM, @TXN_DATE, @YARN, @QTY, @BOXES, " + godown + ", 'G', 'O')", con);
                        cmd.Parameters.AddWithValue("@BOXES", ((TextBox)panel3.Controls.Find("takaMtr" + i, true)[0]).Text);
                    }
                    else if (productType[entry.Key].Equals("Beam"))
                    {
                        string godown;
                        if (gId == -1)
                        {
                            godown = "(SELECT MAX(GID) FROM godown)";
                        }
                        else
                        {
                            godown = gId.ToString();
                        }
                        cmd = new SqlCommand("insert into SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM_TYPE) " +
                    "values(@FIRM, @TXN_DATE, @YARN, @QTY, " + godown + ", 'G', 'O')", con);

                    }
                    else if (productType[entry.Key].Equals("Cloth"))
                    {
                        if (productsMfgType[entry.Key].Equals("Y"))
                        {
                            string godown;
                            if (gId == -1)
                            {
                                godown = "(SELECT MAX(GID) FROM godown)";
                            }
                            else
                            {
                                godown = gId.ToString();
                            }

                            cmd = new SqlCommand("insert into TAKA_ENTRY (FIRM, TXN_DATE, GODOWN, TAKA_CNT, QUALITY, MTR) VALUES (@FIRM, @TXN_DATE, " + godown + ", @QTY, @YARN, @MTR)", con);
                            cmd.Parameters.AddWithValue("@MTR", ((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text);
                        }
                        else
                        {
                            flag = true;

                            if (!(gId == -1 || !godownBalanceInfo.ContainsKey(techName)))
                            {
                                cmd = new SqlCommand("DELETE FROM ROLL_ENTRY WHERE GODOWN = @GID and QUALITY = @QUALITY AND ROLL_NO < 0", con);
                                cmd.Parameters.AddWithValue("@GID", gId);
                                cmd.Parameters.AddWithValue("@QUALITY", entry.Key);
                                cmd.ExecuteNonQuery();
                            }

                            string godown;
                            if (gId == -1)
                            {
                                godown = "(SELECT MAX(GID) FROM godown)";
                            }
                            else
                            {
                                godown = gId.ToString();
                            }

                            int rolls = Int32.Parse(((TextBox)panel3.Controls.Find("takaMtr" + i, true)[0]).Text);
                            if (rolls <= 0)
                            {
                                rolls = 1;
                            }

                            for (int x = 1; x <= rolls; x++)
                            {
                                int rollNo = -x;
                                cmd = new SqlCommand("insert into ROLL_ENTRY (FIRM, TXN_DATE, GODOWN, QUALITY, MTR, ROLL_NO, DESPATCHED) VALUES (@FIRM, @TXN_DATE, " + godown + ", @YARN, @MTR, " + rollNo + ", 'N')", con);

                                double meter;

                                if (x == rolls)
                                {
                                    double totalMeter = Double.Parse(((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text);
                                    meter = (rolls - 1) * AddInvoice.round(Double.Parse(((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text) / rolls);
                                    meter = totalMeter - meter;
                                }
                                else
                                {
                                    meter = AddInvoice.round(Double.Parse(((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text) / rolls);
                                }

                                cmd.Parameters.AddWithValue("@FIRM", firm);
                                cmd.Parameters.AddWithValue("@TXN_DATE", date);
                                cmd.Parameters.AddWithValue("@YARN", entry.Key);
                                cmd.Parameters.AddWithValue("@MTR", meter);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }

                    if (!flag)
                    {
                        cmd.Parameters.AddWithValue("@FIRM", firm);
                        cmd.Parameters.AddWithValue("@TXN_DATE", date);
                        cmd.Parameters.AddWithValue("@YARN", entry.Key);

                        if (productsMfgType[entry.Key].Equals("Y"))
                        {
                            cmd.Parameters.AddWithValue("@QTY", ((TextBox)panel3.Controls.Find("takaMtr" + i, true)[0]).Text);
                        }
                        else
                        {
                            cmd.Parameters.AddWithValue("@QTY", ((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }
                
                i++;
            }

            con.Close();

            MessageBox.Show("Godown "+ button1.Text +"d Successfully");
        }

        private void setTextboxWatermark(TextBox textBox)
        {
            waterMarkActive.Add(textBox.Name, true);
            textBox.ForeColor = Color.Gray;
            textBox.Text = "dd-mm-yy";

            textBox.GotFocus += (source, e) =>
            {
                if (waterMarkActive[textBox.Name])
                {
                    waterMarkActive[textBox.Name] = false;
                    textBox.Text = "";
                    textBox.ForeColor = Color.Black;
                }
            };

            textBox.LostFocus += (source, e) =>
            {
                if (!waterMarkActive[textBox.Name] && string.IsNullOrEmpty(textBox.Text))
                {
                    waterMarkActive[textBox.Name] = true;
                    textBox.Text = "dd-mm-yy";
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new GodownList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String query = "select entry_id from supply_cone where ((supply_from_type = 'G' and supply_from = @GID) OR (supply_TO_type = 'G' and supply_TO = @GID)) AND SUPPLY_FROM_TYPE <> 'O' UNION select entry_id from supply_BEAM where ((supply_from_type = 'G' and supply_from = @GID) OR (supply_TO_type = 'G' and supply_TO = @GID)) AND SUPPLY_FROM_TYPE <> 'O' UNION SELECT ENTRY_ID FROM PURCHASE WHERE GODOWN = @GID UNION SELECT ENTRY_ID FROM TAKA_ENTRY WHERE GODOWN = @GID UNION SELECT ENTRY_ID FROM ROLL_ENTRY WHERE GODOWN = @GID";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@GID", gId);
            con.Open();

            Boolean canDelete = true;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    canDelete = false;
                }
            }

            if (canDelete)
            {
                SqlCommand cmd = new SqlCommand("delete from supply_cone where supply_to_type = 'G' and supply_to = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from supply_beam where supply_to_type = 'G' and supply_to = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from GODOWN where gid = @GID", con);
                cmd.Parameters.AddWithValue("@GID", gId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Godown Deleted");
                Close();
            }
            else
            {
                MessageBox.Show("Cannot delete : " + gName + "\nRemove the dependencies first");
            }
            con.Close();
        }

        private void pictureBox21_Click(object sender, EventArgs e)
        {
            var targetForm = new NewWeaver(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox7_Click(object sender, EventArgs e)
        {
            var targetForm = new NewGodown(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox6_Click(object sender, EventArgs e)
        {
            var targetForm = new NewProduct(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox8_Click(object sender, EventArgs e)
        {
            var targetForm = new NewUnit(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox22_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            var targetForm = new RollEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyBeam(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyCone(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            var targetForm = new Purchase(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox5_Click(object sender, EventArgs e)
        {
            var targetForm = new SaleHome(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox23_Click(object sender, EventArgs e)
        {
            var targetForm = new CalculateSalary(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox11_Click(object sender, EventArgs e)
        {
            var targetForm = new SalaryReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox12_Click(object sender, EventArgs e)
        {
            var targetForm = new GodownStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox13_Click(object sender, EventArgs e)
        {
            var targetForm = new StockValue(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox14_Click(object sender, EventArgs e)
        {
            var targetForm = new WorkerStockReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox24_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
