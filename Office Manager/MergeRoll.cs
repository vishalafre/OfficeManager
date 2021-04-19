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
    public partial class MergeRoll : Form
    {
        Boolean loading = true;
        RollEntry re;

        public MergeRoll(RollEntry re)
        {
            InitializeComponent();
            this.re = re;
        }

        private void dateTimePicker2_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                dateTimePicker1.Value = dateTimePicker2.Value.AddDays(6);
            }
        }

        private void MergeRoll_Load(object sender, EventArgs e)
        {
            CenterToScreen();
            loading = false;
        }

        private void despatch0_Click(object sender, EventArgs e)
        {
            re.changeDate(dateTimePicker2.Value);
            Close();
        }
    }
}
