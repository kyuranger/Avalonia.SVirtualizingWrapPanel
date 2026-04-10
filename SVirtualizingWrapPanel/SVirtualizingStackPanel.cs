using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SVirtualizingWrapPanel
{
    public sealed class SVirtualizingStackPanel : SVirtualizingPanel
    {
        public static readonly StyledProperty<Orientation> OrientationProperty =
         AvaloniaProperty.Register<SVirtualizingStackPanel, Orientation>(nameof(Orientation), Orientation.Vertical);

        public Orientation Orientation
        {
            get { return GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public override bool AreHorizontalSnapPointsRegular { get; set; } = false;
        public override bool AreVerticalSnapPointsRegular { get; set; } = false;


        public override event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public override event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;
        Boolean _isLoadRendered = false;
        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            if (!_isLoadRendered)
            {
                RenderElements(_currentIndex);
            }
        }
        public SVirtualizingStackPanel()
        {
            this.EffectiveViewportChanged += SVirtualizingStackPanel_EffectiveViewportChanged;
        }

        private void SVirtualizingStackPanel_EffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
        {
            //Debug.WriteLine(e.EffectiveViewport.Height);
            //Debug.WriteLine(e.EffectiveViewport.Top);            
            if (Orientation == Orientation.Vertical)
            {
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
                    EffectiveViewportChangedRender(_firstIndex);
                    ScrollToLoadMore();
                    _isLoadRendered = true;
                }
                else
                {
                    _effectiveViewport = e.EffectiveViewport;
                }
            }
            else
            {
                if (e.EffectiveViewport.Top == -1 || e.EffectiveViewport.Width == 0 || e.EffectiveViewport.Height == 0)
                {
                    return;
                }
                if (e.EffectiveViewport.Left != _effectiveViewport.Left || e.EffectiveViewport.Width != _effectiveViewport.Width || e.EffectiveViewport.Height != _effectiveViewport.Height)
                {
                    _effectiveViewport = e.EffectiveViewport;
                    //Debug.WriteLine($"Top:{_EffectiveViewport.Top}");
                    #region//获取进入渲染位置的第一个index
                    var viewportStart = GetHorizontalViewportStart();
                    var _firstIndex = 0;
                    for (int i = 0; i < Items.Count; i++)
                    {
                        if (_elementDictionary.TryGetValue(i, out var _element))
                        {
                            if (_element.Left + _element.Width > viewportStart || _element.Left + _maximumItemWidth > viewportStart)
                            {
                                _firstIndex = i;
                                break;
                            }
                        }
                    }
                    //Debug.WriteLine("firstIndex:" + _firstIndex);
                    #endregion
                    EffectiveViewportChangedRender(_firstIndex);

                    ScrollToLoadMore();
                    _isLoadRendered = true;
                }
                else
                {
                    _effectiveViewport = e.EffectiveViewport;
                }
            }
        }

        protected override void ScrollToLoadMore()
        {
            if (Orientation == Orientation.Vertical)
            {
                if (_effectiveViewport.Top + _effectiveViewport.Height >= _panelSize.Height - 300)
                {
                    OnLoadMore();
                }
            }
            else
            {
                if (_effectiveViewport.Left + _effectiveViewport.Width >= _panelSize.Width - 300)
                {
                    OnLoadMore();
                }
            }
        }
        void EffectiveViewportChangedRender(int firstIndex)
        {
            #region//获取该进行渲染的第一个index
            var _startIndex = 0;
            for (int i = firstIndex; i >= 0; i--)
            {
                if (_elementDictionary.TryGetValue(i, out var _element))
                {
                    _startIndex = i;
                    break;
                }
            }
            _currentIndex = _startIndex;
            //Debug.WriteLine("startIndex:" + _startIndex);
            #endregion
            #region//正式渲染                               
            _lastIndex = RenderElements(_startIndex);
            //Debug.WriteLine("lastIndex:" + _lastIndex);
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

        }




        protected override int RenderElements(int startIndex)
        {
            var _endIndex = Items.Count - 1;
            var _index = startIndex;

            if (Orientation == Orientation.Vertical)
            {
                if (_elementDictionary.TryGetValue(_index, out var _firstElement))
                {
                    _currentLineHeight = _firstElement.Top;
                }
                else
                {
                    _currentLineHeight = 0;
                }
                _currentLineWidth = 0;
                var _boundsWidth = this.Bounds.Width;
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
                            _newValue.Top = _currentLineHeight;
                            _newValue.Left = 0;
                            _currentLineHeight += _element.DesiredSize.Height;
                            _newValue.Width = _boundsWidth;
                            _newValue.Height = _element.DesiredSize.Height;
                            _newValue.Control = _element;
                            _newValue.IsRendered = true;

                            _elementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                            _element = _value.Control;
                            if (_element is { })
                            {
                                _value.Width = _boundsWidth;
                                _value.Height = _element.DesiredSize.Height;
                                _value.Top = _currentLineHeight;
                                _value.Left = 0;
                                _currentLineHeight += _element.DesiredSize.Height;
                            }
                            else
                            {
                                _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                                _value.Top = _currentLineHeight;
                                _value.Left = 0;
                                _currentLineHeight += _element.DesiredSize.Height;
                                _value.Width = _boundsWidth;
                                _value.Height = _element.DesiredSize.Height;
                                _value.Control = _element;
                                _value.IsRendered = true;
                            }
                        }
                        if (_currentLineHeight > GetVerticalViewportEnd())
                        {
                            _endIndex = i;
                            break;
                        }
                    }
                }

                _panelSize = new Size(_effectiveViewport.Width, _currentLineHeight);
            }
            else
            {
                if (_elementDictionary.TryGetValue(_index, out var _firstElement))
                {
                    _currentLineWidth = _firstElement.Left;
                }
                else
                {
                    _currentLineWidth = 0;
                }
                _currentLineHeight = 0;
                var _boundsHeight = this.Bounds.Height;
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
                            _newValue.Top = 0;
                            _newValue.Left = _currentLineWidth;
                            _newValue.Width = _element.DesiredSize.Width;
                            _newValue.Height = _boundsHeight;
                            _currentLineWidth += _element.DesiredSize.Width;

                            _newValue.Control = _element;
                            _newValue.IsRendered = true;

                            _elementDictionary.Add(i, _newValue);
                        }
                        else
                        {
                            _element = _value.Control;
                            if (_element is { })
                            {
                                _value.Width = _element.DesiredSize.Width;
                                _value.Height = _boundsHeight;
                                _value.Top = 0;
                                _value.Left = _currentLineWidth;
                                _currentLineWidth += _element.DesiredSize.Width;
                            }
                            else
                            {
                                _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                                _value.Top = 0;
                                _value.Left = _currentLineWidth;
                                _value.Width = _element.DesiredSize.Width;
                                _value.Height = _boundsHeight;
                                _currentLineWidth += _element.DesiredSize.Width;

                                _value.Control = _element;
                                _value.IsRendered = true;
                            }
                        }
                        if (_currentLineWidth > GetHorizontalViewportEnd())
                        {
                            _endIndex = i;
                            break;
                        }
                    }
                }
                _panelSize = new Size(_currentLineWidth, _effectiveViewport.Height);
            }
            #region//正式Measure自身
            InvalidateMeasure();
            #endregion
            #region//正式Arrange自身
            InvalidateArrange();
            #endregion
            return _endIndex;
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
                        var _clearStartIndex = e.NewStartingIndex >= 0 ? Math.Min(e.NewStartingIndex, _currentIndex) : _currentIndex;
                        ClearElementRange(_clearStartIndex);
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        var _clearStartIndex = e.OldStartingIndex >= 0 ? Math.Min(e.OldStartingIndex, _currentIndex) : _currentIndex;
                        ClearElementRange(_clearStartIndex);
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    {
                        var changedIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : e.OldStartingIndex;
                        var _clearStartIndex = changedIndex >= 0 ? Math.Min(changedIndex, _currentIndex) : _currentIndex;
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

        }



        protected override Control? ScrollIntoView(int index)
        {
            var _items = Items;

            if (index < 0 || index >= _items.Count || !IsEffectivelyVisible)
                return null;

            if (!_elementDictionary.ContainsKey(index))
            {
                RenderElements(index);
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
                RenderElements(index);
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
