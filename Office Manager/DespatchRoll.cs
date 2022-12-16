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
    public partial class DespatchRoll : Form
    {
        private string firm;
        private string godown;
        private string quality;
        private int rollNo;
        private double meters;
        private DateTime maxDate;
        private RollEntry form; 

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        int rollNoFetched = -1;
        string oldRollNo = "";
        Boolean loading = true;

        public DespatchRoll()
        {
            InitializeComponent();
        }

        public DespatchRoll(string firm, string godown, string quality, int rollNo, double meters, DateTime maxDate, RollEntry form)
        {
            InitializeComponent();
            this.firm = firm;
            this.godown = godown;
            this.quality = quality;
            this.rollNo = rollNo;
            this.meters = meters;
            this.maxDate = maxDate;
            this.form = form;
        }

        public DespatchRoll(string firm, int rollNoFetched)
        {
            InitializeComponent();
            this.firm = firm;
            this.rollNoFetched = rollNoFetched;

            textBox1.Text = rollNoFetched.ToString();
        }

        private void DespatchRoll_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;
            CenterToScreen();
            Text = "Despatch Roll";

            label10.Text = meters + "";
            oldRollNo = textBox1.Text;

            if (rollNoFetched != -1)
            {
                button1.Text = "Update";
                button2.Visible = true;

                con.Open();
                string query = "select * from roll where firm = @FIRM AND ROLL_NO = @ROLL_NO ORDER BY DESPATCH_DATE DESC";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@ROLL_NO", rollNoFetched);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        label10.Text = oReader["MTR"].ToString();
                        label7.Text = ((int)Double.Parse(oReader["ELONGATION"].ToString())) + "";
                        textBox5.Text = oReader["WIGHT"].ToString();
                        textBox4.Text = oReader["WIDTH"].ToString();

                        double elongation = Double.Parse(label7.Text);
                        double extension = AddInvoice.round(elongation * 100.00 / Double.Parse(label10.Text), 2);
                        textBox3.Text = extension + "%";

                        int totalMtr = (int)Double.Parse(label10.Text) + (int)elongation;
                        label4.Text = totalMtr + " mtr";

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        dateTimePicker1.Value = DateTime.ParseExact(oReader["DESPATCH_DATE"].ToString().Split(' ')[0], CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern, ci);
                    }
                }
                con.Close();
            }
            loading = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (rollNoFetched == -1)
                {
                    int year = DateTime.Now.Year;
                    int month = DateTime.Now.Month;
                    string fy = "";

                    if (month >= 4)
                    {
                        fy = year + "-" + (year + 1).ToString().Substring(year.ToString().Length - 2);
                    }
                    else
                    {
                        fy = (year - 1) + "-" + year.ToString().Substring(year.ToString().Length - 2);
                    }

                    con.Open();
                    SqlCommand cmd = new SqlCommand("insert into ROLL (FIRM, ROLL_NO, MTR, ELONGATION, EXTENDED_MTR, WIGHT, WIDTH, DESPATCH_DATE, FY) " +
                        "values(@FIRM, @ROLL_NO, @MTR, @ELONGATION, @EXTENDED_MTR, @WIGHT, @WIDTH, @DESPATCH_DATE, @FY)", con);
                    int extendedMtr = Int32.Parse(label10.Text) + Int32.Parse(label7.Text.Replace("%", ""));
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
                    cmd.Parameters.AddWithValue("@MTR", label10.Text);
                    cmd.Parameters.AddWithValue("@ELONGATION", label7.Text);
                    cmd.Parameters.AddWithValue("@EXTENDED_MTR", extendedMtr);
                    cmd.Parameters.AddWithValue("@WIGHT", textBox5.Text);
                    cmd.Parameters.AddWithValue("@WIDTH", textBox4.Text);
                    cmd.Parameters.AddWithValue("@DESPATCH_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd.Parameters.AddWithValue("@FY", fy);
                    cmd.ExecuteNonQuery();

                    SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                    con1.Open();

                    String query = "select ENTRY_ID, ROLL_NO from ROLL_ENTRY " +
                        "where firm = @FIRM and GODOWN = @GODOWN AND QUALITY = @QUALITY AND ROLL_NO = @ROLL_NO AND DESPATCHED = 'N'";

                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);
                    oCmd.Parameters.AddWithValue("@GODOWN", godown);
                    oCmd.Parameters.AddWithValue("@QUALITY", quality);
                    oCmd.Parameters.AddWithValue("@ROLL_NO", rollNo);

                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        while (oReader.Read())
                        {
                            String rollNo = oReader["ROLL_NO"].ToString();
                            String entryId = oReader["ENTRY_ID"].ToString();

                            SqlCommand cmd1 = new SqlCommand("insert into ROLL_CONTENT (FIRM, ROLL_NO, ENTRY_ID) " +
                        "values(@FIRM, @ROLL_NO, @ENTRY_ID)", con1);
                            cmd1.Parameters.AddWithValue("@FIRM", firm);
                            cmd1.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
                            cmd1.Parameters.AddWithValue("@ENTRY_ID", entryId);
                            cmd1.ExecuteNonQuery();
                        }
                    }

                    SqlCommand cmd2 = new SqlCommand("update roll_ENTRY set DESPATCHED = 'Y' " +
                        "WHERE firm = @FIRM and GODOWN = @GODOWN AND QUALITY = @QUALITY AND ROLL_NO = @ROLL_NO", con1);
                    cmd2.Parameters.AddWithValue("@FIRM", firm);
                    cmd2.Parameters.AddWithValue("@GODOWN", godown);
                    cmd2.Parameters.AddWithValue("@QUALITY", quality);
                    cmd2.Parameters.AddWithValue("@ROLL_NO", rollNo);
                    cmd2.ExecuteNonQuery();

                    con1.Close();
                    con.Close();

                    form.refillData();
                }
                else
                {
                    int year = maxDate.Year;
                    int month = maxDate.Month;
                    string fy = "";

                    if (month >= 4)
                    {
                        fy = year + "-" + (year + 1).ToString().Substring(year.ToString().Length - 2);
                    }
                    else
                    {
                        fy = (year - 1) + "-" + year.ToString().Substring(year.ToString().Length - 2);
                    }

                    con.Open();

                    SqlCommand cmd = new SqlCommand("UPDATE ROLL SET ROLL_NO = @ROLL_NO, ELONGATION = @ELONGATION, WIGHT = @WIGHT, WIDTH = @WIDTH, DESPATCH_DATE = @DESPATCH_DATE, EXTENDED_MTR = @EXTENDED_MTR, FY = @FY WHERE FIRM = @FIRM AND ROLL_NO = @OLD_ROLL_NO", con);

                    int extendedMtr = (int)Double.Parse(label10.Text) + Int32.Parse(label7.Text.Replace("%", ""));
                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
                    cmd.Parameters.AddWithValue("@OLD_ROLL_NO", oldRollNo);
                    cmd.Parameters.AddWithValue("@MTR", label10.Text);
                    cmd.Parameters.AddWithValue("@ELONGATION", label7.Text);
                    cmd.Parameters.AddWithValue("@EXTENDED_MTR", extendedMtr);
                    cmd.Parameters.AddWithValue("@WIGHT", textBox5.Text);
                    cmd.Parameters.AddWithValue("@WIDTH", textBox4.Text);
                    cmd.Parameters.AddWithValue("@DESPATCH_DATE", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
                    cmd.Parameters.AddWithValue("@FY", fy);
                    cmd.ExecuteNonQuery();

                    con.Close();

                    MessageBox.Show("Despatch data Updated");
                }
                Close();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("duplicate key"))
                {
                    MessageBox.Show("Duplicate Roll No");
                }
                else
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("update roll_entry set roll_no = (SELECT MIN(RNO) ROLL_NO FROM (SELECT (RN + 1) RNO FROM (select 0 RN UNION SELECT distinct re1.roll_no from roll_entry re, ROLL_ENTRY RE1, roll_content rc, roll r where r.ROLL_NO = rc.roll_no and rc.entry_id = re.entry_id and R.ROLL_NO = @ROLL_NO AND RE1.GODOWN = RE.GODOWN AND RE1.QUALITY = RE.QUALITY AND RE1.DESPATCHED = 'N' AND re.FIRM = @FIRM and re1.roll_no > 0) X WHERE (RN + 1) not in (select re1.roll_no from roll_entry re, ROLL_ENTRY RE1, roll_content rc, roll r where r.ROLL_NO = rc.roll_no and rc.entry_id = re.entry_id and R.ROLL_NO = @ROLL_NO AND RE1.GODOWN = RE.GODOWN AND RE1.QUALITY = RE.QUALITY AND RE1.DESPATCHED = 'N' AND re.FIRM = @FIRM AND RE1.ROLL_NO > 0)) Y), despatched = 'N' where ENTRY_ID IN (SELECT ENTRY_ID FROM ROLL_CONTENT WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO) AND DATEADD(YEAR,1,TXN_DATE) > GETDATE()", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand("DELETE FROM ROLL_CONTENT WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND ENTRY_ID IN (SELECT ENTRY_ID FROM ROLL_ENTRY WHERE DATEADD(YEAR,1,TXN_DATE) > GETDATE())", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
            cmd.ExecuteNonQuery();

            int year = DateTime.Now.Year;
            int month = DateTime.Now.Month;
            string fy = "";

            if (month >= 4)
            {
                fy = year + "-" + (year + 1).ToString().Substring(year.ToString().Length - 2);
            }
            else
            {
                fy = (year - 1) + "-" + year.ToString().Substring(year.ToString().Length - 2);
            }

            cmd = new SqlCommand("DELETE FROM ROLL WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND FY = @FY", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
            cmd.Parameters.AddWithValue("@FY", fy);
            cmd.ExecuteNonQuery();

            con.Close();

            MessageBox.Show("Roll despatch undone");
            Close();
        }

        private void label7_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double mtr = Double.Parse(label10.Text);
                double elongation = Double.Parse(label7.Text);
                double extension = AddInvoice.round(elongation * 100.00 / mtr, 2);
                textBox3.Text = extension + "%";
                button1.Enabled = true;
                label4.Text = mtr + elongation + " mtr";
            }
            catch
            {
                textBox3.Text = "Invalid Value";
                button1.Enabled = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                int year = DateTime.Now.Year;
                if (DateTime.Now.Month >= 1 && DateTime.Now.Month <= 3)
                {
                    year--;
                }

                DateTime fromDt0;
                DateTime toDt0;

                fromDt0 = new DateTime(year, 4, 1, 0, 0, 0);
                toDt0 = fromDt0.AddYears(1).AddDays(-1);

                con.Open();
                string query = "select bill_dt, mtr, weight, width from bill b, bill_item bi where b.bill_id = bi.bill_id and qty = 1 and roll_no = @ROLL_NO AND BILL_DT > '30-SEP-18' and bill_dt between @FROM AND @TO ORDER BY 1 DESC";
                SqlCommand oCmd = new SqlCommand(query, con);
                oCmd.Parameters.AddWithValue("@ROLL_NO", textBox1.Text);
                oCmd.Parameters.AddWithValue("@FROM", fromDt0.ToString("dd-MMM-yyyy"));
                oCmd.Parameters.AddWithValue("@TO", toDt0.ToString("dd-MMM-yyyy"));

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        dateTimePicker1.Value = ((DateTime)oReader["BILL_DT"]);
                        int billMtr = (int)Double.Parse(oReader["MTR"].ToString());
                        label7.Text = (int)(billMtr - Double.Parse(label10.Text)) + "";
                        textBox5.Text = oReader["WEIGHT"].ToString();
                        textBox4.Text = oReader["WIDTH"].ToString();
                    }
                }
                con.Close();
            }
        }
    }
}
