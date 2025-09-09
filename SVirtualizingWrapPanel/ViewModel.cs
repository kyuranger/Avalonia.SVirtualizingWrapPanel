using Avalonia.Collections;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
                LargeScaleDataAvaloniaList.Add(_model);
            }
            for (int i = 0; i <= 8; i++)
            {
                Model _model = new Model();
                _model.Color = new SolidColorBrush(Color.FromArgb(255, (byte)(i % 256), 100, 150));
                _model.Index = i;
                SmallScaleDataAvaloniaList.Add(_model);
            }
            SelectedItem = SmallScaleDataAvaloniaList[7];



            List<Model> _waterfallCache = new List<Model>();
            int _waterfallSkipCount = 0;
            for (int i = 0; i <= 30000; i++)
            {
                Model _model = new Model();
                _model.Color = new SolidColorBrush(Color.FromArgb(255, (byte)(i % 256), 100, 150));
                _model.Index = i;
                _waterfallCache.Add(_model);
            }
            LoadMoreCommand = new RelayCommand(async () => { 
                WaterfallAvaloniaList.AddRange(_waterfallCache.Skip(_waterfallSkipCount).Take(50));
                _waterfallSkipCount += 50;
            });
        }

        [ObservableProperty]
        Model _SelectedItem = new Model();

        [ObservableProperty]
        AvaloniaList<Model> _LargeScaleDataAvaloniaList = new AvaloniaList<Model>();

        [ObservableProperty]
        AvaloniaList<Model> _SmallScaleDataAvaloniaList = new AvaloniaList<Model>();

        [ObservableProperty]
        AvaloniaList<Model> _WaterfallAvaloniaList=new AvaloniaList<Model>();

        
        public RelayCommand LoadMoreCommand { get; }
    }
}
