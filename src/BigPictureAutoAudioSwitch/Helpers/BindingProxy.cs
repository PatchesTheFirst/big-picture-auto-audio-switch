using System.Windows;

namespace BigPictureAutoAudioSwitch.Helpers;

/// <summary>
/// A freezable proxy that allows binding to a DataContext from outside the visual tree.
/// Used for context menus and other popup elements that don't inherit DataContext.
/// </summary>
public class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore()
    {
        return new BindingProxy();
    }

    public object Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
}
