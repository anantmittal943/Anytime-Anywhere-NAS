using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.Models
{
    public class SystemInfo
    {
        public OSPlatform OS { get; set; }
        public int TotalCores { get; set; }
        public double TotalRamGB { get; set; }
    }
}
