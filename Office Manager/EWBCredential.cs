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
    public partial class EWBCredential : Form
    {
        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        string firm;
        public EWBCredential(string firm)
        {
            InitializeComponent();
            this.firm = firm;
        }

        private void EWBCredential_Load(object sender, EventArgs e)
        {
            CenterToScreen();

            String query = "SELECT EWB_USERNAME, EWB_PASSWORD FROM COMPANY WHERE NAME = @FIRM";
            SqlCommand oCmd = new SqlCommand(query, con);
            oCmd.Parameters.AddWithValue("@FIRM", firm);
            con.Open();

            using (SqlDataReader oReader = oCmd.ExecuteReader())
            {
                if (oReader.Read())
                {
                    username.Text = oReader["EWB_USERNAME"].ToString();
                    password.Text = oReader["EWB_PASSWORD"].ToString();
                }
            }

            con.Close();
        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            con.Open();

            // Delete FROM SUPPLY_CONE

            SqlCommand cmd = new SqlCommand("UPDATE COMPANY SET EWB_USERNAME = @USERNAME, EWB_PASSWORD = @PASSWORD WHERE NAME = @FIRM", con);
            cmd.Parameters.AddWithValue("@FIRM", firm);
            cmd.Parameters.AddWithValue("@USERNAME", username.Text);
            cmd.Parameters.AddWithValue("@PASSWORD", password.Text);
            cmd.ExecuteNonQuery();

            con.Close();

            MessageBox.Show("E-waybill credentials updated");
        }
    }
}
