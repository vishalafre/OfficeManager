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
    public partial class TallyConfigure : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");
        string firm;

        public TallyConfigure(string firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void TallyConfigure_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            AcceptButton = button1;

            String query = "select * from TALLY_CONFIGURE where firm = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    textBox1.Text = oReader["OS_CLASS"].ToString();
                    textBox2.Text = oReader["OS_LEDGER"].ToString();
                    textBox3.Text = oReader["LS_LEDGER"].ToString();
                    textBox4.Text = oReader["LS_CLASS"].ToString();
                    textBox5.Text = oReader["CGST"].ToString();
                    textBox6.Text = oReader["SGST"].ToString();
                    textBox7.Text = oReader["IGST"].ToString();
                    textBox8.Text = oReader["ROUND_OFF"].ToString();
                }
            }

            con.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            con.Open();

            SqlCommand cmd = new SqlCommand("DELETE FROM TALLY_CONFIGURE WHERE FIRM = @FIRM", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.ExecuteNonQuery();

            cmd = new SqlCommand("INSERT INTO TALLY_CONFIGURE VALUES(@FIRM, @OS_CLASS, @OS_LEDGER, @OS_CLASS, @LS_LEDGER, @CGST, @SGST, @IGST, @ROUND_OFF)", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@OS_CLASS", textBox1.Text);
            cmd.Parameters.AddWithValue("@OS_LEDGER", textBox2.Text);
            cmd.Parameters.AddWithValue("@LS_CLASS", textBox4.Text);
            cmd.Parameters.AddWithValue("@LS_LEDGER", textBox3.Text);
            cmd.Parameters.AddWithValue("@CGST", textBox5.Text);
            cmd.Parameters.AddWithValue("@SGST", textBox6.Text);
            cmd.Parameters.AddWithValue("@IGST", textBox7.Text);
            cmd.Parameters.AddWithValue("@ROUND_OFF", textBox8.Text);
            cmd.ExecuteNonQuery();

            con.Close();

            MessageBox.Show("Configuration saved");
            Close();
        }
    }
}
