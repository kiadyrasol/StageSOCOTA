using System;
using System.Windows;

namespace GestionProjetSocota.Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WebView.Source = new Uri("http://localhost:5009/");
        }
    }
}