using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Game.UI;

public static class PopupBehaviors
{
    public static readonly AttachedProperty<bool> CloseOnClickOutsideProperty =
        AvaloniaProperty.RegisterAttached<Popup, bool>("CloseOnClickOutside", typeof(PopupBehaviors));

    public static bool GetCloseOnClickOutside(Popup popup) => popup.GetValue(CloseOnClickOutsideProperty);
    public static void SetCloseOnClickOutside(Popup popup, bool value) => popup.SetValue(CloseOnClickOutsideProperty, value);

    static PopupBehaviors()
    {
        CloseOnClickOutsideProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is not Popup popup) return;

            if (args.NewValue is { HasValue: true, Value: true }) 
                Attach(popup);
        });
    }

    private static void Attach(Popup popup)
    {
        popup.Opened += (_, _) =>
        {
            if (popup.Child is { } popupContent) popupContent.PointerExited += (_, _) => popup.SetCurrentValue(Popup.IsOpenProperty, false);
        };
    }
}