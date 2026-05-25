// GameWindow.xaml.cs
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PearlDivers.Engine;

namespace PearlDivers
{
    public partial class GameWindow : Window
    {
        private GameEngine engine;

        public GameWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            engine = new GameEngine(hwnd, (int)this.ActualWidth, (int)this.ActualHeight, () =>
            {
                this.Close();
            });
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            engine?.HandleKey(e.Key, true);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            engine?.HandleKey(e.Key, false);
        }
    }
}