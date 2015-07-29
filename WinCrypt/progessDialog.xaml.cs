using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WinCrypt
{
    /// <summary>
    /// Interaction logic for progessWindow.xaml
    /// </summary>
    public partial class progessDialog : Window
    {
        public float progress
        {
            set
            {
                cryptProgressPB.Value = (double)value;
                cryptProgessText.Text = value + "%";
                if (value > 0)
                    cryptProgressPB.IsIndeterminate = false;
                else
                    cryptProgressPB.IsIndeterminate = true;
            }
            get
            {
                return 0f;
            }
        }

        public progessDialog()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
