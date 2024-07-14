using System.Configuration;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace sync
{
    public partial class MainFrm : Form
    {   
        FileSystemWatcher watcher = new FileSystemWatcher();
        OssHelper ossHelper;
        private HashSet<string> uploadedFilesMd5;
        private const string DataFilePath = "data.json";
        string filter = ConfigurationManager.AppSettings["filter"];
        string bucketName = ConfigurationManager.AppSettings["bucketName"];
        bool start = false;

        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem restoreMenuItem;
        private ToolStripMenuItem exitMenuItem;

        public MainFrm()
        {
            InitializeComponent();
          
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
;
            exitMenuItem.Click += ExitMenuItem_Click;
 


        }

        private void OnAutoCheck(object? sender, EventArgs e)
        {
            AddLog("自动检查已开始");
            FindAndUpdateLoad(this.textBox1.Text).Wait();
            SaveUploadedFilesMd5();
            AddLog("自动检查已结束");

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            AddLog("启动成功");
            this.btnStart.Text="同步中..";
            FindAndUpdateLoad(this.textBox1.Text).Wait();

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

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
           
            AddLog("发现新文件：" + e.FullPath);
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
            var matchArr= extArr.Where(x => e.FullPath.EndsWith(x.Replace("*",""))).ToArray();
            if (matchArr.Length==0)
            {
                AddLog("文件类型不符合");
                return;
            }
             UploadFile(e.FullPath);
        }

        private async Task AyscFindAndUpdateLoad(string folderPath)
        {
            if (start)
            {
                return;
            }
            start = true;
            await FindAndUpdateLoad(folderPath);
            start = false;

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

            AddLog($"发现{fileList.Length}个文件");
            foreach (string file in fileList)
            {
                UploadFile(file);
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
        public void UploadFile(string filePath)
        {
            if (IsFileUploaded(filePath))
            {
                AddLog($"File '{filePath}' has already been uploaded.");
                return;
            }
            try
            {
                // 模拟文件上传过程
                AddLog($"Uploading file '{filePath}'...");
                var fileName = new FileInfo(filePath).FullName;
                string result = ossHelper.UploadFile(filePath, this.txtOssPath.Text+fileName);
                // 上传成功后，将文件的 MD5 值添加到已上传集合中
                string fileMd5 = CalculateMd5(filePath);
                uploadedFilesMd5.Add(fileMd5);
                AddLog($"{filePath} File uploaded {result}.");
            }
            catch (Exception ex)
            {
                AddLog("上传异常：" + ex.Message);
            }
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
    }
}
