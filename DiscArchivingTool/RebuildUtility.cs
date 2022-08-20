using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using static DiscArchivingTool.App;

namespace DiscArchivingTool
{
    public class RebuildUtility
    {
        private Dictionary<string, List<DiscFile>> files;
        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<ProgressUpdatedEventArgs> RebuildProgressUpdated;

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

        public void ReadFileList(string dirs)
        {
            files = FileUtility.ReadFileList(dirs);
        }
        /// <summary>
        /// 进行重建
        /// </summary>
        /// <param name="distDir"></param>
        /// <returns></returns>
        public int Rebuild(string distDir, bool overrideWhenExisted, out List<RebuildError> errorFiles)
        {
            errorFiles = new List<RebuildError>();
            double length = 0;
            double totalLength = files.Values.Sum(p => p.Sum(q => q.Length));
            int count = 0;
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
                        MessageReceived?.Invoke(this, new MessageEventArgs($"正在重建 {file.Path}"));
                        if (!Directory.Exists(distFileDir))
                        {
                            Directory.CreateDirectory(distFileDir);
                        }
                        if (File.Exists(distPath) && !overrideWhenExisted)
                        {
                            throw new Exception("文件已存在");
                        }
                        string md5 = FileUtility.CopyAndGetHash(srcPath, distPath);
                        if (md5 != file.Md5)
                        {
                            throw new Exception("MD5验证失败");
                        }
                        if ((File.GetLastWriteTime(srcPath) - file.LastWriteTime).Duration().TotalSeconds > Configs.MaxTimeTolerance)
                        {
                            throw new Exception("修改时间不一致");
                        }
                        count++;
                    }
                    catch (Exception ex)
                    {
                        errorFiles.Add(new RebuildError(file, ex.Message));
                    }
                    RebuildProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(length += file.Length, totalLength));
                }
            }
            return count;
        }
    }
    public class RebuildError
    {
        public RebuildError(DiscFile file, string error)
        {
            File = file;
            Error = error;
        }

        public string Error { get; set; }
        public DiscFile File { get; set; }
    }
}


