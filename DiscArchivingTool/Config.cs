using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscArchivingTool
{
    public class Config
    {
        public string Dir { get; set; }
        public DateTime StartDateTime { get; set; } = new DateTime(2000, 0, 0);
        public int MaxDiscCount { get; set; } = 1000;
        public double DiscSize { get; set; } = 22;
    }
}
