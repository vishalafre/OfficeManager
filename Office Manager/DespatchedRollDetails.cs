using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class DespatchedRollDetails : Form
    {
        int rollNo;
        string firm;
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        Dictionary<string, string> godowns = new Dictionary<string, string>();
        Dictionary<string, string> weavers = new Dictionary<string, string>();
        Dictionary<string, string> qualities = new Dictionary<string, string>();

        int count;
        string fromDate;
        string toDate;

        public DespatchedRollDetails(int rollNo, string firm)
        {
            InitializeComponent();
            this.rollNo = rollNo;
            this.firm = firm;
        }

        private void DespatchedRollDetails_Load(object sender, EventArgs e)
        {
            CenterToScreen();

            // set godown

            String query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

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

            // set weaver

            query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weavers.Add(oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            if (weavers.Count() > 0)
            {
                weaver0.DataSource = new BindingSource(weavers, null);
                weaver0.DisplayMember = "Value";
                weaver0.ValueMember = "Key";
            }

            // set quality

            query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Cloth' order by TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    qualities.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (qualities.Count() > 0)
            {
                quality.DataSource = new BindingSource(qualities, null);
                quality.DisplayMember = "Value";
                quality.ValueMember = "Key";
            }

            rollNoTxt0.Text = "Roll No : " + rollNo;

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            string fy = "";

            if (month >= 4)
            {
                fy = (year + 1).ToString().Substring(year.ToString().Length - 2);
            }
            else
            {
                fy = year.ToString().Substring(year.ToString().Length - 2);
            }

            int fyYear = Int32.Parse(fy);

            fromDate = "01-APR-" + (fyYear - 1);
            toDate = "31-MAR-" + fyYear;

            query = "select (select tech_name from product where pid = quality) quality, txn_date, isnull((select w_name from weaver where wid = weaver), 'Old Roll') weaver, re.mtr, (select g_name from godown where gid = godown) godown from roll_entry re, roll r, ROLL_CONTENT rc where r.roll_no = rc.roll_no and rc.entry_id = re.entry_id and RE.FIRM = '"+ firm +"' and despatch_date BETWEEN @FROM_DT AND @TO_DT AND re.ENTRY_ID IN (SELECT ENTRY_ID FROM ROLL_CONTENT WHERE ROLL_NO = @ROLL_NO)";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
            oCmd.Parameters.AddWithValue("@FROM_DT", fromDate);
            oCmd.Parameters.AddWithValue("@TO_DT", toDate);

            int index = 0;

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if (index == 0)
                    {
                        godown.SelectedIndex = godown.FindString(oReader["GODOWN"].ToString());
                        quality.SelectedIndex = quality.FindString(oReader["QUALITY"].ToString());
                        weaver0.SelectedIndex = weaver0.FindString(oReader["WEAVER"].ToString());
                        date0.Value = ((DateTime)oReader["TXN_DATE"]);
                        meter0.Text = oReader["MTR"].ToString();
                    }
                    else
                    {
                        var dt = new DateTimePicker()
                        {
                            Name = "date" + index,
                            Location = new Point(date0.Location.X, date0.Location.Y + 25 * index),
                            Size = date0.Size,
                            Value = ((DateTime)oReader["TXN_DATE"]),
                        };

                        var wvr = new ComboBox()
                        {
                            Name = "weaver" + index,
                            Location = new Point(weaver0.Location.X, weaver0.Location.Y + 25 * index),
                            DataSource = new BindingSource(weavers, null),
                            DisplayMember = "Value",
                            ValueMember = "Key",
                            Size = weaver0.Size,
                            DropDownStyle = weaver0.DropDownStyle
                        };

                        var mtr = new Label()
                        {
                            Name = "meter" + index,
                            Location = new Point(meter0.Location.X, meter0.Location.Y + 25 * index),
                            Size = meter0.Size,
                            Text = oReader["MTR"].ToString(),
                            Font = meter0.Font,
                            ForeColor = meter0.ForeColor
                        };
                        
                        this.Controls.Add(dt);
                        this.Controls.Add(wvr);
                        this.Controls.Add(mtr);

                        wvr.SelectedIndex = wvr.FindString(oReader["WEAVER"].ToString());
                    }

                    index++;
                }
            }
            con.Close();

            count = index;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            saveRoll();
        }

        private void saveRoll()
        {
            con.Open();

            List<String> dates = new List<string>();
            List<String> qlts = new List<string>();
            List<String> wvrs = new List<string>();
            List<String> mtrs = new List<string>();
            List<String> gdns = new List<string>();
            List<String> rollNos = new List<string>();

            string mWeaver = ((KeyValuePair<string, string>)((ComboBox) Controls.Find("weaver" + "0", true)[0]).SelectedItem).Key;

            // Delete FROM SUPPLY_CONE

            SqlCommand cmd = new SqlCommand("delete from SUPPLY_CONE WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_content WHERE roll_no = @ROLL_NO) AND SUPPLY_TO_TYPE = 'R' AND TXN_DATE BETWEEN @FROM_DT AND @TO_DT", con);
            cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
            cmd.Parameters.AddWithValue("@FROM_DT", fromDate);
            cmd.Parameters.AddWithValue("@TO_DT", toDate);
            cmd.ExecuteNonQuery();

            // Delete FROM SUPPLY_BEAM

            cmd = new SqlCommand("delete from SUPPLY_BEAM WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_content WHERE roll_no = @ROLL_NO) AND SUPPLY_TO_TYPE = 'R' AND TXN_DATE BETWEEN @FROM_DT AND @TO_DT", con);
            cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
            cmd.Parameters.AddWithValue("@FROM_DT", fromDate);
            cmd.Parameters.AddWithValue("@TO_DT", toDate);

            cmd.ExecuteNonQuery();

            // insert rolls

            for (int i = 0; i < count; i++)
            {
                string weaver = ((KeyValuePair<string, string>)((ComboBox) Controls.Find("weaver" + i, true)[0]).SelectedItem).Key;

                cmd = new SqlCommand("UPDATE ROLL_ENTRY SET TXN_DATE = @TXN_DATE, GODOWN = @GODOWN, QUALITY = @QUALITY, WEAVER = @WEAVER WHERE TXN_DATE BETWEEN @FROM_DT AND @TO_DT AND ENTRY_ID = (SELECT TOP 1 ENTRY_ID FROM (SELECT TOP " + (count - i) +" ENTRY_ID FROM ROLL_CONTENT WHERE ROLL_NO = @ROLL_NO ORDER BY ENTRY_ID DESC) T ORDER BY ENTRY_ID)", con);
                string date = ((DateTimePicker) Controls.Find("date" + i, true)[0]).Value.ToString("dd-MMM-yyyy");
                string quality = ((KeyValuePair<string, string>)((ComboBox) Controls.Find("quality", true)[0]).SelectedItem).Key;

                cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);
                cmd.Parameters.AddWithValue("@TXN_DATE", date);
                cmd.Parameters.AddWithValue("@QUALITY", quality);
                cmd.Parameters.AddWithValue("@WEAVER", weaver);
                cmd.Parameters.AddWithValue("@FROM_DT", fromDate);
                cmd.Parameters.AddWithValue("@TO_DT", toDate);
                cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)((ComboBox) Controls.Find("godown", true)[0]).SelectedItem).Key);

                cmd.ExecuteNonQuery();

                string mtr = ((Label)Controls.Find("meter" + i, true)[0]).Text;

                // INSERT IN SUPPLY_CONE

                cmd = new SqlCommand("INSERT INTO SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_FROM, SUPPLY_FROM_TYPE, SUPPLY_TO, SUPPLY_TO_TYPE) (select '" + firm + "', cast('" + date + "' as date) txn_date, pr.product, round(40*(CAST(" + mtr + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty),0)/40 qty, " + weaver + " SUPPLY_FROM, 'W' FROM_TYPE, (SELECT TOP 1 ENTRY_ID FROM (SELECT TOP " + (count - i) + " ENTRY_ID FROM ROLL_CONTENT WHERE ROLL_NO = "+ rollNo +" ORDER BY ENTRY_ID DESC) T ORDER BY ENTRY_ID) SUPPLY_TO, 'R' TO_TYPE from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Yarn' and pr.pid = " + quality + ")", con);
                cmd.ExecuteNonQuery();

                // INSERT IN SUPPLY_BEAM

                cmd = new SqlCommand("INSERT INTO SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, EXCESS) (select @FIRM, @TXN_DATE, pr.product, CAST(" + mtr + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty qty, (SELECT TOP 1 ENTRY_ID FROM (SELECT TOP " + (count - i) + " ENTRY_ID FROM ROLL_CONTENT WHERE ROLL_NO = "+ rollNo +" ORDER BY ENTRY_ID DESC) T ORDER BY ENTRY_ID), 'R', @SUPPLY_FROM, 'W', 0 from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Beam' and pr.pid = " + quality + ")", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@TXN_DATE", date);
                cmd.Parameters.AddWithValue("@SUPPLY_FROM", weaver);
                cmd.ExecuteNonQuery();
            }
            con.Close();

            MessageBox.Show("Roll Updated");
        }

    }
}
