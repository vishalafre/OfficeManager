using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class NewFirm : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public NewFirm()
        {
            InitializeComponent();
        }

        private void NewFirm_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            AcceptButton = button1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                pictureBox1.Load(openFileDialog1.FileName);
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            }
        }

        private Boolean validateFields()
        {
            int n;
            if(int.TryParse(pin.Text, out n)) {
                MessageBox.Show("Please enter valid PIN Code");
                return false;
            }

            Boolean isValid = true;
            isValid = (!cName.Text.Equals("") && !gstIn.Text.Equals("") && !cAddr.Text.Equals("") &&
                !phone.Text.Equals("") && !office.Text.Equals("") && !bName.Text.Equals("") &&
                !bAddr.Text.Equals("") && !ifsc.Text.Equals("") && !acNo.Text.Equals("") &&
                !email.Text.Equals("") && !openFileDialog1.FileName.Equals("") && !city.Text.Equals("") && !pin.Text.Equals(""));

            if(!isValid)
            {
                MessageBox.Show("Please enter all the fields!!!");
            }
            return isValid;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(!validateFields())
            {
                return;
            }
            con.Open();

            MemoryStream ms = new MemoryStream();
            pictureBox1.Image.Save(ms, ImageFormat.Png);
            byte[] photo_aray = new byte[ms.Length];
            ms.Position = 0;
            ms.Read(photo_aray, 0, photo_aray.Length);

            SqlCommand cmd = new SqlCommand("insert into company values(@NAME, " +
                "@GSTIN, @C_ADDRESS, @MOBILE, @OFFICE, " +
                "@BANK_NAME, @B_ADDRESS, @IFSC, " +
                "@AC_NO, @LOGO_IMG, @EMAIL, @CITY, @PIN)", con);
            cmd.Parameters.AddWithValue("@NAME", cName.Text);
            cmd.Parameters.AddWithValue("@GSTIN", gstIn.Text);
            cmd.Parameters.AddWithValue("@C_ADDRESS", cAddr.Text);
            cmd.Parameters.AddWithValue("@MOBILE", phone.Text);
            cmd.Parameters.AddWithValue("@OFFICE", office.Text);
            cmd.Parameters.AddWithValue("@BANK_NAME", bName.Text);
            cmd.Parameters.AddWithValue("@B_ADDRESS", bAddr.Text);
            cmd.Parameters.AddWithValue("@IFSC", ifsc.Text);
            cmd.Parameters.AddWithValue("@AC_NO", acNo.Text);
            cmd.Parameters.AddWithValue("@EMAIL", email.Text);
            cmd.Parameters.AddWithValue("@LOGO_IMG", photo_aray);
            cmd.Parameters.AddWithValue("@CITY", city.Text);
            cmd.Parameters.AddWithValue("@PIN", pin.Text);
            int i = cmd.ExecuteNonQuery();

            cmd = new SqlCommand("insert into TRANSPORT (FIRM, T_NAME) values(@FIRM, " +
                "@T_NAME, @LR_NO)", con);
            cmd.Parameters.AddWithValue("@FIRM", cName.Text);
            cmd.Parameters.AddWithValue("@T_NAME", "NA");
            i = cmd.ExecuteNonQuery();

            cmd = new SqlCommand("insert into AGENT (FIRM, A_NAME) values(@FIRM, " +
                "@T_NAME, @LR_NO)", con);
            cmd.Parameters.AddWithValue("@FIRM", cName.Text);
            cmd.Parameters.AddWithValue("@A_NAME", "NA");
            i = cmd.ExecuteNonQuery();

            con.Close();

            if (i != 0)
            {
                MessageBox.Show("Firm Created Successfully");

                var home = new Home();
                home.MdiParent = ParentForm;
                home.Show();
                Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var home = new Home();
            home.MdiParent = ParentForm;
            home.Show();
            Close();
        }
    }
}
