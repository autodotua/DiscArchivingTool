using FzLib.DataStorage.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscArchivingTool
{
    public class Configs : IJsonSerializable
    {
        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            this.Save(path, new JsonSerializerSettings().SetIndented());
        }
        public PackingConfigs Packing { get; set; } = new PackingConfigs();
        public RebuildConfigs Rebuild { get; set; } = new RebuildConfigs();
        public CheckConfigs Check { get; set; } = new CheckConfigs();
    }
    public class RebuildConfigs
    {
        public string InputDir { get; set; }
        public string OutputDir { get; set; }
        public bool OverrideWhenExisted { get; set; }
    }
    public class CheckConfigs
    {
        public string Dir { get; set; }
    }

    public class PackingConfigs
    {
        public string BlackList { get; set; } = $"Thumbs.db{Environment.NewLine}desktop.ini";
        public bool CreateISO { get; set; } = false;
        public string Dir { get; set; }
        public string OutputDir { get; set; }
        public int DiscSize { get; set; } = 4480;
        public DateTime EarliestDateTime { get; set; } = DateTime.MinValue;
        public int MaxDiscCount { get; set; } = 1000;
    }
}
