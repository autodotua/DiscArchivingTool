namespace DiscArchivingTool
{
    public class ProgressUpdatedEventArgs : EventArgs
    {
        public ProgressUpdatedEventArgs(double value, double maxValue)
        {
            Value = value;
            MaxValue = maxValue;
        }

        public double Value { get; private set; }
        public double MaxValue { get; private set; }
    }
}
