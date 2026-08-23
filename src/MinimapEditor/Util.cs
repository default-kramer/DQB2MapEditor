using System.Windows;
using System.Windows.Media;

namespace MinimapEditor;

static class Util
{
    public static void SoftAssertFail() => System.Diagnostics.Debugger.Break();

    public static IEnumerable<DependencyObject> VisualAncestors(this DependencyObject startingPoint)
    {
        DependencyObject? obj = startingPoint;
        while (obj != null)
        {
            yield return obj;
            obj = VisualTreeHelper.GetParent(obj);
        }
    }

    public static T LoadOnce<T>(ref T? field, Func<T> valueFactory)
    {
        field = field ?? valueFactory();
        return field;
    }

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> seq) where T : class
    {
        return seq.Where(x => x != null)!;
    }
}
