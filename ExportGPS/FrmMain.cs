﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Windows.Forms;
using static ExportGPS.FilePreprocessing;

namespace ExportGPS {
    public partial class FrmMain : Form {
        private BackgroundWorker backgroundWorker = null;

        private static readonly DataTable datatable = new DataTable("Table_GPS");

        private string filePath;

        public FrmMain() {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e) {
            FolderBrowserDialog folder = new FolderBrowserDialog();
            if (folder.ShowDialog() == DialogResult.OK) {
                btnFolder.Enabled = false;
                filePath = folder.SelectedPath;
                backgroundWorker.RunWorkerAsync();
            }
        }

        public void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var diPath = new DirectoryInfo(filePath);
            var files = new List<FileSystemInfo>();
            files = GetFiles(diPath, files);
            int maximum = files.Count, progress = 0;
            foreach (var file in files) {
                progress += 1;
                Dictionary<string, string> dictResult = FileProcess(file);
                if (dictResult == null) {
                    continue;
                }
                int percentComplete = (int)((float)progress / (float)maximum * 100);
                backgroundWorker.ReportProgress(percentComplete, file.Name + ": " + dictResult["longitude"] + ", " + dictResult["latitude"] + ", " + dictResult["altitude"]);
                AddGps(dictResult);
            }
            SortTable();
            Export2Csv(filePath + "\\data.csv");
        }

        public void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            lblInfo.Text += e.UserState.ToString() + "\n";
            progressBar.Value = e.ProgressPercentage;
        }

        public void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            btnFolder.Enabled = true;
            lblInfo.Text = "";
            progressBar.Value = 0;
            MessageBox.Show("Work Finished!");
        }

        private static void AddGps(IReadOnlyDictionary<string, string> dictResult) {
            if (dictResult == null) {
                return;
            }
            var dataRow = datatable.NewRow();
            dataRow["longitude"] = dictResult["longitude"];
            dataRow["latitude"] = dictResult["latitude"];
            dataRow["timestamp"] = dictResult["timestamp"];
            dataRow["altitude"] = dictResult["altitude"];
            datatable.Rows.Add(dataRow);
        }

        private static void SortTable() {
            using (DataView dv = datatable.DefaultView) {
                dv.Sort = "timestamp ASC";
                dv.ToTable();
            }
        }

        private static List<FileSystemInfo> GetFiles(DirectoryInfo dirInfo, List<FileSystemInfo> fileList) {
            var fsInfos = dirInfo.GetFileSystemInfos();
            foreach (var fsInfo in fsInfos) {
                if (fsInfo is DirectoryInfo) {
                    GetFiles(new DirectoryInfo(fsInfo.FullName), fileList);
                } else {
                    var fileExt = fsInfo.Extension.ToLower();
                    if (PicExt.Contains(fileExt) || VidExt.Contains(fileExt)) {
                        fileList.Add(fsInfo);
                    }
                }
            }
            return fileList;
        }

        public static void Export2Csv(string filePath) {
            FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, new System.Text.UnicodeEncoding());
            for (int i = 0; i < datatable.Columns.Count; i++) {
                sw.Write(datatable.Columns[i].ColumnName);
                sw.Write(",");
            }
            sw.WriteLine("");
            for (int i = 0; i < datatable.Rows.Count; i++) {
                for (int j = 0; j < datatable.Columns.Count; j++) {
                    sw.Write(datatable.Rows[i][j].ToString());
                    if (j < datatable.Columns.Count - 1) {
                        sw.Write(",");
                    }
                }
                sw.WriteLine("");
            }
            sw.Flush();
            sw.Close();
        }

        private void FrmMian_Load(object sender, EventArgs e) {
            backgroundWorker = new BackgroundWorker {
                WorkerReportsProgress = true
            };
            backgroundWorker.DoWork += new DoWorkEventHandler(BackgroundWorker_DoWork);
            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler(BackgroundWorker_ProgressChanged);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundWorker_RunWorkerCompleted);

            datatable.Columns.Add("timestamp", typeof(string));
            datatable.Columns.Add("longitude", typeof(string));
            datatable.Columns.Add("latitude", typeof(string));
            datatable.Columns.Add("altitude", typeof(string));

            progressBar.Maximum = 100;
        }
    }
}
