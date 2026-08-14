using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace MinimapEditor;

/// <summary>
/// Performs a breadth-first-search of the Visual Tree.
/// </summary>
static class VisualTreeBFS
{
    /// <summary>
    /// A filter which considers any object having type <typeparamref name="T"/> to be a match.
    /// The search will go no deeper than the first match,
    /// but other matches at that same depth will be included.
    /// </summary>
    public static Filter<T> MakeSimpleFilter<T>(int maxSearchDepth = int.MaxValue) where T : DependencyObject
    {
        return new SimpleFilter<T>() { MaxSearchDepth = maxSearchDepth };
    }

    private static IEnumerable<T> DoBFS<T>(DependencyObject startingPoint, IFilter<T> filter) where T : DependencyObject
    {
        List<DependencyObject> currentLayer = [startingPoint];
        List<DependencyObject> nextLayer = new();

        int depth = 0;
        while (currentLayer.Count > 0)
        {
            foreach (var obj in currentLayer)
            {
                if (obj is T item && filter.IsMatch(depth, item))
                {
                    yield return item;
                }
                if (filter.ShouldRecurse(depth, obj))
                {
                    int count = VisualTreeHelper.GetChildrenCount(obj);
                    for (int i = 0; i < count; i++)
                    {
                        nextLayer.Add(VisualTreeHelper.GetChild(obj, i));
                    }
                }
            }
            (currentLayer, nextLayer) = (nextLayer, currentLayer);
            nextLayer.Clear();
            depth++;
        }
    }

    interface IFilter<T> where T : DependencyObject
    {
        bool IsMatch(int depth, T obj);
        bool ShouldRecurse(int depth, DependencyObject obj);
    }

    public abstract class Filter<T> : IFilter<T> where T : DependencyObject
    {
        public IEnumerable<T> DoBFS(DependencyObject startingPoint) => VisualTreeBFS.DoBFS(startingPoint, this);

        protected abstract bool IsMatch(int depth, T obj);
        protected abstract bool ShouldRecurse(int depth, DependencyObject obj);
        bool IFilter<T>.IsMatch(int depth, T obj) => IsMatch(depth, obj);
        bool IFilter<T>.ShouldRecurse(int depth, DependencyObject obj) => ShouldRecurse(depth, obj);
    }

    sealed class SimpleFilter<T> : Filter<T> where T : DependencyObject
    {
        private int firstMatchDepth = int.MaxValue;
        public required int MaxSearchDepth { get; init; }

        protected override bool IsMatch(int depth, T obj)
        {
            firstMatchDepth = Math.Min(depth, firstMatchDepth);
            return true;
        }

        protected override bool ShouldRecurse(int depth, DependencyObject obj)
            => depth < firstMatchDepth && depth < MaxSearchDepth;
    }
}
