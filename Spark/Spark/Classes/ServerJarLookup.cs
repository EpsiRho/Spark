using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace Spark.Classes
{
    public static class ServerJarLookup
    {
        public static List<CustomTreeViewItem> BuildTreeView()
        {
            List<CustomTreeViewItem> items = new List<CustomTreeViewItem>();

            var types = GetServerTypes();

            foreach(var type in types.response.servers)
            {
                var jars = GetServerJars(type);
                CustomTreeViewItem item = new CustomTreeViewItem();
                item.Type = type;
                item.Jars = new ObservableCollection<CustomTreeViewItem>();
                foreach (var jar in jars.response)
                {
                    item.Jars.Add(new CustomTreeViewItem() { Type = jar.version });
                }
                items.Add(item);
            }
            foreach (var type in types.response.vanilla)
            {
                var jars = GetServerJars(type);
                CustomTreeViewItem item = new CustomTreeViewItem();
                item.Type = type;
                item.Jars = new ObservableCollection<CustomTreeViewItem>();
                foreach (var jar in jars.response)
                {
                    item.Jars.Add(new CustomTreeViewItem() { Type = jar.version });
                }
                items.Add(item);
            }

            return items;
        }

        public static ServerTypes GetServerTypes()
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri("https://serverjars.com/api/fetchTypes/");
            
            RestRequest restRequest = new RestRequest();
            restRequest.Method = Method.GET;

            var response = client.Execute(restRequest);
            ServerTypes myDeserializedClass = JsonConvert.DeserializeObject<ServerTypes>(response.Content);

            return myDeserializedClass;
        }

        public static ServerJars GetServerJars(string Type)
        {
            RestClient client = new RestClient();
            client.BaseUrl = new Uri($"https://serverjars.com/api/fetchAll/{Type}/");

            RestRequest restRequest = new RestRequest();
            restRequest.Method = Method.GET;

            var response = client.Execute(restRequest);
            ServerJars myDeserializedClass = JsonConvert.DeserializeObject<ServerJars>(response.Content);

            return myDeserializedClass;
        }
    }
}
