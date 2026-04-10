using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SVirtualizingWrapPanel
{
    public class SVirtualizingUniformGrid : SVirtualizingPanel
    {

        public static readonly StyledProperty<int> ColumnsProperty =
  AvaloniaProperty.Register<SVirtualizingUniformGrid, int>(nameof(Columns), 0);

        public int Columns
        {
            get { return GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        public static readonly StyledProperty<double> RowHeightProperty =
 AvaloniaProperty.Register<SVirtualizingUniformGrid, double>(nameof(RowHeight), double.PositiveInfinity);

        public double RowHeight
        {
            get { return GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        public static readonly StyledProperty<double> RowSpacingProperty =
 AvaloniaProperty.Register<SVirtualizingUniformGrid, double>(nameof(RowSpacing), 0);

        public double RowSpacing
        {
            get { return GetValue(RowSpacingProperty); }
            set { SetValue(RowSpacingProperty, value); }
        }

        public static readonly StyledProperty<double> ColumnSpacingProperty =
AvaloniaProperty.Register<SVirtualizingUniformGrid, double>(nameof(ColumnSpacing), 0);

        public double ColumnSpacing
        {
            get { return GetValue(ColumnSpacingProperty); }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        public override bool AreHorizontalSnapPointsRegular { get => Columns > 0 && Bounds.Width > 0; set { } }
        public override bool AreVerticalSnapPointsRegular { get => !double.IsPositiveInfinity(RowHeight) && RowHeight > 0; set { } }

        public override event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public override event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

        public SVirtualizingUniformGrid()
        {
            EffectiveViewportChanged += SVirtualizingUniformGrid_EffectiveViewportChanged;
        }
        Boolean _isLoadRendered = false;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == BoundsProperty ||
                change.Property == ColumnsProperty ||
                change.Property == ColumnSpacingProperty)
            {
                HorizontalSnapPointsChanged?.Invoke(this, CreateSnapPointsChangedEventArgs());
            }

            if (change.Property == BoundsProperty ||
                change.Property == ColumnsProperty ||
                change.Property == RowHeightProperty ||
                change.Property == RowSpacingProperty)
            {
                VerticalSnapPointsChanged?.Invoke(this, CreateSnapPointsChangedEventArgs());
            }
        }

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (!_isLoadRendered)
            {
                RenderElements(_currentIndex);
            }
        }

        private void SVirtualizingUniformGrid_EffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            //Debug.WriteLine(e.EffectiveViewport.Height);
            //Debug.WriteLine(e.EffectiveViewport.Top);            
            if (e.EffectiveViewport.Top == -1 || e.EffectiveViewport.Width == 0 || e.EffectiveViewport.Height == 0)
            {
                return;
            }
            if (e.EffectiveViewport.Top != _effectiveViewport.Top || e.EffectiveViewport.Width != _effectiveViewport.Width || e.EffectiveViewport.Height != _effectiveViewport.Height)
            {
                _effectiveViewport = e.EffectiveViewport;
                //Debug.WriteLine($"Top:{_EffectiveViewport.Top}");
                #region//获取进入渲染位置的第一个index
                var viewportStart = GetVerticalViewportStart();
                var _firstIndex = 0;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (_elementDictionary.TryGetValue(i, out var _element))
                    {
                        if (_element.Top + _element.Height > viewportStart || _element.Top + _maximumItemHeight > viewportStart)
                        {
                            _firstIndex = i;
                            break;
                        }
                    }
                }
                //Debug.WriteLine("firstIndex:" + _firstIndex);
                #endregion
                #region//获取该进行渲染的第一个index
                var _startIndex = 0;
                for (int i = _firstIndex; i >= 0; i--)
                {
                    if (_elementDictionary.TryGetValue(i, out var _element))
                    {
                        if (_element.Left == 0)
                        {
                            _startIndex = i;
                            break;
                        }
                    }
                }
                _currentIndex = _startIndex;
                //Debug.WriteLine("startIndex:" + _startIndex);
                #endregion
                #region//正式渲染                               
                _lastIndex = RenderElements(_startIndex);
                //Debug.WriteLine("lastIndex:" + _LastIndex);
                #endregion
                #region//回收其他元素
                for (int i = 0; i < Items.Count; i++)
                {
                    if (i < _startIndex || i > _lastIndex)
                    {
                        if (_elementDictionary.TryGetValue(i, out var _element))
                        {
                            if (_element.Control is { } && ItemContainerGenerator is { })
                            {
                                RemoveInternalChild(_element.Control);
                                ItemContainerGenerator.ClearItemContainer(_element.Control);
                                _element.Control = null;
                                _element.IsRendered = false;
                                //Debug.WriteLine($"回收{i}");
                            }
                        }
                    }
                }
                #endregion
                InvalidateMeasure();
                InvalidateArrange();
                ScrollToLoadMore();
                _isLoadRendered = true;
            }
            else
            {
                _effectiveViewport = e.EffectiveViewport;
            }
        }

        protected override void ScrollToLoadMore()
        {
            if (Items.Count == 0)
            {
                OnLoadMore();
                return;
            }

            if (_effectiveViewport.Top < 0 || _effectiveViewport.Width <= 0 || _effectiveViewport.Height <= 0 || _panelSize.Height <= 0)
            {
                return;
            }

            if (_effectiveViewport.Top + _effectiveViewport.Height >= _panelSize.Height - 300)
            {
                OnLoadMore();
            }
        }




        protected override int RenderElements(int startIndex)
        {
            if (Columns == 0)
            {
                return 0;
            }
            var _elementWidth = (Bounds.Width - (Columns - 1) * ColumnSpacing) / Columns;
            var _lineIndex = 0;
            var _endIndex = Items.Count - 1;
            var _index = startIndex;
            //Debug.WriteLine("_maxLineWidth" + _maxLineWidth);
            double _maxLineWidth = Bounds.Width;
            double _maxLineHeight = 0.0;
            if (!double.IsPositiveInfinity(RowHeight))
            {
                var rowIndex = startIndex / Columns;
                var columnIndex = startIndex % Columns;
                _currentLineHeight = rowIndex * (RowHeight + RowSpacing);
                _currentLineWidth = columnIndex == 0 ? 0 : columnIndex * (_elementWidth + ColumnSpacing);
                _lineIndex = columnIndex;
                _maxLineHeight = RowHeight;
            }
            else if (_elementDictionary.TryGetValue(_index, out var _firstElement))
            {
                _currentLineHeight = _firstElement.Top;
                _currentLineWidth = _firstElement.Left;
            }
            else
            {
                _currentLineHeight = 0;
                _currentLineWidth = 0;
            }
            #region//先计算需渲染的每个控件所需的空间          
            if (double.IsPositiveInfinity(RowHeight))
            {
                for (int i = startIndex; i < Items.Count; i++)
                {
                    var _item = Items[i];
                    if (_item is { })
                    {
                        Control? _element = null;
                    if (!_elementDictionary.TryGetValue(i, out var _value))
                    {
                        _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                        var _newValue = new ElementRenderModel();
                        CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _newValue, _elementWidth, _element.DesiredSize.Height, _element.DesiredSize.Height);

                        _newValue.Control = _element;
                        _newValue.IsRendered = true;

                            _elementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                        _element = _value.Control;
                        if (_element is { })
                        {
                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, _element.DesiredSize.Height, _element.DesiredSize.Height);
                        }
                        else
                        {
                            _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, _element.DesiredSize.Height, _element.DesiredSize.Height);
                            _value.Control = _element;
                            _value.IsRendered = true;
                        }
                        }
                        if (_effectiveViewport.Top >= 0 && _currentLineHeight > GetVerticalViewportEnd())
                        {
                            _endIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = startIndex; i < Items.Count; i++)
                {
                    var _item = Items[i];
                    if (_item is { })
                    {
                        Control? _element = null;
                    if (!_elementDictionary.TryGetValue(i, out var _value))
                    {
                        _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                        var _newValue = new ElementRenderModel();
                        CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _newValue, _elementWidth, RowHeight, RowHeight);

                        _newValue.Control = _element;
                        _newValue.IsRendered = true;

                            _elementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                        _element = _value.Control;
                        if (_element is { })
                        {
                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, RowHeight, RowHeight);
                        }
                        else
                        {
                            _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, RowHeight, RowHeight);
                            _value.Control = _element;
                            _value.IsRendered = true;
                        }
                        }
                        if (_effectiveViewport.Top >= 0 && _currentLineHeight > GetVerticalViewportEnd())
                        {
                            _endIndex = i;
                            break;
                        }
                    }
                }
            }
            #endregion

            _panelSize = new Size(Bounds.Width, CalculatePanelHeight(_currentLineHeight + _maxLineHeight));
            #region//正式Measure自身
            InvalidateMeasure();
            #endregion
            #region//正式Arrange自身
            InvalidateArrange();
            #endregion                     
            return _endIndex;
        }


        void CalculatingItemPosition(ref double maxLineHeight, ref int lineIndex, int index, ElementRenderModel value, double width, double height, double desiredHeight)
        {
            if (lineIndex == Columns)
            {
                lineIndex = 0;
                _currentLineHeight += maxLineHeight + RowSpacing;
                _currentLineWidth = 0;
                maxLineHeight = 0;
                value.Top = _currentLineHeight;
                value.Left = 0;
                value.Width = width;
                value.Height = height;
                lineIndex = 1;
                _currentLineWidth += width + ColumnSpacing;
            }
            else
            {
                value.Top = _currentLineHeight;
                value.Left = lineIndex == 0 ? 0 : _currentLineWidth;
                value.Width = width;
                value.Height = height;
                _currentLineWidth += width + ColumnSpacing;
                lineIndex++;
            }

            maxLineHeight = Math.Max(maxLineHeight, desiredHeight);
            value.IsRendered = true;
        }

        double CalculatePanelHeight(double renderedBottom)
        {
            if (Items.Count == 0 || Columns <= 0)
            {
                return 0;
            }

            if (!double.IsPositiveInfinity(RowHeight))
            {
                var rowCount = (int)Math.Ceiling((double)Items.Count / Columns);
                if (rowCount <= 0)
                {
                    return 0;
                }

                return rowCount * RowHeight + Math.Max(0, rowCount - 1) * RowSpacing;
            }

            var realizedBottom = _elementDictionary.Count == 0
                ? renderedBottom
                : _elementDictionary.Values.Max(element => element.Top + element.Height);

            var realizedRowCount = (int)Math.Ceiling((double)Math.Max(1, _elementDictionary.Count) / Columns);
            var totalRowCount = (int)Math.Ceiling((double)Items.Count / Columns);
            var remainingRows = Math.Max(0, totalRowCount - realizedRowCount);
            var estimatedRemainingHeight = remainingRows * (_maximumItemHeight + RowSpacing);

            return Math.Max(renderedBottom, realizedBottom + estimatedRemainingHeight);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var i in _elementDictionary)
            {
                if (i.Value.Control is { })
                {

                    i.Value.Control.Measure(new Size(i.Value.Width, i.Value.Height));

                }
            }
            return _panelSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var i in _elementDictionary)
            {
                if (i.Value.Control is { })
                {
                    i.Value.Control.Arrange(new Rect(i.Value.Left, i.Value.Top, i.Value.Width, i.Value.Height));
                }
            }
            return finalSize;
        }

        protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var _clearStartIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : _currentIndex;
                        ClearElementRange(_clearStartIndex);
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        var _clearStartIndex = e.OldStartingIndex >= 0 ? e.OldStartingIndex : _currentIndex;
                        ClearElementRange(_clearStartIndex);
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    {
                        var changedIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : e.OldStartingIndex;
                        var _clearStartIndex = changedIndex >= 0 ? changedIndex : _currentIndex;
                        ClearElementRange(_clearStartIndex);
                        break;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        ClearElementRange(0);
                        break;
                    }
            }
            base.OnItemsChanged(items, e);
            RenderElements(_currentIndex);
            HorizontalSnapPointsChanged?.Invoke(this, CreateSnapPointsChangedEventArgs());
            VerticalSnapPointsChanged?.Invoke(this, CreateSnapPointsChangedEventArgs());
        }



        protected override Control? ScrollIntoView(int index)
        {
            var _items = Items;

            if (index < 0 || index >= _items.Count || !IsEffectivelyVisible)
                return null;

            var renderStartIndex = Columns > 0 ? index - index % Columns : index;

            if (!_elementDictionary.ContainsKey(index))
            {
                RenderElements(renderStartIndex);
            }

            if (_elementDictionary.TryGetValue(index, out var renderModel) && renderModel.IsRendered)
            {
                if (renderModel.Control is Control _element)
                {
                    _element.BringIntoView();
                    return _element;
                }
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                RenderElements(renderStartIndex);
                if (_elementDictionary.TryGetValue(index, out renderModel) && renderModel.Control is Control _element)
                {
                    _element.UpdateLayout();
                    _element.BringIntoView();
                    return _element;
                }
            }
            return null;
        }
        protected override Control? ContainerFromIndex(int index)
        {
            if (_elementDictionary.TryGetValue(index, out var _element))
            {
                return _element.Control;
            }
            return null;
        }

        protected override int IndexFromContainer(Control container)
        {
            foreach (var i in _elementDictionary)
            {
                if (i.Value.Control == container)
                {
                    return i.Key;
                }
            }
            return -1;
        }

        protected override IEnumerable<Control>? GetRealizedContainers()
        {
            return _elementDictionary.Where(_ => _.Value.Control is { }).Select(_ => _.Value.Control).OfType<Control>().ToList();
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;
            var fromControl = from as Control;

            if (count == 0 ||
                fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last)
                return null;

            var fromIndex = fromControl != null ? IndexFromContainer(fromControl) : -1;
            var toIndex = fromIndex;

            switch (direction)
            {
                case NavigationDirection.First:
                    toIndex = 0;
                    break;
                case NavigationDirection.Last:
                    toIndex = count - 1;
                    break;
                case NavigationDirection.Next:
                    ++toIndex;
                    break;
                case NavigationDirection.Previous:
                    --toIndex;
                    break;
                case NavigationDirection.Left:
                    --toIndex;
                    break;
                case NavigationDirection.Right:
                    ++toIndex;
                    break;
                case NavigationDirection.Up:
                    --toIndex;
                    break;
                case NavigationDirection.Down:
                    ++toIndex;
                    break;
                default:
                    return null;
            }

            if (fromIndex == toIndex)
                return from;

            if (wrap)
            {
                if (toIndex < 0)
                    toIndex = count - 1;
                else if (toIndex >= count)
                    toIndex = 0;
            }

            return ScrollIntoView(toIndex);
        }

        public override IReadOnlyList<double> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment)
        {
            if (orientation == Orientation.Vertical)
            {
                if (!double.IsPositiveInfinity(RowHeight))
                {
                    return Array.Empty<double>();
                }

                return _elementDictionary
                    .Values
                    .GroupBy(element => element.Top)
                    .OrderBy(group => group.Key)
                    .Select(group => GetSnapPointValue(group.Key, group.Max(element => element.Height), snapPointsAlignment))
                    .ToList();
            }

            if (Columns <= 0 || Bounds.Width <= 0)
            {
                return Array.Empty<double>();
            }

            return Array.Empty<double>();
        }

        public override double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            offset = 0;

            if (Columns <= 0 || Bounds.Width <= 0)
            {
                return 0;
            }

            var itemWidth = (Bounds.Width - (Columns - 1) * ColumnSpacing) / Columns;
            if (itemWidth <= 0)
            {
                return 0;
            }

            if (orientation == Orientation.Horizontal)
            {
                offset = GetSnapPointValue(0, itemWidth, snapPointsAlignment);
                return itemWidth + ColumnSpacing;
            }

            if (!double.IsPositiveInfinity(RowHeight) && RowHeight > 0)
            {
                offset = GetSnapPointValue(0, RowHeight, snapPointsAlignment);
                return RowHeight + RowSpacing;
            }

            return 0;
        }
    }
}
