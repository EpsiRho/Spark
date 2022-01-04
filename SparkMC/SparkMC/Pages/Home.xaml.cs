using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json;
using SparkMC.Classes;
using SparkMC.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SparkMC.Pages
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
                this.DispatcherQueue.TryEnqueue(() =>
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
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ServerPowerState.Text = "Offline";
                PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                ServerIPPort.Text = "Loading";
                ConsoleInput.IsEnabled = false;
                ServersGridView.IsEnabled = false;
                DebugConsole.Text = "";
                PowerDisable.Visibility = Visibility.Visible;
            });

            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(SelectedServer.FolderPth);

            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);
            StorageFile prop = null;
            try
            {
                prop = await folder.GetFileAsync("server.properties");
                List<string> properties = (List<string>)await FileIO.ReadLinesAsync(prop, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                string[] port = properties.Find(o => o.Contains("server-port")).Split("=");

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ServerIPPort.Text = $"{externalIp}:{port[1]}";
                });
            }
            catch (Exception)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ServerIPPort.Text = $"{externalIp}:25565";
                });
            }

            this.DispatcherQueue.TryEnqueue(() =>
            {
                ConsoleInput.IsEnabled = true;
                ServersGridView.IsEnabled = true;
                PowerDisable.Visibility = Visibility.Collapsed;
            });
        }


        //          Adding New Server Functions          //
        private void JarsView_ItemInvoked(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewItemInvokedEventArgs args)
        {
            if (((CustomTreeViewItem)args.InvokedItem).Jars.Count != 0)
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
            WinRT.Interop.InitializeWithWindow.Initialize(picker, ProcessHandler.hwnd);

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
            WinRT.Interop.InitializeWithWindow.Initialize(picker, ProcessHandler.hwnd);

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
            if (AdditionPivot.SelectedIndex == 0) // If importing local files
            {
                if (LocalFolderText.Text != "" && ServerNameEntry.Text != "" && ServerVersionEntry.Text != "")
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

            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] dirs = Directory.GetDirectories(path);
            if (!dirs.Contains("Spark"))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync($"{path}\\Spark");
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
                output = $"[{input.Replace("[", "").Replace("]", "")},{serial}]";
            }
            else
            {
                output = $"[{serial}]";
            }


            await Windows.Storage.FileIO.WriteTextAsync(listFile, output);

            this.DispatcherQueue.TryEnqueue(() =>
            {
                serverViewModel.Servers.Add(srv);
            });
        }
        private async void LoadServersList()
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string[] dirs = Directory.GetDirectories(path);
            if (!dirs.Contains("Spark"))
            {
                System.IO.Directory.CreateDirectory($"{path}\\Spark");
            }

            StorageFolder storageFolder = await StorageFolder.GetFolderFromPathAsync($"{path}\\Spark");
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
                this.DispatcherQueue.TryEnqueue(() =>
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
            foreach (var file in allFiles)
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
            var red = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
            if (((SolidColorBrush)PowerIcon.Foreground).Color.R == red.Color.R)
            {
                ServersGridView.IsEnabled = false;
                Thread t = new Thread(ServerHandler);
                t.Start();
            }
            else
            {
                ProcessHandler.proc.StandardInput.WriteLine("stop");
            }
        }
        private async void ServerHandler()
        {
            StorageFolder folder = null;
            try 
            { 
                folder = await StorageFolder.GetFolderFromPathAsync(SelectedServer.FolderPth);
            }
            catch (Exception ex)
            {
                return;
            }
            var files = await folder.GetFilesAsync();
            var server = files.First(o => o.FileType == ".jar");
            if (server != null)
            {
                ProcessHandler.proc = new Process();
                var javaExecutable = "C:\\Program Files\\Java\\jdk-17.0.1\\bin\\java.exe";
                var arguments = $" -Xmx8G -Xms8G -jar {server.Name} nogui";
                var processStartInfo = new ProcessStartInfo(javaExecutable, arguments);
                ProcessHandler.proc.StartInfo = processStartInfo;
                ProcessHandler.proc.StartInfo.WorkingDirectory = folder.Path;
                ProcessHandler.proc.StartInfo.UseShellExecute = false;
                ProcessHandler.proc.StartInfo.CreateNoWindow = true;
                ProcessHandler.proc.StartInfo.RedirectStandardOutput = true;
                ProcessHandler.proc.StartInfo.RedirectStandardError = true;
                ProcessHandler.proc.StartInfo.RedirectStandardInput = true;
                ProcessHandler.proc.Start();
            }

            if(!ProcessHandler.proc.HasExited)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 255, 0));
                    ServerPowerState.Text = "Online";
                });
            }
            Thread t = new Thread(ParseJar);
            t.Start();

            ProcessHandler.proc.WaitForExit();

            this.DispatcherQueue.TryEnqueue(() =>
            {
                ServersGridView.IsEnabled = true;
                PowerIcon.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                ServerPowerState.Text = "Offline";
            });
        }

        private void ScrollToBottom(TextBox textBox)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                var grid = (Grid)VisualTreeHelper.GetChild(textBox, 0);
                for (var i = 0; i <= VisualTreeHelper.GetChildrenCount(grid) - 1; i++)
                {
                    object obj = VisualTreeHelper.GetChild(grid, i);
                    if (!(obj is ScrollViewer)) continue;
                    ((ScrollViewer)obj).ChangeView(0.0f, ((ScrollViewer)obj).ExtentHeight, 1.0f, true);
                    break;
                }
            });
        }

        private async void ParseJar()
        {

            while (!ProcessHandler.proc.HasExited)
            {
                try
                {
                    string output = await ProcessHandler.GetStandardOut();
                    this.DispatcherQueue.TryEnqueue( async () =>
                    {
                        try
                        {
                            if (ProcessHandler.proc.HasExited)
                            {
                                DebugConsole.Text += await ProcessHandler.GetError();
                                return;
                            }
                            DebugConsole.Text += $"{output}\n";
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Text += $"[SPARK] {ex.Message}\n";
                        }
                    });
                }
                catch(Exception ex)
                {
                    this.DispatcherQueue.TryEnqueue(async () =>
                    {
                        DebugConsole.Text += await ProcessHandler.GetError();
                    });
                }

                ScrollToBottom(DebugConsole);
                Thread.Sleep(200);
            }
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                DebugConsole.Text += await ProcessHandler.GetError();
            });
        }

        private void ConsoleInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    if (ConsoleInput.Text != "")
                    {
                        if (!ProcessHandler.proc.HasExited)
                        {
                            ProcessHandler.proc.StandardInput.WriteLine(ConsoleInput.Text);
                        }
                        ConsoleInput.Text = "";
                    }
                }
            }
            catch (Exception) 
            {
                ConsoleInput.Text = "";
            }
        }


        // Animation vars
        Compositor _compositor = CompositionTarget.GetCompositorForCurrentThread();
        SpringVector3NaturalMotionAnimation _springAnimation;

        // Animation Functions
        private void CreateOrUpdateSpringAnimation(float finalValue)
        {
            if (_springAnimation == null)
            {
                _springAnimation = _compositor.CreateSpringVector3Animation();
                _springAnimation.Target = "Scale";

            }

            _springAnimation.FinalValue = new Vector3(finalValue);
        }
        private void PowerButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            // Scale up to 1.5
            CreateOrUpdateSpringAnimation(1.05f);

            (sender as UIElement).CenterPoint = new Vector3((float)(PowerButton.ActualWidth / 2.0), (float)(PowerButton.ActualHeight / 2.0), 1f);
            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void PowerButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).CenterPoint = new Vector3((float)(PowerButton.ActualWidth / 2.0), (float)(PowerButton.ActualHeight / 2.0), 1f);
            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void PowerButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (SelectedServer == null)
            {
                return;
            }
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(0.9f);

            (sender as UIElement).CenterPoint = new Vector3((float)(PowerButton.ActualWidth / 2.0), (float)(PowerButton.ActualHeight / 2.0), 1f);
            (sender as UIElement).StartAnimation(_springAnimation);
        }

        private void PowerButton_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (SelectedServer == null)
            {
                return;
            }
            // Scale back down to 1.0
            CreateOrUpdateSpringAnimation(1.0f);

            (sender as UIElement).CenterPoint = new Vector3((float)(PowerButton.ActualWidth / 2.0), (float)(PowerButton.ActualHeight / 2.0), 1f);
            (sender as UIElement).StartAnimation(_springAnimation);
        }
    }
}
