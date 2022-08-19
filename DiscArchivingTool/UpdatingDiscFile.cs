using System.IO;

namespace DiscArchivingTool
{
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

    public class UpdatingDiscFile
    {
        public bool Checked { get; set; }
        public bool Deleted => Type.HasFlag(UpdatingType.Deleted);
        public DiscFile DiscFile { get; set; }
        public bool LengthChanged => Type.HasFlag(UpdatingType.LengthChanged);
        public bool NameChanged => Type.HasFlag(UpdatingType.NameChanged);
        public bool PathChanged => Type.HasFlag(UpdatingType.PathChanged);
        public DiscFile ReferenceFile { get; set; }
        public bool TimeChanged => Type.HasFlag(UpdatingType.TimeChanged);
        public UpdatingType Type { get; set; }
    }
}
