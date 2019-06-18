using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SafeFileTransferClient
{
    public partial class SelectName : Form
    {
        public SelectName()
        {
            InitializeComponent();
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (textBoxNickname.Text == "")
            {
                MessageBox.Show("Musisz podać nazwę użytkownika nim przejdziesz dalej" + Environment.NewLine,"Podaj nazwe użytkownika",MessageBoxButtons.OK,MessageBoxIcon.Information);
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
