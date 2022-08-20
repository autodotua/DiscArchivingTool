using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using static DiscArchivingTool.App;
namespace DiscArchivingTool
{
    public static class FileUtility
    {
        public static string GetMD5(string file)
        {
            using MD5 md5 = MD5.Create();
            using var stream = File.OpenRead(file);
            md5.ComputeHash(stream);
            return BitConverter.ToString(md5.Hash).Replace("-", "");
        }

        /// <summary>
        /// 复制并获取MD5
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static string CopyAndGetHash(string from, string to)
        {
            int bufferSize = 1024 * 1024; //1MB的缓冲区
            using MD5 md5 = MD5.Create();
            using FileStream fileStream = new FileStream(to, FileMode.Create, FileAccess.Write, FileShare.None);
            using FileStream fs = new FileStream(from, FileMode.Open, FileAccess.Read);
            try
            {
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
                fs.Close();
                fs.Dispose();
                fileStream.Close();
                fileStream.Dispose();
                File.SetLastWriteTime(to, File.GetLastWriteTime(from));
                return BitConverter.ToString(md5.Hash).Replace("-", "");
            }
            catch (Exception ex)
            {
                fs.Close();
                fs.Dispose();
                fileStream.Close();
                fileStream.Dispose();
                if (File.Exists(to))
                {
                    try
                    {
                        File.Delete(to);
                    }
                    catch
                    {

                    }
                }
                throw;
            }
        }

        /// <summary>
        /// 解析filelist文件
        /// </summary>
        /// <param name="dirs"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="FormatException"></exception>
        public static Dictionary<string, List<DiscFile>> ReadFileList(string dirs)
        {
            Dictionary<string, List<DiscFile>> files = new Dictionary<string, List<DiscFile>>();
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
                        var file = new DiscFile()
                        {
                            DiscName = parts[0],
                            Path = parts[1],
                            LastWriteTime = DateTime.Parse(parts[2]),
                            Length = long.Parse(parts[3]),
                            Md5 = parts[4],
                            RawName = Path.GetFileName(parts[1]),
                        };
                        return file;
                    }).ToList());

            }
            return files;
        }

    }
}
