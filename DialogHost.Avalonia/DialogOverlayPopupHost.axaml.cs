using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using DialogHostAvalonia.Positioners;

namespace DialogHostAvalonia;

/// <summary>
/// Overlay host control that displays dialog content and manages focus/navigation behavior while open.
/// </summary>
public class DialogOverlayPopupHost : ContentControl, ICustomKeyboardNavigation {
    /// <summary>
    /// Identifies the <see cref="IsOpen"/> property.
    /// </summary>
    public static readonly DirectProperty<DialogOverlayPopupHost, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<DialogOverlayPopupHost, bool>(
            nameof(IsOpen),
            o => o.IsOpen,
            (o, v) => o.IsOpen = v);

    /// <summary>
    /// Identifies the <see cref="IsActuallyOpen"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsActuallyOpenProperty =
        AvaloniaProperty.Register<DialogOverlayPopupHost, bool>(nameof(IsActuallyOpen), true);

    /// <summary>
    /// Identifies the <see cref="DisableOpeningAnimation"/> property.
    /// </summary>
    public static readonly DirectProperty<DialogOverlayPopupHost, bool> DisableOpeningAnimationProperty =
        DialogHost.DisableOpeningAnimationProperty.AddOwner<DialogOverlayPopupHost>(
            host => host.DisableOpeningAnimation,
            (host, b) => host.DisableOpeningAnimation = b);

    /// <summary>
    /// Identifies the <see cref="PopupPositioner"/> property.
    /// </summary>
    public static readonly DirectProperty<DialogOverlayPopupHost, IDialogPopupPositioner?> PopupPositionerProperty =
        DialogHost.PopupPositionerProperty.AddOwner<DialogOverlayPopupHost>(
            o => o.PopupPositioner,
            (o, v) => o.PopupPositioner = v);

    private readonly DialogHost _host;

    private bool _disableOpeningAnimation;
    private bool _isOpen;

    private IDialogPopupPositioner? _popupPositioner;

    internal readonly TaskCompletionSource<object?> DialogTaskCompletionSource = new();
    internal readonly DialogSession Session;

    static DialogOverlayPopupHost() {
        AffectsArrange<DialogOverlayPopupHost>(PopupPositionerProperty);
    }

    /// <summary>
    /// Initializes a new overlay popup host for a dialog session.
    /// </summary>
    /// <param name="host">Owning dialog host.</param>
    /// <param name="open">Optional callback invoked when dialog opens.</param>
    /// <param name="closing">Optional callback invoked before dialog closes.</param>
    public DialogOverlayPopupHost(DialogHost host, DialogOpenedEventHandler? open, DialogClosingEventHandler? closing) {
        _host = host;
        Session = new(host, this, open, closing);
    }

    /// <summary>
    /// Gets or sets whether the popup host is open.
    /// </summary>
    public bool IsOpen {
        get => _isOpen;
        set {
            SetAndRaise(IsOpenProperty, ref _isOpen, value);
            if (value) Show();
        }
    }

    /// <summary>
    /// Controls <see cref="Show"/> and <see cref="Hide"/> calls. Used for closing animations
    /// </summary>
    /// <remarks>
    /// Actually you should use <see cref="IsOpen"/> for opening and closing dialog
    /// </remarks>
    public bool IsActuallyOpen {
        get => GetValue(IsActuallyOpenProperty);
        set => SetValue(IsActuallyOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets whether opening animation is disabled for this popup.
    /// </summary>
    public bool DisableOpeningAnimation {
        get => _disableOpeningAnimation;
        set => SetAndRaise(DisableOpeningAnimationProperty, ref _disableOpeningAnimation, value);
    }

    /// <summary>
    /// Gets or sets popup positioning strategy.
    /// </summary>
    public IDialogPopupPositioner? PopupPositioner {
        get => _popupPositioner;
        set => SetAndRaise(PopupPositionerProperty, ref _popupPositioner, value);
    }

    internal void Show() {
        if (Parent == null) {
            Debug.Assert(_host.Root is not null, "Show called before DialogHost template is applied");
            _host.Root?.Children.Add(this);
        }

        // Set the minimum priority to allow overriding it everywhere
        ClearValue(IsActuallyOpenProperty);
        Focus();
    }

    internal void Hide() {
        _host.Root?.Children.Remove(this);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) {
        if (PopupPositioner is IDialogPopupPositionerConstrainable constrainable) {
            return base.MeasureOverride(constrainable.Constrain(availableSize));
        }

        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(Rect finalRect) {
        var margin = Margin;

        var size = new Size(
            Math.Max(0, finalRect.Width - margin.Left - margin.Right),
            Math.Max(0, finalRect.Height - margin.Top - margin.Bottom));

        var contentSize = new Size(
            Math.Max(0, Math.Min(size.Width, DesiredSize.Width - margin.Left - margin.Right)),
            Math.Max(0, Math.Min(size.Height, DesiredSize.Height - margin.Top - margin.Bottom)));
        var positioner = PopupPositioner ?? CenteredDialogPopupPositioner.Instance;
        var bounds = positioner.Update(size, contentSize);

        var (finalWidth, finalHeight) = ArrangeOverride(bounds.Size).Constrain(size);
        Bounds = new Rect(bounds.X + margin.Left, bounds.Y + margin.Top, finalWidth, finalHeight);
    }

    /// <summary>
    /// Returns the next focus target while the popup is active.
    /// </summary>
    /// <param name="element">Current focused element.</param>
    /// <param name="direction">Requested navigation direction.</param>
    /// <returns>
    /// A tuple indicating whether navigation was handled and the next focus target.
    /// </returns>
    public (bool handled, IInputElement? next) GetNext(IInputElement element, NavigationDirection direction) {
        // If current element isn't this popup host - ignoring
        if (!element.Equals(this)) {
            return (false, null);
        }

        // Finding the focusable descendant
        var focusable = this.GetVisualDescendants()
            .OfType<IInputElement>()
            .FirstOrDefault(visual => visual.Focusable);

        // Or returning the control itself to prevent focus escaping
        return (true, focusable ?? this);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        if (change.Property == IsActuallyOpenProperty && !change.GetNewValue<bool>()) {
            Hide();
        }

        base.OnPropertyChanged(change);
    }

    internal void Pop() {
        Hide();
        Show();
    }
}