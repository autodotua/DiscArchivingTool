using DiscUtils;
using DiscUtils.Iso9660;
using FzLib.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DiscArchivingTool.App;

namespace DiscArchivingTool
{
    public class PackingUtility : DiscUtilityBase
    {
        /// <summary>
        /// 根据时间顺序从早到晚排序后的文件
        /// </summary>
        List<FileInfo> filesOrderedByTime = new List<FileInfo>();

        /// <summary>
        /// 源目录
        /// </summary>
        private string sourceDir = null;

        public event EventHandler<ProgressUpdatedEventArgs> RebuildProgressUpdated;

        /// <summary>
        /// 光盘文件包
        /// </summary>
        public DiscFilePackageCollection Packages { get; private set; }

        /// <summary>
        /// 枚举文件并进行排序
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="earliestTime"></param>
        /// <param name="blackList"></param>
        /// <param name="blackListUseRegex"></param>
        public void EnumerateAndOrderFiles(string sourceDir, DateTime earliestTime, string blackList, bool blackListUseRegex)
        {
            this.sourceDir = sourceDir;
            string[] blacks = blackList.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            List<Regex> blackRegexs = blacks.Select(p => new Regex(p, RegexOptions.IgnoreCase)).ToList();
            filesOrderedByTime = new DirectoryInfo(sourceDir)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(p => p.LastWriteTime > earliestTime)
                .Where(p =>
                {
                    for (int i = 0; i < blacks.Length; i++)
                    {
                        if (blackListUseRegex) //正则
                        {
                            if (blacks[i].Contains('\\') || blacks[i].Contains('/')) //目录
                            {
                                if (blackRegexs[i].IsMatch(p.FullName))
                                {
                                    return false;
                                }
                            }
                            else //文件
                            {
                                if (blackRegexs[i].IsMatch(p.Name))
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            if (blacks[i].Contains('\\') || blacks[i].Contains('/')) //目录
                            {
                                if (p.FullName.Contains(blacks[i]))
                                {
                                    return false;
                                }
                            }
                            else //文件
                            {

                                if (p.Name.Contains(blacks[i]))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                })
                .OrderBy(p => p.LastWriteTime).ToList();
        }

        /// <summary>
        /// 导出（打包）
        /// </summary>
        /// <param name="distDir"></param>
        /// <param name="createISO"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task ExportAsync(string distDir, PackingType type, Func<string, Task<ErrorOperation>> error)
        {
            if (!Directory.Exists(distDir))
            {
                Directory.CreateDirectory(distDir);
            }
            double length = 0;
            double totalLength = Packages.DiscFilePackages
                .Where(p => p.Checked && p.Index > 0)
                .Sum(p => p.Files.Sum(q => q.Length));
            stopping = false;
            foreach (var package in Packages.DiscFilePackages.Where(p => p.Checked && p.Index > 0))
            {
                string dir = Path.Combine(distDir, package.Index.ToString());
                Directory.CreateDirectory(dir);
                string fileListName = $"filelist-{DateTime.Now:yyyyMMddHHmmss}.txt";
                CDBuilder builder = null;
                if (type == PackingType.ISO)
                {
                    builder = new CDBuilder();
                    builder.UseJoliet = true;
                }
                using (var fileListStream = File.OpenWrite(Path.Combine(dir, fileListName)))
                using (var writer = new StreamWriter(fileListStream))
                {

                    writer.WriteLine($"{package.EarliestTime.ToString(DateTimeFormat)}\t{package.LatestTime.ToString(DateTimeFormat)}\t{package.TotalSize}");


                    foreach (var file in package.Files)
                    {
                        bool retry = false;
                        bool abort = false;
                        do
                        {
                            string relativePath = "";
                            try
                            {
                                relativePath = Path.GetRelativePath(sourceDir, file.Path);
                                string newName = relativePath.Replace(":", "#c#").Replace("\\", "#s#");
                                string md5 = null;
                                switch (type)
                                {
                                    case PackingType.Copy:
                                        InvokeMessageReceivedEvent($"正在复制第 {package.Index} 个光盘文件包中的 {relativePath}");
                                        md5 = CopyAndGetHash(file.Path, Path.Combine(dir, newName));
                                        break;
                                    case PackingType.ISO:
                                        InvokeMessageReceivedEvent($"正在读取第 {package.Index} 个光盘文件包中的 {relativePath}");
                                        builder.AddFile(newName, file.Path);
                                        md5 = GetMD5(file.Path);
                                        break;
                                    case PackingType.HardLink:
                                        InvokeMessageReceivedEvent($"正在硬链接第 {package.Index} 个光盘文件包中的 {relativePath}");
                                        CreateHardLink(Path.Combine(dir, newName), file.Path);
                                        md5 = GetMD5(file.Path);
                                        break;
                                }

                                writer.WriteLine($"{newName}\t{relativePath}\t{file.LastWriteTime.ToString(DateTimeFormat)}\t{file.Length}\t{md5}");
                            }
                            catch (Exception ex)
                            {
                                var op = await error($"文件{relativePath}导出失败：{ex.Message}");
                                switch (op)
                                {
                                    case ErrorOperation.Retry:
                                        retry = true;
                                        break;
                                    case ErrorOperation.Abort:
                                        retry = false;
                                        abort = true;
                                        break;
                                    default:
                                        retry = false;
                                        break;
                                }
                            }
                        } while (retry);
                        if (abort || stopping)
                        {
                            throw new OperationCanceledException();
                        }

                        RebuildProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(length += file.Length, totalLength));
                    }
                }
                if (type == PackingType.ISO)
                {
                    InvokeMessageReceivedEvent($"正在创第 {package.Index} 个ISO");
                    builder.AddFile(fileListName, Path.Combine(dir, fileListName));
                    builder.Build(Path.Combine(Path.GetDirectoryName(dir), Path.GetFileName(dir) + ".iso"));

                }
            }
        }

        /// <summary>
        /// 将枚举后的文件分割成光盘所需要的大小
        /// </summary>
        /// <param name="sizeMB"></param>
        /// <param name="maxCount"></param>
        /// <returns>是否完整</returns>
        public bool SplitToDiscs(int sizeMB, int maxCount)
        {
            DiscFilePackageCollection packages = new DiscFilePackageCollection();
            packages.DiscFilePackages.Add(new DiscFilePackage());
            long maxSize = 1L * 1024 * 1024 * sizeMB;
            foreach (var file in filesOrderedByTime)
            {
                DiscFile discFile = new DiscFile()
                {
                    RawName = file.Name,
                    Path = file.FullName,
                    LastWriteTime = file.LastWriteTime,
                    Length = file.Length
                };

                //文件超过单盘大小
                if (file.Length > maxSize)
                {
                    packages.SizeOutOfRangeFiles.Add(discFile);
                    continue;
                }

                //文件超过剩余空间
                var package = packages.DiscFilePackages[^1];
                if (file.Length > maxSize - package.TotalSize)
                {
                    package.EarliestTime = package.Files[0].LastWriteTime;
                    package.LatestTime = package.Files[^1].LastWriteTime;
                    package.Index = packages.DiscFilePackages.Count;
                    if (packages.DiscFilePackages.Count >= maxCount)
                    {
                        Packages = packages;
                        return false;
                    }
                    package = new DiscFilePackage();
                    packages.DiscFilePackages.Add(package);
                }

                //加入文件
                package.Files.Add(discFile);
                package.TotalSize += file.Length;
            }

            //处理最后一个
            var lastPackage = packages.DiscFilePackages[^1];
            lastPackage.EarliestTime = lastPackage.Files[0].LastWriteTime;
            lastPackage.LatestTime = lastPackage.Files[^1].LastWriteTime;
            lastPackage.Index = packages.DiscFilePackages.Count;

            Packages = packages;
            return true;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private void CreateHardLink(string link, string source)
        {
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(source);
            }

            if (File.Exists(link))
            {
                File.Delete(link);
            }

            if (Path.GetPathRoot(link) != Path.GetPathRoot(source))
            {
                throw new IOException("硬链接的两者必须在同一个分区中");
            }

            bool value = CreateHardLink(link, source, IntPtr.Zero);
            if (!value)
            {
                throw new IOException("未知错误，无法创建硬链接");
            }
        }
    }
}
