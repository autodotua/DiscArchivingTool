namespace DiscArchivingTool
{
    public class DiscFile
    {
        public string RawName { get; set; }
        public string DiscName { get; set; }
        public string Path { get; set; }
        public DateTime LastWriteTime { get; set; }
        public long Length { get; set; }
        public string Md5 { get; set; }
    }
}
