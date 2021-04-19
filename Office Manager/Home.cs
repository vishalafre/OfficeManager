using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using Office_Manager;

namespace Office_Manager
{
    public partial class Home : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public Home()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var newFirm = new NewFirm();
            newFirm.MdiParent = ParentForm;
            newFirm.Show();
            Close();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            int i = 0;

            string query = "Select * from company";
            SqlCommand oCmd = new SqlCommand(query, con);
            Boolean connected = false;

            if (!connected)
            {
                try
                {
                    con.Open();
                    connected = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    if(i == 0)
                    {
                        label1.Visible = true;
                    }
                    String firm = oReader["NAME"].ToString();
                    String path = oReader["LOGO_IMG_PATH"].ToString();

                    byte[] photo_aray = (byte[]) oReader["LOGO_IMG"];
                    MemoryStream ms = new MemoryStream(photo_aray);

                    var picture = new PictureBox
                    {
                        Name = "firmLogo" + i,
                        Size = new Size(200, 200),
                        Location = new Point(125 + 458*(i%3), 140 + 300*(i/3)),
                        Image = Image.FromStream(ms),
                        SizeMode = PictureBoxSizeMode.StretchImage,
                        BackColor = Color.White,
                    };

                    picture.Click += (s, evt) => 
                    {
                        var cHome = new CompanyHome(firm, photo_aray);
                        cHome.MdiParent = ParentForm;
                        cHome.Show();
                    };
                    Controls.Add(picture);

                    var label = new Label
                    {
                        Name = "firmLabel" + i,
                        Location = new Point(125 + 458 * (i % 3), 350 + 300 * (i / 3)),
                        Text = firm,
                        Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                        Size = new Size(200, 20)
                    };
                    Controls.Add(label);
                    i++;
                }
                con.Close();
            }

            string coneSuggestion = "Cone Supply Suggestor";

            var supplyLogo = new PictureBox
            {
                Name = "firmLogo" + i,
                Size = new Size(200, 200),
                Location = new Point(125 + 458 * (i % 3), 140 + 300 * (i / 3)),
                Image = new Bitmap(Properties.Resources.cone_supply_suggestion),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.White,
            };

            supplyLogo.Click += (s, evt) =>
            {
                var cHome = new ConeSuggestor();
                cHome.MdiParent = ParentForm;
                cHome.Show();
            };
            Controls.Add(supplyLogo);

            var text = new Label
            {
                Name = "firmLabel" + i,
                Location = new Point(125 + 458 * (i % 3), 350 + 300 * (i / 3)),
                Text = coneSuggestion,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold),
                Size = new Size(200, 20)
            };
            Controls.Add(text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_2(object sender, EventArgs e)
        {
            string query = "SELECT B1.BILL_ID, CONCAT(( select (CASE WHEN b.BILL_DT >= '09-SEP-17' then '11' ELSE '00' END) ITEM_AMT from bill b where b.bill_id = b1.bill_id) , ( SELECT CASE WHEN BILL_DT < '07-SEP-17' AND B.BILL_ID <> 'AA-069/17-18' then '00' ELSE '11' END GST FROM BILL B WHERE B.BILL_ID = B1.BILL_ID), (SELECT '1')) VALUE FROM BILL B1";
            SqlCommand oCmd = new SqlCommand(query, con);

            con.Open();

            SqlConnection con1 = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
            con1.Open();
            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                while (oReader.Read())
                {
                    SqlCommand cmd = new SqlCommand("UPDATE BILL SET ROUNDING_PREF = @ROUNDING_PREF WHERE BILL_ID = @BILL_ID", con1);
                    cmd.Parameters.AddWithValue("@ROUNDING_PREF", oReader["VALUE"].ToString());
                    cmd.Parameters.AddWithValue("@BILL_ID", oReader["BILL_ID"].ToString());
                    cmd.ExecuteNonQuery();
                }
            }

            con1.Close();
            con.Close();

            MessageBox.Show("Done");
        }
    }
}
