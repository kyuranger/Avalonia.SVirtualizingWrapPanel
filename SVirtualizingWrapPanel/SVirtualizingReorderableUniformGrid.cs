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
    public class SVirtualizingReorderableUniformGrid : SVirtualizingPanel
    {
        
        public static readonly StyledProperty<int> ColumnsProperty =
  AvaloniaProperty.Register<SVirtualizingReorderableUniformGrid, int>(nameof(Columns), 0);

        public int Columns
        {
            get { return GetValue(ColumnsProperty); }
            set { SetValue(ColumnsProperty, value); }
        }

        public static readonly StyledProperty<double> RowHeightProperty =
 AvaloniaProperty.Register<SVirtualizingReorderableUniformGrid, double>(nameof(RowHeight), double.PositiveInfinity);

        public double RowHeight
        {
            get { return GetValue(RowHeightProperty); }
            set { SetValue(RowHeightProperty, value); }
        }

        public static readonly StyledProperty<double> RowSpacingProperty =
 AvaloniaProperty.Register<SVirtualizingReorderableUniformGrid, double>(nameof(RowSpacing), 0);

        public double RowSpacing
        {
            get { return GetValue(RowSpacingProperty); }
            set { SetValue(RowSpacingProperty, value); }
        }

        public static readonly StyledProperty<double> ColumnSpacingProperty =
AvaloniaProperty.Register<SVirtualizingReorderableUniformGrid, double>(nameof(ColumnSpacing), 0);

        public double ColumnSpacing
        {
            get { return GetValue(ColumnSpacingProperty); }
            set { SetValue(ColumnSpacingProperty, value); }
        }

        public override bool AreHorizontalSnapPointsRegular { get; set; } = false;
        public override bool AreVerticalSnapPointsRegular { get; set; } = false;

        public override event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public override event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

        public SVirtualizingReorderableUniformGrid()
        {
            this.EffectiveViewportChanged += SVirtualizingReorderableUniformGrid_EffectiveViewportChanged; ;
        }

        private void SVirtualizingReorderableUniformGrid_EffectiveViewportChanged(object? sender, Avalonia.Layout.EffectiveViewportChangedEventArgs e)
        {
            //Debug.WriteLine(e.EffectiveViewport.Height);
            //Debug.WriteLine(e.EffectiveViewport.Top);            
            if (e.EffectiveViewport.Top == -1)
            {
                return;
            }
            if (e.EffectiveViewport.Top != _EffectiveViewport.Top || e.EffectiveViewport.Width != _EffectiveViewport.Width || e.EffectiveViewport.Height != _EffectiveViewport.Height)
            {
                _EffectiveViewport = e.EffectiveViewport;
                //Debug.WriteLine($"Top:{_EffectiveViewport.Top}");
                #region//获取进入渲染位置的第一个index
                var _firstIndex = 0;
                for (int i = 0; i < Items.Count; i++)
                {
                    if (_ElementDictionary.TryGetValue(i, out var _element))
                    {
                        if (_element.Top + _element.Height > _EffectiveViewport.Top || _element.Top + _MaximumItemHeight > _EffectiveViewport.Top)
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
                    if (_ElementDictionary.TryGetValue(i, out var _element))
                    {
                        if (_element.Left == 0)
                        {
                            _startIndex = i;
                            break;
                        }
                    }
                }
                _CurrentIndex = _startIndex;
                //Debug.WriteLine("startIndex:" + _startIndex);
                #endregion
                #region//正式渲染                               
                _LastIndex = RenderElements(_startIndex);
                //Debug.WriteLine("lastIndex:" + _LastIndex);
                #endregion
                #region//回收其他元素
                for (int i = 0; i < Items.Count; i++)
                {
                    if (i < _startIndex || i > _LastIndex)
                    {
                        if (_ElementDictionary.TryGetValue(i, out var _element))
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
            }
            else
            {
                _EffectiveViewport = e.EffectiveViewport;
            }
        }
       
        protected override void ScrollToLoadMore()
        {
            if (_EffectiveViewport.Top + _EffectiveViewport.Height >= _PanelSize.Height - 300)
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
            var _elementWidth = (this.Bounds.Width - (Columns - 1) * ColumnSpacing) / Columns;
            var _lineIndex = 0;
            var _endIndex = Items.Count - 1;
            var _index = startIndex;
            //Debug.WriteLine("_maxLineWidth" + _maxLineWidth);
            double _maxLineWidth = this.Bounds.Width;
            double _maxLineHeight = 0.0;
            if (_ElementDictionary.TryGetValue(_index, out var _firstElement))
            {
                _CurrentLineHeight = _firstElement.Top;
            }
            else
            {
                _CurrentLineHeight = 0;
            }
            _CurrentLineWidth = 0;
            #region//先计算需渲染的每个控件所需的空间          
            if (double.IsPositiveInfinity(RowHeight))
            {
                for (int i = startIndex; i < Items.Count; i++)
                {
                    var _item = Items[i];
                    if (_item is { })
                    {
                        Control? _element = null;
                        if (!_ElementDictionary.TryGetValue(i, out var _value))
                        {
                            _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                            _maxLineHeight = Math.Max(_maxLineHeight, _element.DesiredSize.Height);
                            var _newValue = new ElementRenderModel();

                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _newValue, _elementWidth, _element.DesiredSize.Height);

                            _newValue.Control = _element;
                            _newValue.IsRendered = true;

                            _ElementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                            _element = _value.Control;
                            if (_element is { })
                            {
                                _maxLineHeight = Math.Max(_maxLineHeight, _element.DesiredSize.Height);
                                CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, _element.DesiredSize.Height);
                            }
                            else
                            {
                                _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                                _maxLineHeight = Math.Max(_maxLineHeight, _element.DesiredSize.Height);
                                CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, _element.DesiredSize.Height);
                                _value.Control = _element;
                                _value.IsRendered = true;
                            }
                        }
                        if (_CurrentLineHeight > _EffectiveViewport.Top + _EffectiveViewport.Height)
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
                        if (!_ElementDictionary.TryGetValue(i, out var _value))
                        {
                            _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                            _maxLineHeight = RowHeight;
                            var _newValue = new ElementRenderModel();

                            CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _newValue, _elementWidth, RowHeight);

                            _newValue.Control = _element;
                            _newValue.IsRendered = true;

                            _ElementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                            _element = _value.Control;
                            if (_element is { })
                            {
                                _maxLineHeight = RowHeight;
                                CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, RowHeight);
                            }
                            else
                            {
                                _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                                _maxLineHeight = RowHeight;
                                CalculatingItemPosition(ref _maxLineHeight, ref _lineIndex, i, _value, _elementWidth, RowHeight);
                                _value.Control = _element;
                                _value.IsRendered = true;
                            }
                        }
                        if (_CurrentLineHeight > _EffectiveViewport.Top + _EffectiveViewport.Height)
                        {
                            _endIndex = i;
                            break;
                        }
                    }
                }
            }
            #endregion

            _PanelSize = new Size(_EffectiveViewport.Width, _CurrentLineHeight + _maxLineHeight);
            #region//正式Measure自身
            InvalidateMeasure();
            #endregion
            #region//正式Arrange自身
            InvalidateArrange();
            #endregion                     
            return _endIndex;
        }


        void CalculatingItemPosition(ref double maxLineHeight, ref int lineIndex, int index, ElementRenderModel value, double width, double height)
        {
            if (lineIndex == Columns)
            {
                lineIndex = 0;
                _CurrentLineHeight += maxLineHeight + RowSpacing;
                _CurrentLineWidth = 0;
                if (index != Items.Count - 1)
                {
                    maxLineHeight = 0;
                }
                value.Top = _CurrentLineHeight;
                value.Left = 0;
                value.Width = width;
                value.Height = height;
                lineIndex =1;
                _CurrentLineWidth += width + ColumnSpacing;
            }
            else
            {
                value.Top = _CurrentLineHeight;
                value.Left = lineIndex == 0 ? 0 : _CurrentLineWidth;
                value.Width = width;
                value.Height = height;
                _CurrentLineWidth += width + ColumnSpacing;
                lineIndex++;
            }
            value.IsRendered = true;
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var i in _ElementDictionary)
            {
                if (i.Value.Control is { })
                {

                    i.Value.Control.Measure(new Size(i.Value.Width, i.Value.Height));

                }
            }
            return _PanelSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var i in _ElementDictionary)
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
                        int _clearStartIndex = 0;
                        if (e.NewStartingIndex < _LastIndex)
                        {
                            _clearStartIndex = Math.Min(e.NewStartingIndex, _CurrentIndex);
                        }
                        else
                        {
                            _clearStartIndex = _LastIndex;
                        }
                        for (int i = _clearStartIndex; i < Items.Count; i++)
                        {
                            if (_ElementDictionary.TryGetValue(i, out var _element))
                            {
                                if (_element.Control is { })
                                {
                                    RemoveInternalChild(_element.Control);
                                    ItemContainerGenerator?.ClearItemContainer(_element.Control);
                                    _ElementDictionary.Remove(i);
                                }
                            }

                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        int _clearStartIndex = 0;
                        if (e.OldStartingIndex < _LastIndex)
                        {
                            _clearStartIndex = Math.Min(e.NewStartingIndex, _CurrentIndex);
                        }
                        else
                        {
                            _clearStartIndex = _LastIndex;
                        }
                        for (int i = _clearStartIndex; i < Items.Count; i++)
                        {
                            if (_ElementDictionary.TryGetValue(i, out var _element))
                            {
                                if (_element.Control is { })
                                {
                                    RemoveInternalChild(_element.Control);
                                    ItemContainerGenerator?.ClearItemContainer(_element.Control);
                                    _ElementDictionary.Remove(i);
                                }
                            }

                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Reset:
                    {
                        for (int i = 0; i < Items.Count; i++)
                        {
                            if (_ElementDictionary.TryGetValue(i, out var _element))
                            {
                                if (_element.Control is { })
                                {
                                    RemoveInternalChild(_element.Control);
                                    ItemContainerGenerator?.ClearItemContainer(_element.Control);
                                    _ElementDictionary.Remove(i);
                                }
                            }

                        }
                        break;
                    }
            }
            base.OnItemsChanged(items, e);
            RenderElements(_CurrentIndex);
        }



        protected override Control? ScrollIntoView(int index)
        {
            var _items = Items;

            if (index < 0 || index >= _items.Count || !IsEffectivelyVisible)
                return null;

            if (!_ElementDictionary.ContainsKey(index))
            {
                OnLoadMore();
                return null;
            }

            if (index < _ElementDictionary.Count && _ElementDictionary[index].IsRendered)
            {
                if (_ElementDictionary[index].Control is Control _element)
                {
                    _element.BringIntoView();
                    return _element;
                }
            }
            else if (this.GetVisualRoot() is ILayoutRoot root)
            {
                RenderElements(index);
                if (_ElementDictionary[index].Control is Control _element)
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
            if (_ElementDictionary.TryGetValue(index, out var _element))
            {
                return _element.Control;
            }
            return null;
        }

        protected override int IndexFromContainer(Control container)
        {
            foreach (var i in _ElementDictionary)
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
            return _ElementDictionary.Where(_ => _.Value.Control is { }).Select(_ => _.Value.Control).OfType<Control>().ToList();
        }

        protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
        {
            var count = Items.Count;
            var fromControl = from as Control;

            if (count == 0 ||
                (fromControl is null && direction is not NavigationDirection.First and not NavigationDirection.Last))
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
            return new List<double>();
        }

        public override double GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment snapPointsAlignment, out double offset)
        {
            throw new NotImplementedException();
        }
    }
}
