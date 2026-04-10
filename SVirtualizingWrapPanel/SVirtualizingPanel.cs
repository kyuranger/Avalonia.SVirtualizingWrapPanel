using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SVirtualizingWrapPanel
{
    public abstract class SVirtualizingPanel : VirtualizingPanel, IScrollSnapPointsInfo
    {

        public static readonly StyledProperty<ICommand?> LoadMoreProperty =
  AvaloniaProperty.Register<SVirtualizingPanel, ICommand?>(nameof(LoadMore));

        public ICommand? LoadMore
        {
            get { return GetValue(LoadMoreProperty); }
            set { SetValue(LoadMoreProperty, value); }
        }


        public static readonly StyledProperty<Boolean> IsReachEndProperty =
AvaloniaProperty.Register<SVirtualizingPanel, Boolean>(nameof(IsReachEnd));

        public Boolean IsReachEnd
        {
            get { return GetValue(IsReachEndProperty); }
            set { SetValue(IsReachEndProperty, value); }
        }
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            ScrollToLoadMore();
        }

        public abstract bool AreHorizontalSnapPointsRegular { get; set; }
        public abstract bool AreVerticalSnapPointsRegular { get; set; }

        public abstract event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public abstract event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

        public abstract IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment);
        public abstract double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset);

        public Boolean IsLoadingMore { get; protected set; } = false;

        protected int _currentIndex = 0;
        protected int _lastIndex = 0;

        protected abstract void ScrollToLoadMore();
        protected void OnLoadMore()
        {
            if (IsLoadingMore || IsReachEnd)
                return;

            IsLoadingMore = true;
            try
            {
                LoadMore?.Execute(null);
            }
            catch (Exception)
            {
                // 可以加日志记录
            }
            finally
            {
                IsLoadingMore = false;
            }
        }
        protected class ElementRenderModel
        {
            public Control? Control { get; set; } = null;
            public double Left { get; set; } = 0;
            public double Top { get; set; } = 0;

            public double Width { get; set; } = 0;

            public double Height { get; set; } = 0;
            public Boolean IsRendered { get; set; } = false;

        }
        protected Dictionary<int, ElementRenderModel> _elementDictionary = new Dictionary<int, ElementRenderModel>();
        protected Rect _effectiveViewport = new Rect(0, -1, 0, 0);
        protected double _maximumItemHeight = 0.0;
        protected double _maximumItemWidth = 0.0;
        protected Size _panelSize = new Size();
        protected double _currentLineWidth = 0;
        protected double _currentLineHeight = 0;
        protected const double VirtualizationCacheLength = 300;

        protected abstract int RenderElements(int startIndex);
        //protected abstract Boolean IsMeasureFinished(Control control);

        protected void UpdateMaximumElementSize(Size desiredSize)
        {
            _maximumItemWidth = Math.Max(_maximumItemWidth, desiredSize.Width);
            _maximumItemHeight = Math.Max(_maximumItemHeight, desiredSize.Height);
        }

        protected void ClearElementRange(int startIndex)
        {
            if (_elementDictionary.Count == 0)
            {
                return;
            }

            var keysToRemove = _elementDictionary.Keys.Where(key => key >= startIndex).OrderBy(key => key).ToList();
            foreach (var key in keysToRemove)
            {
                if (_elementDictionary.TryGetValue(key, out var element))
                {
                    if (element.Control is { } control)
                    {
                        RemoveInternalChild(control);
                        ItemContainerGenerator?.ClearItemContainer(control);
                    }
                }

                _elementDictionary.Remove(key);
            }
        }

        protected double GetVerticalViewportStart()
        {
            return Math.Max(0, _effectiveViewport.Top - Math.Max(VirtualizationCacheLength, _maximumItemHeight));
        }

        protected double GetVerticalViewportEnd()
        {
            return _effectiveViewport.Top + _effectiveViewport.Height + Math.Max(VirtualizationCacheLength, _maximumItemHeight);
        }

        protected double GetHorizontalViewportStart()
        {
            return Math.Max(0, _effectiveViewport.Left - Math.Max(VirtualizationCacheLength, _maximumItemWidth));
        }

        protected double GetHorizontalViewportEnd()
        {
            return _effectiveViewport.Left + _effectiveViewport.Width + Math.Max(VirtualizationCacheLength, _maximumItemWidth);
        }

        protected Control CreateVirtualizingElement(object item, int index, string recycleKey)
        {
            var _generator = ItemContainerGenerator!;
            var _container = _generator.CreateContainer(item, index, recycleKey);
            _generator.PrepareItemContainer(_container, item, index);
            AddInternalChild(_container);
            _generator.ItemContainerPrepared(_container, item, index);

            _container.Measure(Size.Infinity);
            UpdateMaximumElementSize(_container.DesiredSize);
            return _container;
        }
    }

}
