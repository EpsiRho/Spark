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
using Windows.Storage;
using Newtonsoft.Json;
using System.Net;
using Windows.UI.Popups;
using Windows.System;
using Windows.ApplicationModel.Core;
using Windows.Storage.Search;
using System.Threading.Tasks;
using Windows.UI;
using System.Diagnostics;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Spark.Pages
{
    public sealed partial class Home : Page
    {
        // Page Vars
        public ServerViewModel serverViewModel { get; set; }
        public Server SelectedServer { get; set; }
        private ObservableCollection<CustomTreeViewItem> TreeViewSource { get; set; }
        private StorageFolder SelectedFolder { get; set; }

        //          Page Startup          //
        public Home()
        {
            this.InitializeComponent();
            serverViewModel = new ServerViewModel();
            TreeViewSource = new ObservableCollection<CustomTreeViewItem>();
            Thread t = new Thread(AsyncStartingFunctions);
            t.Start();
            Thread d = new Thread(LoadServersList);
            d.Start();
            VerifyPerms();
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


        //          Server list functions          //
        private void ServersGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            SelectedServer = (Server)e.ClickedItem;
            Thread t = new Thread(GetServerInfo);
            t.Start();
            ServersGridView.SelectedIndex = -1;
        }
        private void AddNewServerButton_Click(object sender, RoutedEventArgs e)
        {
            NewServerDialog.ShowAsync();
        }
        private async void GetServerInfo()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                ServerPowerState.Text = "Offline";
                PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                ServerWorldSize.Text = "Loading";
                WorldProgress.Visibility = Visibility.Visible;
                IPPortProgress.Visibility = Visibility.Visible;
                ServerIPPort.Text = "Loading";
                ServerPlayerCount.Text = "Offline";
            });

            StorageFolder folder =  await StorageFolder.GetFolderFromPathAsync(SelectedServer.FolderPth);

            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);
            StorageFile prop = null;
            try
            {
                prop = await folder.GetFileAsync("server.properties");
                List<string> properties = (List<string>)await FileIO.ReadLinesAsync(prop, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                string[] port = properties.Find(o => o.Contains("server-port")).Split("=");

                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ServerIPPort.Text = $"{externalIp.ToString()}:{port[1]}";
                    IPPortProgress.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ServerIPPort.Text = $"{externalIp.ToString()}:25565";
                    IPPortProgress.Visibility = Visibility.Collapsed;
                });
            }

            StorageFolder wrld = null;
            try
            {
                wrld = await folder.GetFolderAsync("world");
                double size = await GetFolderSize(wrld) / 1048576;
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ServerWorldSize.Text = $"{size.ToString("F")} MB";
                    WorldProgress.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ServerWorldSize.Text = "No world yet!";
                    WorldProgress.Visibility = Visibility.Collapsed;
                });
            }
        }


        //          Adding New Server Functions          //
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
        private async void DownloadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            SelectedFolder = await picker.PickSingleFolderAsync();
            if (SelectedFolder != null)
            {
                DownloadFolderText.Text = SelectedFolder.Path;
                JarsView.Visibility = Visibility.Visible;
            }
        }
        private async void LocalFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            SelectedFolder = await picker.PickSingleFolderAsync();
            if (SelectedFolder != null)
            {
                LocalFolderText.Text = SelectedFolder.Path;
            }
        }
        private void NewServerDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if(AdditionPivot.SelectedIndex == 0) // If importing local files
            {
                if(LocalFolderText.Text != "" && ServerNameEntry.Text != "" && ServerVersionEntry.Text != "")
                {
                    Thread t = new Thread(CreateNewServerEntry);
                    t.Start(new Server() { Date = DateTime.Now, Name = ServerNameEntry.Text, FolderPth = LocalFolderText.Text, FolderVersion = ServerVersionEntry.Text });
                }
                else
                {
                    args.Cancel = true;
                }
            }
            else // If downloading new jar
            {
                args.Cancel = true;
            }


            LocalFolderText.Text = "";
            ServerNameEntry.Text = "";
            ServerVersionEntry.Text = "";
        }


        //          File Functions          //
        private async void CreateNewServerEntry(object s)
        {
            Server srv = (Server)s;

            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile listFile = null;
            try
            {
                listFile = await storageFolder.GetFileAsync($"servers.lst");
            }
            catch (Exception)
            {
                try
                {
                    listFile = await storageFolder.CreateFileAsync($"servers.lst");
                }
                catch (Exception)
                {

                }
            }

            string input = await Windows.Storage.FileIO.ReadTextAsync(listFile, Windows.Storage.Streams.UnicodeEncoding.Utf8);
            string serial = JsonConvert.SerializeObject(srv);
            string output = "";
            if (input != "")
            {
                output = $"[{input.Replace("[","").Replace("]","")},{serial}]";
            }
            else
            {
                output = $"[{serial}]";
            }


            await Windows.Storage.FileIO.WriteTextAsync(listFile, output);

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                serverViewModel.Servers.Add(srv);
            });
        }
        private async void LoadServersList()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile listFile = null;
            try
            {
                listFile = await storageFolder.GetFileAsync($"servers.lst");
            }
            catch (Exception)
            {
                return;
            }

            List<Server> servers = JsonConvert.DeserializeObject<List<Server>>(await Windows.Storage.FileIO.ReadTextAsync(listFile, Windows.Storage.Streams.UnicodeEncoding.Utf8));

            foreach (Server server in servers)
            {
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    serverViewModel.Servers.Add(server);
                });
            }
        }
        private async void VerifyPerms()
        {
            StorageFolder folder = null;
            try
            {
                folder = await StorageFolder.GetFolderFromPathAsync("C:\\");
            }
            catch (Exception ex)
            {
                PermsDialog.ShowAsync();
            }
            return;
        }
        private async Task<double> GetFolderSize(StorageFolder folder)
        {
            double TotalBytes = 0.0;

            var queryOptions = new QueryOptions
            {
                FolderDepth = FolderDepth.Deep,
                IndexerOption = IndexerOption.UseIndexerWhenAvailable
            };
            var query = folder.CreateFileQueryWithOptions(queryOptions);
            var allFiles = await query.GetFilesAsync();
            foreach(var file in allFiles)
            {
                var props = await file.GetBasicPropertiesAsync();
                TotalBytes += props.Size;
            }

            return TotalBytes;
        }

        private async void PermsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-settings:appsfeatures-app"));

            CoreApplication.Exit();
        }

        private void PermsDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            CoreApplication.Exit();
        }

        private void PowerIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Thread t = new Thread(ServerHandler);
            t.Start();
        }
        private async void ServerHandler()
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(SelectedServer.FolderPth);
            var files = await folder.GetFilesAsync();
            var server = files.First(o => o.FileType == ".jar");
            if (server != null)
            {
                using (Process compiler = new Process())
                {
                    compiler.StartInfo.FileName = $"{SelectedServer.FolderPth}\\{server.Name}";
                    compiler.StartInfo.Arguments = "java -Xmx8G -Xms8G -jar";
                    compiler.StartInfo.UseShellExecute = false;
                    compiler.StartInfo.RedirectStandardOutput = true;
                    Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", SelectedServer.FolderPth);
                    compiler.Start();

                    Console.WriteLine(compiler.StandardOutput.ReadToEnd());
                    while (true)
                    {
                        await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            DebugConsole.Text += (compiler.StandardOutput.ReadToEnd());
                        });
                        Thread.Sleep(100);
                    }

                    compiler.WaitForExit();
                }
            }
        }
    }
}
