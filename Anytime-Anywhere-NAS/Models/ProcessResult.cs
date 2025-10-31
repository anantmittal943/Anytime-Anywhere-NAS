using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.Models
{
    public class ProcessResult
    {
        public string Output { get; set; } = "";
        public string Error { get; set; } = "";
        public int ExitCode { get; set; }
        public bool IsSuccess => ExitCode == 0;
    }
}
