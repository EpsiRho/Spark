using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Classes
{
    public class Server
    {
        public string Name { get; set; }
        public string FolderPth { get; set; }
        public string FolderVersion { get; set; }
        public DateTime Date { get; set; }
    }

    public class AllServers
    {
        public List<Server> Items { get; set;}
    }
}
