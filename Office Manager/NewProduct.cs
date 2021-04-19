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
    public partial class NewProduct : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;

        int productCount = 1;
        Dictionary<string, string> products = new Dictionary<string, string>();
        Dictionary<string, string> units = new Dictionary<string, string>();
        string pName;
        int pId = -1;

        public NewProduct()
        {
            InitializeComponent();
        }

        public NewProduct(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public NewProduct(string firm, byte[] logo, string pName)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.pName = pName;
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void NewQuality_Load(object sender, EventArgs e)
        {
            MemoryStream ms = new MemoryStream(logo);
            pictureBox17.Image = Image.FromStream(ms);

            String query = "select UID, UNIT_NAME from UNIT where firm = @FIRM order by UNIT_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    units.Add(oReader["UID"].ToString(), oReader["UNIT_NAME"].ToString());
                }
            }

            if (units.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(units, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            if (pName != null)
            {
                Boolean manufactured = false;

                button1.Text = "Update";
                button2.Visible = true;

                query = "select firm, pid, tech_name, comm_name, category, unit, mfg_uid, UNIT_EQUIVALENT, calc_ratio, WEAVING_RATE, taka, (SELECT TOP 1 PR.PID FROM PRODUCT_REQ PR WHERE PR.PID = P.PID) MFG from product p where tech_name = @P_NAME AND FIRM = @FIRM";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@P_NAME", pName);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        pId = Int32.Parse(oReader["PID"].ToString());
                        textBox3.Text = oReader["TECH_NAME"].ToString();
                        if (!oReader["COMM_NAME"].ToString().Equals(""))
                        {
                            checkBox2.Checked = true;
                            textBox1.Text = oReader["COMM_NAME"].ToString();
                        }

                        Dictionary<string, string> categories = new Dictionary<string, string>();
                        categories.Add("Yarn", "Yarn");
                        categories.Add("Beam", "Beam");
                        categories.Add("Cloth", "Cloth");
                        comboBox1.SelectedIndex = comboBox1.FindString(categories[oReader["CATEGORY"].ToString()]);

                        comboBox3.SelectedIndex = comboBox3.FindString(units[oReader["UNIT"].ToString()]);
                        checkBox4.Checked = (oReader["TAKA"].ToString().Equals("Y"));
                        checkBox1.Checked = (!oReader["MFG"].ToString().Equals(""));
                        // salary calculation

                        if (checkBox1.Checked)
                        {
                            manufactured = true;
                            if (!oReader["UNIT_EQUIVALENT"].ToString().Equals(""))
                            {
                                checkBox3.Checked = true;
                                comboBox5.SelectedIndex = comboBox5.FindString(units[oReader["MFG_UID"].ToString()]);
                                textBox5.Text = oReader["UNIT_EQUIVALENT"].ToString();
                                textBox6.Text = oReader["CALC_RATIO"].ToString();
                                textBox4.Text = oReader["WEAVING_RATE"].ToString();
                            }
                        }
                    }
                }

                if (manufactured)
                {
                    query = "select * from product_req where firm = @FIRM AND PID = @PID";
                    oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);
                    oCmd.Parameters.AddWithValue("@PID", pId);

                    int index = 0;
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if(index != 0)
                            {
                                addRow(productCount);
                            }

                            ComboBox pro = (ComboBox)panel4.Controls.Find("product" + index, true)[0];
                            pro.SelectedIndex = pro.FindString(products[oReader["PRODUCT"].ToString()]);
                            ((TextBox)panel4.Controls.Find("qty" + index, true)[0]).Text = oReader["QTY"].ToString();

                            index++;
                        }
                    }
                }
            }

            con.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox manufactured = (CheckBox)sender;
            if(manufactured.Checked)
            {
                panel3.Visible = true;
            } else
            {
                panel3.Visible = false;
            }

            // Set Mfg Unit

            comboBox5.DataSource = new BindingSource(units, null);
            comboBox5.DisplayMember = "Value";
            comboBox5.ValueMember = "Key";

            // Set product drop down

            SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

            String query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM order by TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    products.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }
            con.Close();

            if (products.Count() > 0)
            {
                product0.DataSource = new BindingSource(products, null);
                product0.DisplayMember = "Value";
                product0.ValueMember = "Key";
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox2.Checked)
            {
                textBox1.Visible = true;
            }
            else
            {
                textBox1.Visible = false;
                textBox1.Text = "";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SqlCommand cmd = null;
            List<SqlCommand> productReqCmds = new List<SqlCommand>();
            SqlCommand delCmd = null;
            con.Open();

            if (pId == -1)
            {
                // INSERT INTO PRODUCT
                if (!checkBox1.Checked)
                {
                    char taka = 'N';
                    if (checkBox4.Checked)
                    {
                        taka = 'Y';
                    }
                    
                    cmd = new SqlCommand("insert into PRODUCT (FIRM, TECH_NAME, COMM_NAME, CATEGORY, UNIT, TAKA) " +
                        "values(@FIRM, @TECH_NAME, @COMM_NAME, @CATEGORY, @UNIT, @TAKA)", con);
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@TECH_NAME", textBox3.Text);
                    cmd.Parameters.AddWithValue("@COMM_NAME", textBox1.Text);
                    cmd.Parameters.AddWithValue("@CATEGORY", comboBox1.Text);
                    cmd.Parameters.AddWithValue("@UNIT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                    cmd.Parameters.AddWithValue("@TAKA", taka);

                    //cmd.ExecuteNonQuery();
                }
                else
                {
                    if (checkBox3.Checked)
                    {
                        char taka = 'N';
                        if (checkBox4.Checked)
                        {
                            taka = 'Y';
                        }

                        cmd = new SqlCommand("insert into PRODUCT (FIRM, TECH_NAME, COMM_NAME, CATEGORY, UNIT, MFG_UID, UNIT_EQUIVALENT, CALC_RATIO, WEAVING_RATE, TAKA) " +
                            "values(@FIRM, @TECH_NAME, @COMM_NAME, @CATEGORY, @UNIT, @MFG_UID, @UNIT_EQUIVALENT, @CALC_RATIO, @WEAVING_RATE, @TAKA)", con);
                        cmd.Parameters.AddWithValue("@FIRM", firm);
                        cmd.Parameters.AddWithValue("@TECH_NAME", textBox3.Text);
                        cmd.Parameters.AddWithValue("@COMM_NAME", textBox1.Text);
                        cmd.Parameters.AddWithValue("@CATEGORY", comboBox1.Text);
                        cmd.Parameters.AddWithValue("@UNIT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@MFG_UID", ((KeyValuePair<string, string>)comboBox5.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@UNIT_EQUIVALENT", textBox5.Text);
                        cmd.Parameters.AddWithValue("@CALC_RATIO", textBox6.Text);
                        cmd.Parameters.AddWithValue("@WEAVING_RATE", textBox4.Text);
                        cmd.Parameters.AddWithValue("@TAKA", taka);

                        //cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        char taka = 'N';
                        if (checkBox4.Checked)
                        {
                            taka = 'Y';
                        }

                        cmd = new SqlCommand("insert into PRODUCT (FIRM, TECH_NAME, COMM_NAME, CATEGORY, UNIT, TAKA) " +
                            "values(@FIRM, @TECH_NAME, @COMM_NAME, @CATEGORY, @UNIT, @TAKA)", con);
                        cmd.Parameters.AddWithValue("@FIRM", firm);
                        cmd.Parameters.AddWithValue("@TECH_NAME", textBox3.Text);
                        cmd.Parameters.AddWithValue("@COMM_NAME", textBox1.Text);
                        cmd.Parameters.AddWithValue("@CATEGORY", comboBox1.Text);
                        cmd.Parameters.AddWithValue("@UNIT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                        cmd.Parameters.AddWithValue("@TAKA", taka);

                        //cmd.ExecuteNonQuery();
                    }

                    // INSERT INTO PRODUCT_REQ

                    for (int i = 0; i < productCount; i++)
                    {
                        SqlCommand cmd1 = new SqlCommand("insert into PRODUCT_REQ (FIRM, PID, PRODUCT, QTY) " +
                        "values(@FIRM, (select max(pid) from product), @PRODUCT, @QTY)", con);
                        cmd1.Parameters.AddWithValue("@FIRM", firm);
                        cmd1.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)((ComboBox)Controls.Find($"product{i}", true)[0]).SelectedItem).Key);
                        cmd1.Parameters.AddWithValue("@QTY", ((TextBox)Controls.Find($"qty{i}", true)[0]).Text);

                        productReqCmds.Add(cmd1);
                        //cmd.ExecuteNonQuery();
                    }

                    List<String> wids = new List<string>();
                    List<String> gids = new List<string>();

                    // INITIALIZE GODOWNS & PRODUCTS LIST

                    String query = "SELECT WID FROM WEAVER WHERE FIRM = @FIRM";
                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            wids.Add(oReader["WID"].ToString());
                        }
                    }

                    query = "SELECT GID FROM godown WHERE FIRM = @FIRM";
                    oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            gids.Add(oReader["GID"].ToString());
                        }
                    }
                }
            }
            else
            {
                string commName = "''";
                if (checkBox2.Checked)
                {
                    commName = "'" + textBox1.Text + "'";
                }

                string mfgUid = "null";
                string unitEq = "null";
                string calcRatio = "null";
                string wRate = "null";

                if (checkBox3.Checked)
                {
                    mfgUid = ((KeyValuePair<string, string>)comboBox5.SelectedItem).Key;
                    unitEq = textBox5.Text;
                    calcRatio = textBox6.Text;
                    wRate = textBox4.Text;
                }

                cmd = new SqlCommand("UPDATE PRODUCT SET TECH_NAME = @TECH_NAME, COMM_NAME = " + commName + ", CATEGORY = @CATEGORY, UNIT = @UNIT, MFG_UID = " + mfgUid + ", UNIT_EQUIVALENT = " + unitEq + ", CALC_RATIO = " + calcRatio + ", WEAVING_RATE = " + wRate + ", TAKA = @TAKA where PID = @PID", con);
                cmd.Parameters.AddWithValue("@TECH_NAME", textBox3.Text);
                cmd.Parameters.AddWithValue("@CATEGORY", comboBox1.Text);
                cmd.Parameters.AddWithValue("@UNIT", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);

                char taka = 'N';
                if (checkBox4.Checked)
                {
                    taka = 'Y';
                }
                cmd.Parameters.AddWithValue("@TAKA", taka);
                cmd.Parameters.AddWithValue("@PID", pId);

                //cmd.ExecuteNonQuery();

                // delete & insert into product_req

                if (checkBox1.Checked)
                {
                    // DELETE PRODUCT_REQ

                    delCmd = new SqlCommand("delete from product_req where pid = @PID", con);
                    delCmd.Parameters.AddWithValue("@PID", pId);

                    //cmd.ExecuteNonQuery();

                    // insert

                    for (int i = 0; i < productCount; i++)
                    {
                        SqlCommand cmd1 = new SqlCommand("insert into PRODUCT_REQ (FIRM, PID, PRODUCT, QTY) " +
                        "values(@FIRM, " + pId + ", @PRODUCT, @QTY)", con);
                        cmd1.Parameters.AddWithValue("@FIRM", firm);
                        cmd1.Parameters.AddWithValue("@PRODUCT", ((KeyValuePair<string, string>)((ComboBox)Controls.Find($"product{i}", true)[0]).SelectedItem).Key);
                        cmd1.Parameters.AddWithValue("@QTY", ((TextBox)Controls.Find($"qty{i}", true)[0]).Text);

                        productReqCmds.Add(cmd1);
                        //cmd.ExecuteNonQuery();
                    }
                }
            }


            if (checkBox2.Checked)
            {
                new AddItem(firm, logo, pId, cmd, productReqCmds, delCmd, textBox1.Text, con).Show();
            }
            else
            {
                if (delCmd != null)
                {
                    delCmd.ExecuteNonQuery();
                }

                if (cmd != null)
                {
                    cmd.ExecuteNonQuery();
                }

                if (productReqCmds.Count > 0)
                {
                    foreach (SqlCommand command in productReqCmds)
                    {
                        command.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Product " + button1.Text + "d Successfully");
                con.Close();
            }
        }

        private void pictureBox10_Click(object sender, EventArgs e)
        {
            addRow(productCount);
        }

        private void addRow(int index)
        {
            var product = new ComboBox()
            {
                Name = "product" + index,
                Location = new Point(product0.Location.X, product0.Location.Y + 25 * index),
                DataSource = new BindingSource(products, null),
                DisplayMember = "Value",
                ValueMember = "Key",
                Size = product0.Size,
                DropDownStyle = product0.DropDownStyle
            };
            var qty = new TextBox()
            {
                Name = "qty" + index,
                Location = new Point(qty0.Location.X, qty0.Location.Y + 25 * index),
                Size = qty0.Size
            };
            var add = new PictureBox()
            {
                Name = "add" + index,
                Location = new Point(add0.Location.X, add0.Location.Y + 25 * index),
                SizeMode = add0.SizeMode,
                Image = add0.Image,
                Size = add0.Size
            };
            var del = new PictureBox()
            {
                Name = "del" + index,
                Location = new Point(del0.Location.X, del0.Location.Y + 25 * index),
                Visible = true,
                SizeMode = del0.SizeMode,
                Image = del0.Image,
                Size = del0.Size
            };
            var unitLbl = new Label()
            {
                Name = "unitLbl" + index,
                Location = new Point(unitLbl0.Location.X, unitLbl0.Location.Y + 25 * index),
                Font = unitLbl0.Font,
                Text = unitLbl0.Text,
                Size = unitLbl0.Size,
                ForeColor = unitLbl0.ForeColor
            };

            productCount++;

            add.Click += (s, evt) =>
            {
                addRow(productCount);
            };

            del.Click += (s, evt) =>
            {
                copyCellsForDelete(Int32.Parse(del.Name.Replace("del", "")));
                int i = productCount - 1;
                panel4.Controls.Remove(panel4.Controls.Find("product" + i, true)[0]);
                panel4.Controls.Remove(panel4.Controls.Find("qty" + i, true)[0]);
                panel4.Controls.Remove(panel4.Controls.Find("add" + i, true)[0]);
                panel4.Controls.Remove(panel4.Controls.Find("del" + i, true)[0]);

                productCount--;
            };

            panel4.Controls.Add(product);
            panel4.Controls.Add(qty);
            panel4.Controls.Add(unitLbl);
            panel4.Controls.Add(add);
            panel4.Controls.Add(del);
        }

        private void copyCellsForDelete(int index)
        {
            for(int i = index; i < (productCount - 1); i++)
            {
                TextBox qty = (TextBox)panel4.Controls.Find("qty" + i, true)[0];
                ComboBox product = (ComboBox)panel4.Controls.Find("product" + i, true)[0];
                
                TextBox qtyPrev = (TextBox)panel4.Controls.Find("qty" + (i+1), true)[0];
                ComboBox productPrev = (ComboBox)panel4.Controls.Find("product" + (i+1), true)[0];
                
                qty.Text = qtyPrev.Text;
                product.SelectedIndex = productPrev.SelectedIndex;
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox3.Checked)
            {
                panel5.Visible = true;
            }
            else
            {
                panel5.Visible = false;
            }
        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new ProductList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String query = "select entry_id from supply_cone where yarn = @PID UNION select entry_id from supply_BEAM where BEAM = @PID UNION SELECT ENTRY_ID FROM PURCHASE WHERE PRODUCT = @PID UNION SELECT ENTRY_ID FROM TAKA_ENTRY WHERE QUALITY = @PID UNION SELECT ENTRY_ID FROM ROLL_ENTRY WHERE QUALITY = @PID UNION SELECT PID FROM PRODUCT_REQ WHERE PRODUCT = @PID";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@PID", pId);
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
                SqlCommand cmd = new SqlCommand("delete from PRODUCT_REQ where PID = @PID", con);
                cmd.Parameters.AddWithValue("@PID", pId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from PRODUCT where pid = @PID", con);
                cmd.Parameters.AddWithValue("@PID", pId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("delete from item where pid_pk = @PID", con);
                cmd.Parameters.AddWithValue("@PID", pId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Product Deleted");
                Close();
            }
            else
            {
                MessageBox.Show("Cannot delete : " + pName + "\nRemove the dependencies first");
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

        private void pictureBox10_Click_1(object sender, EventArgs e)
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
