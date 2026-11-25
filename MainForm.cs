using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace LocalFileLocker
{
    #region class MainForm
    public partial class MainForm : Form
    {
        private TableLayoutPanel table;
        private RadioButton rbLock;
        private RadioButton rbUnlock;
        private TextBox txtSource;
        private TextBox txtTarget;
        private TextBox txtLog;
        private Button btnExecute;

        /// <summary>构造函数：初始化窗体组件。</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        /// <summary>初始化界面布局与控件，并从嵌入资源加载窗口图标 favicon.ico（不依赖磁盘路径，跨 .NET Framework 稳定）。</summary>
        private void InitializeComponent()
        {
            try
            {
                foreach (string name in typeof(MainForm).Assembly.GetManifestResourceNames())
                {
                    if (name.EndsWith("favicon.ico", StringComparison.OrdinalIgnoreCase))
                    {
                        using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream(name))
                        {
                            if (stream != null)
                            {
                                Icon = new Icon(stream);
                            }
                        }
                        break;
                    }
                }
            }
            catch { }
            if (Icon == null)
            {
                Icon = SystemIcons.Application;
            }
            Text = "本地文件锁工具 LocalFileLocker ";
            ClientSize = new Size(320, 240);
            MinimumSize = new Size(320, 240);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;

            table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 4,
                Padding = new Padding(4),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            for (int i = 0; i < 3; i++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            }
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Label lblSource = new Label
            {
                Text = "源文件(夹)",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
            };

            txtSource = new TextBox
            {
                ReadOnly = true,
                AutoSize = false,
                Height = 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 0, 2)
            };

            Button btnFile = new Button
            {
                Text = "文件",
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
            };
            btnFile.Click += new EventHandler(BtnFile_Folder_Click);

            Button btnFolder = new Button
            {
                Text = "文件夹",
                Dock = DockStyle.Fill,
                Margin = new Padding(0)
            };
            btnFolder.Click += new EventHandler(BtnFile_Folder_Click);

            TableLayoutPanel sourcePanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            sourcePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sourcePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sourcePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            sourcePanel.Controls.Add(btnFile, 0, 0);
            sourcePanel.Controls.Add(btnFolder, 1, 0);

            table.Controls.Add(lblSource, 0, 0);
            table.Controls.Add(txtSource, 1, 0);
            table.Controls.Add(sourcePanel, 2, 0);

            Label lblTarget = new Label
            {
                Text = "输出目录",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
            };

            txtTarget = new TextBox
            {
                ReadOnly = true,
                AutoSize = false,
                Height = 24,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
                Margin = new Padding(0, 2, 0, 2)
            };

            Button btnTarget = new Button
            {
                Text = "选择输出目录",
                Dock = DockStyle.Fill,
                Margin = new Padding(3)
            };
            btnTarget.Click += new EventHandler(BtnTarget_Click);

            table.Controls.Add(lblTarget, 0, 1);
            table.Controls.Add(txtTarget, 1, 1);
            table.Controls.Add(btnTarget, 2, 1);

            Label lblAction = new Label
            {
                Text = "操作",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleRight,
            };

            rbLock = new RadioButton
            {
                Text = "加锁",
                Checked = true,
                Dock = DockStyle.Fill,
                AutoSize = true,
            };

            rbUnlock = new RadioButton
            {
                Text = "解锁",
                Dock = DockStyle.Fill,
                AutoSize = true,
            };

            TableLayoutPanel actionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            actionPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            actionPanel.Controls.Add(rbLock);
            actionPanel.Controls.Add(rbUnlock);

            Button btnHelp = new Button
            {
                Text = "帮助",
                Width = 80,
                Margin = new Padding(0),
            };
            btnHelp.Click += new EventHandler(BtnHelp_Click);

            Button btnReset = new Button
            {
                Text = "重置",
                Width = 80,
                Margin = new Padding(0),
            };
            btnReset.Click += new EventHandler(BtnReset_Click);

            btnExecute = new Button
            {
                Text = "执行",
                Width = 80,
                Margin = new Padding(0),
            };
            btnExecute.Click += new EventHandler(BtnExecute_Click);

            TableLayoutPanel buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
            };
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
            buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            buttonPanel.Controls.Add(btnHelp);
            buttonPanel.Controls.Add(btnReset);
            buttonPanel.Controls.Add(btnExecute);


            table.Controls.Add(lblAction, 0, 2);
            table.Controls.Add(actionPanel, 1, 2);
            table.Controls.Add(buttonPanel, 2, 2);

            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 10, 0, 0),
            };

            table.Controls.Add(txtLog, 0, 3);
            table.SetColumnSpan(txtLog, 3);

            Controls.Add(table);

            // 窗体关闭（点右上角 X）后结束整个应用。
            FormClosed += new FormClosedEventHandler(MainForm_FormClosed);
        }

        /// <summary>窗体关闭后立即结束整个应用。本工具只有一个窗口，关闭窗口即退出进程；
        /// 用 Environment.Exit 立即终止，避免控制台子系统（隐藏的控制台窗口）在
        /// Windows Server 2008 R2 等旧系统上等待控制台宿主进程而卡住不退出。</summary>
        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        /// <summary>“文件/文件夹”按钮点击：按类型调用 PickFiles 选择源文件（夹）并刷新显示。</summary>
        private void BtnFile_Folder_Click(object sender, EventArgs e)
        {
            LocalFileLocker.PickFiles(((Button)sender).Text == "文件" ? DialogResult.Yes : DialogResult.No);
            txtSource.Text = LocalFileLocker.FileFullnames;
        }

        /// <summary>“选择输出目录”按钮点击：选择输出目录并刷新显示。</summary>
        private void BtnTarget_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "请选择输出目录";
                dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LocalFileLocker.outputFolder = dialog.SelectedPath;
                    txtTarget.Text = LocalFileLocker.outputFolder;
                }
            }
        }

        /// <summary>“帮助”按钮点击：在日志区域显示帮助信息。</summary>
        private void BtnHelp_Click(object sender, EventArgs e)
        {
            txtLog.Text = LocalFileLocker.ShowMessage(LocalFileLocker.helpMessage, "帮助");
        }

        /// <summary>“重置”按钮点击：清空源、输出目录与日志，恢复默认加锁选项。</summary>
        private void BtnReset_Click(object sender, EventArgs e)
        {
            txtSource.Text = "";
            LocalFileLocker.outputFolder = "";
            txtTarget.Text = LocalFileLocker.outputFolder;
            rbLock.Checked = true;
            txtLog.Text = "";
        }

        /// <summary>“执行”按钮点击：在后台线程执行加锁/解锁，实时逐条显示进度，UI 不冻结。</summary>
        private void BtnExecute_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSource.Text))
            {
                MessageBox.Show("请先选择源文件（夹）", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string action = rbLock.Checked ? "lock" : "unlock";

            txtLog.Clear();
            btnExecute.Enabled = false;

            Thread worker = new Thread(delegate ()
            {
                string summary = LocalFileLocker.Execute(action, delegate (string line)
                {
                    // 从工作线程安全切回 UI 线程，逐条追加到日志并滚动到末尾。
                    BeginInvoke((MethodInvoker)delegate ()
                    {
                        txtLog.AppendText(line + Environment.NewLine);
                        txtLog.SelectionStart = txtLog.TextLength;
                        txtLog.ScrollToCaret();
                    });
                });

                BeginInvoke((MethodInvoker)delegate ()
                {
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        txtLog.AppendText(Environment.NewLine + summary);
                        txtLog.SelectionStart = txtLog.TextLength;
                        txtLog.ScrollToCaret();
                    }
                    btnExecute.Enabled = true;
                });
            })
            {
                IsBackground = true
            };
            worker.Start();
        }
    }
    #endregion
}