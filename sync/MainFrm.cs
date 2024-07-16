using System.Configuration;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace sync
{
    public partial class MainFrm : Form
    {
        FileSystemWatcher watcher ;
        OssHelper ossHelper;
        private HashSet<string> uploadedFilesMd5;
        private readonly string DataFilePath = DateTime.Now.ToString("yyyy-MM-dd")+ "_data.json";
        string filter = ConfigurationManager.AppSettings["filter"];
        string bucketName = ConfigurationManager.AppSettings["bucketName"];
        bool start = false;
        int uploadCount = 0;
        int dupCount = 0;
        int totalCount = 0;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem restoreMenuItem;
        private ToolStripMenuItem exitMenuItem;

        public MainFrm()
        {
            InitializeComponent();

            this.button2.Enabled=false;
            this.label2.Text="";

            this.timer1.Interval = 60 * 60 * 60;
            this.timer1.Tick +=OnAutoCheck;
            this.timer1.Start();
            ossHelper = new OssHelper("xxx", "xx", "xx", bucketName);
            uploadedFilesMd5 = new HashSet<string>();
            LoadUploadedFilesMd5();

            notifyIcon1.Icon = SystemIcons.Information;
            notifyIcon1.Text = "同步助手后台运行中";
            notifyIcon1.Visible = false; // 初始状态不可见

            contextMenu = new ContextMenuStrip();
            restoreMenuItem = new ToolStripMenuItem("恢复");
            exitMenuItem = new ToolStripMenuItem("退出");

            contextMenu.Items.AddRange(new ToolStripItem[] { restoreMenuItem, exitMenuItem });
            notifyIcon1.ContextMenuStrip = contextMenu;

            restoreMenuItem.Click += RestoreMenuItem_Click;
            exitMenuItem.Click += ExitMenuItem_Click;



        }

        private async void OnAutoCheck(object? sender, EventArgs e)
        {

            AddLog("自动检查已开始");
            if (!start)
            {
                AddLog("同步未启动");
            }
            await FindAndUpdateLoad(this.textBox1.Text);
            SaveUploadedFilesMd5();
            AddLog("自动检查已结束");

        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox1.Text)||string.IsNullOrEmpty(this.txtOssPath.Text))
            {
                MessageBox.Show("上传路径或者oss路径为空");
                return;
            }
            AddLog("启动成功");
            this.btnStart.Enabled=false;
            this.btnStart.Text="同步中..";
            this.button2.Enabled=true;
            this.txtOssPath.Enabled=false;
            start=true;

            await FindAndUpdateLoad(this.textBox1.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
                watcher = new FileSystemWatcher();
                watcher.Path=this.textBox1.Text;

                watcher.NotifyFilter = NotifyFilters.LastAccess
                                    | NotifyFilters.LastWrite
                                      | NotifyFilters.FileName
                                      | NotifyFilters.DirectoryName;
                watcher.Filter ="*.*";
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                watcher.Created += OnChanged;
            }
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
           
            AddLog("发现新文件：" + e.FullPath);
            if (!start)
            {
                AddLog("服务未启动" );
                return;
            }
            for (int i = 0; i < 10; i++)
            {
                var fileInfo = new FileInfo(e.FullPath);
                if (fileInfo.Exists)
                {
                    break;
                }
                Thread.Sleep(500);
            }
            string[] extArr = getExtArr();
            var matchArr = extArr.Where(x => e.FullPath.EndsWith(x.Replace("*", ""))).ToArray();
            if (matchArr.Length==0)
            {
                AddLog("文件类型不符合");
                return;
            }
            totalCount++;
            setProgress(uploadCount, dupCount, totalCount);
            await UploadFile(e.FullPath);
        }

         


        private async Task FindAndUpdateLoad(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                AddLog("目录不合法");
                return;
            }
            string[] fileList = getExtArr().SelectMany(ext => Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories)).ToArray();
            if (fileList.Length == 0)
            {
                AddLog("文件列表为空");
                return;
            }
            totalCount=fileList.Length;
            setProgress(uploadCount,dupCount, totalCount);
            AddLog($"发现{fileList.Length}个文件");
            var semaphore = new SemaphoreSlim(5); // 信号量
            var tasks = new List<Task>();
            foreach (string file in fileList)
            {
                if (!start)
                {
                    AddLog("服务已停止");
                    return;
                }
                await semaphore.WaitAsync(); // 等待信号量

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                       await  UploadFile(file);
                    }
                    finally
                    {
                        semaphore.Release(); // 释放信号量
                    }
                }));
                await Task.WhenAll(tasks); // 等待所有任务完成

            }
        }

        private string[] getExtArr()
        {
            return filter.Split("|");
        }


        // 定义一个方法，用于安全地在跨线程情况下将日志消息添加到ListBox中
        private void AddLog(string log)
        {
            log =$"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}:{log}";
            if (listLog.InvokeRequired)
            {
                // 如果需要跨线程调用
                listLog.Invoke(new Action<string>(AddLog), log);
            }
            else
            {
                listLog.Items.Add(log);

                // 如果日志超过100条，删除最早的一条
                if (listLog.Items.Count > 100)
                {
                    listLog.Items.RemoveAt(0);
                }
                // 保持滚动条在最底部
                listLog.TopIndex = listLog.Items.Count - 1;
            }
        }

        private string CalculateMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        // 判断文件是否已上传
        public bool IsFileUploaded(string filePath)
        {
            string fileMd5 = CalculateMd5(filePath);
            return uploadedFilesMd5.Contains(fileMd5);
        }

        // 上传文件
        public Task UploadFile(string filePath)
        {
            try
            {
                if (IsFileUploaded(filePath))
                {
                    AddLog($"File '{filePath}' has already been uploaded.");
                    dupCount++;
                    return Task.CompletedTask;
                }
                // 模拟文件上传过程
                AddLog($"Uploading file '{filePath}'...");
                var fileName = new FileInfo(filePath).Name;
               var ossPath= txtOssPath.Text.Replace($"oss://{bucketName}/", "");
                if (!ossPath.EndsWith("/"))
                {
                    ossPath += "/";
                }
                string result = ossHelper.UploadFile(filePath, ossPath+fileName);
                // 上传成功后，将文件的 MD5 值添加到已上传集合中
                string fileMd5 = CalculateMd5(filePath);
                uploadedFilesMd5.Add(fileMd5);
                AddLog($"{filePath} File uploaded {result}.");
                uploadCount++;
            }
            catch (Exception ex)
            {
                AddLog("上传异常：" + ex.Message);
            }
            finally
            {
                setProgress(uploadCount,dupCount, totalCount);
            }

            return Task.CompletedTask;
        }

        // 加载已上传文件的 MD5 值
        private void LoadUploadedFilesMd5()
        {
            if (File.Exists(DataFilePath))
            {
                var json = File.ReadAllText(DataFilePath);
                uploadedFilesMd5 = JsonSerializer.Deserialize<HashSet<string>>(json);
            }
        }

        // 保存已上传文件的 MD5 值
        public void SaveUploadedFilesMd5()
        {
            var json = JsonSerializer.Serialize(uploadedFilesMd5);
            File.WriteAllText(DataFilePath, json);
        }

        private void exit()
        {
            AddLog("保存进度");
            SaveUploadedFilesMd5();
            AddLog("保存进度成功");
            Application.Exit();
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            exit();
        }

        private void MainFrm_Resize(object sender, EventArgs e)
        {

            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                notifyIcon1.BalloonTipText ="程序进入后台运行";

                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(2000);
            }

        }
        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RestoreFromTray();
        }

        private void showMain_Click(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void RestoreMenuItem_Click(object sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void ExitMenuItem_Click(object sender, EventArgs e)
        {
            exit();
        }

        private void setProgress(int progress,int dupCount, int totalCount)
        {
            var percent = 0;
            if (totalCount>0)
            {
                  percent = (dupCount+progress)*100/totalCount;
                setProgressbar(percent);
            }
            var info = $"总数：{totalCount}，已上传:{uploadCount} , 重复数量：{dupCount}，进度：{percent}%";
            setProgressInfo(info);
        }



        private void setProgressInfo(string info )
        {
            if (label2.InvokeRequired)
            {
                // 使用委托和Invoke方法在正确的线程上调用
                label2.Invoke(new Action<string>(setProgressInfo), info);
            }
            else
            {
                // 在当前线程上，直接更新进度条
                this.label2.Text =info;
            }
        }
        private void setProgressbar(int value)
        {
            if (value>100)
            {
                value=100;
            }

            if (progressBar1.InvokeRequired)
            {
                // 使用委托和Invoke方法在正确的线程上调用
                progressBar1.Invoke(new Action<int>(setProgressbar), value);
            }
            else
            {
                // 在当前线程上，直接更新进度条
                progressBar1.Value = value;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            this.button2.Enabled = false;
            this.btnStart.Enabled=true;
            this.btnStart.Text="开始同步";
            this.txtOssPath.Enabled=true;
            start=false;
            totalCount=0;
            dupCount=0;
            uploadCount=0;
            setProgress(uploadCount, dupCount, totalCount);


        }
    }
}
