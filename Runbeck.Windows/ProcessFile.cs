using log4net;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Runbeck.Windows
{
    /// <summary>
    /// DJT : 2/22/2020
    /// This application will process a delimited text file and output results to a text file. 
    /// </summary>
    public partial class ProcessFile : Form
    {

        private readonly string _badRecordFileName = ConfigurationManager.AppSettings.Get("BadFileName");
        private readonly string _correctRecordFileName = ConfigurationManager.AppSettings.Get("CorrectFileName");

        /// <summary>
        /// Add logging for the application. the log files are dumped into a folder named "log" relative to the bin folder.
        /// Folder Name: Runbecklogs
        /// File Name : RunbeckApplicationLog.log
        /// </summary>
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);// LogHelper.GetLogger();

        public ProcessFile()
        {
            FileInfo fi = new FileInfo("log4net.xml");
            log4net.Config.XmlConfigurator.Configure(fi);
            log4net.GlobalContext.Properties["host"] = Environment.MachineName;
            InitializeComponent();
        }


        /// <summary>
        /// Process the file selected by user. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonProcessFile_Click(object sender, EventArgs e)
        {
            if (openFileDialog.FileName != string.Empty && numericUpDownFieldToDisplay.Minimum > 0)
            {
                string directoryPath = Path.GetDirectoryName(openFileDialog.FileName);
                string fileTypeSelected = string.Empty;
                if (radioButtonCSV.Checked)
                {
                    fileTypeSelected = ",";
                }
                else
                {
                    fileTypeSelected = "\t";
                }
                //ClearFiles(directoryPath);
                ProcessDataFile(openFileDialog.FileName, fileTypeSelected, numericUpDownFieldToDisplay.Value, directoryPath);
            }
        }


        /// <summary>
        /// Opens the file dialog box. Method will get the file extensions for the allowed file types.
        /// It will also get the count of the records in the file to set the maximum file count for the fields to be displayed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            log.Info("file started");
            try
            {
                //Only allow "csv" or "tsv" files to be selected per requirements. 
                var fileExtension = radioButtonCSV.Checked ? "csv files (*.csv)|*.csv" : "tsv files (*.txt)|*.txt";
                openFileDialog.Filter = fileExtension;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {

                    FileInfo file = new FileInfo(openFileDialog.FileName);

                    //Get record count in text file
                    var recordCount = GetRecordCount(file);

                    txtFileSelected.Text = openFileDialog.SafeFileName;

                    //Limit the number of fields to be displayed.
                    if (recordCount > 0)
                    {
                        numericUpDownFieldToDisplay.Minimum = 1;
                        numericUpDownFieldToDisplay.Maximum = recordCount;
                    }
                    else
                    {
                        numericUpDownFieldToDisplay.Minimum = 0;
                        numericUpDownFieldToDisplay.Maximum = 0;
                        ShowDialog("No Data In File!", Color.FromArgb(255, 187, 51));
                    }
                }
                else
                {
                    ShowDialog("No File Selected", Color.FromArgb(255, 187, 51));
                }


                //verify that a file has been selected and that there is data in the file before enabling the button.
                EnableDisableProcessButton();
            }
            catch (Exception ex)
            {
                log.Error("btnSelectFile_Click" + ex.Message);
                ShowDialog("Error Selecting File!", Color.Red);
            }
        }

        /// <summary>
        /// Close the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are You Sure To Exit ?", "Exit", MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                Application.Exit();
            }
        }


        /// <summary>
        /// Process the file selected.
        /// </summary>
        /// <param name="filePath">The file path of the selected file from the Dialog Box.</param>
        /// <param name="fileType">The type of file to be processed. Currently only CSV and TSV file allowed.</param>
        /// <param name="columnsToDisplay">The column count to be displayed in the out</param>
        /// <param name="saveProcessDataFileLocation">The file location where the files will be created.</param>
        public void ProcessDataFile(string filePath, string fileType, decimal columnsToDisplay, string saveProcessDataFileLocation)
        {
            //this will all the process to continue past the header info. Requirements state that if no
            //header information is present, to stop processing. 
            bool startedProcessing = false;
            var textReader = new TextFieldParser(filePath);
            using (textReader)
            {
                textReader.TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited;
                textReader.Delimiters = new String[] { fileType };

                List<string> currentRow = new List<string>();
                List<string> correctNumberOfFields = new List<string>();
                List<string> incorrectNumberOfFields = new List<string>();
                //loop through text file.
                while (!textReader.EndOfData)
                {
                    try
                    {
                        //Get the current row of data in file.
                        currentRow = textReader.ReadFields().ToList();

                        //Skip header row but make sure the header is presnet in file per requirement. 
                        if (!currentRow.Contains("First Name") && !startedProcessing)
                        {
                            log.Info("File Does Not Include Header Record!");
                            ShowDialog("File Does Not Include Header Record!", Color.Red);
                            return;
                        }
                        else
                        {
                            startedProcessing = true;
                            string recordToWriteToNewFile = string.Empty;

                            //This will get the count of the fields in the record and compare if they match what the user has selected.
                            if (currentRow.Count >= (int)columnsToDisplay)
                            {
                                recordToWriteToNewFile = String.Join(fileType, currentRow.Select(x => x.ToString()).ToArray(), 0, (int)columnsToDisplay);
                                correctNumberOfFields.Add(recordToWriteToNewFile);
                            }
                            else
                            {
                                recordToWriteToNewFile = String.Join(fileType, currentRow.Select(x => x.ToString()).ToArray());
                                incorrectNumberOfFields.Add(recordToWriteToNewFile);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("ProcessDataFile : " + ex.Message);
                        ShowDialog("Process File Failed!", Color.Red);
                    }
                    //Write out records to file.
                    WriteRecord(correctNumberOfFields, saveProcessDataFileLocation);
                    WriteBadRecord(incorrectNumberOfFields, saveProcessDataFileLocation);
                }
            }
            ShowDialog("Process File Successfully Completed", Color.Green);
        }


        /// <summary>
        /// Write out bad records. Bad records are records that does not meet the amount of fields to display per user
        /// </summary>
        /// <param name="incorrectNumberOfFields">A list of users that does not have the minmum amount of fields requested by user.</param>
        /// <param name="saveProcessDataFileLocation">Location to where the file will be saved.</param>
        private void WriteBadRecord(List<string> incorrectNumberOfFields, string saveProcessDataFileLocation)
        {
            using (var w = new StreamWriter(saveProcessDataFileLocation + "\\" + _badRecordFileName))
            {
                foreach (var item in incorrectNumberOfFields)
                {
                    w.WriteLine(item);
                }
            }
        }

        /// <summary>
        /// Write out records to file. Records that meet the amount of fields to display per user.
        /// </summary>
        /// <param name="incorrectNumberOfFields">A list of users that have the minmum amount of fields requested by user.</param>
        /// <param name="saveProcessDataFileLocation">Location to where the file will be saved.</param>
        private void WriteRecord(List<string> correctNumberOfFields, string saveProcessDataFileLocation)
        {
            using (var w = new StreamWriter(saveProcessDataFileLocation + "\\" + _correctRecordFileName))
            {
                foreach (var item in correctNumberOfFields)
                {
                    w.WriteLine(item);
                }
            }
        }

        private void ClearFiles(string saveProcessDataFileLocation)
        {
            File.WriteAllText(saveProcessDataFileLocation + "\\" + _correctRecordFileName, String.Empty);
            File.WriteAllText(saveProcessDataFileLocation + "\\" + _badRecordFileName, String.Empty);
        }


        /// <summary>
        /// Gets the record count. The count will be what we use to set the maximum value for the number of fields to display.
        /// </summary>
        /// <param name="file">Selected File.</param>
        /// <returns>Count of records.</returns>
        public long GetRecordCount(FileInfo file)
    => File.ReadLines(file.FullName).Count();

        /// <summary>
        /// Disables or enables the process file button. 
        /// </summary>
        private void EnableDisableProcessButton()
        {
            btnProcessFile.Enabled = false;
            if (openFileDialog.FileName != string.Empty && numericUpDownFieldToDisplay.Minimum > 0)
            {
                btnProcessFile.Enabled = true;
            }
        }

        /// <summary>
        /// Gives a notification to the user of the application. 
        /// </summary>
        /// <param name="message">Friendly message to display to the user.</param>
        /// <param name="bgColor">Color of the popup.</param>
        private void ShowDialog(string message, Color bgColor)
        {
            frmNotificationDialog dialog = new frmNotificationDialog(message, bgColor);
            dialog.Show();
        }
    }
}