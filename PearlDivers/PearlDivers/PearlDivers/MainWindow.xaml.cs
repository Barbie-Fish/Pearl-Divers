using System;
using System.Windows;

namespace PearlDivers
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            var gameWindow = new GameWindow();
            this.Hide();
            gameWindow.ShowDialog();
            this.Show();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}