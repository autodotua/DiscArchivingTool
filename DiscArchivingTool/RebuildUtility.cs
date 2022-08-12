using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using static DiscArchivingTool.App;

namespace DiscArchivingTool
{
    public class RebuildUtility
    {
        private Dictionary<string, List<DiscFile>> files = new Dictionary<string, List<DiscFile>>();
        private string inputDirs;
        public void ReadFileList(string dirs)
        {
            inputDirs = dirs;
            foreach (var dir in dirs.Split('|'))
            {

                string filelistName = Directory.EnumerateFiles(dir, "filelist-*.txt")
                     .OrderByDescending(p => p)
                     .FirstOrDefault();
                if (filelistName == null)
                {
                    throw new Exception("不存在filelist，目录有误或文件缺失！");
                }

                var lines = File.ReadAllLines(filelistName);
                var header = lines[0].Split('\t');
                files.Add(dir,
                    lines.Skip(1).Select(p =>
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
             }).ToList());
            }
        }
        /// <summary>
        /// 重建分析
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FormatException"></exception>
        public FreeFileSystemTree BuildTree()
        {
            FreeFileSystemTree tree = FreeFileSystemTree.CreateRoot();
            foreach (var dir in files.Keys)
            {
                foreach (var file in files[dir])
                {
                    string filePath = Path.Combine(dir, file.DiscName);
                    if (!File.Exists(filePath))
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
            }
            return tree;
        }

        /// <summary>
        /// 进行重建
        /// </summary>
        /// <param name="distDir"></param>
        /// <returns></returns>
        public IReadOnlyList<RebuildError> Rebuild(string distDir)
        {
            List<RebuildError> errorFiles = new List<RebuildError>();
            foreach (var dir in files.Keys)
            {
                foreach (var file in files[dir])
                {
                    try
                    {
                        var srcPath = Path.Combine(dir, file.DiscName);
                        var distPath = Path.Combine(distDir, file.Path);
                        var distFileDir = Path.GetDirectoryName(distPath);
                        var name = Path.GetFileName(file.Path);
                        MessageReceived?.Invoke(this, new MessageEventArgs($"正在重建{file.Path}"));
                        Directory.CreateDirectory(distFileDir);
                        string md5 = FileUtility.CopyAndGetHash(srcPath, distPath);
                        if (md5 != file.Md5)
                        {
                            errorFiles.Add(new RebuildError(file, "文件验证失败"));
                        }
                    }
                    catch (Exception ex)
                    {
                        errorFiles.Add(new RebuildError(file, ex.Message));
                    }
                }
            }
            return errorFiles;
        }

        public event EventHandler<MessageEventArgs> MessageReceived;

        public class RebuildError
        {
            public RebuildError(DiscFile file, string error)
            {
                File = file;
                Error = error;
            }

            public DiscFile File { get; set; }
            public string Error { get; set; }
        }
    }
}
