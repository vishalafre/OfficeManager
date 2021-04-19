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
    public partial class SalaryFilter : Form
    {
        List<string> weavers = new List<string>();
        Dictionary<string, string> weaverIds = new Dictionary<string, string>();
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        string firm;
        SalaryReport sr;

        Dictionary<string, bool> waterMarkActive = new Dictionary<string, bool>();
        string filterCondition;
        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"};

        public SalaryFilter()
        {
            InitializeComponent();
        }

        public SalaryFilter(string firm, SalaryReport sr)
        {
            InitializeComponent();
            this.firm = firm;
            this.sr = sr;
        }

        private void SalaryFilter_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;

            filterCondition = "SS.FIRM = '"+ firm +"'";

            con.Open();
            String query = "select WID, W_NAME from WEAVER where firm = @FIRM order by W_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    weaverIds.Add(oReader["W_NAME"].ToString(), oReader["WID"].ToString());
                    weavers.Add(oReader["W_NAME"].ToString());
                }
            }
            con.Close();

            listBox1.DataSource = weavers;
            setTextboxWatermark(textBox2);
            setTextboxWatermark(textBox3);

            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                listBox1.SetSelected(i, true);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // Salary filter

            if(!fromSalary.Text.Equals(""))
            {
                filterCondition += " AND SS.PAYABLE_SALARY >= " + fromSalary.Text;
            }

            if (!toSalary.Text.Equals(""))
            {
                filterCondition += " AND SS.PAYABLE_SALARY <= " + toSalary.Text;
            }

            //Date filter

            if (!textBox3.Text.Equals("") && !textBox3.Text.Equals("dd-mm-yy"))
            {
                string date = textBox3.Text;
                int month = Int32.Parse(date.Split('-')[1].Split('-')[0]);
                string year = DateTime.Now.Year.ToString();
                string century = year.Substring(0, year.Length - 2);

                date = date.Replace("-"+ month +"-", "-" + months[month - 1] + "-"+ century);
                filterCondition += " AND SS.TO_DATE >= '" + date + "'";
            }

            if (!textBox2.Text.Equals("") && !textBox2.Text.Equals("dd-mm-yy"))
            {
                string date = textBox2.Text;
                int month = Int32.Parse(date.Split('-')[1].Split('-')[0]);
                string year = DateTime.Now.Year.ToString();
                string century = year.Substring(0, year.Length - 2);

                date = date.Replace("-" + month + "-", "-" + months[month - 1] + "-" + century);
                filterCondition += " AND SS.TO_DATE <= '" + date + "'";
            }

            // weaver filter

            string weavers = "(";

            foreach (object item in listBox1.SelectedItems)
            {
                weavers += "'" + weaverIds[item.ToString()] + "', ";
            }

            if(!weavers.Equals("("))
            {
                weavers = weavers.Substring(0, weavers.Length - 2) + ")";
                filterCondition += "AND SS.WEAVER IN " + weavers;
            }

            sr.updateReport(filterCondition);

            Close();
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
    }
}
