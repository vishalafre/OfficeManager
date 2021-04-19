using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class RollEntry : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        private string firm;
        private byte[] logo;

        Dictionary<string, string> godowns = new Dictionary<string, string>();
        Dictionary<string, string> weavers = new Dictionary<string, string>();
        Dictionary<string, string> weaversWithOldRoll = new Dictionary<string, string>();
        Dictionary<string, string> qualities = new Dictionary<string, string>();

        int rollCount = 1;
        List<int> rowCounts = new List<int>();
        Boolean loading = true;

        public static string mergedRoll = "";
        DateTime mergedDate1;
        DateTime mergedFromDate1;
        string mergedGodown;
        string mergedQuality;

        Color controlLight;

        Dictionary<int, string> loadedQualities;
        Dictionary<int, string> loadedGodowns;
        Dictionary<int, string> loadedRollNos;

        public RollEntry()
        {
            InitializeComponent();
        }

        public RollEntry(string firm, byte[] logo)
        {
            InitializeComponent();
            this.firm = firm;
            this.logo = logo;
        }

        private void RollEntry_Load(object sender, EventArgs e)
        {
            controlLight = merge0.BackColor;
            meter00.KeyDown += new KeyEventHandler(mtr_KeyDown);

            MemoryStream ms = new MemoryStream(logo);
            pictureBox17.Image = Image.FromStream(ms);
            String[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", };

            DateTime dt = dateTimePicker2.Value;
            String today = dt.DayOfWeek.ToString();
            int index = days.ToList().IndexOf(today);

            dateTimePicker2.Value = dt.AddDays(-1 * index);
            dateTimePicker1.Value = dt.AddDays(6 - index);

            rowCounts.Add(1);

            Dictionary<string, string> godownsSel = new Dictionary<string, string>();
            Dictionary<string, string> qualitiesSel = new Dictionary<string, string>();

            // set godown

            String query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            godownsSel.Add("Select", "Select");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                    godownsSel.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godownsSel.Count() > 0)
            {
                comboBox1.DataSource = new BindingSource(godownsSel, null);
                comboBox1.DisplayMember = "Value";
                comboBox1.ValueMember = "Key";

                if (godowns.Count() > 0)
                {
                    godown0.DataSource = new BindingSource(godowns, null);
                    godown0.DisplayMember = "Value";
                    godown0.ValueMember = "Key";
                }
            }

            // set weaver

            query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            weaversWithOldRoll.Add("Old Roll", "Old Roll");
            weavers.Add("-1", "Old Roll");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weavers.Add(oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                    weaversWithOldRoll.Add(oReader["WID"].ToString(), oReader["W_NAME"].ToString());
                }
            }

            if (weavers.Count() > 0)
            {
                weaver00.DataSource = new BindingSource(weavers, null);
                weaver00.DisplayMember = "Value";
                weaver00.ValueMember = "Key";
            }

            // set quality

            query = "select PID, TECH_NAME from PRODUCT where firm = @FIRM AND CATEGORY = 'Cloth' order by TECH_NAME";
            oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            qualitiesSel.Add("Select", "Select");
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    qualities.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                    qualitiesSel.Add(oReader["PID"].ToString(), oReader["TECH_NAME"].ToString());
                }
            }

            if (qualitiesSel.Count() > 0)
            {
                comboBox3.DataSource = new BindingSource(qualitiesSel, null);
                comboBox3.DisplayMember = "Value";
                comboBox3.ValueMember = "Key";

                if (qualities.Count() > 0)
                {
                    quality0.DataSource = new BindingSource(qualities, null);
                    quality0.DisplayMember = "Value";
                    quality0.ValueMember = "Key";
                }
            }

            // show rolls
            showRolls();
            loading = false;

            con.Close();
        }

        private void showRolls()
        {
            loadedQualities = new Dictionary<int, string>();
            loadedGodowns = new Dictionary<int, string>();
            loadedRollNos = new Dictionary<int, string>();

            int c = 0;  // COUNT
            String quality = "";
            String rollNo = "";
            String godown = "";

            int panelIndex = 0;
            int entryIndex = 0;

            string godownCriteria = "";
            string qualityCriteria = "";

            if(!comboBox1.Text.Equals("Select"))
            {
                godownCriteria = "and GODOWN = @GODOWN";
            }

            if (!comboBox3.Text.Equals("Select"))
            {
                qualityCriteria = "AND QUALITY = @QUALITY";
            }

            String query = "select QUALITY, ROLL_NO, GODOWN, CONCAT('',WEAVER) WEAVER, TXN_DATE, MTR from ROLL_ENTRY " +
                "where firm = @FIRM "+ godownCriteria +" "+ qualityCriteria + " AND DESPATCHED = 'N' AND ROLL_NO > 0" +
                " AND (TXN_DATE BETWEEN @FROM AND @TO or (txn_date > @TO and exists (select * from ROLL_ENTRY re where firm = " +
                "re.firm  and godown = re.godown AND QUALITY = re.quality AND DESPATCHED = 'N' and roll_no = re.roll_no and " +
                "TXN_DATE BETWEEN @FROM AND @TO))) union all " +
                "select QUALITY, ROLL_NO, GODOWN, 'Old Roll' WEAVER, MAX(TXN_DATE) TXN_DATE, sum(mtr) MTR " +
                "from ROLL_ENTRY where firm = @FIRM " + godownCriteria + " " + qualityCriteria + " AND TXN_DATE < @FROM " +
                "AND DESPATCHED = 'N' AND ROLL_NO > 0 group by QUALITY, ROLL_NO, GODOWN order by QUALITY, ROLL_NO, GODOWN, TXN_DATE";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@FROM", dateTimePicker2.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@TO", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)(comboBox1).SelectedItem).Key);
            oCmd.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)(comboBox3).SelectedItem).Key);

            Boolean rowsFetched = false;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    rowsFetched = true;

                    String q = oReader["QUALITY"].ToString();
                    String r = oReader["ROLL_NO"].ToString();
                    String g = oReader["GODOWN"].ToString();
                    String wvr = oReader["WEAVER"].ToString();
                    String date = oReader["TXN_DATE"].ToString();
                    String meter = oReader["MTR"].ToString();

                    Panel main = (Panel)Controls.Find("mPanel" + panelIndex, true)[0];
                    Panel rPanel = (Panel)Controls.Find("rollDetails" + panelIndex, true)[0];

                    if (c > 0)
                    {
                        if (!q.Equals(quality) || !r.Equals(rollNo) || !g.Equals(godown))
                        {
                            addRoll();
                            quality = q;
                            rollNo = r;
                            godown = g;
                            panelIndex++;

                            if (entryIndex != 0)
                            {
                                entryIndex = 0;

                                loadedQualities.Add(panelIndex, q);
                                loadedGodowns.Add(panelIndex, g);
                                loadedRollNos.Add(panelIndex, r);

                                loading = true;
                                ComboBox gc = (ComboBox)Controls.Find("godown" + panelIndex, true)[0];
                                gc.SelectedIndex = gc.FindString(godowns[g]);

                                ComboBox qc = (ComboBox)Controls.Find("quality" + panelIndex, true)[0];
                                qc.SelectedIndex = qc.FindString(qualities[q]);

                                CultureInfo ci = CultureInfo.InvariantCulture;
                                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                                DateTimePicker dtp = (DateTimePicker)Controls.Find("date" + panelIndex + entryIndex, true)[0];
                                dtp.Value = DateTime.ParseExact(date.Split(' ')[0], sysFormat, ci);

                                TextBox mtrTb = (TextBox)Controls.Find("meter" + panelIndex + entryIndex, true)[0];
                                mtrTb.Text = meter;

                                ComboBox wc = (ComboBox)Controls.Find("weaver" + panelIndex + entryIndex, true)[0];
                                if(wvr.Equals(""))
                                {
                                    wc.DataSource = new BindingSource(weavers, null);
                                    wc.SelectedIndex = wc.FindString(weavers["-1"]);
                                }
                                else if(wvr.Equals("Old Roll"))
                                {
                                    wc.DataSource = new BindingSource(weaversWithOldRoll, null);
                                    wc.SelectedIndex = wc.FindString(weaversWithOldRoll[wvr]);
                                }
                                else
                                {
                                    wc.SelectedIndex = wc.FindString(weavers[wvr]);
                                }
                                loading = false;

                                //gc.Enabled = false;
                                //qc.Enabled = false;

                                if (wvr.Equals("Old Roll"))
                                {
                                    dtp.Enabled = false;
                                    mtrTb.Enabled = false;
                                    wc.Enabled = false;
                                }

                                ((Label)Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + r;

                                entryIndex++;
                            }
                        }
                        else
                        {
                            addRow(panelIndex, rowCounts[panelIndex]);
                            string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                            loading = true;

                            CultureInfo ci = CultureInfo.InvariantCulture;
                            ((DateTimePicker)main.Controls.Find("date" + panelIndex + entryIndex, true)[0]).Value = DateTime.ParseExact(date.Split(' ')[0], sysFormat, ci);

                            ((TextBox)main.Controls.Find("meter" + panelIndex + entryIndex, true)[0]).Text = meter;

                            ComboBox wc = (ComboBox)rPanel.Controls.Find("weaver" + panelIndex + entryIndex, true)[0];

                            if(wvr.Equals(""))
                            {
                                wc.DataSource = new BindingSource(weavers, null);
                                wc.SelectedIndex = wc.FindString(weavers["-1"]);
                            }
                            else if (wvr.Equals("Old Roll"))
                            {
                                wc.DataSource = new BindingSource(weaversWithOldRoll, null);
                                wc.SelectedIndex = wc.FindString(weaversWithOldRoll[wvr]);
                            }
                            else
                            {
                                wc.SelectedIndex = wc.FindString(weavers[wvr]);
                            }
                            loading = false;

                            //MessageBox.Show("Add Entry");

                            entryIndex++;
                        }
                    }
                    else
                    {
                        c++;
                        quality = q;
                        rollNo = r;
                        godown = g;

                        loadedQualities.Add(panelIndex, q);
                        loadedGodowns.Add(panelIndex, g);
                        loadedRollNos.Add(panelIndex, r);

                        ((Label)main.Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + r;

                        loading = true;
                        ComboBox gc = (ComboBox)rPanel.Controls.Find("godown" + panelIndex, true)[0];
                        gc.SelectedIndex = gc.FindString(godowns[g]);

                        ComboBox qc = (ComboBox)rPanel.Controls.Find("quality" + panelIndex, true)[0];
                        qc.SelectedIndex = qc.FindString(qualities[q]);

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        DateTimePicker dtp = (DateTimePicker)main.Controls.Find("date" + panelIndex + entryIndex, true)[0];
                        string sysUIFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                        dtp.Value = DateTime.ParseExact(date.Split(' ')[0], sysUIFormat, ci);

                        TextBox mtrTb = (TextBox)main.Controls.Find("meter" + panelIndex + entryIndex, true)[0];
                        mtrTb.Text = meter;

                        ComboBox wc = (ComboBox)rPanel.Controls.Find("weaver" + panelIndex + entryIndex, true)[0];
                        if (wvr.Equals("Old Roll"))
                        {
                            wc.DataSource = new BindingSource(weaversWithOldRoll, null);
                            wc.SelectedIndex = wc.FindString(weaversWithOldRoll[wvr]);
                        }
                        else
                        {
                            if (wvr.Equals(""))
                            {
                                wc.SelectedIndex = 0;
                            }
                            else
                            {
                                wc.SelectedIndex = wc.FindString(weavers[wvr]);
                            }
                        }
                        loading = false;

                        entryIndex++;
                        //gc.Enabled = false;
                        //qc.Enabled = false;

                        if(wvr.Equals("Old Roll"))
                        {
                            dtp.Enabled = false;
                            mtrTb.Enabled = false;
                            wc.Enabled = false;
                        }

                        delRoll0.Visible = true;
                    }
                }
            }

            if(!rowsFetched)
            {
                rollNoTxt0.Text = "Roll No : 1";
            }
        }

        private void showAllRolls()
        {
            loadedQualities = new Dictionary<int, string>();
            loadedGodowns = new Dictionary<int, string>();
            loadedRollNos = new Dictionary<int, string>();

            int c = 0;  // COUNT
            String quality = "";
            String rollNo = "";
            String godown = "";

            int panelIndex = 0;
            int entryIndex = 0;

            string godownCriteria = "";
            string qualityCriteria = "";

            if (!comboBox1.Text.Equals("Select"))
            {
                godownCriteria = "and GODOWN = @GODOWN";
            }

            if (!comboBox3.Text.Equals("Select"))
            {
                qualityCriteria = "AND QUALITY = @QUALITY";
            }

            String query = "select (select roll_no from roll_content where entry_id = re.entry_id) DES_ROLL_NO, QUALITY, " +
                "ROLL_NO, GODOWN, CONCAT('',WEAVER) WEAVER, TXN_DATE, MTR from ROLL_ENTRY RE " +
                "where firm = @FIRM " + godownCriteria + " " + qualityCriteria + " AND ROLL_NO > 0" +
                " AND (TXN_DATE BETWEEN @FROM AND @TO or (txn_date > @TO))";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@FROM", dateTimePicker2.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@TO", dateTimePicker1.Value.ToString("dd-MMM-yyyy"));
            oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)(comboBox1).SelectedItem).Key);
            oCmd.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)(comboBox3).SelectedItem).Key);

            Boolean rowsFetched = false;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    rowsFetched = true;

                    String q = oReader["QUALITY"].ToString();
                    String r = oReader["ROLL_NO"].ToString();
                    String g = oReader["GODOWN"].ToString();
                    String wvr = oReader["WEAVER"].ToString();
                    String date = oReader["TXN_DATE"].ToString();
                    String meter = oReader["MTR"].ToString();

                    string desRollNo = oReader["DES_ROLL_NO"].ToString();

                    Panel main = (Panel)Controls.Find("mPanel" + panelIndex, true)[0];
                    Panel rPanel = (Panel)Controls.Find("rollDetails" + panelIndex, true)[0];

                    if (c > 0)
                    {
                        if (!q.Equals(quality) || !r.Equals(rollNo) || !g.Equals(godown))
                        {
                            addRoll();
                            quality = q;
                            rollNo = r;
                            godown = g;
                            panelIndex++;

                            if (entryIndex != 0)
                            {
                                entryIndex = 0;

                                loadedQualities.Add(panelIndex, q);
                                loadedGodowns.Add(panelIndex, g);
                                loadedRollNos.Add(panelIndex, r);

                                loading = true;
                                ComboBox gc = (ComboBox)Controls.Find("godown" + panelIndex, true)[0];
                                gc.SelectedIndex = gc.FindString(godowns[g]);

                                ComboBox qc = (ComboBox)Controls.Find("quality" + panelIndex, true)[0];
                                qc.SelectedIndex = qc.FindString(qualities[q]);

                                CultureInfo ci = CultureInfo.InvariantCulture;
                                string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                                DateTimePicker dtp = (DateTimePicker)Controls.Find("date" + panelIndex + entryIndex, true)[0];
                                dtp.Value = DateTime.ParseExact(date.Split(' ')[0], sysFormat, ci);

                                TextBox mtrTb = (TextBox)Controls.Find("meter" + panelIndex + entryIndex, true)[0];
                                mtrTb.Text = meter;

                                ComboBox wc = (ComboBox)Controls.Find("weaver" + panelIndex + entryIndex, true)[0];
                                if (wvr.Equals(""))
                                {
                                    wc.DataSource = new BindingSource(weavers, null);
                                    wc.SelectedIndex = wc.FindString(weavers["-1"]);
                                }
                                else if (wvr.Equals("Old Roll"))
                                {
                                    wc.DataSource = new BindingSource(weaversWithOldRoll, null);
                                    wc.SelectedIndex = wc.FindString(weaversWithOldRoll[wvr]);
                                }
                                else
                                {
                                    wc.SelectedIndex = wc.FindString(weavers[wvr]);
                                }
                                loading = false;

                                ((Label)Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + r;

                                ((Button)Controls.Find("merge" + panelIndex, true)[0]).Visible = false;
                                ((Button)Controls.Find("despatch" + panelIndex, true)[0]).Visible = false;
                                ((Button)Controls.Find("save" + panelIndex, true)[0]).Visible = false;

                                ((PictureBox)Controls.Find("addRoll" + panelIndex, true)[0]).Visible = false;
                                ((PictureBox)Controls.Find("delRoll" + panelIndex, true)[0]).Visible = false;

                                dtp.Enabled = false;
                                mtrTb.Enabled = false;
                                wc.Enabled = false;
                                gc.Enabled = false;
                                qc.Enabled = false;

                                if (!desRollNo.Equals(""))
                                {
                                    ((Label)Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + desRollNo + " (Despatched)";
                                }

                                entryIndex++;
                            }
                        }
                        else
                        {
                            addRow(panelIndex, rowCounts[panelIndex]);
                            string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                            loading = true;

                            CultureInfo ci = CultureInfo.InvariantCulture;
                            ((DateTimePicker)main.Controls.Find("date" + panelIndex + entryIndex, true)[0]).Value = DateTime.ParseExact(date.Split(' ')[0], sysFormat, ci);

                            ((TextBox)main.Controls.Find("meter" + panelIndex + entryIndex, true)[0]).Text = meter;

                            ComboBox wc = (ComboBox)rPanel.Controls.Find("weaver" + panelIndex + entryIndex, true)[0];

                            if (wvr.Equals(""))
                            {
                                wc.DataSource = new BindingSource(weavers, null);
                                wc.SelectedIndex = wc.FindString(weavers["-1"]);
                            }
                            else
                            {
                                wc.SelectedIndex = wc.FindString(weavers[wvr]);
                            }
                            loading = false;

                            ((Button)Controls.Find("merge" + panelIndex, true)[0]).Visible = false;
                            ((Button)Controls.Find("despatch" + panelIndex, true)[0]).Visible = false;
                            ((Button)Controls.Find("save" + panelIndex, true)[0]).Visible = false;

                            ((PictureBox)Controls.Find("addRoll" + panelIndex, true)[0]).Visible = false;
                            ((PictureBox)Controls.Find("delRoll" + panelIndex, true)[0]).Visible = false;

                            ((DateTimePicker)main.Controls.Find("date" + panelIndex + entryIndex, true)[0]).Enabled = false;
                            ((TextBox)main.Controls.Find("meter" + panelIndex + entryIndex, true)[0]).Enabled = false;
                            wc.Enabled = false;

                            if (!desRollNo.Equals(""))
                            {
                                ((Label)Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + desRollNo + " (Despatched)";
                            }

                            entryIndex++;
                        }
                    }
                    else
                    {
                        c++;
                        quality = q;
                        rollNo = r;
                        godown = g;

                        loadedQualities.Add(panelIndex, q);
                        loadedGodowns.Add(panelIndex, g);
                        loadedRollNos.Add(panelIndex, r);

                        ((Label)main.Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + r;

                        loading = true;
                        ComboBox gc = (ComboBox)rPanel.Controls.Find("godown" + panelIndex, true)[0];
                        gc.SelectedIndex = gc.FindString(godowns[g]);

                        ComboBox qc = (ComboBox)rPanel.Controls.Find("quality" + panelIndex, true)[0];
                        qc.SelectedIndex = qc.FindString(qualities[q]);

                        CultureInfo ci = CultureInfo.InvariantCulture;
                        DateTimePicker dtp = (DateTimePicker)main.Controls.Find("date" + panelIndex + entryIndex, true)[0];
                        string sysUIFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
                        dtp.Value = DateTime.ParseExact(date.Split(' ')[0], sysUIFormat, ci);

                        TextBox mtrTb = (TextBox)main.Controls.Find("meter" + panelIndex + entryIndex, true)[0];
                        mtrTb.Text = meter;

                        ComboBox wc = (ComboBox)rPanel.Controls.Find("weaver" + panelIndex + entryIndex, true)[0];
                        if (wvr.Equals("Old Roll"))
                        {
                            wc.DataSource = new BindingSource(weaversWithOldRoll, null);
                            wc.SelectedIndex = wc.FindString(weaversWithOldRoll[wvr]);
                        }
                        else
                        {
                            if (wvr.Equals(""))
                            {
                                wc.SelectedIndex = 0;
                            }
                            else
                            {
                                wc.SelectedIndex = wc.FindString(weavers[wvr]);
                            }
                        }
                        loading = false;

                        entryIndex++;

                        ((Button)Controls.Find("merge" + panelIndex, true)[0]).Visible = false;
                        ((Button)Controls.Find("despatch" + panelIndex, true)[0]).Visible = false;
                        ((Button)Controls.Find("save" + panelIndex, true)[0]).Visible = false;

                        ((PictureBox)Controls.Find("addRoll" + panelIndex, true)[0]).Visible = false;
                        ((PictureBox)Controls.Find("delRoll" + panelIndex, true)[0]).Visible = false;

                        dtp.Enabled = false;
                        mtrTb.Enabled = false;
                        wc.Enabled = false;
                        gc.Enabled = false;
                        qc.Enabled = false;

                        if (!desRollNo.Equals(""))
                        {
                            ((Label)Controls.Find("rollNoTxt" + panelIndex, true)[0]).Text = "Roll No : " + desRollNo + " (Despatched)";
                        }

                        delRoll0.Visible = true;
                    }
                }
            }

            if (!rowsFetched)
            {
                rollNoTxt0.Text = "Roll No : 1";
            }
        }

        private void pictureBox18_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Button despatchBtn = (Button) sender;
            int index = Int32.Parse(despatchBtn.Name.Replace("despatch", ""));
            double meters = 0;

            ComboBox gc = (ComboBox)Controls.Find("godown" + index, true)[0];
            ComboBox qc = (ComboBox)Controls.Find("quality" + index, true)[0];

            string godown = ((KeyValuePair<string, string>)(gc).SelectedItem).Key;
            string quality = ((KeyValuePair<string, string>)(qc).SelectedItem).Key;

            String rText = ((Label)Controls.Find("rollNoTxt" + index, true)[0]).Text;
            int rollNo = Int32.Parse(rText.Split(':')[1].Trim());

            for (int i=0; i<rowCounts[index]; i++)
            {
                meters += Double.Parse(((TextBox)Controls.Find("meter" + index + i, true)[0]).Text);
            }

            DateTime dt = DateTime.MinValue;

            for (int i = 0; i < rowCounts[index]; i++)
            {
                DateTime dt1 = ((DateTimePicker)Controls.Find("date" + index + i, true)[0]).Value;
                if(dt1.CompareTo(dt) > 0)
                {
                    dt = dt1;
                }
            }

            var targetForm = new DespatchRoll(firm, godown, quality, rollNo, meters, dt, this);
            targetForm.StartPosition = FormStartPosition.CenterParent;
            targetForm.Show();
        }

        private void add00_Click(object sender, EventArgs e)
        {
            addRow(0, rowCounts[0]);
        }

        private void mtr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                TextBox pb = (TextBox) sender;
                string index = pb.Name.Replace("meter", "");
                int i = Int32.Parse(index.Substring(0, index.Length - 1));
                addRow(i, rowCounts[i]);

                ((DateTimePicker)Controls.Find("date" + i + (rowCounts[i] - 1), true)[0]).Focus();
            }
        }

        private void addRow(int rollIndex, int rowIndex)
        {
            var date = new DateTimePicker()
            {
                Name = "date" + rollIndex + rowIndex,
                Location = new Point(date00.Location.X, date00.Location.Y + 25 * rowIndex),
                Value = ((DateTimePicker) Controls.Find("date" + rollIndex + (rowIndex- 1), true)[0]).Value
            };

            var weaver = new ComboBox()
            {
                Name = "weaver" + rollIndex + rowIndex,
                Location = new Point(weaver00.Location.X, weaver00.Location.Y + 25 * rowIndex),
                DataSource = new BindingSource(weavers, null),
                DisplayMember = "Value",
                ValueMember = "Key",
                Size = weaver00.Size,
                DropDownStyle = weaver00.DropDownStyle
            };
            var meter = new TextBox()
            {
                Name = "meter" + rollIndex + rowIndex,
                Location = new Point(meter00.Location.X, meter00.Location.Y + 25 * rowIndex),
                Size = meter00.Size
            };

            meter.KeyDown += new KeyEventHandler(mtr_KeyDown);

            var add = new PictureBox()
            {
                Name = "add" + rollIndex + rowIndex,
                Location = new Point(add00.Location.X, add00.Location.Y + 25 * rowIndex),
                SizeMode = add00.SizeMode,
                Image = add00.Image,
                Size = add00.Size
            };
            var del = new PictureBox()
            {
                Name = "del" +rollIndex + rowIndex,
                Location = new Point(del00.Location.X, del00.Location.Y + 25 * rowIndex),
                Visible = true,
                SizeMode = del00.SizeMode,
                Image = del00.Image,
                Size = del00.Size
            };

            rowCounts[rollIndex]++;

            add.Click += (s, evt) =>
            {
                addRow(rollIndex, rowCounts[rollIndex]);
            };

            del.Click += (s, evt) =>
            {
                delRow(rollIndex, (PictureBox) s);
            };

            ((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Add(date);
            ((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Add(weaver);
            ((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Add(meter);
            ((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Add(add);
            ((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Add(del);
        }

        private void delRow(int rollIndex, PictureBox del)
        {
            copyCellsForDelete(rollIndex, Int32.Parse(del.Name.Replace("del" + rollIndex, "")));
            int i = rowCounts[rollIndex] - 1;

            Panel p = (Panel)Controls.Find("rollDetails" + rollIndex, true)[0];
            p.Controls.Remove(p.Controls.Find("date" + rollIndex + i, true)[0]);
            p.Controls.Remove(p.Controls.Find("weaver" + rollIndex + i, true)[0]);
            p.Controls.Remove(p.Controls.Find("meter" + rollIndex + i, true)[0]);
            p.Controls.Remove(p.Controls.Find("add" + rollIndex + i, true)[0]);
            p.Controls.Remove(p.Controls.Find("del" + rollIndex + i, true)[0]);

            rowCounts[rollIndex]--;
        }

        private void copyCellsForDelete(int rollIndex, int index)
        {
            for (int i = index; i < (rowCounts[rollIndex] - 1); i++)
            {
                TextBox meter = (TextBox)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("meter" + rollIndex + i, true)[0];
                ComboBox weaver = (ComboBox)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("weaver" + rollIndex + i, true)[0];
                DateTimePicker date = (DateTimePicker)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("date" + rollIndex + i, true)[0];

                TextBox meterPrev = (TextBox)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("meter" + rollIndex + (i + 1), true)[0];
                ComboBox weaverPrev = (ComboBox)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("weaver" + rollIndex + (i + 1), true)[0];
                DateTimePicker datePrev = (DateTimePicker)((Panel) Controls.Find("rollDetails" + rollIndex, true)[0]).Controls.Find("date" + rollIndex + (i + 1), true)[0];

                meter.Text = meterPrev.Text;
                weaver.SelectedIndex = weaverPrev.SelectedIndex;
                date.Value = datePrev.Value;
            }
        }

        private void setRollNo(int index)
        {
            if(!loading)
            {
                SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

                con1.Open();
                String query = "SELECT MIN(RNO) ROLL_NO FROM (SELECT (RN + 1) RNO FROM (select 0 RN UNION SELECT distinct roll_no from roll_entry where godown = @GODOWN and despatched = 'N' and quality = @QUALITY AND FIRM = @FIRM and roll_no > 0) X WHERE (RN + 1) not in (select roll_no from roll_entry where godown = @GODOWN and despatched = 'N' and quality = @QUALITY AND FIRM = @FIRM)) Y";
                SqlCommand oCmd = new SqlCommand(query, con1);

                string godown = ((KeyValuePair<string, string>)((ComboBox)Controls.Find("godown" + index, true)[0]).SelectedItem).Key;
                string quality = ((KeyValuePair<string, string>)((ComboBox)Controls.Find("quality" + index, true)[0]).SelectedItem).Key;

                oCmd.Parameters.AddWithValue("@FIRM", firm);
                oCmd.Parameters.AddWithValue("@QUALITY", quality);
                oCmd.Parameters.AddWithValue("@GODOWN", godown);

                using (SqlDataReader oReader = oCmd.ExecuteReader())
                {
                    if (oReader.Read())
                    {
                        ((Label)Controls.Find("rollNoTxt" + index, true)[0]).Text = "Roll No : " + oReader["ROLL_NO"].ToString();
                    }
                }

                con1.Close();
            }
        }

        private void pictureBox24_Click(object sender, EventArgs e)
        {
            addRoll();
            setRollNo(rollCount - 1);
            ((ComboBox)Controls.Find("quality" + (rollCount - 1), true)[0]).Focus();
        }

        private void addRoll()
        {
            Panel panel = new Panel
            {
                Name = "mPanel" + rollCount,
                Size = mPanel0.Size,
                BackColor = mPanel0.BackColor,
                Location = new Point(mPanel0.Location.X, mPanel0.Location.Y + rollCount + rollCount * (mPanel0.Height + 25))
            };

            rowCounts.Add(1);
            addCustomer.Controls.Add(panel);
            addElementsInRollPanel(rollCount, panel);
            rollCount++;

            del00.Visible = true;
        }

        private void addElementsInRollPanel(int rollIndex, Panel p)
        {
            var elem1 = new Label()
            {
                Name = "rollNoTxt" + rollIndex,
                Location = rollNoTxt0.Location,
                Font = rollNoTxt0.Font,
                ForeColor = rollNoTxt0.ForeColor,
                Size = new Size(291,24),
                Text = "Roll No : 2"
            };

            var elem2 = new Label()
            {
                Name = "godownLbl" + rollIndex,
                Location = godownLbl0.Location,
                Font = godownLbl0.Font,
                ForeColor = godownLbl0.ForeColor,
                Text = godownLbl0.Text,
                Site = godownLbl0.Site
            };

            var elem3 = new Label()
            {
                Name = "qualityLbl" + rollIndex,
                Location = qualityLbl0.Location,
                Font = qualityLbl0.Font,
                ForeColor = qualityLbl0.ForeColor,
                Text = qualityLbl0.Text,
                Site = qualityLbl0.Site
            };

            var elem4 = new Label()
            {
                Name = "dateLbl" + rollIndex,
                Location = dateLbl0.Location,
                Font = dateLbl0.Font,
                ForeColor = dateLbl0.ForeColor,
                Text = dateLbl0.Text,
                Site = dateLbl0.Site
            };

            var elem5 = new Label()
            {
                Name = "weaverLbl" + rollIndex,
                Location = weaverLbl0.Location,
                Font = weaverLbl0.Font,
                ForeColor = weaverLbl0.ForeColor,
                Text = weaverLbl0.Text,
                Site = weaverLbl0.Site
            };

            var elem6 = new Label()
            {
                Name = "meterLbl" + rollIndex,
                Location = meterLbl0.Location,
                Font = meterLbl0.Font,
                ForeColor = meterLbl0.ForeColor,
                Text = meterLbl0.Text,
                Site = meterLbl0.Site
            };

            var elem7 = new DateTimePicker()
            {
                Name = "date" + rollIndex + "0",
                Location = date00.Location
            };

            var elem8 = new ComboBox()
            {
                Name = "weaver" + rollIndex + "0",
                Location = weaver00.Location,
                DataSource = new BindingSource(weavers, null),
                DisplayMember = "Value",
                ValueMember = "Key",
                Size = weaver00.Size,
                DropDownStyle = weaver00.DropDownStyle
            };

            var elem9 = new TextBox()
            {
                Name = "meter" + rollIndex + "0",
                Location = meter00.Location,
                Size = meter00.Size
            };

            elem9.KeyDown += new KeyEventHandler(mtr_KeyDown);

            var elem10 = new PictureBox()
            {
                Name = "add" + rollIndex + "0",
                Location = add00.Location,
                SizeMode = add00.SizeMode,
                Image = add00.Image,
                Size = add00.Size
            };

            var elem11 = new PictureBox()
            {
                Name = "del" + +rollIndex + "0",
                Location = del00.Location,
                SizeMode = del00.SizeMode,
                Image = del00.Image,
                Size = del00.Size
            };

            var elem12 = new Button()
            {
                Name = "despatch" + rollIndex,
                Font = despatch0.Font,
                Text = "DESPATCH",
                ForeColor = despatch0.ForeColor,
                BackColor = despatch0.BackColor,
                Size = despatch0.Size,
                Location = despatch0.Location
            };

            var elem13 = new Button()
            {
                Name = "save" + rollIndex,
                Font = save0.Font,
                Text = "SAVE",
                ForeColor = save0.ForeColor,
                BackColor = save0.BackColor,
                Size = save0.Size,
                Location = save0.Location
            };

            var elem18 = new Button()
            {
                Name = "merge" + rollIndex,
                Font = merge0.Font,
                Text = merge0.Text,
                ForeColor = merge0.ForeColor,
                BackColor = merge0.BackColor,
                Size = merge0.Size,
                Location = merge0.Location
            };

            var elem14 = new PictureBox()
            {
                Name = "addRoll" + rollIndex,
                Location = addRoll0.Location,
                SizeMode = addRoll0.SizeMode,
                Image = addRoll0.Image,
                Size = addRoll0.Size
            };

            var elem15 = new PictureBox()
            {
                Name = "delRoll" + +rollIndex,
                Location = delRoll0.Location,
                SizeMode = delRoll0.SizeMode,
                Image = delRoll0.Image,
                Size = delRoll0.Size
            };

            var elem16 = new ComboBox()
            {
                Name = "godown" + rollIndex,
                Location = godown0.Location,
                DataSource = new BindingSource(godowns, null),
                DisplayMember = "Value",
                ValueMember = "Key",
                Size = godown0.Size,
                DropDownStyle = godown0.DropDownStyle
            };

            var elem17 = new ComboBox()
            {
                Name = "quality" + rollIndex,
                Location = quality0.Location,
                DataSource = new BindingSource(qualities, null),
                DisplayMember = "Value",
                ValueMember = "Key",
                Size = quality0.Size,
                DropDownStyle = quality0.DropDownStyle
            };

            elem16.SelectedIndexChanged += (s, evt) =>
            {
                setRollNo(rollIndex);
            };

            elem17.SelectedIndexChanged += (s, evt) =>
            {
                setRollNo(rollIndex);
            };

            elem7.ValueChanged += (s, evt) =>
            {
                //setRollNo(rollIndex);
            };

            Panel pan = new Panel()
            {
                Name = "rollDetails" + rollIndex,
                Location = rollDetails0.Location,
                Size = rollDetails0.Size,
                BackColor = rollDetails0.BackColor,
                AutoScroll = true
            };

            elem10.Click += (s, evt) =>
            {
                addRow(rollIndex, rowCounts[rollIndex]);
            };

            elem11.Click += (s, evt) =>
            {
                delRow(rollIndex + 1, (PictureBox) s);
            };

            elem12.Click += (s, evt) =>
            {
                button2_Click(s, evt);
            };

            elem13.Click += (s, evt) =>
            {
                saveRoll(rollIndex);
            };

            elem18.Click += (s, evt) =>
            {
                button1_Click(s, evt);
            };

            elem14.Click += (s, evt) =>
            {
                addRoll();
                setRollNo(rollCount - 1);
                ((ComboBox)Controls.Find("quality" + (rollCount - 1), true)[0]).Focus();
            };

            elem15.Click += (s, evt) =>
            {
                deleteRoll(rollIndex);
            };

            if(mergedRoll.Equals(""))
            {
                elem18.BackColor = controlLight;
            }
            else
            {
                elem18.BackColor = Color.PaleTurquoise;
            }

            p.Controls.Add(pan);
            p.Controls.Add(elem1);
            p.Controls.Add(elem2);
            p.Controls.Add(elem3);
            p.Controls.Add(elem4);
            p.Controls.Add(elem5);
            p.Controls.Add(elem6);
            p.Controls.Add(elem12);
            p.Controls.Add(elem13);
            p.Controls.Add(elem18);
            p.Controls.Add(elem14);
            p.Controls.Add(elem15);

            pan.Controls.Add(elem16);
            pan.Controls.Add(elem17);
            pan.Controls.Add(elem7);
            pan.Controls.Add(elem8);
            pan.Controls.Add(elem9);
            pan.Controls.Add(elem10);
            pan.Controls.Add(elem11);
        }

        private void deleteRoll(int index)
        {
            var confirmResult = MessageBox.Show("Are you sure you want to delete this roll?",
                                     "Confirm Delete",
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                con.Open();

                // Delete FROM SUPPLY_CONE

                String rText = ((Label)Controls.Find("rollNoTxt" + index, true)[0]).Text;
                int rollNo = Int32.Parse(rText.Split(':')[1].Trim());

                string mRollNo = rollNo + ""; ;
                string mQuality = ((KeyValuePair<string, string>)((ComboBox) Controls.Find("quality" + index, true)[0]).SelectedItem).Key;
                string mGodown = ((KeyValuePair<string, string>)((ComboBox) Controls.Find("godown" + index, true)[0]).SelectedItem).Key;

                if (!loadedGodowns.ContainsKey(index))
                {
                    loadedQualities.Add(index, mQuality);
                    loadedGodowns.Add(index, mGodown);
                    loadedRollNos.Add(index, mRollNo);
                }
                else
                {
                    mRollNo = loadedRollNos[index];
                    mQuality = loadedQualities[index];
                    mGodown = loadedGodowns[index];
                }

                SqlCommand cmd = new SqlCommand("delete from SUPPLY_CONE WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0) AND SUPPLY_TO_TYPE = 'R'", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@QUALITY", mQuality);
                cmd.Parameters.AddWithValue("@GODOWN", mGodown);
                cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);
                cmd.ExecuteNonQuery();

                // Delete FROM SUPPLY_BEAM

                cmd = new SqlCommand("delete from SUPPLY_BEAM WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0) AND SUPPLY_TO_TYPE = 'R'", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@QUALITY", mQuality);
                cmd.Parameters.AddWithValue("@GODOWN", mGodown);
                cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);
                cmd.ExecuteNonQuery();

                // DELETE FROM ROLL ENTRY 

                cmd = new SqlCommand("DELETE FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0", con);
                cmd.Parameters.AddWithValue("@FIRM", firm);
                cmd.Parameters.AddWithValue("@QUALITY", mQuality);
                cmd.Parameters.AddWithValue("@GODOWN", mGodown);
                cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);

                cmd.ExecuteNonQuery();

                con.Close();

                loadedQualities.Remove(index);
                loadedGodowns.Remove(index);
                loadedRollNos.Remove(index);

                if (rollCount > 1)
                {
                    Panel p1 = (Panel)Controls.Find("mPanel" + index, true)[0];

                    for (int i = index + 1; i < rollCount; i++)
                    {
                        Panel p2 = (Panel)Controls.Find("mPanel" + i, true)[0];
                        p2.Location = new Point(p2.Location.X, p2.Location.Y - (p2.Height + 25));
                    }
                    addCustomer.Controls.Remove(p1);
                    rollCount--;
                    rowCounts.RemoveAt(rollCount);
                }
                else
                {
                    for (int j = 1; j < rowCounts[0]; j++)
                    {
                        rollDetails0.Controls.Remove(Controls.Find("date0" + j, true)[0]);
                        rollDetails0.Controls.Remove(Controls.Find("weaver0" + j, true)[0]);
                        rollDetails0.Controls.Remove(Controls.Find("meter0" + j, true)[0]);
                        rollDetails0.Controls.Remove(Controls.Find("add0" + j, true)[0]);
                        rollDetails0.Controls.Remove(Controls.Find("del0" + j, true)[0]);
                    }
                    meter00.Text = "";
                }
            }
        }

        private void save0_Click(object sender, EventArgs e)
        {
            saveRoll(0);
        }

        private Boolean isSalaryCalculated(int index)
        {
            int count = rowCounts[index];
            Boolean ret = false;
            /*Panel p = (Panel)Controls.Find("rollDetails" + index, true)[0];
            con.Open();

            for (int i = 0; i < count; i++)
            {
                String wvrText = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("weaver" + index + i, true)[0]).SelectedItem).Value;
                if (!wvrText.Equals("Old Roll") && ((ComboBox)p.Controls.Find("weaver" + index + i, true)[0]).Enabled)
                {
                    String query = "select * FROM SALARY_SUMMARY WHERE WEAVER = @WEAVER AND FIRM = @FIRM AND FROM_DATE <= @TXN_DATE AND TO_DATE >= @TXN_DATE";
                    SqlCommand oCmd = new SqlCommand(query, con);
                    oCmd.Parameters.AddWithValue("@FIRM", firm);
                    oCmd.Parameters.AddWithValue("@WEAVER", ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("weaver" + index + i, true)[0]).SelectedItem).Key);
                    oCmd.Parameters.AddWithValue("@TXN_DATE", ((DateTimePicker)p.Controls.Find("date" + index + i, true)[0]).Value.ToString("dd-MMM-yyyy"));


                    using (SqlDataReader oReader = oCmd.ExecuteReader())
                    {
                        if (oReader.Read())
                        {
                            ret = true;
                            break;
                        }
                    }
                }
            }
            
            
            con.Close();*/
            return ret;
        }

        private void saveRoll(int index)
        {
            if (isSalaryCalculated(index))
            {
                MessageBox.Show("Unable to process transaction. Salary is calculated for the one of the weavers for selected date.");
                return;
            }

            Panel p = (Panel)Controls.Find("rollDetails" + index, true)[0];
            Panel main = (Panel)Controls.Find("mPanel" + index, true)[0];
            int count = rowCounts[index];

            con.Open();

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();

            String query = "select TXN_DATE, QUALITY, WEAVER, MTR, ROLL_NO, GODOWN from ROLL_ENTRY " +
                "where firm = @FIRM AND TXN_DATE <= @FROM AND ROLL_NO = @ROLL_NO AND DESPATCHED = 'N'" +
                "AND GODOWN = @GODOWN AND QUALITY = @QUALITY " +
                "order by QUALITY, ROLL_NO, GODOWN, TXN_DATE";
            SqlCommand oCmd1 = new SqlCommand(query, con1);
            oCmd1.Parameters.AddWithValue("@FIRM", firm);

            DateTimePicker mDtp = ((DateTimePicker)p.Controls.Find("date" + index + "0", true)[0]);
            oCmd1.Parameters.AddWithValue("@FROM", mDtp.Value.ToString("dd-MMM-yyyy"));

            String rText = ((Label)main.Controls.Find("rollNoTxt" + index, true)[0]).Text;
            int rollNo = Int32.Parse(rText.Split(':')[1].Trim());

            string mRollNo = rollNo + ""; ;
            string mQuality = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("quality" + index, true)[0]).SelectedItem).Key;
            string mGodown = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("godown" + index, true)[0]).SelectedItem).Key;

            if (!loadedGodowns.ContainsKey(index))
            {
                loadedQualities.Add(index, mQuality);
                loadedGodowns.Add(index, mGodown);
                loadedRollNos.Add(index, mRollNo);
            }
            else
            {
                mRollNo = loadedRollNos[index];
                mQuality = loadedQualities[index];
                mGodown = loadedGodowns[index];
            }

            oCmd1.Parameters.AddWithValue("@ROLL_NO", mRollNo);
            oCmd1.Parameters.AddWithValue("@GODOWN", mGodown);
            oCmd1.Parameters.AddWithValue("@QUALITY", mQuality);

            List<String> dates = new List<string>();
            List<String> qlts = new List<string>();
            List<String> wvrs = new List<string>();
            List<String> mtrs = new List<string>();
            List<String> gdns = new List<string>();
            List<String> rollNos = new List<string>();

            Boolean ret = false;
            string mWeaver = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("weaver" + index + "0", true)[0]).SelectedItem).Key;

            if (mWeaver.Equals("Old Roll"))
            {
                SqlConnection con2 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
                con2.Open();
                
                using (SqlDataReader oReader = oCmd1.ExecuteReader())
                {
                    while (oReader.Read())
                    {
                        query = "select * FROM SALARY_SUMMARY WHERE WEAVER = @WEAVER AND FIRM = @FIRM AND FROM_DATE <= @TXN_DATE AND TO_DATE >= @TXN_DATE";
                        SqlCommand oCmd = new SqlCommand(query, con2);
                        oCmd.Parameters.AddWithValue("@FIRM", firm);
                        oCmd.Parameters.AddWithValue("@WEAVER", oReader["WEAVER"].ToString());
                        oCmd.Parameters.AddWithValue("@TXN_DATE", ((DateTime)oReader["TXN_DATE"]).ToString("dd-MMM-yy"));


                        using (SqlDataReader oReader2 = oCmd.ExecuteReader())
                        {
                            if (oReader2.Read())
                            {
                                ret = true;
                                //break;
                            }
                        }

                        dates.Add(((DateTime)oReader["TXN_DATE"]).ToString("dd-MMM-yy"));
                        qlts.Add(oReader["QUALITY"].ToString());
                        wvrs.Add(oReader["WEAVER"].ToString());
                        mtrs.Add(oReader["MTR"].ToString());
                        gdns.Add(oReader["GODOWN"].ToString());
                        rollNos.Add(oReader["ROLL_NO"].ToString());
                    }
                }
                con2.Close();
            }
            con1.Close();

            if (ret && ((ComboBox)p.Controls.Find("weaver" + index + "0", true)[0]).Enabled)
            {
                MessageBox.Show("Unable to process transaction. Salary is calculated for the one of the weavers for selected date.");
                return;
            }

            // Delete FROM SUPPLY_CONE

            SqlCommand cmd = new SqlCommand("delete from SUPPLY_CONE WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0) AND SUPPLY_TO_TYPE = 'R'", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@QUALITY", mQuality);
            cmd.Parameters.AddWithValue("@GODOWN", mGodown);
            cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);
            cmd.ExecuteNonQuery();

            // Delete FROM SUPPLY_BEAM

            cmd = new SqlCommand("delete from SUPPLY_BEAM WHERE SUPPLY_TO IN (SELECT ENTRY_ID FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0) AND SUPPLY_TO_TYPE = 'R'", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@QUALITY", mQuality);
            cmd.Parameters.AddWithValue("@GODOWN", mGodown);
            cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);
            cmd.ExecuteNonQuery();

            // Delete Rolls

            cmd = new SqlCommand("DELETE FROM ROLL_ENTRY WHERE FIRM = @FIRM AND ROLL_NO = @ROLL_NO AND QUALITY = @QUALITY AND GODOWN = @GODOWN AND DESPATCHED = 'N' AND ROLL_NO > 0", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@QUALITY", mQuality);
            cmd.Parameters.AddWithValue("@GODOWN", mGodown);
            cmd.Parameters.AddWithValue("@ROLL_NO", mRollNo);

            cmd.ExecuteNonQuery();

            // insert old rolls

            for (int i = 0; i < dates.ToArray().Length; i++)
            {
                string w = wvrs[i];
                if(w.Equals(""))
                {
                    w = "null";
                }
                SqlCommand cmd1 = new SqlCommand("insert into ROLL_ENTRY (FIRM, TXN_DATE, QUALITY, WEAVER, MTR, ROLL_NO, GODOWN) " +
                "values(@FIRM, @TXN_DATE, @QUALITY, "+ w +", @MTR, @ROLL_NO, @GODOWN)", con);
                cmd1.Parameters.AddWithValue("@FIRM", firm);
                cmd1.Parameters.AddWithValue("@TXN_DATE", dates[i]);
                cmd1.Parameters.AddWithValue("@QUALITY", ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("quality" + index, true)[0]).SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@MTR", mtrs[i]);
                cmd1.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("godown" + index, true)[0]).SelectedItem).Key);
                cmd1.Parameters.AddWithValue("@ROLL_NO", rollNo);

                cmd1.ExecuteNonQuery();
                
                if (!w.Equals("null"))
                {
                    // INSERT IN SUPPLY_CONE

                    cmd1 = new SqlCommand("INSERT INTO SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_FROM, SUPPLY_FROM_TYPE, SUPPLY_TO, SUPPLY_TO_TYPE) (select '" + firm + "', cast('" + dates[i] + "' as date) txn_date, pr.product, round(40*(CAST(" + mtrs[i] + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty),0)/40 qty, " + wvrs[i] + " SUPPLY_FROM, 'W' FROM_TYPE, (select max(entry_id) from roll_entry) SUPPLY_TO, 'R' TO_TYPE from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Yarn' and pr.pid = " + qlts[i] + ")", con);
                    cmd1.ExecuteNonQuery();

                    // INSERT IN SUPPLY_BEAM

                    cmd1 = new SqlCommand("INSERT INTO SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, EXCESS) (select @FIRM, @TXN_DATE, pr.product, CAST(" + mtrs[i] + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty qty, (SELECT MAX(ENTRY_ID) FROM ROLL_ENTRY), 'R', @SUPPLY_FROM, 'W', 0 from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Beam' and pr.pid = " + qlts[i] + ")", con);
                    cmd1.Parameters.AddWithValue("@FIRM", firm);
                    cmd1.Parameters.AddWithValue("@TXN_DATE", dates[i]);
                    cmd1.Parameters.AddWithValue("@SUPPLY_FROM", wvrs[i]);
                    cmd1.ExecuteNonQuery();
                }
            }

            con1.Close();

            for (int i = 0; i < count; i++)
            {
                String wvrText = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("weaver" + index + i, true)[0]).SelectedItem).Key;
                if(!wvrText.Equals("Old Roll"))
                {
                    string weaver;
                    if (wvrText.Equals("-1"))
                    {
                        weaver = "null";
                    }
                    else
                    {
                        weaver = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("weaver" + index + i, true)[0]).SelectedItem).Key;
                    }

                    cmd = new SqlCommand("insert into ROLL_ENTRY (FIRM, TXN_DATE, QUALITY, WEAVER, MTR, ROLL_NO, GODOWN) " +
                "values(@FIRM, @TXN_DATE, @QUALITY, "+ weaver +", @MTR, @ROLL_NO, @GODOWN)", con);
                    string date = ((DateTimePicker)p.Controls.Find("date" + index + i, true)[0]).Value.ToString("dd-MMM-yyyy");
                    string quality = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("quality" + index, true)[0]).SelectedItem).Key;
                    
                    string mtr = ((TextBox)p.Controls.Find("meter" + index + i, true)[0]).Text;

                    cmd.Parameters.AddWithValue("@FIRM", firm);
                    cmd.Parameters.AddWithValue("@TXN_DATE", date);
                    cmd.Parameters.AddWithValue("@QUALITY", quality);
                    cmd.Parameters.AddWithValue("@WEAVER", weaver);
                    cmd.Parameters.AddWithValue("@MTR", mtr);
                    cmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("godown" + index, true)[0]).SelectedItem).Key);

                    rText = ((Label)main.Controls.Find("rollNoTxt" + index, true)[0]).Text;
                    rollNo = Int32.Parse(rText.Split(':')[1].Trim());

                    cmd.Parameters.AddWithValue("@ROLL_NO", rollNo);

                    cmd.ExecuteNonQuery();

                    if (!weaver.Equals("null"))
                    {

                        // INSERT IN SUPPLY_CONE

                        cmd = new SqlCommand("INSERT INTO SUPPLY_CONE (FIRM, TXN_DATE, YARN, QTY, SUPPLY_FROM, SUPPLY_FROM_TYPE, SUPPLY_TO, SUPPLY_TO_TYPE) (select '" + firm + "', cast('" + date + "' as date) txn_date, pr.product, round(40*(CAST(" + mtr + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty),0)/40 qty, " + weaver + " SUPPLY_FROM, 'W' FROM_TYPE, (select max(entry_id) from roll_entry) SUPPLY_TO, 'R' TO_TYPE from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Yarn' and pr.pid = " + quality + ")", con);
                        cmd.ExecuteNonQuery();

                        // INSERT IN SUPPLY_BEAM

                        cmd = new SqlCommand("INSERT INTO SUPPLY_BEAM (FIRM, TXN_DATE, BEAM, CUTS, SUPPLY_TO, SUPPLY_TO_TYPE, SUPPLY_FROM, SUPPLY_FROM_TYPE, EXCESS) (select @FIRM, @TXN_DATE, pr.product, CAST(" + mtr + " AS DECIMAL(10,3))/P1.UNIT_EQUIVALENT*P1.CALC_RATIO/100*pr.qty qty, (SELECT MAX(ENTRY_ID) FROM ROLL_ENTRY), 'R', @SUPPLY_FROM, 'W', 0 from product_req pr, product p, product p1 where p1.pid = pr.pid and p.pid = pr.product and p.CATEGORY = 'Beam' and pr.pid = " + quality + ")", con);
                        cmd.Parameters.AddWithValue("@FIRM", firm);
                        cmd.Parameters.AddWithValue("@TXN_DATE", date);
                        cmd.Parameters.AddWithValue("@SUPPLY_FROM", weaver);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            con.Close();

            loadedGodowns[index] = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("godown" + index, true)[0]).SelectedItem).Key;
            loadedQualities[index] = ((KeyValuePair<string, string>)((ComboBox)p.Controls.Find("quality" + index, true)[0]).SelectedItem).Key;
            loadedRollNos[index] = rollNo + "";

            MessageBox.Show("Roll Saved");
        }

        private void clearScreen()
        {
            for (int j = 1; j < rollCount; j++)
            {
                Panel p1 = (Panel)Controls.Find("mPanel" + j, true)[0];

                for (int i = j + 1; i < rollCount; i++)
                {
                    Panel p2 = (Panel)Controls.Find("mPanel" + i, true)[0];
                    p2.Location = new Point(p2.Location.X, p2.Location.Y - (p2.Height + 25));
                }
                addCustomer.Controls.Remove(p1);
                rowCounts.RemoveAt(1);
            }

            for (int j = 1; j < rowCounts[0]; j++)
            {
                rollDetails0.Controls.Remove(Controls.Find("date0" + j, true)[0]);
                rollDetails0.Controls.Remove(Controls.Find("weaver0" + j, true)[0]);
                rollDetails0.Controls.Remove(Controls.Find("meter0" + j, true)[0]);
                rollDetails0.Controls.Remove(Controls.Find("add0" + j, true)[0]);
                rollDetails0.Controls.Remove(Controls.Find("del0" + j, true)[0]);
            }

            quality0.Enabled = true;
            date00.Enabled = true;
            weaver00.Enabled = true;
            meter00.Enabled = true;
            godown0.Enabled = true;
            meter00.Text = "";

            addRoll0.Visible = true;
            delRoll0.Visible = true;
            merge0.Visible = true;
            despatch0.Visible = true;
            save0.Visible = true;

            weaver00.DataSource = new BindingSource(weavers, null);
            weaver00.DisplayMember = "Value";
            weaver00.ValueMember = "Key";

            rollCount = 1;
            rowCounts[0] = 1;

            loading = false;
            setRollNo(0);
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            dateTimePicker1.Value = dateTimePicker2.Value.AddDays(6);
        }

        public void refillData()
        {
            if (!loading)
            {
                if (checkBox1.Checked)
                {
                    loading = true;
                    clearScreen();

                    if (!comboBox3.Text.Equals("Select"))
                    {
                        quality0.SelectedIndex = comboBox3.SelectedIndex - 1;
                    }

                    con.Open();
                    showAllRolls();
                    con.Close();
                    loading = false;
                }
                else
                {
                    loading = true;
                    clearScreen();

                    if (!comboBox3.Text.Equals("Select"))
                    {
                        quality0.SelectedIndex = comboBox3.SelectedIndex - 1;
                    }

                    con.Open();
                    if (checkBox1.Checked)
                    {
                        showAllRolls();
                    }
                    else
                    {
                        showRolls();
                    }
                    con.Close();
                    loading = false;
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            refillData();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            refillData();
        }

        private void delRoll0_Click(object sender, EventArgs e)
        {
            deleteRoll(0);
        }

        private void quality0_SelectedIndexChanged(object sender, EventArgs e)
        {
            setRollNo(rollCount - 1);
        }

        private void godown0_SelectedIndexChanged(object sender, EventArgs e)
        {
            setRollNo(rollCount - 1);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (mergedRoll.Equals(""))
            {
                mergedDate1 = dateTimePicker1.Value;
                mergedFromDate1 = dateTimePicker2.Value;

                Button mergeBtn = (Button)sender;
                int index = Int32.Parse(mergeBtn.Name.Replace("merge", ""));

                ComboBox gc = (ComboBox)Controls.Find("godown" + index, true)[0];
                ComboBox qc = (ComboBox)Controls.Find("quality" + index, true)[0];

                mergedGodown = ((KeyValuePair<string, string>)(gc).SelectedItem).Key;
                mergedQuality = ((KeyValuePair<string, string>)(qc).SelectedItem).Key;

                String rText = ((Label)Controls.Find("rollNoTxt" + index, true)[0]).Text;
                mergedRoll = rText.Split(':')[1].Trim();

                var targetForm = new MergeRoll(this);
                targetForm.StartPosition = FormStartPosition.CenterParent;
                targetForm.Show();
            }
            else
            {
                DateTime mergedDate2 = dateTimePicker1.Value;
                Button mergeBtn = (Button)sender;
                int index = Int32.Parse(mergeBtn.Name.Replace("merge", ""));

                String rText = ((Label)Controls.Find("rollNoTxt" + index, true)[0]).Text;
                string currentRollNo = rText.Split(':')[1].Trim();

                ComboBox gc = (ComboBox)Controls.Find("godown" + index, true)[0];
                ComboBox qc = (ComboBox)Controls.Find("quality" + index, true)[0];

                string currentGodown = ((KeyValuePair<string, string>)(gc).SelectedItem).Key;
                string currentQuality = ((KeyValuePair<string, string>)(qc).SelectedItem).Key;

                if(!currentQuality.Equals(mergedQuality))
                {
                    MessageBox.Show("Qualities of rolls to be merged should be same");
                }
                else if (!currentGodown.Equals(mergedGodown))
                {
                    MessageBox.Show("Godowns of rolls to be merged should be same");
                }
                else if(mergedDate2.CompareTo(mergedDate1) == 0 && currentRollNo.Equals(mergedRoll))
                {
                    ((Button) sender).BackColor = controlLight;
                }
                else
                {
                    string oldRollNo;
                    string newRollNo;
                    int dateCompare = mergedDate2.CompareTo(mergedDate1);
                    DateTime mergedFromDt;

                    if (dateCompare > 0)
                    {
                        oldRollNo = mergedRoll;
                        newRollNo = currentRollNo;
                        mergedFromDt = dateTimePicker2.Value;
                    }
                    else
                    {
                        newRollNo = mergedRoll;
                        oldRollNo = currentRollNo;
                        mergedFromDt = mergedFromDate1;
                    }

                    con.Open();

                    MessageBox.Show(newRollNo + " - " + oldRollNo + ", " + mergedGodown + ", " + mergedQuality);

                    SqlCommand cmd1 = new SqlCommand("update roll_entry set roll_no = @NEW_ROLL_NO where despatched = 'N' and ROLL_NO = @OLD_ROLL_NO AND FIRM = @FIRM AND GODOWN = @GODOWN AND QUALITY = @QUALITY", con);
                    cmd1.Parameters.AddWithValue("@FIRM", firm);
                    cmd1.Parameters.AddWithValue("@NEW_ROLL_NO", newRollNo);
                    cmd1.Parameters.AddWithValue("@OLD_ROLL_NO", oldRollNo);
                    cmd1.Parameters.AddWithValue("@GODOWN", mergedGodown);
                    cmd1.Parameters.AddWithValue("@QUALITY", mergedQuality);

                    cmd1.ExecuteNonQuery();
                    
                    MessageBox.Show("Rolls Merged");
                    mergedRoll = "";

                    if (dateCompare == 0)
                    {
                        con.Close();
                        clearScreen();

                        showRolls();
                    }
                    else
                    {
                        con.Close();
                        dateTimePicker2.Value = mergedFromDt;
                    }
                }

                mergedRoll = "";
                merge0.BackColor = controlLight;
            }
        }

        public void changeDate(DateTime dt)
        {
            dateTimePicker2.Value = dt;
            merge0.BackColor = Color.PaleTurquoise;
        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                clearScreen();

                con.Open();
                if (checkBox1.Checked)
                {
                    showAllRolls();
                }
                else
                {
                    showRolls();
                }
                con.Close();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            var targetForm = new DespatchedRollsList(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
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
            var targetForm = new CartonStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox24_Click_1(object sender, EventArgs e)
        {
            var targetForm = new TakaStock(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox22_Click_1(object sender, EventArgs e)
        {
            var targetForm = new BeamReport(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox25_Click(object sender, EventArgs e)
        {
            var targetForm = new CalculateSalary(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void pictureBox26_Click(object sender, EventArgs e)
        {
            var targetForm = new TakaEntry(firm, logo);
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }

        private void date00_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                //setRollNo(rollCount - 1);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                if (dateTimePicker2.Value.AddDays(6).CompareTo(dateTimePicker1.Value) == 0)
                {
                    loading = true;
                    clearScreen();

                    if (!comboBox3.Text.Equals("Select"))
                    {
                        quality0.SelectedIndex = comboBox3.SelectedIndex - 1;
                    }

                    con.Open();
                    showAllRolls();
                    con.Close();
                    loading = false;
                }
                else
                {
                    MessageBox.Show("Please select date range of 1 week only");
                    checkBox1.Checked = false;
                }
            }
            else
            {
                loading = true;
                clearScreen();

                if (!comboBox3.Text.Equals("Select"))
                {
                    quality0.SelectedIndex = comboBox3.SelectedIndex - 1;
                }

                con.Open();
                showRolls();
                con.Close();
                loading = false;
            }
        }

        private void pictureBox9_Click(object sender, EventArgs e)
        {
            var targetForm = new Home();
            targetForm.MdiParent = ParentForm;
            targetForm.Show();
        }
    }
}
