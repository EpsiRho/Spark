using Spark.ViewModels;
using Spark.Classes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spark.Pages
{
    public sealed partial class Home : Page
    {
        // Page Vars
        public ServerViewModel serverViewModel { get; set; }
        private ObservableCollection<CustomTreeViewItem> TreeViewSource { get; set; }

        public Home()
        {
            this.InitializeComponent();
            serverViewModel = new ServerViewModel();
            TreeViewSource = new ObservableCollection<CustomTreeViewItem>();
            Thread t = new Thread(AsyncStartingFunctions);
            t.Start();
        }

        private async void AsyncStartingFunctions()
        {
            var items = ServerJarLookup.BuildTreeView();
            foreach (var item in items)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    TreeViewSource.Add(item);
                });
            }
        }

        private void ServersGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ServersGridView.SelectedIndex = 0;
        }

        private void AddNewServerButton_Click(object sender, RoutedEventArgs e)
        {
            NewServerDialog.ShowAsync();
        }

        private void NewServerDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {

        }

        private void JarsView_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {
            if(((CustomTreeViewItem)args.InvokedItem).Jars.Count != 0)
            {
                JarsView.SelectedItem = -1;
                var container = JarsView.ContainerFromItem(args.InvokedItem);
                var node = JarsView.NodeFromContainer(container);
                if (node.IsExpanded == false)
                {
                    JarsView.Expand(node);
                }
                else
                {
                    JarsView.Collapse(node);
                }
            }
            else
            {
                JarsView.SelectedItem = args.InvokedItem;
            }
        }
    }
}
