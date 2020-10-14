using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicLogger.jnet.systems.Objects
{
    public class Error
    {
        public Logger.LogLevel LogLevel { get; set; }
        public DateTime DateTime { get; set; }
        public string Message { get; set; }
    }
}
