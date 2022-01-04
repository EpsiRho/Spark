using SparkMC.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SparkMC.ViewModels
{
    public class ServerViewModel
    {
        // UI notification functions
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private ObservableCollection<Server> servers = new ObservableCollection<Server>();
        public ObservableCollection<Server> Servers
        {
            get { return servers; }
            set
            {
                servers = value;
                this.OnPropertyChanged("Servers");
            }
        }
        public ServerViewModel() { }
    }
}
