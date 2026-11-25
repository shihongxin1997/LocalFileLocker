using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace LocalFileLocker
{
    #region class LocalFileLocker
    public class LocalFileLocker
    {
        static readonly List<string> allowedOptions = new List<string> { "help", "gui", "lock", "unlock", "output" };
        public static readonly string helpMessage = @"
本地文件锁工具 LocalFileLocker 

用法:
  LocalFileLocker.exe [选项] [文件或文件夹路径]

选项:
  -help, -h    显示帮助信息
  -gui, -g     使用图形界面
  -lock, -l    加锁文件
  -unlock, -u  解锁文件
  -output, -o  指定输出文件夹（省略时默认到文件同目录）

示例:
  LocalFileLocker.exe -g
  LocalFileLocker.exe -l C:\\path\\to\\file.docx
  LocalFileLocker.exe -u C:\\path\\to\\file.docx
  LocalFileLocker.exe -u C:\\path\\to\\folder\ -o C:\\output\\folder
".Trim();
        static readonly List<string> options = new List<string>();
        static readonly List<string> files = new List<string>();
        /// <summary>用户实际选择的源项目（文件或文件夹路径，不展开），用于界面展示。</summary>
        static readonly List<string> selectedItems = new List<string>();
        public static string FileFullnames
        {
            get
            {
                if (selectedItems.Count == 0)
                {
                    return "";
                }
                if (selectedItems.Count == 1)
                {
                    return selectedItems[0];
                }
                return string.Format("{0} ... 等 {1} 个项目", selectedItems[0], selectedItems.Count);
            }
        }
        public static string outputFolder = "";

        /// <summary>程序入口：解析参数，无参数默认进入图形界面，按选项执行加锁/解锁或显示帮助。</summary>
        [STAThread]
        public static void Main(string[] args)
        {
            options.Clear();
            files.Clear();
            selectedItems.Clear();

            ParseArguments(args);

            if (options.Count == 0) 
            {
                options.Add("gui");
            }

            if (options.Contains("gui"))
            {
                HideConsole();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (files.Count == 0)
                {
                    new MainForm().ShowDialog();
                    Environment.Exit(0);
                    return;
                }
            }

            if (options.Contains("help"))
            {
                ShowMessage(helpMessage, "帮助");
                return;
            }

            if (files.Count == 0)
            {
                if (!PickFiles())
                {
                    return;
                }
            }

            if (!(options.Contains("lock") || options.Contains("unlock")))
            {
                DialogResult choice = MessageBox.Show("请选择操作类型:\n\n是 → 解锁文件\n否 → 加锁文件\n取消 → 退出",
                    "选择操作", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (choice == DialogResult.Yes)
                {
                    options.Add("unlock");
                }
                else if (choice == DialogResult.No)
                {
                    options.Add("lock");
                }
                else
                {
                    return;
                }
            }
            string result = "";
            // 命令行模式实时逐条输出进度；GUI 模式由 MainForm 负责实时日志。
            Action<string> itemCallback = options.Contains("gui") ? null : new Action<string>(s => Console.WriteLine(s));
            if (options.Contains("lock"))
            {
                result = Execute("lock", itemCallback);
            }
            if (options.Contains("unlock"))
            {
                result = Execute("unlock", itemCallback);
            }
            ShowMessage(result);
        }

        /// <summary>解析命令行参数：识别选项、处理 -output 的目标文件夹，其余归为文件/文件夹收集。</summary>
        private static void ParseArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Trim();
                if (string.IsNullOrWhiteSpace(arg))
                {
                    continue;
                }
                if (arg.StartsWith("-", StringComparison.Ordinal) || arg.StartsWith("/", StringComparison.Ordinal))
                {
                    string name = arg.Substring(1);
                    string alias = name.StartsWith("-", StringComparison.Ordinal) ? name.Substring(1) : name;
                    List<string> matchedOptions = new List<string>();
                    foreach (string allowed in allowedOptions)
                    {
                        if (string.Equals(allowed, alias, StringComparison.OrdinalIgnoreCase) ||
                            (alias.Length >= 1 && string.Equals(allowed.Substring(0, 1), alias.Substring(0, 1), StringComparison.OrdinalIgnoreCase)))
                        {
                            matchedOptions.Add(allowed);
                        }
                    }
                    if (matchedOptions.Count == 0)
                    {
                        Console.WriteLine(string.Format("未知选项: {0}", arg));
                        continue;
                    }
                    if (matchedOptions.Count > 1)
                    {
                        Console.WriteLine(string.Format("选项 '{0}' 不明确，匹配到多个选项: {1}", arg, string.Join(", ", matchedOptions)));
                        continue;
                    }
                    string option = matchedOptions[0];
                    if (option == "output" && i + 1 < args.Length)
                    {
                        string nextArg = GetFullPath(args[i + 1]);
                        if (!string.IsNullOrWhiteSpace(nextArg) && Directory.Exists(nextArg))
                        {
                            outputFolder = nextArg;
                            i++;
                        }
                        else
                        {
                            Console.WriteLine(string.Format("无效的输出文件夹路径: {0}", args[i + 1]));
                        }
                    }
                    else
                    {
                        options.Add(option);
                    }
                }
                else
                {
                    string fullPath = GetFullPath(arg);
                    CollectFiles(files, fullPath);
                    selectedItems.Add(fullPath);
                }
            }
        }

        /// <summary>显示消息：图形界面模式用 MessageBox 弹窗，否则输出到控制台。</summary>
        public static string ShowMessage(string message = "", string title = "提示")
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "";
            }
            if (options.Contains("gui"))
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            } else {
                Console.WriteLine(message);
            }
            return message;
        }

        /// <summary>无参数时，用对话框让用户选择文件或文件夹；返回是否最终取得了文件（false = 用户取消）。</summary>
        public static bool PickFiles(DialogResult choice = DialogResult.None)
        {
            if (choice == DialogResult.None)
            {
                choice = MessageBox.Show("是否仅选择文件？\n\n是 → 选择文件\n否 → 选择文件夹\n取消 → 退出",
                "选择类型", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            }
            if (choice == DialogResult.Cancel)
            {
                Environment.Exit(0);
                return false;
            }
            files.Clear();
            selectedItems.Clear();
            if (choice == DialogResult.Yes)
            {
                using (OpenFileDialog dialog = new OpenFileDialog())
                {
                    dialog.Title = "请选择文件（可多选）";
                    dialog.Filter = "所有文件 (*.*)|*.*";
                    dialog.Multiselect = true;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        foreach (string file in dialog.FileNames)
                        {
                            CollectFiles(files, file);
                            selectedItems.Add(file);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            if (choice == DialogResult.No)
            {
                using (FolderBrowserDialog dialog = new FolderBrowserDialog())
                {
                    dialog.Description = "请选择文件夹";
                    dialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        CollectFiles(files, dialog.SelectedPath);
                        selectedItems.Add(dialog.SelectedPath);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return files.Count > 0;
        }

        /// <summary>将路径转换为规范化绝对路径（去除引号，基于当前目录合并）。</summary>
        private static string GetFullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }
            Regex regex = new Regex(@"^['""](.+)['""]$");
            if (regex.IsMatch(path))
            {
                path = regex.Match(path).Groups[1].Value.Trim();
            }
            if (string.IsNullOrWhiteSpace(Path.GetPathRoot(path)))
            {
                path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, path));
            }
            return path;
        }

        /// <summary>收集文件路径（文件夹则递归收集其中的所有文件，跳过解锁中间产物）</summary>
        private static void CollectFiles(List<string> list, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }
            if (File.Exists(path))
            {
                list.Add(new FileInfo(path).FullName);
            }
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".backup", StringComparison.OrdinalIgnoreCase))
                    {
                        string basePath = file.Substring(0, file.Length - ".backup".Length);
                        if (File.Exists(basePath))
                        {
                            continue;
                        }
                    }
                    list.Add(file);
                }
            }
        }

        /// <summary>遍历待处理文件，按 action（lock/unlock）调用 FileLocker 执行加锁/解锁；每处理完一个文件即通过 onItemResult 回调该条结果（用于实时进度显示）。</summary>
        public static string Execute(string action, Action<string> onItemResult = null)
        {
            List<string> results = new List<string>();
            foreach (string file in files)
            {
                string itemResult;
                if (!File.Exists(file))
                {
                    itemResult = string.Format("文件不存在: {0}", file);
                }
                else if (action == "lock")
                {
                    itemResult = FileLocker.Lock(file, outputFolder);
                }
                else if (action == "unlock")
                {
                    itemResult = FileLocker.UnlockAll(file, outputFolder);
                }
                else
                {
                    itemResult = string.Format("未知操作: {0}: {1}", action, file);
                }
                results.Add(itemResult);
                if (onItemResult != null)
                {
                    onItemResult(itemResult);
                }
            }

            string output = "未进行任何操作";
            if (results.Count > 0)
            {
                output = string.Format("{0} {1} 个文件操作完成{2}", action == "lock" ? "加锁" : "解锁", results.Count, Environment.NewLine);
                if (results.Count < 10)
                {
                    output += string.Join(Environment.NewLine, results);
                }
            }
            return output;
        }

        /// <summary>隐藏控制台窗口（仅在图形界面模式调用）。</summary>
        private static void HideConsole()
        {
            try
            {
                IntPtr hwnd = GetConsoleWindow();
                if (hwnd != IntPtr.Zero)
                {
                    ShowWindow(hwnd, SW_HIDE);
                }
            }
            catch { }
        }

        private const int SW_HIDE = 0;

        /// <summary>获取当前控制台窗口句柄（kernel32）。</summary>
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        /// <summary>控制窗口显示状态（user32）。</summary>
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
    #endregion
}