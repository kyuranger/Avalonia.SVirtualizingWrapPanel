using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Generators;
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

namespace SVirtualizingWrapPanel
{
    public sealed class SVirtualizingWrapPanel : SVirtualizingPanel
    {
        public override bool AreHorizontalSnapPointsRegular { get; set; } = false;
        public override bool AreVerticalSnapPointsRegular { get; set; } = false;


        Boolean _IsPauseLoadMoreRequested = false;

        public override event EventHandler<RoutedEventArgs>? HorizontalSnapPointsChanged;
        public override event EventHandler<RoutedEventArgs>? VerticalSnapPointsChanged;

        public override Boolean IsPauseLoadMoreRequested
        {
            get => _IsPauseLoadMoreRequested;
            set
            {
                if (_IsPauseLoadMoreRequested != value)
                {
                    _IsPauseLoadMoreRequested = value;
                    if (!_IsPauseLoadMoreRequested)
                    {
                        DetermineWhetherToLoadMore();
                    }
                }
            }
        }
        public SVirtualizingWrapPanel()
        {
            this.EffectiveViewportChanged += VirtualizingWrapPanel_EffectiveViewportChanged;
        }
        private void VirtualizingWrapPanel_EffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e)
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
                        if (_element.Top >= _EffectiveViewport.Top || _element.Top + _element.Height > _EffectiveViewport.Top || _element.Top + _MaximumItemHeight > _EffectiveViewport.Top)
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
                //Debug.WriteLine("lastIndex:" + _lastIndex);
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
                                //_element.IsRendered = false;
                                //Debug.WriteLine($"回收{i}");
                            }
                        }
                    }
                }
                #endregion
                InvalidateMeasure();
                InvalidateArrange();
                if (!IsPauseLoadMoreRequested)
                {
                    DetermineWhetherToLoadMore();
                }
            }
            else
            {
                _EffectiveViewport = e.EffectiveViewport;
            }
        }

        protected override void DetermineWhetherToLoadMore()
        {
            if (_EffectiveViewport.Top + _EffectiveViewport.Height >= _PanelSize.Height - 300)
            {
                LoadMore(); // fire and forget
            }
        }


        protected override int RenderElements(int startIndex)
        {
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


            for (int i = _index; i < Items.Count; i++)
            {
                var _item = Items[i];
                if (_item is { })
                {
                    Control _element = new Control();
                    if (!_ElementDictionary.TryGetValue(i, out var _value))//字典里面不包含该元素
                    {
                        _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                        _value = new ElementRenderModel() { Width = _element.DesiredSize.Width, Height = _element.DesiredSize.Height, Control = _element, IsRendered = true };
                        _ElementDictionary.Add(i, _value);
                    }
                    else
                    {
                        if (_value.Control is not { })
                        {
                            _element = CreateVirtualizingElement(_item, i, Guid.NewGuid().ToString());
                            _value = new ElementRenderModel() { Width = _element.DesiredSize.Width, Height = _element.DesiredSize.Height, Control = _element, IsRendered = true };
                            _ElementDictionary[i] = _value;
                        }
                        else
                        {
                            _element = _value.Control;
                        }
                    }
                    if (IsMeasureFinished(_element))
                    {
                        _endIndex = i;
                        break;
                    }
                }
            }
            #endregion
            #region//尝试进行排列
            _CurrentLineHeight = 0;
            _CurrentLineWidth = 0;
            _maxLineHeight = 0;
            for (int i = 0; i < Items.Count; i++)
            {

                if (_ElementDictionary.TryGetValue(i, out var _element))
                {
                    var _control = _element.Control;
                    if (_element.IsRendered)
                    {
                        _maxLineHeight = Math.Max(_maxLineHeight, _element.Height);
                        if (_CurrentLineWidth + _element.Width > _maxLineWidth)
                        {
                            _CurrentLineHeight += _maxLineHeight;
                            _CurrentLineWidth = 0;
                            if (i != Items.Count - 1)
                            {
                                _maxLineHeight = 0;
                            }
                            _element.Height = _element.Height;
                            _element.Width = _element.Width;
                            _element.Top = _CurrentLineHeight;
                            _element.Left = _CurrentLineWidth;
                            _CurrentLineWidth = _element.Width;
                        }
                        else
                        {
                            _element.Height = _element.Height;
                            _element.Width = _element.Width;
                            _element.Top = _CurrentLineHeight;
                            _element.Left = _CurrentLineWidth;
                            _CurrentLineWidth += _element.Width;
                        }
                    }
                    else
                    {
                        _maxLineHeight = Math.Max(_maxLineHeight, _MaximumItemHeight);
                        if (_CurrentLineWidth + _MaximumItemWidth > _maxLineWidth)
                        {
                            _CurrentLineHeight += _maxLineHeight;
                            _CurrentLineWidth = 0;
                            _element.Left = _CurrentLineWidth;
                            _element.Top = _CurrentLineHeight;
                        }
                        else
                        {
                            _element.Left = _CurrentLineWidth;
                            _element.Top = _CurrentLineHeight;
                            _CurrentLineWidth += _MaximumItemWidth;
                        }
                    }
                }
                else
                {
                    _maxLineHeight = Math.Max(_maxLineHeight, _MaximumItemHeight);
                    if (_CurrentLineWidth + _MaximumItemWidth > _maxLineWidth)
                    {
                        _CurrentLineHeight += _maxLineHeight;
                        _CurrentLineWidth = 0;
                        _ElementDictionary.Add(i, new ElementRenderModel() { Left = _CurrentLineWidth, Top = _CurrentLineHeight });
                    }
                    else
                    {
                        _ElementDictionary.Add(i, new ElementRenderModel() { Left = _CurrentLineWidth, Top = _CurrentLineHeight });
                        _CurrentLineWidth += _MaximumItemWidth;
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
        #region//计算是否Measure完成        
        protected override Boolean IsMeasureFinished(Control control)
        {
            if (_CurrentLineWidth + control.DesiredSize.Width > this.Bounds.Width)//换行
            {
                if (_CurrentLineHeight + control.DesiredSize.Height > _EffectiveViewport.Height + _EffectiveViewport.Top)
                {
                    return true;
                }
                _CurrentLineHeight += control.DesiredSize.Height;
                _CurrentLineWidth = control.DesiredSize.Width;
                return false;
            }
            else
            {
                _CurrentLineWidth += control.DesiredSize.Width;
                _MaximumItemHeight = Math.Max(control.DesiredSize.Height, _MaximumItemHeight);
                _MaximumItemWidth = Math.Max(control.DesiredSize.Width, _MaximumItemWidth);
                return false;
            }
        }
        #endregion
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
                var _control = i.Value.Control;
                if (_control is { })
                {
                    _control.Arrange(new Rect(i.Value.Left, i.Value.Top, i.Value.Width, i.Value.Height));
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
                        var _newIndex = e.NewStartingIndex;
                        if (_newIndex <= _LastIndex)
                        {
                            var _clearStartIndex = Math.Min(_newIndex, _CurrentIndex);
                            for (int i = _clearStartIndex; i < Items.Count; i++)
                            {
                                if (_ElementDictionary.ContainsKey(i))
                                {
                                    _ElementDictionary.Remove(i);
                                }
                            }
                            var _removeStartIndex = _newIndex - _CurrentIndex;
                            if (_removeStartIndex >= 0)
                            {
                                RemoveInternalChildRange(_removeStartIndex, VisualChildren.Count - _removeStartIndex);
                            }
                            else
                            { 
                                RemoveInternalChildRange(0, VisualChildren.Count);
                            }
                        }
                        else
                        {
                            for (int i = _LastIndex; i < Items.Count; i++)
                            {
                                if (_ElementDictionary.ContainsKey(i))
                                {
                                    _ElementDictionary.Remove(i);
                                }
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Remove: {
                        var _oldIndex = e.OldStartingIndex;
                        if (_oldIndex <= _LastIndex)
                        {
                            var _clearStartIndex = Math.Min(_oldIndex, _CurrentIndex);
                            for (int i = _clearStartIndex; i < Items.Count; i++)
                            {
                                if (_ElementDictionary.ContainsKey(i))
                                {
                                    _ElementDictionary.Remove(i);
                                }
                            }
                            var _removeStartIndex = _oldIndex - _CurrentIndex;
                            if (_removeStartIndex >= 0)
                            {
                                RemoveInternalChildRange(_removeStartIndex, VisualChildren.Count - _removeStartIndex);
                            }
                            else
                            {
                                RemoveInternalChildRange(0, VisualChildren.Count);
                            }
                        }
                        else
                        {
                            for (int i = _LastIndex; i < Items.Count; i++)
                            {
                                if (_ElementDictionary.ContainsKey(i))
                                {
                                    _ElementDictionary.Remove(i);
                                }
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Reset: 
                    {
                        _ElementDictionary.Clear();
                        RemoveInternalChildRange(0, VisualChildren.Count);
                        break;
                    }
            }
            base.OnItemsChanged(items, e);
            RenderElements(_CurrentIndex);
            if (!IsLoadingMore && !IsPauseLoadMoreRequested)
            {
                LoadMore();
            }
        }



        protected override Control? ScrollIntoView(int index)
        {
            var _items = Items;

            if (index < 0 || index >= _items.Count || !IsEffectivelyVisible)
                return null;

            if (!_ElementDictionary.ContainsKey(index))
            {
                if (HasMoreItems)
                {
                    LoadMore();
                }
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
