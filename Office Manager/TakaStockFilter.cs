using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class TakaStockFilter : Form
    {
        string firm;
        TakaStock ts;

        Dictionary<string, bool> waterMarkActive = new Dictionary<string, bool>();
        string filterCondition;
        string[] months = { "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };

        public TakaStockFilter(string firm, TakaStock ts)
        {
            InitializeComponent();
            this.firm = firm;
            this.ts = ts;
        }

        private void TakaStockFilter_Load(object sender, EventArgs e)
        {
            AcceptButton = button1;
            filterCondition = "SS.FIRM = '" + firm + "'";

            setTextboxWatermark(textBox2);
            setTextboxWatermark(textBox3);
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

        private void button1_Click(object sender, EventArgs e)
        {
            string firmFilter = "WHERE FIRM = '" + firm + "'";
            string filterCondition = "";
            //Date filter

            if (!textBox3.Text.Equals("") && !textBox3.Text.Equals("dd-mm-yy"))
            {
                string date = textBox3.Text;
                int day = Int32.Parse(date.Split('-')[0]);
                int month = Int32.Parse(date.Split('-')[1]);
                string yy = date.Split('-')[2];
                date = day + "-" + month + "-" + yy;

                string year = DateTime.Now.Year.ToString();
                string century = year.Substring(0, year.Length - 2);

                date = date.Replace("-" + month + "-", "-" + months[month - 1] + "-" + century);
                filterCondition += " AND TXN_DATE >= '" + date + "'";
            }

            if (!textBox2.Text.Equals("") && !textBox2.Text.Equals("dd-mm-yy"))
            {
                string date = textBox2.Text;
                int month = Int32.Parse(date.Split('-')[1].Split('-')[0]);
                string year = DateTime.Now.Year.ToString();
                string century = year.Substring(0, year.Length - 2);

                date = date.Replace("-" + month + "-", "-" + months[month - 1] + "-" + century);
                filterCondition += " AND TXN_DATE <= '" + date + "'";
            }

            ts.clearAndPopulate(firmFilter, filterCondition);
            Close();
        }
    }
}
