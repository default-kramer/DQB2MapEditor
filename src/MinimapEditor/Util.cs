using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace MinimapEditor;

static class Util
{
    public static IEnumerable<DependencyObject> VisualAncestors(this DependencyObject startingPoint)
    {
        DependencyObject? obj = startingPoint;
        while (obj != null)
        {
            yield return obj;
            obj = VisualTreeHelper.GetParent(obj);
        }
    }
}
