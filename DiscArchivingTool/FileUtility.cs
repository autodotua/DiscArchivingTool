using System.IO;
using System.Security.Cryptography;

namespace DiscArchivingTool
{
    public static class FileUtility
    {
        /// <summary>
        /// 复制并获取MD5
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public static string CopyAndGetHash(string from, string to)
        {
            int bufferSize = 1024 * 1024;
            MD5 md5 = MD5.Create();
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
                if(File.Exists(to))
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

    }
}
