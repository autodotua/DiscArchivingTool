using DiscUtils;
using DiscUtils.Iso9660;
using FzLib.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscArchivingTool
{
    public class FileUtility
    {
        /// <summary>
        /// 统一的日期时间格式
        /// </summary>
        public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        /// <summary>
        /// 停止导出（打包）
        /// </summary>
        public void StopExporting()
        {
            stoppingExport = true;
        }
        /// <summary>
        /// 已经收到停止导出信号
        /// </summary>
        private bool stoppingExport = false;
        /// <summary>
        /// 根据时间顺序从早到晚排序后的文件
        /// </summary>
        List<FileInfo> filesOrderedByTime = new List<FileInfo>();

        /// <summary>
        /// 光盘文件包
        /// </summary>
        public DiscFilePackageCollection Packages { get; private set; }
        /// <summary>
        /// 源目录
        /// </summary>
        private string sourceDir = null;
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
        /// <summary>
        /// 导出（打包）
        /// </summary>
        /// <param name="distDir"></param>
        /// <param name="createISO"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task ExportAsync(string distDir, bool createISO, Func<string, Task<ErrorOperation>> error)
        {
            if (!Directory.Exists(distDir))
            {
                Directory.CreateDirectory(distDir);
            }

            stoppingExport = false;
            foreach (var package in Packages.DiscFilePackages)
            {
                string dir = Path.Combine(distDir, package.Index.ToString());
                Directory.CreateDirectory(dir);
                using var fileListStream = File.OpenWrite(Path.Combine(dir, $"filelist-{DateTime.Now:yyyyMMddHHmmss}.txt"));
                using var writer = new StreamWriter(fileListStream);

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
                            MessageReceived?.Invoke(this, new MessageEventArgs() { Message = $"正在复制第 {package.Index} 个光盘文件包中的 {relativePath}" });
                            string md5 = CopyAndGetHash(file.Path, Path.Combine(dir, newName));

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
                                    abort = true;
                                    break;
                                default:
                                    break;
                            }
                        }
                    } while (retry);
                    if (abort || stoppingExport)
                    {
                        throw new OperationCanceledException();
                    }
                }

                if (createISO)
                {
                    MessageReceived?.Invoke(this, new MessageEventArgs() { Message = $"正在创第 {package.Index} 个ISO" });
                    CreateISO(dir);
                }
            }
        }
        /// <summary>
        /// 复制并获取MD5
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private string CopyAndGetHash(string from, string to)
        {
            int bufferSize = 1024 * 1024;
            MD5 md5 = MD5.Create();
            using FileStream fileStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.None);
            using FileStream fs = new FileStream(from, FileMode.Open, FileAccess.ReadWrite);
            fileStream.SetLength(fs.Length);
            int bytesRead = -1;
            byte[] bytes = new byte[bufferSize];
            int offset = 0;
            while ((bytesRead = fs.Read(bytes, 0, bufferSize)) > 0)
            {
                md5.TransformBlock(bytes, 0, bytesRead, null, 0);
                fileStream.Write(bytes, 0, bytesRead);
                offset += bytesRead;
            }
            md5.TransformFinalBlock(new byte[0], 0, 0);
            return BitConverter.ToString(md5.Hash).Replace("-", "");
        }
        /// <summary>
        /// 创建ISO
        /// </summary>
        /// <param name="dir"></param>
        private void CreateISO(string dir)
        {
            CDBuilder builder = new CDBuilder();
            builder.UseJoliet = true;
            foreach (var file in Directory.EnumerateFiles(dir))
            {
                builder.AddFile(Path.GetFileName(file), file);
            }
            builder.Build(Path.Combine(Path.GetDirectoryName(dir), Path.GetFileName(dir) + ".iso"));
        }

        /// <summary>
        /// 重建分析
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FormatException"></exception>
        public static FreeFileSystemTree RebuildAnalyze(string dir)
        {
            var filelistName = Directory.EnumerateFiles(dir, "filelist-*.txt")
                     .OrderByDescending(p => p)
                     .FirstOrDefault();
            if (filelistName == null)
            {
                throw new Exception("不存在filelist，目录有误或文件缺失！");
            }

            var lines = File.ReadAllLines(filelistName);
            var header = lines[0].Split('\t');
            var files = lines.Skip(1).Select(p =>
            {
                var parts = p.Split('\t');
                if (parts.Length != 5)
                {
                    throw new FormatException("filelist格式错误，无法解析");
                }
                return new DiscFile()
                {
                    DiscName = parts[0],
                    Path = parts[1],
                    LastWriteTime = DateTime.ParseExact(parts[2], DateTimeFormat, CultureInfo.InvariantCulture),
                    Length = long.Parse(parts[3]),
                    Md5 = parts[4],
                };
            }).ToList();

            FreeFileSystemTree tree = FreeFileSystemTree.CreateRoot();

            foreach (var file in files)
            {
                string filePath = Path.Combine(dir, file.DiscName);
                if(!File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }
                var pathParts = file.Path.Split('\\', '/');
                file.RawName = pathParts[^1];
                var current = tree;
                for (int i = 0; i < pathParts.Length - 1; i++)
                {
                    var part = pathParts[i];
                    if (current.Directories.Any(p => p.Name == part))
                    {
                        current = current.Directories.First(p => p.Name == part);
                    }
                    else
                    {
                        current = current.AddChild(part);
                    }
                }
                var treeFile = current.AddFile(file.RawName);
                treeFile.File = file;
            }
            return tree;
        }

        public event EventHandler<MessageEventArgs> MessageReceived;
        public class MessageEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}
