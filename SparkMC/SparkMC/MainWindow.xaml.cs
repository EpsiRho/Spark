using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SparkMC.Classes;
using SparkMC.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SparkMC
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        Process proc;

        public MainWindow()
        {
            this.InitializeComponent();
            ProcessHandler.hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            contentFrame.Navigate(typeof(Home));
            NavPanel.SelectedItem = NavPanel.MenuItems.First();
            NavPanel.IsPaneOpen = false;
        }

        //private async void myButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var picker = new Windows.Storage.Pickers.FolderPicker();
        //    // Get the current window's HWND by passing in the Window object
        //    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

        //    // Associate the HWND with the file picker
        //    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        //    picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        //    picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        //    picker.FileTypeFilter.Add("*");

        //    var SelectedFolder = await picker.PickSingleFolderAsync();
        //    if (SelectedFolder != null)
        //    {
        //        Thread t = new Thread(ThreadedOpen);
        //        t.Start(SelectedFolder);
        //    }
        //}

        //private async void ThreadedOpen(object f)
        //{
        //    StorageFolder folder = (StorageFolder)f;

        //    var files = await folder.GetFilesAsync();
        //    var server = files.First(o => o.FileType == ".jar");
        //    if (server != null)
        //    {
        //        proc = new Process();
        //        var javaExecutable = "C:\\Program Files\\Java\\jdk-17.0.1\\bin\\java.exe";
        //        var arguments = $" -Xmx8G -Xms8G -jar {server.Name} nogui";
        //        var processStartInfo = new ProcessStartInfo(javaExecutable, arguments);
        //        proc.StartInfo = processStartInfo;
        //        proc.StartInfo.WorkingDirectory = folder.Path;
        //        proc.StartInfo.UseShellExecute = false;
        //        proc.StartInfo.CreateNoWindow = true;
        //        proc.StartInfo.RedirectStandardOutput = true;
        //        proc.StartInfo.RedirectStandardError = true;
        //        proc.StartInfo.RedirectStandardInput = true;
        //        //proc.StartInfo.FileName = server.Path;
        //        //proc.StartInfo.Arguments = "java -Xmx8G -Xms8G -jar server.jar nogui";
        //        //proc.StartInfo.UseShellExecute = false;
        //        //proc.StartInfo.RedirectStandardOutput = true;
        //        //Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", folder.Path);
        //        proc.Start();

        //        bool fuck = true;
        //        while (fuck)
        //        {
        //            try
        //            {
        //                string output = proc.StandardOutput.ReadLine();
        //                this.DispatcherQueue.TryEnqueue(() =>
        //                {
        //                    try
        //                    {
        //                        if (proc.HasExited)
        //                        {
        //                            DebugBox.Text += "[SPARK] What the fuck.\n";
        //                            DebugBox.Text += proc.StandardError.ReadToEnd();
        //                            fuck = false;
        //                            return;
        //                        }
        //                        DebugBox.Text += $"{output}\n";
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        DebugBox.Text += $"[SPARK] {ex.Message}\n";
        //                    }
        //                });
        //            }
        //            catch (Exception ex)
        //            {
        //                DebugBox.Text += $"[SPARK] {ex.Message}\n";
        //            }
        //            Thread.Sleep(200);
        //        }

        //        proc.WaitForExit();
        //    }
        //}

        //private void GetOutputButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (proc.HasExited)
        //    {
        //        DebugBox.Text += "What the fuck.\n";
        //        DebugBox.Text += proc.StandardError.ReadToEnd();
        //        return;
        //    }
        //    DebugBox.Text += proc.StandardOutput.ReadToEnd();
        //}

        //private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
        //{
        //    if(e.Key == Windows.System.VirtualKey.Enter)
        //    {
        //        if(InputBox.Text != "")
        //        {
        //            proc.StandardInput.WriteLine(InputBox.Text);
        //        }
        //    }
        //}
    }
}
