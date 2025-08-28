using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace SVirtualizingWrapPanel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            ViewModel _vm = new ViewModel();
            InitializeComponent();
            this.DataContext = _vm;           
        }
    }
}