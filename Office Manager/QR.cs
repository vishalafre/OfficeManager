using QRCoder;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Office_Manager
{
    public partial class QR : Form
    {
        string signedInvoice = "";
        public QR(string signedInvoice)
        {
            InitializeComponent();
            this.signedInvoice = signedInvoice;
        }

        private void QR_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            itemLbl.Text = signedInvoice;

            int qrSizePixels;

            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                // ECCLevel.Q is good for long strings
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(signedInvoice, QRCodeGenerator.ECCLevel.Q);

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
    }
}
