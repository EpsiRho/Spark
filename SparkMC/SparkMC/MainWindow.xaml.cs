using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using SparkMC.Classes;
using SparkMC.Pages;
using System.Linq;

namespace SparkMC
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            ProcessHandler.hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this); // Get the window handle for future use.

            contentFrame.Navigate(typeof(Home)); // Navigate the main Frame to the home page

            NavPanel.SelectedItem = NavPanel.MenuItems.First(); // Select the home item on the left navigation bar

            NavPanel.IsPaneOpen = false; // Make sure the pane is closed on start
        }
    }
}
