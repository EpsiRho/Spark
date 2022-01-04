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
using Windows.Web.Http;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SparkMC.Pages
{
    //                                   To-Do                                  //
    // * Handle recieving a Server object when navigating to this page          //
    // *                                                                        //
    public sealed partial class Home : Page
    {
        //          Page Vars          //
        private ObservableCollection<CustomTreeViewItem> TreeViewSource { get; set; } // Holds the server jar versions for the download tree
        private ServerViewModel serverViewModel { get; set; } // Holds the servers listed in servers.lst
        private StorageFolder SelectedFolder { get; set; } // The laast selected folder, used in adding new local folders
        public Server SelectedServer { get; set; } // The currently selected server



        //          Page Startup          //
        public Home() // Initalizer function for this page 
        {
            this.InitializeComponent();

            serverViewModel = new ServerViewModel(); // Instantiates this page's ServerViewModel
            TreeViewSource = new ObservableCollection<CustomTreeViewItem>(); // Instantiates the treeview collection

            Thread t = new Thread(AsyncStartingFunctions); // Start running startup code that should be done in the background
            t.Start();

            Thread d = new Thread(LoadServersList); // Load in added servers from servers.lst
            d.Start();

            //VerifyPerms(); // Not needed for now, but used in the case that this application is packaged and needs filesystem access.
        }
        private async void AsyncStartingFunctions() // Some code that should run in the background when this page loads 
        {
            // Get the server jar listing from the serverjars.com API and add it to the treeview collection
            var items = ServerJarLookup.BuildTreeView();
            foreach (var item in items)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    TreeViewSource.Add(item);
                });
            }
        }



        //          Server List Interaction Handling          //
        private void ServersGridView_ItemClick(object sender, ItemClickEventArgs e) // Switch the currently selected server when a list item is clicked 
        {
            SelectedServer = (Server)e.ClickedItem;
            Thread t = new Thread(GetServerInfo);
            t.Start();
        }
        // To-Do: Right click servers to choose to edit / remove / remove with files.



        //          Server Addition Interaction Handling          //
        private void AddNewServerButton_Click(object sender, RoutedEventArgs e) // Show the dialog for adding a new server to the app list 
        {
            NewServerDialog.ShowAsync();
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
                if (JarsView.SelectedItem != null)
                {
                    var version = JarsView.SelectedItem as CustomTreeViewItem;
                    var versionNode = JarsView.SelectedNode;
                    var type = versionNode.Parent.Content as CustomTreeViewItem;
                    DownloadServerJar(new string[] { type.Type, version.Type });
                }
                else
                {
                    args.Cancel = true;
                }
            }


            LocalFolderText.Text = "";
            ServerNameEntry.Text = "";
            ServerVersionEntry.Text = "";
        }




        //          File Functions          //
        private async void GetServerInfo() // Gets info about a server to populate the home page with 
        {
            // Call the UI thread to lock options that should not change while loading server info
            // Also resets already populated fields to a default while new info is loaded
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

            // Get the folder from the currently selected server's path
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(SelectedServer.FolderPth);

            // Gets the user's public ip
            string externalIpString = new WebClient().DownloadString("http://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);

            // Get the server's set port
            // If an error occurs, then it's likely there is no properties file yet, so we just choose the default 25565
            StorageFile prop = null;
            try
            {
                prop = await folder.GetFileAsync("server.properties");
                List<string> properties = (List<string>)await FileIO.ReadLinesAsync(prop, Windows.Storage.Streams.UnicodeEncoding.Utf8);
                string[] port = properties.Find(o => o.Contains("server-port")).Split("=");

                // Call the UI thread to set the IP/Port display
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

            // Call the UI thread to unlock parts of the page
            this.DispatcherQueue.TryEnqueue(() =>
            {
                ConsoleInput.IsEnabled = true;
                ServersGridView.IsEnabled = true;
                PowerDisable.Visibility = Visibility.Collapsed;
            });
        }
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
        public async void DownloadServerJar(string[] args)
        {
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string specific = $"{path}\\Spark\\{args[0]}-{args[1]}";
            if (Directory.Exists(specific))
            {
                return;
            }
            Directory.CreateDirectory(specific);


            this.DispatcherQueue.TryEnqueue(() =>
            {
                DownloadText.Text = "Preparing";
                DownloadProgress.Value = 0;
                DownloadProgress.IsIndeterminate = true;
                DownloadPopup.Visibility = Visibility.Visible;
            });

            var uri = new Uri($"https://serverjars.com/api/fetchJar/{args[0]}/{args[1]}");
            HttpClient client = new HttpClient();
            var response = client.GetAsync(uri);
            this.DispatcherQueue.TryEnqueue(() =>
            {
                DownloadText.Text = "Downloading";
                DownloadProgress.Value = 0;
                DownloadProgress.IsIndeterminate = false;
            });
            response.Progress = async (res, progressInfo) =>
            {
                try
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        DownloadProgress.Value = (double)(100.0 * progressInfo.BytesReceived / 40000000);
                    });
                }
                catch (Exception)
                {

                }
            };
            var httpResponse = await response;

            try
            {
                httpResponse.EnsureSuccessStatusCode();
            }
            catch (Exception)
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    DownloadText.Text = "Failed!";
                    DownloadProgress.Value = 0;
                });
                return;
            }

            this.DispatcherQueue.TryEnqueue(() =>
            {
                DownloadText.Text = "Saving";
                DownloadProgress.Value = 0;
            });
            

            using (var fs = new FileStream($"{specific}\\server.jar", FileMode.CreateNew))
            {
                var pr = httpResponse.Content.WriteToStreamAsync(fs.AsOutputStream());
                pr.Progress = async (res, progressInfo) =>
                {
                    try
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            DownloadProgress.Value = (double)(progressInfo);
                        });
                    }
                    catch (Exception)
                    {

                    }
                };
                var thing = await pr;
            }

            CreateNewServerEntry(new Server() { Name = $"{args[0]} {args[1]}", Date = DateTime.Now, FolderPth = $"{path}\\Spark\\{args[0]}-{args[1]}", FolderVersion = args[1] });

            this.DispatcherQueue.TryEnqueue(() =>
            {
                DownloadPopup.Visibility = Visibility.Collapsed;
            });
        }
        public async Task<bool> CheckEulaStatus()
        {
            if (File.Exists($"{SelectedServer.FolderPth}\\eula.txt"))
            {
                string eula = File.ReadAllText($"{SelectedServer.FolderPth}\\eula.txt");
                if(eula.Contains("eula=false"))
                {
                    await EULADialog.ShowAsync();
                    return false;
                }
                return true;
            }
            else
            {
                await EULADialog.ShowAsync();
                return false;
            }
        }


        //          Sever Jar / Console Handling          //
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



        //          Misc UI Interactions          //
        private async void PowerIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (await CheckEulaStatus())
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
        }



        //          Animation Vars          //
        Compositor _compositor = CompositionTarget.GetCompositorForCurrentThread();
        SpringVector3NaturalMotionAnimation _springAnimation;

        //          Animation Handling          //
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
            CreateOrUpdateSpringAnimation(1.03f);

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


        //          Not Used But Might Need Later          //
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

        private void EULADialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            File.WriteAllText($"{SelectedServer.FolderPth}\\eula.txt", "eula=true");
            ServersGridView.IsEnabled = false;
            Thread t = new Thread(ServerHandler);
            t.Start();
        }
    }
}
