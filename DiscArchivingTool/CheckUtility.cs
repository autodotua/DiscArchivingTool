using System.IO;

namespace DiscArchivingTool
{
    public class CheckUtility : DiscUtilityBase
    {
        private Dictionary<string, List<DiscFile>> files;

        public event EventHandler<ProgressUpdatedEventArgs> CheckProgressUpdated;

        public List<CheckResult> Check()
        {
            stopping = false;
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
                    InvokeMessageReceivedEvent($"正在校验 {path}");
                    try
                    {
                        FileInfo file = new FileInfo(path);
                        if (!file.Exists)
                        {
                            result.NotExist = true;
                        }
                        else
                        {
                            if ((file.LastWriteTime - discFile.LastWriteTime).Duration().TotalSeconds > Configs.MaxTimeTolerance)
                            {
                                result.ErrorTime = true;
                            }
                            if (file.Length != discFile.Length)
                            {
                                result.ErrorLength = true;
                            }
                            if (GetMD5(path) != discFile.Md5)
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

                    if(stopping)
                    {
                        throw new OperationCanceledException();
                    }
                }
            }
            return results;
        }

        public void InitFileList(string dirs)
        {
            files = ReadFileList(dirs);
        }
    }
}
