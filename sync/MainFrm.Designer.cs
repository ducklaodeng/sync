namespace sync
{
    partial class MainFrm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            folderBrowserDialog1 = new FolderBrowserDialog();
            splitContainer1 = new SplitContainer();
            label1 = new Label();
            txtOssPath = new TextBox();
            btnStart = new Button();
            button1 = new Button();
            textBox1 = new TextBox();
            listLog = new ListBox();
            timer1 = new System.Windows.Forms.Timer(components);
            notifyIcon1 = new NotifyIcon(components);
            info = new ContextMenuStrip(components);
            showMain = new ToolStripMenuItem();
            actionExit = new ToolStripMenuItem();
            toolTip1 = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            info.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(txtOssPath);
            splitContainer1.Panel1.Controls.Add(btnStart);
            splitContainer1.Panel1.Controls.Add(button1);
            splitContainer1.Panel1.Controls.Add(textBox1);
            splitContainer1.Panel1.RightToLeft = RightToLeft.No;
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(listLog);
            splitContainer1.Size = new Size(1706, 766);
            splitContainer1.SplitterDistance = 130;
            splitContainer1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(32, 39);
            label1.Name = "label1";
            label1.Size = new Size(104, 31);
            label1.TabIndex = 4;
            label1.Text = "Oss路径";
            // 
            // txtOssPath
            // 
            txtOssPath.Location = new Point(174, 20);
            txtOssPath.Multiline = true;
            txtOssPath.Name = "txtOssPath";
            txtOssPath.Size = new Size(520, 76);
            txtOssPath.TabIndex = 3;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(1519, 26);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(150, 70);
            btnStart.TabIndex = 2;
            btnStart.Text = "开始同步";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // button1
            // 
            button1.Location = new Point(1303, 26);
            button1.Name = "button1";
            button1.Size = new Size(82, 70);
            button1.TabIndex = 1;
            button1.Text = "...";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(788, 26);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.PlaceholderText = "选择路径";
            textBox1.ReadOnly = true;
            textBox1.Size = new Size(524, 70);
            textBox1.TabIndex = 0;
            // 
            // listLog
            // 
            listLog.Dock = DockStyle.Fill;
            listLog.FormattingEnabled = true;
            listLog.Location = new Point(0, 0);
            listLog.Name = "listLog";
            listLog.Size = new Size(1706, 632);
            listLog.TabIndex = 0;
            // 
            // notifyIcon1
            // 
            notifyIcon1.Text = "同步助手";
            notifyIcon1.Visible = true;
            notifyIcon1.MouseDoubleClick += notifyIcon1_MouseDoubleClick;
            // 
            // info
            // 
            info.ImageScalingSize = new Size(32, 32);
            info.Items.AddRange(new ToolStripItem[] { showMain, actionExit });
            info.Name = "info";
            info.Size = new Size(209, 80);
            // 
            // showMain
            // 
            showMain.CheckOnClick = true;
            showMain.Name = "showMain";
            showMain.Size = new Size(208, 38);
            showMain.Text = "显示主界面";
            showMain.Click += showMain_Click;
            // 
            // actionExit
            // 
            actionExit.Name = "actionExit";
            actionExit.Size = new Size(208, 38);
            actionExit.Text = "退出";
            // 
            // toolTip1
            // 
            toolTip1.IsBalloon = true;
            toolTip1.ToolTipIcon = ToolTipIcon.Info;
            toolTip1.ToolTipTitle = "同步助手";
            // 
            // MainFrm
            // 
            AutoScaleDimensions = new SizeF(14F, 31F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1706, 766);
            Controls.Add(splitContainer1);
            Name = "MainFrm";
            Text = "同步助手";
            FormClosing += MainFrm_FormClosing;
            Resize += MainFrm_Resize;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            info.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private FolderBrowserDialog folderBrowserDialog1;
        private SplitContainer splitContainer1;
        private Button button1;
        private TextBox textBox1;
        private Button btnStart;
        private ListBox listLog;
        private System.Windows.Forms.Timer timer1;
        private NotifyIcon notifyIcon1;
        private ContextMenuStrip info;
        private ToolStripMenuItem showMain;
        private ToolStripMenuItem actionExit;
        private Label label1;
        private TextBox txtOssPath;
        private ToolTip toolTip1;
    }
}
