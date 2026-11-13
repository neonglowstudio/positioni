using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace Positioni;

public sealed class ProfilePreviewCanvas : Canvas
{
    private readonly Dictionary<InputSystemCursorShape, InputSystemCursor> cursors = [];
    private InputSystemCursorShape shape = InputSystemCursorShape.Arrow;

    internal void SetCursor(InputSystemCursorShape value)
    {
        if (value == shape) return;
        shape = value;
        if (value == InputSystemCursorShape.Arrow)
        {
            ProtectedCursor = null;
            return;
        }
        if (!cursors.TryGetValue(value, out var cursor))
            cursors[value] = cursor = InputSystemCursor.Create(value);
        ProtectedCursor = cursor;
    }
}
