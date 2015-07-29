using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Collections.ObjectModel;
using Crypt;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;

namespace WinCrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ObservableCollection<fileDetails> files = new ObservableCollection<fileDetails>();
        private cryptography _cryptography;
        private encryptDialog _encryptDialog;
        private progessDialog _progessDialog;
        private fileSelectDialog _fileSelectDialog;
        private BackgroundWorker encryptWorker = new BackgroundWorker();
        private BackgroundWorker decryptWorker = new BackgroundWorker();
        private string currentVaultFile, currentIVFile = null, currentPassword=null;
        private List<string> selectedFiles = new List<string>();
        private Dictionary<string, string[]> arguments = new Dictionary<string, string[]>();

        public MainWindow()
        {
            InitializeComponent();
            fileListView.ItemsSource = files;
            _cryptography = new cryptography(false);
            encryptWorker.WorkerSupportsCancellation = true;
            encryptWorker.WorkerReportsProgress = true;
            encryptWorker.DoWork += new DoWorkEventHandler(encryptWorker_DoWork);
            encryptWorker.ProgressChanged += new ProgressChangedEventHandler(encryptWorker_ProgressChanged);
            encryptWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(encryptWorker_RunWorkerCompleted);
            decryptWorker.WorkerSupportsCancellation = true;
            decryptWorker.WorkerReportsProgress = true;
            decryptWorker.DoWork += new DoWorkEventHandler(decryptWorker_DoWork);
            decryptWorker.ProgressChanged += new ProgressChangedEventHandler(decryptWorker_ProgressChanged);
            decryptWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(decryptWorker_RunWorkerCompleted);

            parseCmdLineArgs();

            if (arguments.ContainsKey("altkeyexpansion"))
                _cryptography = new cryptography(false, true);
            else
                _cryptography = new cryptography(false);
        }

        private void parseCmdLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();

            string key = "";
            List<string> values = new List<string>();

            if (args.Length > 1)
            {
                for (int i = 1; i < args.Length; i++)
                    if (args[i][0] == '-')
                    {
                        if (values.Count > 0)
                            arguments.Add(key, values.ToArray());
                        key = args[i].TrimStart('-').ToLower();
                        values.Clear();
                    }
                    else
                        values.Add(args[i]);

                arguments.Add(key, values.ToArray());
            }
        }

        /// <summary>
        /// Add file button handler
        /// </summary>
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Multiselect = true;
            dlg.Filter = "All Files |*.*"; // Filter files by extension 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result == true)
            {
                foreach(string filename in dlg.FileNames)
                {
                    System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filename);
                    files.Add(new fileDetails(new FileInfo(filename),icon));
                }
                
            }
        }

        /// <summary>
        /// New Button Handler - Just reinitialise the display
        /// </summary>
        private void newButton_Click(object sender, RoutedEventArgs e)
        {
            files.Clear();
            addButton.IsEnabled = true;
            removeButton.IsEnabled = true;
            encryptButton.IsEnabled = true;
            decryptButton.IsEnabled = false;
        }

        /// <summary>
        /// Open File Button Handler
        /// </summary>
        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, long> fileTable = new Dictionary<string, long>();
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "All Files |*.*"; // Filter files by extension 

            currentVaultFile = null;
            currentIVFile = null;

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results 
            if (result != true)
                return;

            files.Clear();
           
            var _passwordDialog = new passwordDialog();

            result = _passwordDialog.ShowDialog();
            if (result != true)
                return;

            addButton.IsEnabled = false;
            removeButton.IsEnabled = false;
            encryptButton.IsEnabled = false;
            decryptButton.IsEnabled = true;

            try
            {
                fileTable = _cryptography.decryptFileTable(dlg.FileName, _passwordDialog.Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

            foreach (KeyValuePair<string, long> file in fileTable)
            {
                try
                {
                    //Spit out empty files to get the system to generate the icon - kind of ugly
                    string tempFilename = System.IO.Path.GetTempPath() + "extTemp" + System.IO.Path.GetExtension(file.Key);
                    FileStream temp = File.Create(tempFilename);
                    System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(tempFilename);

                    files.Add(new fileDetails(file.Key, file.Value, icon));
                    temp.Dispose();
                    File.Delete(tempFilename);
                }
                catch (Exception ex)
                {
                    files.Clear();
                    throw ex;
                }
            }
            currentVaultFile = dlg.FileName;
            currentPassword = _passwordDialog.Password;
        }

        /// <summary>
        /// Remove Button Handler - Removes an item from the file list
        /// </summary>
        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            fileDetails[] filesToRemove = new fileDetails[fileListView.SelectedItems.Count];
            fileListView.SelectedItems.CopyTo(filesToRemove, 0);
            foreach (fileDetails file in filesToRemove)
            {
                files.Remove(file);
            }
        }

        /// <summary>
        /// Encrypt button handler most work is done in the encrypt Worker
        /// </summary>
        private void encryptButton_Click(object sender, RoutedEventArgs e)
        {
            if (files.Count <= 0)
                return;
            List<string> fullnames = new List<string>();
            foreach (fileDetails file in files)
                fullnames.Add(file.fullname);

            _encryptDialog = new encryptDialog();
            if (_encryptDialog.ShowDialog()!=true)
                return;

            statusText.Text = "Key Stretching...";

            encryptWorker.RunWorkerAsync(fullnames);
            _progessDialog = new progessDialog();
            if (_progessDialog.ShowDialog() == false)
            {
                encryptWorker.CancelAsync();
                statusText.Text = "Ready...";
            }
        }

        /// <summary>
        /// Update the status
        /// </summary>
        void encryptWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _progessDialog.progress = e.ProgressPercentage;
            if(e.ProgressPercentage>0)
                statusText.Text = "Encrypting...";
        }

        /// <summary>
        /// Fire off the Encryption in a separate task 
        /// </summary>
        void encryptWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusText.Text = "Ready...";
            _progessDialog.Close();
        }

        void encryptWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Task encryptTask = Task.Factory.StartNew(() => _cryptography.encrypt((e.Argument as List<string>).ToArray(), _encryptDialog.password, _encryptDialog.internetIVSource, _encryptDialog.filenameValue,_encryptDialog.ivFilenameValue));
            while (!encryptTask.IsCompleted)
            {
                Thread.Sleep(500);
                encryptWorker.ReportProgress((int)_cryptography.encryptionProgress);
            }

            if (encryptTask.IsFaulted)
            {
                MessageBox.Show(encryptTask.Exception.InnerException.Message,"Error", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Progress updater
        /// </summary>
        void decryptWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _progessDialog.progress = e.ProgressPercentage;
            if(e.ProgressPercentage>0)
                statusText.Text = "Decrypting...";
        }

        /// <summary>
        /// Update the status
        /// </summary>
        void decryptWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statusText.Text = "Ready...";
            _progessDialog.Close();
        }

        /// <summary>
        /// Fire off the decryption in a separate task
        /// </summary>
        void decryptWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Task decryptTask = Task.Factory.StartNew(() => _cryptography.decrypt(currentVaultFile, currentPassword, currentIVFile, e.Argument as String+"\\",selectedFiles));
            while (!decryptTask.IsCompleted)
            {
                Thread.Sleep(500); //Hold for half a second to allow final completion
                decryptWorker.ReportProgress((int)_cryptography.encryptionProgress);
            }
            
            if (decryptTask.IsFaulted)
            {
                MessageBox.Show(decryptTask.Exception.InnerException.Message,"Error", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// Open a file where the Initialisation Vector is stored in a seperate file
        /// </summary>
        private void openSeparateIV_Click(object sender, RoutedEventArgs e)
        {
            currentVaultFile = null;
            currentIVFile = null;
            _fileSelectDialog = new fileSelectDialog();
            
            if (_fileSelectDialog.ShowDialog() != true)
                return;

            Dictionary<string, long> fileTable = new Dictionary<string, long>();

            files.Clear();

            var _passwordDialog = new passwordDialog();

            Nullable<bool> result = _passwordDialog.ShowDialog();
            if (result != true)
                return;

            addButton.IsEnabled = false;
            removeButton.IsEnabled = false;
            encryptButton.IsEnabled = false;
            decryptButton.IsEnabled = true;

            try
            {
                fileTable = _cryptography.decryptFileTable(_fileSelectDialog.vaultFilenameValue, _passwordDialog.Password, _fileSelectDialog.ivFilenameValue);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }

            foreach (KeyValuePair<string, long> file in fileTable)
            {
                try
                {
                    string tempFilename = System.IO.Path.GetTempPath() + "extTemp" + System.IO.Path.GetExtension(file.Key);
                    FileStream temp = File.Create(tempFilename);
                    System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(tempFilename);

                    files.Add(new fileDetails(file.Key, file.Value, icon));
                    temp.Dispose();
                    File.Delete(tempFilename);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            currentVaultFile = _fileSelectDialog.vaultFilenameValue;
            currentIVFile = _fileSelectDialog.ivFilenameValue;
            currentPassword = _passwordDialog.Password;
        }

        /// <summary>
        /// Get list of dropped files, iterate through and add them to the file list.
        /// </summary>
        private void fileListView_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] fileNames;
                fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string filename in fileNames)
                {
                    System.Drawing.Icon icon = System.Drawing.Icon.ExtractAssociatedIcon(filename);
                    files.Add(new fileDetails(new FileInfo(filename), icon));
                }
            }
        }

        /// <summary>
        /// Disable visual cues if a non-file is dragged in
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void fileListView_DragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void decryptButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.Description = "Select folder to decrypt into";
            folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (folderBrowserDialog.ShowDialog(this.GetIWin32Window()) != System.Windows.Forms.DialogResult.OK)
                return;

            if (fileListView.SelectedItems.Count > 0)
                foreach (fileDetails fd in fileListView.SelectedItems)
                    if (File.Exists(folderBrowserDialog.SelectedPath + "\\" + fd.filename))
                    {
                        MessageBox.Show(fd.filename + " already exists in " + folderBrowserDialog.SelectedPath + ".", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

            statusText.Text = "Key Stretching...";

            selectedFiles.Clear();

            if (fileListView.SelectedItems.Count > 0)
                foreach (fileDetails fd in fileListView.SelectedItems)
                    selectedFiles.Add(fd.filename);
            else
                foreach (fileDetails fd in files)
                    selectedFiles.Add(fd.filename);

            decryptWorker.RunWorkerAsync(folderBrowserDialog.SelectedPath);
            _progessDialog = new progessDialog();
            if (_progessDialog.ShowDialog() == false)
            {
                encryptWorker.CancelAsync();
                statusText.Text = "Ready...";
            }
        }

        /// <summary>
        /// Deselect files if a user clicks on a non-list item
        /// </summary>
        private void fileListView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            fileListView.UnselectAll();
        }
    }

}
