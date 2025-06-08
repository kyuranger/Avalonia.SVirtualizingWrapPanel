using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVirtualizingWrapPanel
{
    public partial class Model: ObservableObject
    {
        [ObservableProperty]
        int _Index = 0;

        [ObservableProperty]
        IBrush _Color= new SolidColorBrush();
    }
}
