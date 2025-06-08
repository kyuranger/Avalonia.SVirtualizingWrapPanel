using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace SVirtualizingWrapPanel
{
    public partial class ViewModel: ObservableObject
    {
        public ViewModel() {            
            for (int i = 0; i < 50000; i++)
            {
                Model _model = new Model();
                _model.Color = new SolidColorBrush(Color.FromArgb(255, (byte)(i % 256), 100, 150));
                _model.Index = i;
                LargeScaleDataOC.Add(_model);
            }
            for (int i = 0; i <= 8; i++)
            {
                Model _model = new Model();
                _model.Color = new SolidColorBrush(Color.FromArgb(255, (byte)(i % 256), 100, 150));
                _model.Index = i;
                SmallScaleDataOC.Add(_model);
            }
            SelectedItem = SmallScaleDataOC[5];
        }

        [ObservableProperty]
        Model _SelectedItem = new Model();

        [ObservableProperty]
        ObservableCollection<Model> _LargeScaleDataOC = new ObservableCollection<Model>();

        [ObservableProperty]
        ObservableCollection<Model> _SmallScaleDataOC = new ObservableCollection<Model>();
    }
}
