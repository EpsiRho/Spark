using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spark.Classes
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Response
    {
        public List<string> bedrock { get; set; }
        public List<string> modded { get; set; }
        public List<string> proxies { get; set; }
        public List<string> servers { get; set; }
        public List<string> vanilla { get; set; }
    }

    public class ServerTypes
    {
        public string status { get; set; }
        public Response response { get; set; }
    }
}
