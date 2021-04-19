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
    public partial class NewWeaver : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;
        Dictionary<string, string> products = new Dictionary<string, string>();
        Dictionary<string, string> productType = new Dictionary<string, string>();
        Dictionary<string, bool> waterMarkActive = new Dictionary<string, bool>();
        Dictionary<string, string> weaverBalanceInfo = new Dictionary<string, string>();

        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };
        string wName;
        int wId = -1;

        public NewWeaver()
        {
            InitializeComponent();
        }

        public NewWeaver(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public NewWeaver(string firm, byte[] logo, string wName)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.wName = wName;
        }

        private void NewWeaver_Load(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream(logo);
            pictureBox17.Image = Image.FromStream(ms);

            String query = "select PID, TECH_NAME, UNIT_NAME, CATEGORY from PRODUCT P, UNIT U where P.FIRM = @FIRM AND P.UNIT = U.UID AND CATEGORY <> 'Cloth' ORDER BY TECH_NAME";
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

                    var product = new Label()
                    {
                        Name = "pName" + i,
                        Location = new Point(pName.Location.X, pName.Location.Y + 25 * i),
                        Font = pName.Font,
                        Text = oReader["TECH_NAME"].ToString()
                    };

                    var unit = new Label()
                    {
                        Name = "unit" + i,
                        Location = new Point(unitLbl.Location.X, unitLbl.Location.Y + 25 * i),
                        Font = unitLbl.Font,
                        ForeColor = unitLbl.ForeColor,
                        Text = oReader["UNIT_NAME"].ToString()
                    };

                    var qty = new TextBox()
                    {
                        Name = "qty" + i,
                        Location = new Point(this.qty.Location.X, this.qty.Location.Y + 25 * i),
                        Size = this.qty.Size,
                        Text = "0"
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

                    i++;
                }
            }

            if (wName != null)
            {
                button1.Text = "Update";
                button2.Visible = true;

                query = "select * from weaver where w_name = @W_NAME AND FIRM = @FIRM";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@W_NAME", wName);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        wId = Int32.Parse(oReader["WID"].ToString());
                        textBox1.Text = wName;
                        textBox2.Text = oReader["TDS"].ToString();
                    }
                }

                query = "select TECH_NAME, CUTS QTY, txn_date from supply_beam SB, PRODUCT P where supply_from_type = 'O' AND P.PID = SB.BEAM and supply_to_type = 'W' and supply_to = @WID and P.firm = @FIRM UNION select TECH_NAME, QTY, txn_date from supply_CONE SC, PRODUCT P WHERE P.PID = SC.YARN and supply_to_type = 'W' AND supply_from_type = 'O' and supply_to_type = 'W' and supply_to = @WID and P.firm = @FIRM";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@WID", wId);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        weaverBalanceInfo.Add(oReader["TECH_NAME"].ToString(), oReader["QTY"].ToString() + "|" + ((DateTime) oReader["TXN_DATE"]).ToString("dd-MM-yy"));
                    }
                }

                for (int j=0; j<i; j++)
                {
                    string product = ((Label)panel3.Controls.Find("pName" + j, true)[0]).Text;
                    if(weaverBalanceInfo.ContainsKey(product))
                    {
                        string[] parts = weaverBalanceInfo[product].Split('|');
                        ((TextBox)panel3.Controls.Find("qty" + j, true)[0]).Text = parts[0];
                        ((TextBox)panel3.Controls.Find("obDate" + j, true)[0]).Text = parts[1];
                    }
                }
            }

            con.Close();
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd;
            if (wId == -1)
            {
                cmd = new SqlCommand("insert into WEAVER (FIRM, W_NAME, TDS) " +
                    "values(@FIRM, @W_NAME, @TDS)", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@W_NAME", textBox1.Text);
                cmd.Parameters.AddWithValue("@TDS", textBox2.Text);
            }
            else
            {
                cmd = new SqlCommand("update WEAVER set w_name = @W_NAME, TDS = @TDS WHERE WID = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.Parameters.AddWithValue("@W_NAME", textBox1.Text);
                cmd.Parameters.AddWithValue("@TDS", textBox2.Text);
            }

            cmd.ExecuteNonQuery();

            if(wId != -1)
            {
                cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_FROM_TYPE = 'O' AND SUPPLY_TO_TYPE = 'W' AND SUPPLY_TO = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM SUPPLY_BEAM WHERE SUPPLY_FROM_TYPE = 'O' AND SUPPLY_TO_TYPE = 'W' AND SUPPLY_TO = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.ExecuteNonQuery();
            }

            int i = 0;
            foreach (KeyValuePair<string, string> entry in products)
            {
                // Update weaver balance
                string balance = ((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text;
                
                if(!balance.Equals("0"))
                {
                    // MAKE TXN ENTRY

                    string date = ((TextBox)panel3.Controls.Find("obDate" + i, true)[0]).Text;
                    string techName = ((Label)panel3.Controls.Find("pName" + i, true)[0]).Text;

                    if (!String.IsNullOrEmpty(date))
                    {
                        string monthStr = date.Split('-')[1].Split('-')[0];
                        int month = Int32.Parse(monthStr);
                        string year = DateTime.Now.Year.ToString();
                        string century = year.Substring(0, year.Length - 2);
                        date = date.Replace("-" + monthStr + "-", "-" + months[month - 1] + "-" + century);
                    }

                    Boolean flag = false;
                    if (productType[entry.Key].Equals("Yarn"))
                    {
                        string weaver;
                        if (wId == -1)
                        {
                            weaver = "(SELECT MAX(WID) FROM WEAVER)";
                        }
                        else
                        {
                            weaver = wId.ToString();
                        }

                        cmd = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM_TYPE) " +
                    "values(@FIRM, @TXN_DATE, @YARN, @QTY, " + weaver + ", 'W', 'O')", con);
                    }
                    else if (productType[entry.Key].Equals("Beam") || !weaverBalanceInfo.ContainsKey(techName))
                    {
                        string weaver;
                        if (wId == -1)
                        {
                            weaver = "(SELECT MAX(WID) FROM WEAVER)";
                        }
                        else
                        {
                            weaver = wId.ToString();
                        }

                        cmd = new SqlCommand("insert into SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM_TYPE) " +
                    "values(@FIRM, @TXN_DATE, @YARN, @QTY, " + weaver + ", 'W', 'O')", con);
                    }
                    else
                    {
                        flag = true;
                    }

                    if (!flag)
                    {
                        cmd.Parameters.AddWithValue("@FIRM", firm);
                        cmd.Parameters.AddWithValue("@TXN_DATE", date);
                        cmd.Parameters.AddWithValue("@YARN", entry.Key);
                        cmd.Parameters.AddWithValue("@QTY", ((TextBox)panel3.Controls.Find("qty" + i, true)[0]).Text);
                        cmd.ExecuteNonQuery();
                    }
                }

                i++;
            }

            con.Close();

            MessageBox.Show("Weaver "+ button1.Text +"d Successfully");
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
            var targetForm = new WeaverList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String query = "select entry_id from supply_cone where ((supply_from_type = 'W' and supply_from = @WID) OR (supply_TO_type = 'W' and supply_TO = @WID)) AND SUPPLY_FROM TYPE <> 'O' UNION select entry_id from supply_BEAM where ((supply_from_type = 'W' and supply_from = @WID) OR (supply_TO_type = 'W' and supply_TO = @WID)) AND SUPPLY_FROM_TYPE <> 'O' UNION SELECT ENTRY_ID FROM TAKA_ENTRY WHERE WEAVER = @WID UNION SELECT ENTRY_ID FROM ROLL_ENTRY WHERE WEAVER = @WID";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@WID", wId);
            con.Open();

            Boolean canDelete = true;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    canDelete = false;
                }
            }

            if(canDelete)
            {
                SqlCommand cmd = new SqlCommand("delete from supply_cone where supply_to_type = 'W' and supply_to = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from supply_beam where supply_to_type = 'W' and supply_to = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from weaver where wid = @WID", con);
                cmd.Parameters.AddWithValue("@WID", wId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Weaver Deleted");
                Close();
            }
            else
            {
                MessageBox.Show("Cannot delete : " + wName + "\nRemove the dependencies first");
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
