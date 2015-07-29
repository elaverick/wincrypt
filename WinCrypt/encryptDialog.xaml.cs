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
using Microsoft.Win32;
using Crypt;
using System.IO;

namespace WinCrypt
{
    /// <summary>
    /// Interaction logic for encryptDialog.xaml
    /// </summary>
    public partial class encryptDialog : Window
    {
        private SaveFileDialog saveFileDialog;
        private cryptography _cryptography;
        private passwordDialog _passwordDialog;

        private string randomString;

        private string _filenameValue,_ivFilenameValue=null;
        private bool _internetIVSource = false;

        public encryptDialog()
        {
            InitializeComponent();
            _cryptography = new cryptography(false);
            randomString = _cryptography.randomString(16);
        }

        public bool separateIVValue
        {
            get { return separateIV.IsChecked.Value; }
        }

        public string password
        {
            get { return _passwordDialog.Password; }
        }

        public bool internetIVSource
        {
            get { return _internetIVSource; }
        }

        public string filenameValue
        {
            get { return _filenameValue; }
        }

        public string ivFilenameValue
        {
            get { return _ivFilenameValue; }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void fileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = randomString + ".dat";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
                filenameTextBox.Text = saveFileDialog.FileName;
        }

        private void ivFileLocationButton_Click(object sender, RoutedEventArgs e)
        {
            saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.FileName = randomString + ".iv";
            Nullable<bool> result = saveFileDialog.ShowDialog();
            if (result == true)
                ivFilenameTextBox.Text = saveFileDialog.FileName;
        }

        private bool checkFilename(string filename)
        {
            if (filename.IndexOfAny(System.IO.Path.GetInvalidPathChars()) < 0 && !string.IsNullOrEmpty(filename))
            {
                if (File.Exists(filename))
                    if (MessageBox.Show("File already exists!\nDo you want to replace it?", "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                        return false;

                try
                {
                    File.Create(filenameTextBox.Text).Dispose();
                }
                catch (PathTooLongException)
                {
                    MessageBox.Show("File Path is too long", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Directory not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (IOException ioex)
                {
                    MessageBox.Show(ioex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (Exception genex)
                {
                    MessageBox.Show(genex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                try
                {
                    File.Delete(filename);
                }
                catch (PathTooLongException)
                {
                    MessageBox.Show("File Path is too long", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Directory not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (IOException ioex)
                {
                    MessageBox.Show(ioex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                catch (Exception genex)
                {
                    MessageBox.Show(genex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                return true;
            }
            else
            {
                MessageBox.Show("Invalid filename", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (checkFilename(filenameTextBox.Text))
            {
                if (separateIV.IsChecked == true)
                {
                    if (!checkFilename(ivFilenameTextBox.Text))
                        return;
                }
                _passwordDialog = new passwordDialog();
                if (_passwordDialog.ShowDialog() != true)
                    return;

                _filenameValue = filenameTextBox.Text;
                if (separateIV.IsChecked == true)
                    _ivFilenameValue = ivFilenameTextBox.Text;
                else
                    _ivFilenameValue = null;

                _internetIVSource = internetIV.IsChecked.Value;

                DialogResult = true;
                this.Close();
            }
            else
                return;

        }
    }
}
