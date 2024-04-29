using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class SupplyBeam : Form
    {
        private string firm;
        private byte[] logo;
        private int txnId = -1;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> beams = new Dictionary<string, string>();
        Dictionary<string, string> entities = new Dictionary<string, string>();

        int beamCount = 1;
        string periodFilter = "";
        Boolean isOpen;
        Dictionary<string, string> setEntryIds = new Dictionary<string, string>();
        Dictionary<string, string> beamCuts = new Dictionary<string, string>();

        Boolean loading = true;
        Dictionary<string, string> beamNoInfo = new Dictionary<string, string>();

        public SupplyBeam()
        {
            InitializeComponent();
        }

        public SupplyBeam(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        public SupplyBeam(string firm, byte[] logo, int txnId)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
            this.txnId = txnId;
        }

        private void SupplyBeam_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;

            checkBox1.Visible = false;
            cuts0.KeyDown += new KeyEventHandler(cuts_KeyDown);
            con.Open();
            isOpen = true;
            // set quality

            String query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Beam' order by TECH_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    beams.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (beams.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(beams, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";
            }

            beamNo0.TextChanged += new EventHandler(beam_TextChanged);

            // set godown

            query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    entities.Add("G" + oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            // set weaver

            query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    entities.Add("W" + oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            if (entities.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(entities, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";

                comboBox2.DataSource = new BindingSource(entities, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            if(txnId != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;
                radioButton3.Visible = false;

                SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con.Open();
                
                query = "select * from supply_beam where txn_id = @TXN_ID";
                oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@TXN_ID", txnId);

                int index = 0;
                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        if (index == 0)
                        {
                            textBox4.Text = oReader["DO_NO"].ToString();
                            textBox3.Text = oReader["SET_NO"].ToString();
                            textBox1.Text = oReader["EXCESS"].ToString();

                            if(textBox1.Text.Equals(""))
                            {
                                radioButton2.Checked = true;
                            }

                            if (oReader["SET_NO"].ToString().Equals(""))
                            {
                                checkBox1.Checked = true;
                            }

                            string supplyToType = oReader["SUPPLY_TO_TYPE"].ToString();
                            string supplyFromType = oReader["SUPPLY_FROM_TYPE"].ToString();
                            
                            if (!radioButton2.Checked)
                            {
                                comboBox2.SelectedIndex = comboBox2.FindString(entities[supplyToType + oReader["SUPPLY_TO"].ToString()]);
                            }
                            comboBox1.SelectedIndex = comboBox1.FindString(entities[supplyFromType.Replace("S", "G") + oReader["SUPPLY_FROM"].ToString()]);
                            comboBox3.SelectedIndex = comboBox3.FindString(beams[oReader["BEAM"].ToString()]);

                            CultureInfo ci = CultureInfo.InvariantCulture;
                            dateTimePicker1.Value = DateTime.ParseExact(oReader["TXN_DATE"].ToString().Split(' ')[0], CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, ci);

                            if (supplyFromType.Equals("S"))
                            {
                                radioButton3.Visible = true;
                                radioButton3.Checked = true;
                                textBox3_TextChanged(textBox3, null);
                            }
                        }
                        else
                        {
                            addRow(beamCount);
                        }
                        ((TextBox)panel3.Controls.Find("beamNo" + index, true)[0]).Text = oReader["BEAM_NO"].ToString();
                        ((TextBox)panel3.Controls.Find("cuts" + index, true)[0]).Text = oReader["CUTS"].ToString();
                        index++;
                    }
                }

                // SET WARP WEIGHT

                if (textBox1.Visible)
                {
                    query = "select qty from supply_cone where supply_to_type = 'B' and supply_to = @TXN_ID union select qty from supply_cone where supply_from_type = 'E' and supply_from = @TXN_ID";
                    oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@TXN_ID", txnId);
                    int i = 0;

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            if(i == 0)
                            {
                                textBox1.Text = oReader["QTY"].ToString();
                            }
                            else
                            {
                                textBox2.Text = oReader["QTY"].ToString();
                            }
                            i++;
                        }
                    }
                }

                con.Close();
            }

            con.Close();
            isOpen = false;

            loading = false;
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                beamNo0.Enabled = false;
                textBox3.Enabled = false;
                textBox4.Enabled = false;
            }
            else
            {
                beamNo0.Enabled = true;
                textBox3.Enabled = true;
                textBox4.Enabled = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (txnId == -1)
            {
                saveBeamEntry();
                MessageBox.Show("Beam Supplied Successfully");
            }
            else
            {
                con.Open();
                if (radioButton2.Checked)
                {
                    beamNoInfo = new Dictionary<string, string>();

                    string query = "select beam_no, do_no, supply_to, supply_to_type from supply_beam where txn_id = @TXN_ID";
                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@TXN_ID", txnId);
                    int i = 0;

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            string beamNo = oReader["BEAM_NO"].ToString();
                            string doNo = oReader["DO_NO"].ToString();
                            string weaver = oReader["SUPPLY_TO"].ToString();
                            string stt = oReader["SUPPLY_TO_TYPE"].ToString();
                            beamNoInfo.Add(beamNo, doNo + "|" + weaver + "|" + stt);
                        }
                    }
                }
                
                SqlCommand cmd = new SqlCommand("DELETE FROM SUPPLY_BEAM WHERE TXN_ID = @TXN_ID", con);
                cmd.Parameters.AddWithValue("@TXN_ID", txnId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO = @TXN_ID", con);
                cmd.Parameters.AddWithValue("@TXN_ID", txnId);
                cmd.ExecuteNonQuery();

                cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_FROM_TYPE = 'E' AND SUPPLY_FROM = @TXN_ID", con);
                cmd.Parameters.AddWithValue("@TXN_ID", txnId);
                cmd.ExecuteNonQuery();
                con.Close();

                saveBeamEntry();
                MessageBox.Show("Beam Entry Updated");
            }
        }

        private void saveBeamEntry()
        {
            double totalCuts = 0;

            string supplyTo = "null";
            string supplyToType = "null";

            if (!radioButton2.Checked)
            {
                supplyTo = ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(1);
                supplyToType = "'" + ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(0, 1) + "'";
            }

            con.Open();
            isOpen = true;

            if (radioButton3.Checked)
            {
                for (int i = 0; i < beamCount; i++)
                {
                    SqlCommand cmd = new SqlCommand("UPDATE SUPPLY_BEAM SET DO_NO = @DO_NO, CUTS = @CUTS, SUPPLY_TO = @SUPPLY_TO, SUPPLY_TO_TYPE = 'W' WHERE SET_NO = @SET_NO AND FIRM = @FIRM AND BEAM_NO = @BEAM_NO " + periodFilter, con);
                    cmd.Parameters.AddWithValue("@DO_NO", textBox4.Text);
                    cmd.Parameters.AddWithValue("@SUPPLY_TO", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key.Substring(1));
                    cmd.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@CUTS", ((TextBox)Controls.Find("cuts" + i, true)[0]).Text);
                    cmd.Parameters.AddWithValue("@BEAM_NO", ((TextBox)Controls.Find("beamNo" + i, true)[0]).Text);
                    cmd.ExecuteNonQuery();

                    string cuts = ((TextBox)Controls.Find("cuts" + i, true)[0]).Text;
                    if (cuts.Equals(""))
                    {
                        cuts = "null";
                    }

                    cmd = new SqlCommand("insert into SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, DO_NO, SET_NO, BEAM_NO, " +
                        "SUPPLY_TO, SUPPLY_TO_TYPE, CUTS, SUPPLY_FROM, SUPPLY_FROM_TYPE, TXN_ID, EXCESS) values(@FIRM, @TXN_DATE, @BEAM, " +
                        "@DO_NO, @SET_NO, @BEAM_NO, " + supplyTo + ", " + supplyToType + ", "+ cuts +", @SUPPLY_FROM, 'S', (SELECT (IDENT_CURRENT( 'supply_beam' ) - " + i + ")), @EXCESS)", con);
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd.Parameters.AddWithValue("@BEAM", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                    cmd.Parameters.AddWithValue("@DO_NO", textBox4.Text);
                    cmd.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                    cmd.Parameters.AddWithValue("@BEAM_NO", ((TextBox)Controls.Find("beamNo" + i, true)[0]).Text);
                    cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
                    cmd.Parameters.AddWithValue("@EXCESS", 0);

                    cmd.ExecuteNonQuery();
                }
            }
            else
            {
                // INSERT INTO SUPPLY_BEAM
                for (int i = 0; i < beamCount; i++)
                {
                    string cuts = ((TextBox)Controls.Find("cuts" + i, true)[0]).Text;
                    if (cuts.Equals(""))
                    {
                        cuts = "null";
                    }

                    string excess = textBox1.Text;
                    string doNo = textBox4.Text;
                    string setNo = textBox3.Text;
                    string beamNo = ((TextBox)Controls.Find("beamNo" + i, true)[0]).Text;

                    if (checkBox1.Checked)
                    {
                        setNo = "null";
                        beamNo = "null";
                    }

                    if(radioButton2.Checked)
                    {
                        excess = "null";
                        doNo = "null";

                        if(txnId != -1)
                        {
                            if (beamNoInfo.ContainsKey(beamNo))
                            {
                                doNo = beamNoInfo[beamNo].Split('|')[0];
                                supplyTo = beamNoInfo[beamNo].Split('|')[1];
                                supplyToType = "'" + beamNoInfo[beamNo].Split('|')[2] + "'";
                            }

                            SqlCommand cmd1 = new SqlCommand("UPDATE SUPPLY_BEAM SET CUTS = @CUTS, BEAM = @BEAM, SUPPLY_TO_TYPE = "+ supplyToType +", SUPPLY_TO = "+ supplyTo +" WHERE SUPPLY_FROM_TYPE = 'S' AND SET_NO = "+ setNo +" AND FIRM = @FIRM AND BEAM_NO = @BEAM_NO " + periodFilter, con);
                            cmd1.Parameters.AddWithValue("@FIRM", firm);
                            cmd1.Parameters.AddWithValue("@BEAM", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                            cmd1.Parameters.AddWithValue("@CUTS", ((TextBox)Controls.Find("cuts" + i, true)[0]).Text);
                            cmd1.Parameters.AddWithValue("@BEAM_NO", ((TextBox)Controls.Find("beamNo" + i, true)[0]).Text);
                            cmd1.ExecuteNonQuery();
                        }
                    }

                    if(checkBox1.Checked)
                    {
                        doNo = "null";
                    }

                    SqlCommand cmd = new SqlCommand("insert into SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, DO_NO, SET_NO, BEAM_NO, " +
                        "CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, TXN_ID, EXCESS) values(@FIRM, @TXN_DATE, @BEAM, " +
                        doNo +", "+ setNo +", "+ beamNo +", " + cuts + ", " + supplyTo + ", " + supplyToType + ", @SUPPLY_FROM, @SUPPLY_FROM_TYPE, (SELECT (IDENT_CURRENT( 'supply_beam' ) - " + i + ")), "+ excess +")", con);
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd.Parameters.AddWithValue("@BEAM", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);
                    cmd.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                    cmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
                    cmd.Parameters.AddWithValue("@SUPPLY_FROM_TYPE", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(0, 1));

                    cmd.ExecuteNonQuery();

                    if (!cuts.Equals("null"))
                    {
                        totalCuts += Double.Parse((Controls.Find("cuts" + i, true)[0]).Text);
                    }
                }
            }
            con.Close();
            isOpen = false;

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            // If supplied from godown
            string requiredYarn = "";
            double requiredQty = 0;

            if (((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(0, 1).Equals("G") && !checkBox1.Checked)
            {
                String query = "SELECT PR.PRODUCT, PR.QTY FROM PRODUCT_REQ PR, PRODUCT P1 WHERE PR.PRODUCT = P1.PID AND P1.CATEGORY = 'Yarn' AND PR.PID = @PID";
                SqlCommand oCmd = new SqlCommand(query, con1);
                oCmd.Parameters.AddWithValue("@PID", ((KeyValuePair<string, string>)comboBox3.SelectedItem).Key);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        requiredYarn = oReader["PRODUCT"].ToString();
                        requiredQty = Double.Parse(oReader["QTY"].ToString());
                    }
                }
            }

            // excess and yarn entry

            if (!radioButton3.Checked && !requiredYarn.Equals("") && !checkBox1.Checked)
            {
                //setBalanceEntry(con1, textBox3.Text);

                // delete next sets and insert again woth updated values
                //deleteAndReinsertSets(con1);

                // EXCESS ENTRY,

                SqlCommand cmd3 = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, BOXES, SUPPLY_TO, " +
                    "SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE) values(@FIRM, @TXN_DATE, @YARN, " +
                        "@QTY, 0, @SUPPLY_TO, 'G', (select max(txn_id) from supply_beam), 'E')", con1);
                cmd3.Parameters.AddWithValue("@FIRM", firm);
                cmd3.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                cmd3.Parameters.AddWithValue("@YARN", requiredYarn);
                cmd3.Parameters.AddWithValue("@QTY", textBox1.Text);
                cmd3.Parameters.AddWithValue("@SUPPLY_TO", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                cmd3.ExecuteNonQuery();

                // UPDATE EXCESS QTY FOR OTHER ENTRIES OF SAME SET NO

                cmd3 = new SqlCommand("UPDATE SUPPLY_CONE SET QTY = @QTY WHERE SUPPLY_FROM_TYPE = 'E' AND SUPPLY_FROM IN (SELECT TXN_ID FROM SUPPLY_BEAM WHERE FIRM = @FIRM AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @GODOWN AND SET_NO = @SET_NO "+ periodFilter +")", con1);
                cmd3.Parameters.AddWithValue("@FIRM", firm);
                cmd3.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                cmd3.Parameters.AddWithValue("@QTY", textBox1.Text);
                cmd3.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                cmd3.ExecuteNonQuery();

                // reduce yarn from godown

                if (textBox1.Visible)
                {
                    SqlCommand cmd4 = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, BOXES, SUPPLY_TO, " +
                            "SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE) values(@FIRM, @TXN_DATE, @YARN, " +
                                "@QTY, 0, (select max(txn_id) from supply_beam), 'B', @SUPPLY_FROM, 'G')", con1);
                    cmd4.Parameters.AddWithValue("@FIRM", firm);
                    cmd4.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd4.Parameters.AddWithValue("@YARN", requiredYarn);
                    cmd4.Parameters.AddWithValue("@QTY", textBox2.Text);
                    cmd4.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                    cmd4.ExecuteNonQuery();

                    // UPDATE WARP WEIGHT QTY FOR OTHER ENTRIES OF SAME SET NO

                    cmd4 = new SqlCommand("UPDATE SUPPLY_CONE SET QTY = @QTY WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO IN (SELECT TXN_ID FROM SUPPLY_BEAM WHERE FIRM = @FIRM AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @GODOWN AND SET_NO = @SET_NO " + periodFilter + ")", con1);
                    cmd4.Parameters.AddWithValue("@FIRM", firm);
                    cmd4.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd4.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                    cmd4.Parameters.AddWithValue("@QTY", textBox2.Text);
                    cmd4.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                    cmd4.ExecuteNonQuery();
                }
                else if (((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(0, 1).Equals("G"))
                {
                    // check if supply cone entry present

                    string query = "select * from supply_CONE WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO IN (SELECT TXN_ID FROM SUPPLY_BEAM WHERE FIRM = @FIRM AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @GODOWN AND SET_NO = @SET_NO " + periodFilter + ")";
                    SqlCommand oCmd = new SqlCommand(query, con1);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);
                    oCmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    oCmd.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                    oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                    Boolean entryPresent = false;
                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            entryPresent = true;
                        }
                    }

                    double totalQty = totalCuts * requiredQty;

                    if (!entryPresent)
                    {
                        SqlCommand cmd4 = new SqlCommand("insert into SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, BOXES, SUPPLY_TO, " +
                                "SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE) values(@FIRM, @TXN_DATE, @YARN, " +
                                    totalQty + ", 0, (select max(txn_id) from supply_beam), 'B', @SUPPLY_FROM, 'G')", con1);
                        cmd4.Parameters.AddWithValue("@FIRM", firm);
                        cmd4.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                        cmd4.Parameters.AddWithValue("@YARN", requiredYarn);
                        cmd4.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                        cmd4.ExecuteNonQuery();
                    }
                    else
                    {
                        // UPDATE WARP WEIGHT QTY FOR OTHER ENTRIES OF SAME SET NO

                        SqlCommand cmd4 = new SqlCommand("UPDATE SUPPLY_CONE SET QTY = " + totalQty + " + (select sum(qty) from supply_cone WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO IN (SELECT TXN_ID FROM SUPPLY_BEAM WHERE FIRM = @FIRM AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @GODOWN AND SET_NO = @SET_NO " + periodFilter + ")) WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO IN (SELECT TXN_ID FROM SUPPLY_BEAM WHERE FIRM = @FIRM AND SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @GODOWN AND SET_NO = @SET_NO " + periodFilter + ")", con1);
                        cmd4.Parameters.AddWithValue("@FIRM", firm);
                        cmd4.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                        cmd4.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                        cmd4.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                        cmd4.ExecuteNonQuery();
                    }
                }
            }

            con1.Close();
        }

        private void setBalanceEntry(SqlConnection con1, string setNo)
        {
            double openBal = 0;
            double warpWeight = 0;
            double input = 0;
            double excess = Double.Parse(textBox1.Text);
            double closeBal = 0;

            string query1 = "SELECT (select isnull((select close_bal from SET_BALANCE SBL, SUPPLY_BEAM SBM where SBL.ENTRY_ID = SBM.ENTRY_ID AND FIRM = @FIRM" + periodFilter + " AND SUPPLY_FROM = @GODOWN AND SUPPLY_FROM_TYPE = 'G' and set_no = (select max(set_no) from supply_beam where firm = @FIRM and supply_from_type = 'G' and supply_from = @GODOWN and set_no < @SET_NO)), 0)) OPEN_BAL , (ISNULL((SELECT SUM(INPUT) FROM (SELECT SUM(QTY) INPUT FROM SUPPLY_CONE SC where FIRM = @FIRM AND SUPPLY_TO = @GODOWN AND SUPPLY_FROM IS NOT NULL and SUPPLY_TO_TYPE = 'G' and txn_date BETWEEN (select ISNULL(DATEADD(DAY, 1, MAX(txn_date)), '01-01-1900') from SUPPLY_BEAM where firm = @FIRM" + periodFilter + " and set_no = (select max(set_no) from supply_beam where firm = @FIRM and supply_from_type = 'G' and supply_from = @GODOWN and set_no < @SET_NO) and supply_from_type = 'G' and supply_from = @GODOWN) AND (select MAX(txn_date) from SUPPLY_BEAM where firm = @FIRM and set_no = @SET_NO and supply_from_type = 'G' and supply_from = @GODOWN) group by txn_date UNION SELECT SUM(QTY) FROM PURCHASE WHERE FIRM = @FIRM AND GODOWN = @GODOWN and txn_date BETWEEN (select ISNULL(DATEADD(DAY, 1, MAX(txn_date)), '01-01-1900') from SUPPLY_BEAM where firm = @FIRM and set_no = (select max(set_no) from supply_beam where firm = @FIRM and supply_from_type = 'G' and supply_from = @GODOWN and set_no < @SET_NO) and supply_from_type = 'G' and supply_from = @GODOWN) AND (select MAX(txn_date) from SUPPLY_BEAM where firm = @FIRM and set_no = @SET_NO and supply_from_type = 'G' and supply_from = @GODOWN)) T), 0)) INPUT , (SELECT SUM(YARN) FROM ( SELECT SUM(CUTS)*QTY YARN FROM SUPPLY_BEAM SB, PRODUCT_REQ PR, PRODUCT P where SB.firm = @FIRM and set_no = @SET_NO and supply_from_type = 'G' and supply_from = @GODOWN AND SB.BEAM = PR.PID AND P.PID = PR.PRODUCT AND P.CATEGORY = 'Yarn' group by qty) T) WARP_WEIGHT";
            SqlCommand oCmd1 = new SqlCommand(query1, con1);
            oCmd1.Parameters.AddWithValue("@FIRM", firm);
            //oCmd1.Parameters.AddWithValue("@YARN", requiredYarn);
            oCmd1.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
            oCmd1.Parameters.AddWithValue("@SET_NO", setNo);

            using (SqlDataReader oReader = oCmd1.ExecuteReader())
            {
                while (oReader.Read())
                {
                    openBal = Double.Parse(oReader["OPEN_BAL"].ToString());
                    input = Double.Parse(oReader["INPUT"].ToString());

                    string wWeight = oReader["WARP_WEIGHT"].ToString();
                    if (wWeight != null && !wWeight.Equals(""))
                    {
                        warpWeight = Double.Parse(wWeight);
                    }
                    else
                    {
                        warpWeight = 0;
                    }
                    closeBal = openBal + input - warpWeight + excess;
                }
            }

            string eId = "(SELECT MAX(ENTRY_ID) FROM SUPPLY_BEAM)";
            if(setEntryIds.ContainsKey(setNo))
            {
                eId = setEntryIds[setNo];
            }

            SqlCommand cmd2 = new SqlCommand("INSERT INTO SET_BALANCE VALUES ("+ eId +", @OPEN_BAL, @INPUT, @WARP_WEIGHT, @EXCESS, @CLOSE_BAL, NULL)", con1);
            cmd2.Parameters.AddWithValue("@OPEN_BAL", openBal.ToString());
            cmd2.Parameters.AddWithValue("@INPUT", input.ToString());
            cmd2.Parameters.AddWithValue("@WARP_WEIGHT", warpWeight.ToString());
            cmd2.Parameters.AddWithValue("@EXCESS", excess.ToString());
            cmd2.Parameters.AddWithValue("@CLOSE_BAL", closeBal.ToString());
            cmd2.ExecuteNonQuery();
        }

        private void deleteAndReinsertSets(SqlConnection con1)
        {
            string query1 = "SELECT SBM.ENTRY_ID, SBM.SET_NO FROM SUPPLY_BEAM SBM, SET_BALANCE SBL WHERE SBL.ENTRY_ID = SBM.ENTRY_ID AND FIRM = @FIRM AND SUPPLY_FROM = @GODOWN AND SUPPLY_FROM_TYPE = 'G' AND SET_NO > @SET_NO" + periodFilter;
            SqlCommand oCmd1 = new SqlCommand(query1, con1);
            oCmd1.Parameters.AddWithValue("@FIRM", firm);
            oCmd1.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
            oCmd1.Parameters.AddWithValue("@SET_NO", textBox3.Text);
            string entryIds = "(";

            using (SqlDataReader oReader = oCmd1.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if (!setEntryIds.ContainsKey(oReader["SET_NO"].ToString()))
                    {
                        setEntryIds.Add(oReader["SET_NO"].ToString(), oReader["ENTRY_ID"].ToString());
                        entryIds += (oReader["ENTRY_ID"].ToString() + ",");
                    }
                }
            }

            if (setEntryIds.Count > 0)
            {
                entryIds = entryIds.Substring(0, entryIds.Length - 1) + ")";

                // delete

                SqlCommand cmd2 = new SqlCommand("DELETE FROM SET_BALANCE WHERE ENTRY_ID IN " + entryIds, con1);
                cmd2.ExecuteNonQuery();

                foreach (string set in setEntryIds.Keys)
                {
                    setBalanceEntry(con1, set);
                }
            }
        }

        private void add00_Click(object sender, EventArgs e)
        {
            addRow(beamCount);
        }

        private void addRow(int index)
        {
            var bn = new TextBox()
            {
                Name = "beamNo" + index,
                Location = new Point(beamNo0.Location.X, beamNo0.Location.Y + 25 * index),
                Size = beamNo0.Size,
                Enabled = beamNo0.Enabled
            };

            bn.TextChanged += new EventHandler(beam_TextChanged);

            var c = new TextBox()
            {
                Name = "cuts" + index,
                Location = new Point(cuts0.Location.X, cuts0.Location.Y + 25 * index),
                Size = cuts0.Size,
                Enabled = cuts0.Enabled
            };

            c.KeyDown += new KeyEventHandler(cuts_KeyDown);

            var bnl = new Label()
            {
                Name = "beamNoLbl" + index,
                Location = new Point(beamNoLbl0.Location.X, beamNoLbl0.Location.Y + 25 * index),
                Size = beamNoLbl0.Size,
                Text = beamNoLbl0.Text,
                Font = beamNoLbl0.Font
            };

            var cl = new Label()
            {
                Name = "cutsLbl" + index,
                Location = new Point(cutsLbl0.Location.X, cutsLbl0.Location.Y + 25 * index),
                Size = cutsLbl0.Size,
                Text = cutsLbl0.Text,
                Font = cutsLbl0.Font
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

            beamCount++;

            add.Click += (s, evt) =>
            {
                addRow(beamCount);
            };

            del.Click += (s, evt) =>
            {
                delRow((PictureBox)s);
            };

            panel3.Controls.Add(bnl);
            panel3.Controls.Add(bn);
            panel3.Controls.Add(cl);
            panel3.Controls.Add(c);
            panel3.Controls.Add(add);
            panel3.Controls.Add(del);
        }
        
        private void delRow(PictureBox del)
        {
            copyCellsForDelete(Int32.Parse(del.Name.Replace("del", "")));

            Panel p = panel3;
            p.Controls.Remove(p.Controls.Find("beamNo" + (beamCount - 1), true)[0]);
            p.Controls.Remove(p.Controls.Find("beamNoLbl" + (beamCount - 1), true)[0]);
            p.Controls.Remove(p.Controls.Find("cuts" + (beamCount - 1), true)[0]);
            p.Controls.Remove(p.Controls.Find("cutsLbl" + (beamCount - 1), true)[0]);
            p.Controls.Remove(p.Controls.Find("add" + (beamCount - 1), true)[0]);
            p.Controls.Remove(p.Controls.Find("del" + (beamCount - 1), true)[0]);

            beamCount--;
        }

        private void copyCellsForDelete(int index)
        {
            for (int i = index; i < (beamCount - 1); i++)
            {
                TextBox bn = (TextBox)(panel3.Controls.Find("beamNo" + i, true)[0]);
                TextBox cu = (TextBox)(panel3.Controls.Find("cuts" + i, true)[0]);

                TextBox bnPrev = (TextBox)(panel3.Controls.Find("beamNo" + (i + 1), true)[0]);
                TextBox cuPrev = (TextBox)(panel3.Controls.Find("cuts" + (i + 1), true)[0]);

                bn.Text = bnPrev.Text;
                cu.Text = cuPrev.Text;
            }
        }

        private void cuts_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string index = (beamCount - 1) + "";
                PictureBox add = (PictureBox) Controls.Find("add" + index, true)[0];
                add00_Click(add, new EventArgs());

                TextBox beamNo = (TextBox) Controls.Find("beamNo" + (beamCount - 1), true)[0];
                TextBox cuts = (TextBox)Controls.Find("cuts" + (beamCount - 1), true)[0];

                TextBox beamNoPrev = (TextBox)Controls.Find("beamNo" + (beamCount - 2), true)[0];
                TextBox cutsPrev = (TextBox)Controls.Find("cuts" + (beamCount - 2), true)[0];

                beamNo.Text = Int32.Parse(beamNoPrev.Text) + 1 + "";
                cuts.Text = cutsPrev.Text;

                cuts.Focus();
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
            {
                label2.Text = "Godown";
                comboBox2.Visible = false;
                label3.Visible = false;
                label11.Visible = false;
                textBox4.Visible = false;
                checkBox1.Visible = false;

                label5.Visible = true;
                label6.Visible = true;
                label13.Visible = true;
                label14.Visible = true;
                textBox1.Visible = true;
                textBox2.Visible = true;
            }
            else
            {
                label2.Text = "Supply To";
                comboBox2.Visible = true;
                label3.Visible = true;
                label11.Visible = true;
                textBox4.Visible = true;
                checkBox1.Visible = true;

                label5.Visible = false;
                label6.Visible = false;
                label13.Visible = false;
                label14.Visible = false;
                textBox1.Visible = false;
                textBox2.Visible = false;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void addRoll0_Click(object sender, EventArgs e)
        {
            var targetForm = new SupplyBeamList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void save0_Click(object sender, EventArgs e)
        {
            var targetForm = new PeriodManagement(firm);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            setBeamPeriod();
        }

        private void setBeamPeriod()
        {
            if (!isOpen)
            {
                con.Open();
                isOpen = true;
            }

            String query = "select from_dt, to_dt from beam_period where firm = @FIRM AND GODOWN = @GODOWN AND FROM_DT <= @DATE AND TO_DT >= @DATE";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));
            oCmd.Parameters.AddWithValue("@DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    periodFilter = " AND TXN_DATE BETWEEN '" + ((DateTime) oReader["FROM_DT"]).ToString("dd-MMM-yyyy") + "' AND '" + ((DateTime)oReader["TO_DT"]).ToString("dd-MMM-yyyy") + "'";
                }
            }

            if (isOpen)
            {
                con.Close();
                isOpen = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            setBeamPeriod();
            if(!loading)
            {
                textBox3_TextChanged(textBox3, null);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("DELETE FROM SUPPLY_BEAM WHERE TXN_ID = @TXN_ID", con);
            cmd.Parameters.AddWithValue("@TXN_ID", txnId);
            cmd.ExecuteNonQuery();

            // DELETE WARP WEIGHT
            cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_TO_TYPE = 'B' AND SUPPLY_TO = @TXN_ID", con);
            cmd.Parameters.AddWithValue("@TXN_ID", txnId);
            cmd.ExecuteNonQuery();

            // DELETE EXCESS
            cmd = new SqlCommand("DELETE FROM SUPPLY_CONE WHERE SUPPLY_FROM_TYPE = 'E' AND SUPPLY_FROM = @TXN_ID", con);
            cmd.Parameters.AddWithValue("@TXN_ID", txnId);
            cmd.ExecuteNonQuery();
            con.Close();

            radioButton1.Checked = true;
            textBox3.Text = "";
            textBox4.Text = "";
            textBox4.Text = "0";

            beamNo0.Text = "";
            cuts0.Text = "";

            for (int i=1; i<=beamCount; i++)
            {
                PictureBox pb = (PictureBox)panel3.Controls.Find("del" + 1, true)[0];
                delRow(pb);
            }
            MessageBox.Show("Transaction Cancelled");
        }

        private void del0_Click(object sender, EventArgs e)
        {

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

        private void cuts0_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton3.Checked)
            {
                comboBox3.Enabled = false;
                cuts0.Enabled = false;
                label2.Text = "Supply From";
            }
            else
            {
                comboBox3.Enabled = true;
                cuts0.Enabled = true;
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if(radioButton3.Checked)
            {
                beamCuts = new Dictionary<string, string>();
                con.Open();

                string exclusionQuery = "AND BEAM_NO NOT IN (SELECT BEAM_NO FROM SUPPLY_BEAM WHERE SUPPLY_FROM_TYPE = 'S' AND SUPPLY_FROM = @SUPPLY_FROM AND SET_NO = @SET_NO " + periodFilter + ") ";
                if(txnId != -1)
                {
                    exclusionQuery = "AND BEAM_NO NOT IN (SELECT BEAM_NO FROM SUPPLY_BEAM WHERE SUPPLY_FROM_TYPE = 'S' AND SUPPLY_FROM = @SUPPLY_FROM AND SET_NO = @SET_NO and txn_id <> "+ txnId +") ";
                }

                String query = "select beam, BEAM_NO, CUTS from supply_beam SB where sb.FIRM = @FIRM and SET_NO = @SET_NO and SUPPLY_FROM_TYPE = 'G' AND SUPPLY_FROM = @SUPPLY_FROM " + exclusionQuery + periodFilter;
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@SET_NO", textBox3.Text);
                oCmd.Parameters.AddWithValue("@TXN_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                oCmd.Parameters.AddWithValue("@SUPPLY_FROM", ((KeyValuePair<string, string>)comboBox1.SelectedItem).Key.Substring(1));

                string quality = "";

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        quality = oReader["BEAM"].ToString();
                        beamCuts.Add(oReader["BEAM_NO"].ToString(), oReader["CUTS"].ToString());
                    }
                }

                if (!quality.Equals(""))
                {
                    comboBox3.SelectedIndex = comboBox3.FindString(beams[quality]);
                }
                else
                {
                    if (panel3.Controls.Find("del" + 1, true).Length > 0)
                    {
                        for (int J = 1; J <= beamCount; J++)
                        {
                            PictureBox pb = (PictureBox)panel3.Controls.Find("del" + 1, true)[0];
                            delRow(pb);
                        }
                    }
                    beamNo0.Text = "";
                    cuts0.Text = "";
                }
                con.Close();
            }
        }

        private void beam_TextChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                string i = ((TextBox)sender).Name.Replace("beamNo", "");
                if (beamCuts.ContainsKey(((TextBox)sender).Text))
                {
                    ((TextBox)Controls.Find("cuts" + i, true)[0]).Text = beamCuts[((TextBox)sender).Text];
                }
                else
                {
                    ((TextBox)Controls.Find("cuts" + i, true)[0]).Text = "";
                }
            }
        }

        private void beamNo0_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
