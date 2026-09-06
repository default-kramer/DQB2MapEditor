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

    public static readonly DependencyProperty IsActive2987Property =
        DependencyProperty.RegisterAttached("IsActive2987", typeof(bool), typeof(Util));
    public static bool GetIsActive2987(DependencyObject x) => (bool)x.GetValue(IsActive2987Property);
    public static void SetIsActive2987(DependencyObject x, bool value) => x.SetValue(IsActive2987Property, value);
}
