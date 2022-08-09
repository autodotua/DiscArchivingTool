using System.IO;

namespace DiscArchivingTool
{
    public class DiscFilePackageCollection
    {
        public List<DiscFilePackage> DiscFilePackages { get; } = new List<DiscFilePackage>();
        public List<FileInfo> SizeOutOfRangeFiles { get; } = new List<FileInfo>();
    }
}
