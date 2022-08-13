using FzLib;
using System.ComponentModel;

namespace DiscArchivingTool
{
    public class DiscFilePackage:INotifyPropertyChanged
    {
        public int Index { get; set; }
        public List<DiscFile> Files { get; } = new List<DiscFile>();
        public long TotalSize { get; set; }
        public DateTime EarliestTime { get; set; }
        public DateTime LatestTime { get; set; }
        private bool isChecked;

        public event PropertyChangedEventHandler PropertyChanged;

        public bool Checked
        {
            get => isChecked;
            set => this.SetValueAndNotify(ref isChecked, value, nameof(Checked));
        }

    }
}
