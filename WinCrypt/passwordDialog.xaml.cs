using System.Windows;

namespace WinCrypt
{
    /// <summary>
    /// Interaction logic for passwordDialog.xaml
    /// </summary>
    public partial class passwordDialog : Window
    {
        public passwordDialog()
        {
            InitializeComponent();
        }

        public string Password
        {
            get { return passwordBox.Password; }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBox.Password == passwordConfirmBox.Password)
            {
                DialogResult = true;
                this.Close();
            }
            else
                MessageBox.Show("Passwords do not match!", "Error", MessageBoxButton.OK);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }
    }
}
