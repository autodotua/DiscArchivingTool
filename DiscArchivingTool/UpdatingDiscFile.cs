using System.IO;

namespace DiscArchivingTool
{
    public class UpdatingDiscFile
    {
        public bool Checked { get; set; }
        public DiscFile File { get; set; }
        public FileInfo SourceFile { get; set; }
        public UpdatingType Type { get; set; }
        public bool NameChanged => Type.HasFlag(UpdatingType.NameChanged);
        public bool PathChanged => Type.HasFlag(UpdatingType.PathChanged);
        public bool LengthChanged => Type.HasFlag(UpdatingType.LengthChanged);
        public bool TimeChanged => Type.HasFlag(UpdatingType.TimeChanged);
        public bool Deleted => Type.HasFlag(UpdatingType.Deleted);
    }
    [Flags]
    public enum UpdatingType
    {
        None = 0x00,
        NameChanged = 0x01,
        PathChanged = 0x02,
        LengthChanged = 0x04,
        TimeChanged = 0x08,
        Deleted = 0x10,
    }

}
