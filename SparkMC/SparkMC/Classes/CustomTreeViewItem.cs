using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SparkMC.Classes
{
    public class CustomTreeViewItem
    {
        public string Type { get; set; }
        public ObservableCollection<CustomTreeViewItem> Jars { get; set; } = new ObservableCollection<CustomTreeViewItem>();

        public override string ToString()
        {
            return Type;
        }
    }
}
