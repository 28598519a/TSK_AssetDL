using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace TSK_AssetDL
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void btn_catalog_Click(object sender, RoutedEventArgs e)
        {
            btn_catalog.IsEnabled = false;
            List<Task> tasks = new List<Task>
            {
                DownLoadFile(App.ResUrl, Path.Combine(App.Root, "catalog.json"), true)
            };
            await Task.WhenAll(tasks);
            tasks.Clear();

            if (App.glocount > 0)
                System.Windows.MessageBox.Show($"Download catalog finish", "Finish");
            else
                System.Windows.MessageBox.Show($"Download catalog fail", "Fail");

            App.glocount = 0;
            btn_catalog.IsEnabled = true;
        }

        private async void btn_download_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "catalog.json|*.json";
            if (!openFileDialog.ShowDialog() == true)
                return;

            string catalog = File.ReadAllText(openFileDialog.FileName).Split("], \"m_KeyDataString\"")[0];

            // current: 1957
            int bundle_id = Int32.Parse(Regex.Match(Regex.Matches(catalog, ",\"[0-9]+#/").Cast<Match>().Select(m => m.Value).ToArray().AsQueryable().Last(), "[0-9]+").Value);
            string[] AssetList = catalog.Split($"\",\"{bundle_id}#/").Skip(1).ToArray();

            App.TotalCount = AssetList.Length;

            if (App.TotalCount > 0)
            {
                App.Respath = Path.Combine(App.Root, "Asset");
                if (!Directory.Exists(App.Respath))
                    Directory.CreateDirectory(App.Respath);

                int count = 0;
                List<Task> tasks = new List<Task>();

                foreach (string asset in AssetList)
                {
                    string url = App.ServerURL + asset;
                    string path = Path.Combine(App.Respath, asset);

                    tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                    count++;

                    // 阻塞線程，等待現有工作完成再給新工作
                    if ((count % pool).Equals(0) || App.TotalCount == count)
                    {
                        // await is better than Task.Wait()
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                    }

                    // 用await將線程讓給UI更新
                    lb_counter.Content = $"進度 : {count} / {App.TotalCount}";
                    await Task.Delay(1);
                }

                if (cb_Debug.IsChecked == true && App.log.Count > 0)
                {
                    using (StreamWriter outputFile = new StreamWriter("404.log", false))
                    {
                        foreach (string s in App.log)
                            outputFile.WriteLine(s);
                    }
                }

                string failmsg = String.Empty;
                if (App.TotalCount - App.glocount > 0)
                    failmsg = $"，{App.TotalCount - App.glocount}個檔案失敗";

                System.Windows.MessageBox.Show($"下載完成，共{App.glocount}個檔案{failmsg}", "Finish");
                lb_counter.Content = String.Empty;
            }
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            
            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Don't use DownloadFileTaskAsync, if 404 it will create a empty file, use DownloadDataTaskAsync instead.
                    byte[] data = await wc.DownloadDataTaskAsync(downPath);
                    File.WriteAllBytes(savePath, data);
                    
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
                
            }
            return Task.FromResult(0);
        }
    }
}
