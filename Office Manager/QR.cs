using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class QR : Form
    {
        string signedInvoice = "";
        string billId;
        AddInvoice addInvoice;

        SqlConnection con = new SqlConnection("Data Source=(localdb)\\VISHAL;AttachDbFilename=|DataDirectory|\\Files\\DBQuery.mdf;Integrated Security=True");

        public QR(string signedInvoice, string billId, AddInvoice addInvoice)
        {
            InitializeComponent();
            this.signedInvoice = signedInvoice;
            this.billId = billId;
            this.addInvoice = addInvoice;
        }

        private void QR_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            signedInvoiceTb.Text = signedInvoice;

            if(string.IsNullOrEmpty(signedInvoice))
            {
                return;
            }
            int qrSizePixels;
            byte[] payloadBytes = Encoding.UTF8.GetBytes(signedInvoice);

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // ECCLevel.Q is good for long strings
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(payloadBytes, QRCodeGenerator.ECCLevel.Q);

                // Use the standard QRCode class for more rendering options
                using (QRCode qrCode = new QRCode(qrCodeData))
                {
                    // GetGraphic parameters: 
                    // pixelsPerModule (20), darkColor, lightColor, drawQuietZones (false)
                    using (Bitmap qrBitmap = qrCode.GetGraphic(2, Color.Black, Color.White, false))
                    {
                        qrSizePixels = (int)(qrBitmap.Width * 1);

                        using (Bitmap scaledBitmap = new Bitmap(qrSizePixels, qrSizePixels))
                        {
                            using (Graphics g = Graphics.FromImage(scaledBitmap))
                            {
                                // IMPORTANT: This prevents the image from getting blurry when scaling!
                                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                                g.PixelOffsetMode = PixelOffsetMode.Half;

                                // Draw the base image onto our new scaled bitmap
                                g.DrawImage(qrBitmap, 0, 0, qrSizePixels, qrSizePixels);
                            }

                            // 4. Save our newly scaled image to the byte array for NPOI
                            using (MemoryStream ms = new MemoryStream())
                            {
                                scaledBitmap.Save(ms, ImageFormat.Png);
                                pictureBox17.Image = Image.FromStream(ms);
                            }
                        }
                    }
                }
            }
        }

        private void signedInvoiceTb_TextChanged(object sender, EventArgs e)
        {

        }

        private void updateBtn_Click(object sender, EventArgs e)
        {
            con.Open();
            SqlCommand cmd = new SqlCommand("UPDATE BILL SET SIGNED_INVOICE = @SIGNED WHERE BILL_ID = @BILL_ID", con);
            cmd.Parameters.AddWithValue("@SIGNED", signedInvoiceTb.Text);
            cmd.Parameters.AddWithValue("@BILL_ID", billId);
            cmd.ExecuteNonQuery();

            con.Close();

            MessageBox.Show("QR Data updated successfully");
            addInvoice.UpdateQrBtn(signedInvoiceTb.Text);
            Close();
        }
    }
}
