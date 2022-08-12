namespace DiscArchivingTool
{
    public class DiscFilePackage
    {
        public int Index { get; set; }
        public List<DiscFile> Files { get; } = new List<DiscFile>();
        public long TotalSize { get; set; }
        public DateTime EarliestTime { get; set; }
        public DateTime LatestTime { get; set; }
        public bool Checked { get; set; } = true;
    }
}
