using System.IO;

namespace DiscArchivingTool
{
    public class DiscFilePackageCollection
    {
        public List<DiscFilePackage> DiscFilePackages { get; } = new List<DiscFilePackage>();
        public List<DiscFile> SizeOutOfRangeFiles { get; } = new List<DiscFile>();
    }
}
