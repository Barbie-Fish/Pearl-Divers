using System;
using System.Windows;
using System.Windows.Interop;
using PearlDivers.Engine;

namespace PearlDivers
{
    public partial class MainWindow : Window
    {
        private GameEngine engine;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            engine = new GameEngine(hwnd, (int)this.ActualWidth, (int)this.ActualHeight);

            this.KeyDown += (s, args) => engine.HandleKey(args.Key, true);
            this.KeyUp += (s, args) => engine.HandleKey(args.Key, false);
        }
    }
}