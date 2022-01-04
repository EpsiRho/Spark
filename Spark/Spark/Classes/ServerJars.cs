using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Classes
{
    public class ResponseJ
    {
        public string version { get; set; }
        public string file { get; set; }
        public string md5 { get; set; }
        public object built { get; set; }
        public string stability { get; set; }
    }

    public class ServerJars
    {
        public string status { get; set; }
        public List<ResponseJ> response { get; set; }
    }
}
