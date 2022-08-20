using System.IO;

namespace DiscArchivingTool
{
    public class CheckUtility
    {
        private Dictionary<string, List<DiscFile>> files;
        public event EventHandler<MessageEventArgs> MessageReceived;

        public event EventHandler<ProgressUpdatedEventArgs> CheckProgressUpdated;

        public List<CheckResult> Check()
        {
            List<CheckResult> results = new List<CheckResult>();
            double length = 0;
            double totalLength = files.Values.Sum(p => p.Sum(q => q.Length));

            foreach (var dir in files.Keys)
            {
                foreach (var discFile in files[dir])
                {
                    var result = new CheckResult()
                    {
                        File = discFile,
                        Dir = dir
                    };
                    var path = Path.Combine(dir, discFile.DiscName);

                    MessageReceived?.Invoke(this, new MessageEventArgs($"正在校验 {path}"));
                    try
                    {
                        FileInfo file = new FileInfo(path);
                        if (!file.Exists)
                        {
                            result.NotExist = true;
                        }
                        else
                        {
                            if ((file.LastWriteTime-discFile.LastWriteTime).Duration().TotalSeconds>Configs.MaxTimeTolerance)
                            {
                                result.ErrorTime = true;
                            }
                            if (file.Length != discFile.Length)
                            {
                                result.ErrorLength = true;
                            }
                            if (FileUtility.GetMD5(path) != discFile.Md5)
                            {
                                result.ErrorMD5 = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Message = ex.Message;
                    }

                    CheckProgressUpdated?.Invoke(this, new ProgressUpdatedEventArgs(length += discFile.Length, totalLength));
                    results.Add(result);
                }
            }
            return results;
        }

        public void ReadFileList(string dirs)
        {
            files = FileUtility.ReadFileList(dirs);
        }
        public class CheckResult
        {
            public string Dir { get; set; }
            public bool ErrorLength { get; set; }
            public bool ErrorMD5 { get; set; }
            public bool ErrorTime { get; set; }
            public DiscFile File { get; set; }
            public string Message { get; set; }
            public bool NotExist { get; set; }
            public bool NoProblem =>!(ErrorLength||ErrorMD5||ErrorTime||NotExist)&&Message==null;
        }
    }
}
