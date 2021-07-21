using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace MultiBatchForBzip2
{
    public delegate void ChangingElementsStatusDelegate(bool stat1, bool stat2, String statusString, ProgressBarStyle progressBarStyle);
    public delegate void UpdatingStatusLabelDelegate(String statusString);

    public partial class Form1 : Form
    {
        private String bzip2String = "";
        private String parametersString = "";
        private ChangingElementsStatusDelegate changingElementsStatusDelegate = null;
        private UpdatingStatusLabelDelegate updatingStatusLabelDelegate = null;
        private Thread processThread = null;
        private bool isProcessThreadJoin = false;

        public Form1()
        {
            InitializeComponent();

            changingElementsStatusDelegate = new ChangingElementsStatusDelegate(ChangingElementsStatus);
            updatingStatusLabelDelegate = new UpdatingStatusLabelDelegate(UpdatingStatusLabel);
        }

        private void exitButton_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel.Text = "Stoping ...";
            if (processThread != null && processThread.IsAlive)
            {
                isProcessThreadJoin = true;
                processThread.Join();
            }
            Application.Exit();
        }

        private void CheckEmptyFields()
        {
            if (bzip2FileTextBox.Text.Equals(""))
            {
                bzip2FileTextBox.ForeColor = Color.Red;
                bzip2FileTextBox.Text = "Bzip2 executable file (bzip2.exe)";
            }
            if (rootFolderTextBox.Text.Equals(""))
            {
                rootFolderTextBox.ForeColor = Color.Red;
                rootFolderTextBox.Text = @"Root folder for compression, without space (C:\Files\FilesForCompression)";
            }
            if (patternTextBox.Text.Equals(""))
            {
                patternTextBox.ForeColor = Color.Red;
                patternTextBox.Text = "Pattern files for compression (*.*)";
            }
            if (parametersTextBox.Text.Equals(""))
            {
                parametersTextBox.ForeColor = SystemColors.GrayText;
                parametersTextBox.Text = "Parameters separate by space (-v -9)";
            }
        }

        private void StartProcess()
        {
            try
            {
                bzip2String = bzip2FileTextBox.Text.Trim();
                String rootFolderString = rootFolderTextBox.Text.Trim();
                parametersString = parametersTextBox.Text.Trim();
                if (parametersString.Equals("Parameters separate by space (-v -9)"))
                {
                    parametersString = "";
                }
                parametersString = String.Format("{0} {1}", parametersString, patternTextBox.Text.Trim()).Trim();

                if (!File.Exists(bzip2String) || !new FileInfo(bzip2String).Extension.Equals(".exe"))
                {
                    MessageBox.Show("Bzip2 file not found or has an incorrect format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else if (!Directory.Exists(rootFolderString))
                {
                    MessageBox.Show("Root folder for compression not found", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Process(rootFolderString);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                //Без условия - взаимная блокировка, при "processThread.Join()" в главном потоке
                if (!isProcessThreadJoin)
                {
                    this.Invoke(changingElementsStatusDelegate, new object[] { false, true, "Done", ProgressBarStyle.Blocks });
                }
            }
        }

        private void Process(String folderString)
        {
            this.Invoke(updatingStatusLabelDelegate, new object[] { folderString });

            Process process = new Process();
            process.StartInfo.FileName = bzip2String;
            process.StartInfo.WorkingDirectory = folderString;
            process.StartInfo.Arguments = parametersString;
            process.Start();
            process.WaitForExit();

            foreach (DirectoryInfo directoryInfo in new DirectoryInfo(folderString).GetDirectories())
            {
                if (Directory.Exists(directoryInfo.FullName))
                {
                    if (!isProcessThreadJoin)
                    {
                        Process(directoryInfo.FullName);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void ChangingElementsStatus(bool stat1, bool stat2, String statusString, ProgressBarStyle progressBarStyle)
        {
            bzip2FileTextBox.Enabled = stat2;
            rootFolderTextBox.Enabled = stat2;
            patternTextBox.Enabled = stat2;
            parametersTextBox.Enabled = stat2;
            resetButton.Enabled = stat2;
            startButton.Enabled = stat2;
            stopButton.Enabled = stat1;
            toolStripProgressBar.Style = progressBarStyle;
            toolStripStatusLabel.Text = statusString;
        }

        private void UpdatingStatusLabel(String statusString)
        {
            toolStripStatusLabel.Text = statusString;
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            bzip2FileTextBox.ForeColor = Color.Red;
            bzip2FileTextBox.Text = "Bzip2 executable file (bzip2.exe)";
            rootFolderTextBox.ForeColor = Color.Red;
            rootFolderTextBox.Text = @"Root folder for compression, without space (C:\Files\FilesForCompression)";
            patternTextBox.ForeColor = Color.Red;
            patternTextBox.Text = "Pattern files for compression (*.*)";
            parametersTextBox.ForeColor = SystemColors.GrayText;
            parametersTextBox.Text = "Parameters separate by space (-v -9)";
            toolStripProgressBar.Style = ProgressBarStyle.Blocks;
            toolStripStatusLabel.Text = "";
            startButton.Enabled = false;
            stopButton.Enabled = false;
        }

        private void bzip2FileTextBox_Click(object sender, EventArgs e)
        {
            CheckEmptyFields();
            if (bzip2FileTextBox.Text.Equals("Bzip2 executable file (bzip2.exe)"))
            {
                bzip2FileTextBox.ForeColor = SystemColors.WindowText;
                bzip2FileTextBox.Text = "";
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ".";
            openFileDialog.Filter = "*.exe|*.exe";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                bzip2FileTextBox.Text = openFileDialog.FileName;
            }
        }

        private void rootFolderTextBox_Click(object sender, EventArgs e)
        {
            CheckEmptyFields();
            if (rootFolderTextBox.Text.Equals(@"Root folder for compression, without space (C:\Files\FilesForCompression)"))
            {
                rootFolderTextBox.ForeColor = SystemColors.WindowText;
                rootFolderTextBox.Text = "";
            }
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                rootFolderTextBox.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void patternTextBox_Click(object sender, EventArgs e)
        {
            CheckEmptyFields();
            if (patternTextBox.Text.Equals("Pattern files for compression (*.*)"))
            {
                patternTextBox.ForeColor = SystemColors.WindowText;
                patternTextBox.Text = "";
            }
        }

        private void parametersTextBox_Click(object sender, EventArgs e)
        {
            CheckEmptyFields();
            if (parametersTextBox.Text.Equals("Parameters separate by space (-v -9)"))
            {
                parametersTextBox.ForeColor = SystemColors.WindowText;
                parametersTextBox.Text = "";
            }
        }

        private void ChangingStartButtonStatus(object sender, EventArgs e)
        {
            if (bzip2FileTextBox.Text.Equals("") || bzip2FileTextBox.Text.Equals("Bzip2 executable file (bzip2.exe)") ||
                rootFolderTextBox.Text.Equals("") || rootFolderTextBox.Text.Equals(@"Root folder for compression, without space (C:\Files\FilesForCompression)") ||
                patternTextBox.Text.Equals("") || patternTextBox.Text.Equals("Pattern files for compression (*.*)"))
            {
                startButton.Enabled = false;
            }
            else
            {
                startButton.Enabled = true;
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            processThread = new Thread(new ThreadStart(StartProcess));
            processThread.Start();
            ChangingElementsStatus(true, false, "Starting ...", ProgressBarStyle.Marquee);
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel.Text = "Stoping ...";
            if (processThread != null && processThread.IsAlive)
            {
                isProcessThreadJoin = true;
                processThread.Join();
            }
            ChangingElementsStatus(false, true, "Stoped", ProgressBarStyle.Blocks);
            isProcessThreadJoin = false;
        }
    }
}
