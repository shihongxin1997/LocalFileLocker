using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LocalFileLocker
{
    #region class FileLocker
    /// <summary>文件加锁/解锁核心逻辑（独立类，方法通过参数接收所需状态，不依赖全局字段）。</summary>
    public static class FileLocker
    {
        private static readonly List<string> copyExtensions = new List<string> { ".pdf", ".wps", ".png" };

        /// <summary>加锁文件：复制为 copyExtensions[0] 副本后再移动回原文件名，完成加锁。</summary>
        public static string Lock(string filePath, string outputFolder)
        {
            try
            {
                string copyFilePath = filePath + copyExtensions[0];
                string outputFilePath = filePath;
                if (!string.IsNullOrWhiteSpace(outputFolder))
                {
                    string fileName = Path.GetFileName(filePath);
                    copyFilePath = Path.Combine(outputFolder, fileName + copyExtensions[0]);
                    outputFilePath = Path.Combine(outputFolder, fileName);
                    File.Copy(filePath, copyFilePath, true);
                }
                else
                {
                    if (File.Exists(copyFilePath))
                    {
                        File.Delete(copyFilePath);
                    }
                    File.Move(filePath, copyFilePath);
                }
                if (File.Exists(outputFilePath))
                {
                    File.Delete(outputFilePath);
                }
                File.Move(copyFilePath, outputFilePath);
            }
            catch (Exception ex)
            {
                return string.Format("加锁失败: {0} ({1})", filePath, ex.Message);
            }
            return string.Format("加锁成功: {0}", filePath);
        }

        /// <summary>按所有候选扩展名依次尝试解锁，返回操作结果。</summary>
        public static string UnlockAll(string filePath, string outputFolder)
        {
            for (int i = 0; i < copyExtensions.Count; i++)
            {
                string unlockResult = Unlock(filePath, copyExtensions[i], outputFolder);
                if (!unlockResult.Contains("成功") && i < copyExtensions.Count - 1)
                {
                    continue;
                }
                return unlockResult;
            }
            return string.Format("解锁失败: {0}", filePath);
        }

        /// <summary>
        /// 解锁单个文件：
        /// 0. 先拷贝：保留原扩展名，追加 copyExtension（如 test.docx → test.docx.pdf）
        /// 1. 读取原文件字节，重新写入为 .backup 后缀
        /// 2. 检查 copyExtension 文件大小是否正好比 .backup 大 1024 字节
        /// 3. 是 → 删除原始文件和 copyExtension，将 .backup 重命名移除 .backup 后缀（还原）
        ///    否 → 删除 copyExtension 和 .backup
        /// </summary>
        private static string Unlock(string filePath, string copyFileExtension, string outputFolder)
        {
            string copyFilePath = filePath + copyFileExtension;
            string backupFilePath = filePath + ".backup";
            string outputFilePath = filePath;
            if (!string.IsNullOrWhiteSpace(outputFolder))
            {
                string fileName = Path.GetFileName(filePath);
                copyFilePath = Path.Combine(outputFolder, fileName + copyFileExtension);
                backupFilePath = Path.Combine(outputFolder, fileName + ".backup");
                outputFilePath = Path.Combine(outputFolder, fileName);
            }

            if (!File.Exists(backupFilePath))
            {
                if (!File.Exists(copyFilePath))
                {
                    try
                    {
                        File.Copy(filePath, copyFilePath, true);
                    }
                    catch (Exception ex)
                    {
                        return string.Format("复制失败: {0} ({1})", filePath, ex.Message);
                    }
                }

                byte[] bytes;
                try
                {
                    bytes = File.ReadAllBytes(copyFilePath);
                }
                catch (Exception ex)
                {
                    return string.Format("读取失败: {0} ({1})", copyFilePath, ex.Message);
                }

                try
                {
                    File.WriteAllBytes(backupFilePath, bytes);
                }
                catch (Exception ex)
                {
                    return string.Format("写入 .backup 失败: {0} ({1})", backupFilePath, ex.Message);
                }
            }

            if (File.Exists(copyFilePath))
            {
                File.Delete(copyFilePath);
            }

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = string.Format("-noprofile -nologo -executionpolicy bypass -windowstyle hidden -command \"$f = '{0}'; $b = '{1}'; $o = '{2}'; $r = ((gi $f).Length - (gi $b).Length); $c = (cp $b $o -force); $r; \"", filePath, backupFilePath, outputFilePath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using (Process process = Process.Start(psi))
            {
                string sOutput = process.StandardOutput.ReadToEnd();
                string sError = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (File.Exists(backupFilePath))
                {
                    File.Delete(backupFilePath);
                }
                if (process.ExitCode != 0 || !string.IsNullOrEmpty(sError))
                {
                    return string.Format("解锁失败: {0} ({1})", filePath, sError.Trim());
                }
                long sizeDifference;
                if (!long.TryParse(sOutput.Trim(), out sizeDifference))
                {
                    return string.Format("解锁失败，无法获取文件大小差异: {0}", sOutput.Trim());
                }
                if (sizeDifference == 1024)
                {
                    return string.Format("解锁成功: {0}", filePath);
                }
                if (sizeDifference == 0)
                {
                    return string.Format("解锁失败，或许文件未加锁: {0}", filePath);
                }
                return string.Format("解锁失败: {0}", filePath);
            }
        }
    }
    #endregion
}