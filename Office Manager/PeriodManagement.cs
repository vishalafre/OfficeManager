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
    public partial class PeriodManagement : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        Dictionary<string, string> godowns = new Dictionary<string, string>();
        string firm;
        int rowCount = 1;
        Boolean loading = true;

        public PeriodManagement(string firm)
        {
            this.firm = firm;
            InitializeComponent();
        }

        private void PeriodManagement_Load(object sender, EventArgs e)
        {
            CenterToScreen();

            con.Open();
            // set quality

            String query = "select GID, G_NAME from GODOWN where firm = @FIRM order by G_NAME";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    godowns.Add(oReader["GID"].ToString(), oReader["G_NAME"].ToString());
                }
            }

            if (godowns.Count() > 0)
            {
                comboBox2.DataSource = new BindingSource(godowns, null);
                comboBox2.DisplayMember = "Value";
                comboBox2.ValueMember = "Key";
            }

            fetchPeriods();
            con.Close();

            int year = DateTime.Now.Year;
            if(DateTime.Now.Month >= 1 && DateTime.Now.Month <=3)
            {
                year--;
            }

            fromDt0.Value = new DateTime(year, 4, 1, 0, 0, 0);
            toDt0.Value = fromDt0.Value.AddYears(1).AddDays(-1);

            loading = false;
        }

        private void fetchPeriods()
        {
            string query = "select * FROM BEAM_PERIOD WHERE FIRM = @FIRM AND GODOWN = @GODOWN";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            oCmd.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);

            int count = 0;
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(count == 0)
                    {
                        fromDt0.Value = (DateTime)oReader["FROM_DT"];
                        toDt0.Value = (DateTime)oReader["TO_DT"];
                    }
                    else
                    {
                        addRow(count);
                        DateTimePicker fromDtp = (DateTimePicker)Controls.Find("fromDt" + count, true)[0];
                        DateTimePicker toDtp = (DateTimePicker)Controls.Find("toDt" + count, true)[0];

                        fromDtp.Value = (DateTime)oReader["FROM_DT"];
                        toDtp.Value = (DateTime)oReader["TO_DT"];
                    }
                    count++;
                }
            }

            label10.Text = count + " entries";
        }

        private void add0_Click(object sender, EventArgs e)
        {
            addRow(rowCount);
        }

        private void addRow(int index)
        {
            var from = new DateTimePicker()
            {
                Name = "fromDt" + index,
                Location = new Point(fromDt0.Location.X, fromDt0.Location.Y + 25 * index),
                Size = fromDt0.Size
            };
            from.Value = ((DateTimePicker) Controls.Find("toDt" + (index - 1),true)[0]).Value.AddDays(1);

            var to = new DateTimePicker()
            {
                Name = "toDt" + index,
                Location = new Point(toDt0.Location.X, toDt0.Location.Y + 25 * index),
                Size = toDt0.Size
            };
            to.Value = from.Value.AddYears(1).AddDays(-1);

            var t = new Label()
            {
                Name = "toLbl" + index,
                Location = new Point(toLbl0.Location.X, toLbl0.Location.Y + 25 * index),
                Size = toLbl0.Size,
                Text = toLbl0.Text,
                Font = toLbl0.Font
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

            rowCount++;

            add.Click += (s, evt) =>
            {
                addRow(rowCount);
            };

            del.Click += (s, evt) =>
            {
                delRow((PictureBox)s);
            };

            Controls.Add(from);
            Controls.Add(to);
            Controls.Add(t);
            Controls.Add(add);
            Controls.Add(del);
        }

        private void delRow(PictureBox del)
        {
            copyCellsForDelete(Int32.Parse(del.Name.Replace("del", "")));
            
            Controls.Remove(Controls.Find("fromDt" + (rowCount - 1), true)[0]);
            Controls.Remove(Controls.Find("toDt" + (rowCount - 1), true)[0]);
            Controls.Remove(Controls.Find("toLbl" + (rowCount - 1), true)[0]);
            Controls.Remove(Controls.Find("add" + (rowCount - 1), true)[0]);
            Controls.Remove(Controls.Find("del" + (rowCount - 1), true)[0]);

            rowCount--;
        }

        private void copyCellsForDelete(int index)
        {
            for (int i = index; i < (rowCount - 1); i++)
            {
                DateTimePicker from = (DateTimePicker)(Controls.Find("fromDt" + i, true)[0]);
                DateTimePicker to = (DateTimePicker)(Controls.Find("toDt" + i, true)[0]);

                DateTimePicker fromPrev = (DateTimePicker)(Controls.Find("fromDt" + (i + 1), true)[0]);
                DateTimePicker toPrev = (DateTimePicker)(Controls.Find("toDt" + (i + 1), true)[0]);

                from.Value = fromPrev.Value;
                to.Value = toPrev.Value;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd2 = new SqlCommand("DELETE FROM BEAM_PERIOD WHERE FIRM = @FIRM AND GODOWN = @GODOWN", con);
            cmd2.Parameters.AddWithValue("@FIRM", firm);
            cmd2.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
            cmd2.ExecuteNonQuery();

            for(int i=0; i<rowCount; i++)
            {
                DateTimePicker from = (DateTimePicker)Controls.Find("fromDt" + i, true)[0];
                DateTimePicker to = (DateTimePicker)Controls.Find("toDt" + i, true)[0];

                cmd2 = new SqlCommand("INSERT INTO BEAM_PERIOD (FIRM, GODOWN, FROM_DT, TO_DT) VALUES (@FIRM, @GODOWN, @FROM_DT, @TO_DT)", con);
                cmd2.Parameters.AddWithValue("@FIRM", firm);
                cmd2.Parameters.AddWithValue("@GODOWN", ((KeyValuePair<string, string>)comboBox2.SelectedItem).Key);
                cmd2.Parameters.AddWithValue("@FROM_DT", from.Value.ToString("dd-MMM-yyyy"));
                cmd2.Parameters.AddWithValue("@TO_DT", to.Value.ToString("dd-MMM-yyyy"));
                cmd2.ExecuteNonQuery();
            }

            MessageBox.Show("Period(s) saved");
            con.Close();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!loading)
            {
                int x = rowCount;
                for(int i=1; i<x; i++)
                {
                    PictureBox delPb = (PictureBox)Controls.Find("del1", true)[0];
                    delRow(delPb);
                }

                con.Open();
                fetchPeriods();
                con.Close();
            }
        }
    }
}
