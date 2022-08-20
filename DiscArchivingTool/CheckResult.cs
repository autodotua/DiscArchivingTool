namespace DiscArchivingTool
{
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
