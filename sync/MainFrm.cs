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
            notifyIcon1.Text = "ͬ�����ֺ�̨������";
            notifyIcon1.Visible = false; // ��ʼ״̬���ɼ�

            contextMenu = new ContextMenuStrip();
            restoreMenuItem = new ToolStripMenuItem("�ָ�");
            exitMenuItem = new ToolStripMenuItem("�˳�");

            contextMenu.Items.AddRange(new ToolStripItem[] { restoreMenuItem, exitMenuItem });
            notifyIcon1.ContextMenuStrip = contextMenu;

            restoreMenuItem.Click += RestoreMenuItem_Click;
            exitMenuItem.Click += ExitMenuItem_Click;



        }

        private async void OnAutoCheck(object? sender, EventArgs e)
        {

            AddLog("�Զ�����ѿ�ʼ");
            if (!start)
            {
                AddLog("ͬ��δ����");
            }
            await FindAndUpdateLoad(this.textBox1.Text);
            SaveUploadedFilesMd5();
            AddLog("�Զ�����ѽ���");

        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox1.Text)||string.IsNullOrEmpty(this.txtOssPath.Text))
            {
                MessageBox.Show("�ϴ�·������oss·��Ϊ��");
                return;
            }
            AddLog("�����ɹ�");
            this.btnStart.Enabled=false;
            this.btnStart.Text="ͬ����..";
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
           
            AddLog("�������ļ���" + e.FullPath);
            if (!start)
            {
                AddLog("����δ����" );
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
                AddLog("�ļ����Ͳ�����");
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
                AddLog("Ŀ¼���Ϸ�");
                return;
            }
            string[] fileList = getExtArr().SelectMany(ext => Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories)).ToArray();
            if (fileList.Length == 0)
            {
                AddLog("�ļ��б�Ϊ��");
                return;
            }
            totalCount=fileList.Length;
            setProgress(uploadCount,dupCount, totalCount);
            AddLog($"����{fileList.Length}���ļ�");
            var semaphore = new SemaphoreSlim(5); // �ź���
            var tasks = new List<Task>();
            foreach (string file in fileList)
            {
                if (!start)
                {
                    AddLog("������ֹͣ");
                    return;
                }
                await semaphore.WaitAsync(); // �ȴ��ź���

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                       await  UploadFile(file);
                    }
                    finally
                    {
                        semaphore.Release(); // �ͷ��ź���
                    }
                }));
                await Task.WhenAll(tasks); // �ȴ������������

            }
        }

        private string[] getExtArr()
        {
            return filter.Split("|");
        }


        // ����һ�����������ڰ�ȫ���ڿ��߳�����½���־��Ϣ��ӵ�ListBox��
        private void AddLog(string log)
        {
            log =$"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}:{log}";
            if (listLog.InvokeRequired)
            {
                // �����Ҫ���̵߳���
                listLog.Invoke(new Action<string>(AddLog), log);
            }
            else
            {
                listLog.Items.Add(log);

                // �����־����100����ɾ�������һ��
                if (listLog.Items.Count > 100)
                {
                    listLog.Items.RemoveAt(0);
                }
                // ���ֹ���������ײ�
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

        // �ж��ļ��Ƿ����ϴ�
        public bool IsFileUploaded(string filePath)
        {
            string fileMd5 = CalculateMd5(filePath);
            return uploadedFilesMd5.Contains(fileMd5);
        }

        // �ϴ��ļ�
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
                // ģ���ļ��ϴ�����
                AddLog($"Uploading file '{filePath}'...");
                var fileName = new FileInfo(filePath).Name;
               var ossPath= txtOssPath.Text.Replace($"oss://{bucketName}/", "");
                if (!ossPath.EndsWith("/"))
                {
                    ossPath += "/";
                }
                string result = ossHelper.UploadFile(filePath, ossPath+fileName);
                // �ϴ��ɹ��󣬽��ļ��� MD5 ֵ��ӵ����ϴ�������
                string fileMd5 = CalculateMd5(filePath);
                uploadedFilesMd5.Add(fileMd5);
                AddLog($"{filePath} File uploaded {result}.");
                uploadCount++;
            }
            catch (Exception ex)
            {
                AddLog("�ϴ��쳣��" + ex.Message);
            }
            finally
            {
                setProgress(uploadCount,dupCount, totalCount);
            }

            return Task.CompletedTask;
        }

        // �������ϴ��ļ��� MD5 ֵ
        private void LoadUploadedFilesMd5()
        {
            if (File.Exists(DataFilePath))
            {
                var json = File.ReadAllText(DataFilePath);
                uploadedFilesMd5 = JsonSerializer.Deserialize<HashSet<string>>(json);
            }
        }

        // �������ϴ��ļ��� MD5 ֵ
        public void SaveUploadedFilesMd5()
        {
            var json = JsonSerializer.Serialize(uploadedFilesMd5);
            File.WriteAllText(DataFilePath, json);
        }

        private void exit()
        {
            AddLog("�������");
            SaveUploadedFilesMd5();
            AddLog("������ȳɹ�");
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
                notifyIcon1.BalloonTipText ="��������̨����";

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
            var info = $"������{totalCount}�����ϴ�:{uploadCount} , �ظ�������{dupCount}�����ȣ�{percent}%";
            setProgressInfo(info);
        }



        private void setProgressInfo(string info )
        {
            if (label2.InvokeRequired)
            {
                // ʹ��ί�к�Invoke��������ȷ���߳��ϵ���
                label2.Invoke(new Action<string>(setProgressInfo), info);
            }
            else
            {
                // �ڵ�ǰ�߳��ϣ�ֱ�Ӹ��½�����
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
                // ʹ��ί�к�Invoke��������ȷ���߳��ϵ���
                progressBar1.Invoke(new Action<int>(setProgressbar), value);
            }
            else
            {
                // �ڵ�ǰ�߳��ϣ�ֱ�Ӹ��½�����
                progressBar1.Value = value;
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            watcher.EnableRaisingEvents = false;
            this.button2.Enabled = false;
            this.btnStart.Enabled=true;
            this.btnStart.Text="��ʼͬ��";
            this.txtOssPath.Enabled=true;
            start=false;
            totalCount=0;
            dupCount=0;
            uploadCount=0;
            setProgress(uploadCount, dupCount, totalCount);


        }
    }
}
