using MinimapEditor;

namespace MinimapEditorTests;

class FakeMouseEvent : MouseEventInfo
{
    public override bool Handled { get; set; }

    public static FakeMouseEvent LeftDown() => new FakeMouseEvent
    {
        LeftButton = System.Windows.Input.MouseButtonState.Pressed,
        RightButton = System.Windows.Input.MouseButtonState.Released,
    };

    public static FakeMouseEvent AllRelease() => new FakeMouseEvent
    {
        LeftButton = System.Windows.Input.MouseButtonState.Released,
        RightButton = System.Windows.Input.MouseButtonState.Released,
    };
}
