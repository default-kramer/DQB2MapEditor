using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace MinimapEditor;

public abstract class MouseEventInfo
{
    public required MouseButtonState LeftButton { get; init; }
    public required MouseButtonState RightButton { get; init; }
    public abstract bool Handled { get; set; }

    public static MouseEventInfo Create(MouseEventArgs args) => new RealMouseEvent()
    {
        LeftButton = args.LeftButton,
        RightButton = args.RightButton,
        Args = args,
    };

    sealed class RealMouseEvent : MouseEventInfo
    {
        public required MouseEventArgs Args { get; init; }
        public override bool Handled
        {
            get => Args.Handled;
            set => Args.Handled = value;
        }
    }
}
