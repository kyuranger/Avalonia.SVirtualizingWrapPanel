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

namespace SVirtualizingWrapPanel
{
    public abstract class SVirtualizingPanel : VirtualizingPanel, IScrollSnapPointsInfo
    {
        public abstract bool AreHorizontalSnapPointsRegular { get; set; }
        public abstract bool AreVerticalSnapPointsRegular { get; set; }

        public abstract event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public abstract event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

        public abstract IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment);
        public abstract double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset);

        public Boolean HasMoreItems { get; set; } = true;
        public Boolean IsLoadingMore { get; protected set; } = false;
        public EventHandler? LoadMoreRequested;

        public abstract Boolean IsPauseLoadMoreRequested { get; set; }
        protected int _CurrentIndex = 0;
        protected int _LastIndex = 0;     
        protected void LoadMore()
        {
            if (IsLoadingMore || !HasMoreItems)
                return;

            IsLoadingMore = true;
            try
            {
                LoadMoreRequested?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // 可以加日志记录
            }
            finally
            {
                IsLoadingMore = false;
            }
        }
        protected abstract void DetermineWhetherToLoadMore();
        protected class ElementRenderModel
        {
            public Control? Control { get; set; } = null;
            public double Width { get; set; } = 0;
            public double Height { get; set; } = 0;
            public double Left { get; set; } = 0;
            public double Top { get; set; } = 0;
            public Boolean IsRendered { get; set; } = false;

        }
        protected Dictionary<int, ElementRenderModel> _ElementDictionary = new Dictionary<int, ElementRenderModel>();
        protected Rect _EffectiveViewport = new Rect(0, -1, 0, 0);
        protected double _MaximumItemHeight = 0.0;
        protected double _MaximumItemWidth = 0.0;
        protected Size _PanelSize = new Size();
        protected double _CurrentLineWidth = 0;
        protected double _CurrentLineHeight = 0;

        protected abstract int RenderElements(int startIndex);
        protected abstract Boolean IsMeasureFinished(Control control);

        protected Control CreateVirtualizingElement(object item, int index, string recycleKey)
        {
            var generator = ItemContainerGenerator!;
            var container = generator.CreateContainer(item, index, recycleKey);
            generator.PrepareItemContainer(container, item, index);
            AddInternalChild(container);
            generator.ItemContainerPrepared(container, item, index);

            container.Measure(Size.Infinity);
            return container;
        }

    }
}
