﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DiscArchivingTool
{
    public class FileUtility
    {
        List<FileInfo> filesOrderedByTime = new List<FileInfo>();

        public DiscFilePackageCollection Packages { get; private set; }
        private string sourceDir = null;

        public void EnumerateAndOrderFiles(string sourceDir, DateTime earliestTime)
        {
            this.sourceDir = sourceDir;
            filesOrderedByTime = new DirectoryInfo(sourceDir).EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(p => p.LastWriteTime > earliestTime)
                .OrderBy(p => p.LastWriteTime).ToList();
        }

        public void SplitToDiscs(int sizeMB, int maxCount)
        {
            DiscFilePackageCollection packages = new DiscFilePackageCollection();
            packages.DiscFilePackages.Add(new DiscFilePackage());
            long maxSize = 1L * 1024 * 1024 * sizeMB;
            foreach (var file in filesOrderedByTime)
            {
                //文件超过单盘大小
                if (file.Length > maxSize)
                {
                    packages.SizeOutOfRangeFiles.Add(file);
                    continue;
                }

                //文件超过剩余空间
                var package = packages.DiscFilePackages[^1];
                if (file.Length > maxSize - package.TotalSize)
                {
                    package.EarliestTime = package.Files[0].LastWriteTime;
                    package.LatestTime = package.Files[^1].LastWriteTime;
                    package.Index = packages.DiscFilePackages.Count;
                    package = new DiscFilePackage();
                    packages.DiscFilePackages.Add(package);
                }

                //加入文件

                DiscFile discFile = new DiscFile()
                {
                    Path = file.FullName,
                    LastWriteTime = file.LastWriteTime,
                    Length = file.Length
                };
                package.Files.Add(discFile);
                package.TotalSize += file.Length;
            }

            //处理最后一个
            var lastPackage = packages.DiscFilePackages[^1];
            lastPackage.EarliestTime = lastPackage.Files[0].LastWriteTime;
            lastPackage.LatestTime = lastPackage.Files[^1].LastWriteTime;
            lastPackage.Index = packages.DiscFilePackages.Count;

            Packages = packages;
        }

        public void Export(string distDir)
        {
            if (!Directory.Exists(distDir))
            {
                Directory.CreateDirectory(distDir);
            }

            foreach (var package in Packages.DiscFilePackages)
            {
                string dir = Path.Combine(distDir, package.Index.ToString());
                Directory.CreateDirectory(dir);
                using var fileListStream = File.OpenWrite(Path.Combine(dir, "filelist.txt"));
                using var writer = new StreamWriter(fileListStream);
                foreach (var file in package.Files)
                {
                    string relativePath = Path.GetRelativePath(sourceDir, file.Path);
                    string newName = relativePath.Replace(":", "#c#").Replace("\\", "#s#");
                    string md5 = CopyAndGetHash(file.Path, Path.Combine(dir, newName));
                    MessageReceived?.Invoke(this, new MessageEventArgs() { Message = $"正在复制第 {package.Index} 个光盘文件包中的 {relativePath}" });
                    writer.WriteLine($"{newName}\t{relativePath}\t{md5}");
                }
            }
        }

        private string CopyAndGetHash(string from, string to)
        {
            int bufferSize = 1024 * 1024;
            MD5 md5 = MD5.Create();
            using FileStream fileStream = new FileStream(to, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
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

        public event EventHandler<MessageEventArgs> MessageReceived;
        public class MessageEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}
