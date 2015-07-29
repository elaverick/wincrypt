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
using System.IO;

namespace WinCrypt
{
    /// <summary>
    /// Interaction logic for fileSelectDialog.xaml
    /// </summary>
    public partial class fileSelectDialog : Window
    {
        private Microsoft.Win32.OpenFileDialog openFileDialog;
        private string _vaultFilenameValue, _ivFilenameValue = null;

        public string vaultFilenameValue
        {
            get { return _vaultFilenameValue; }
        }

        public string ivFilenameValue
        {
            get { return _ivFilenameValue; }
        }

        public fileSelectDialog()
        {
            InitializeComponent();
        }

        private void vaultFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog = new Microsoft.Win32.OpenFileDialog();
            
            Nullable<bool> result = openFileDialog.ShowDialog();
            if (result == true)
                vaultFileTextBox.Text = openFileDialog.FileName;
        }

        private void ivFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            openFileDialog = new Microsoft.Win32.OpenFileDialog();

            Nullable<bool> result = openFileDialog.ShowDialog();
            if (result == true)
                ivFileTextBox.Text = openFileDialog.FileName;
        }

        private bool checkFilename(string filename)
        {
            if (filename.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0 && !string.IsNullOrEmpty(filename))
            {
                return File.Exists(filename);
            }
            else
            {
                MessageBox.Show("Invalid filename", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkFilename(vaultFileTextBox.Text)&&checkFilename(ivFileTextBox.Text))
            {
                _vaultFilenameValue = vaultFileTextBox.Text;
                _ivFilenameValue = ivFileTextBox.Text;

                DialogResult = true;
                this.Close();
            }
        }
    }
}
