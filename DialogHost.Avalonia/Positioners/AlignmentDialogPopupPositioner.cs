using Avalonia;
using Avalonia.Layout;

namespace DialogHostAvalonia.Positioners;

/// <summary>
/// Positions the popup according to <see cref="HorizontalAlignment"/>, <see cref="VerticalAlignment"/> and even <see cref="Margin"/>
/// </summary>
/// <remarks>
/// Default values for <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/> is <c>Stretch</c> and it will be act TopLeft alignment
/// </remarks>
public class AlignmentDialogPopupPositioner : AvaloniaObject, IDialogPopupPositioner, IDialogPopupPositionerConstrainable {
    /// <summary>
    /// Identifies the <see cref="HorizontalAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<HorizontalAlignment> HorizontalAlignmentProperty
        = Layoutable.HorizontalAlignmentProperty.AddOwner<AlignmentDialogPopupPositioner>();

    /// <summary>
    /// Identifies the <see cref="VerticalAlignment"/> property.
    /// </summary>
    public static readonly StyledProperty<VerticalAlignment> VerticalAlignmentProperty
        = Layoutable.VerticalAlignmentProperty.AddOwner<AlignmentDialogPopupPositioner>();

    /// <summary>
    /// Identifies the <see cref="Margin"/> property.
    /// </summary>
    public static readonly StyledProperty<Thickness> MarginProperty
        = Layoutable.MarginProperty.AddOwner<AlignmentDialogPopupPositioner>();

    /// <summary>
    /// Gets or sets horizontal alignment of the popup within available bounds.
    /// </summary>
    public HorizontalAlignment HorizontalAlignment {
        get => GetValue(HorizontalAlignmentProperty);
        set => SetValue(HorizontalAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets vertical alignment of the popup within available bounds.
    /// </summary>
    public VerticalAlignment VerticalAlignment {
        get => GetValue(VerticalAlignmentProperty);
        set => SetValue(VerticalAlignmentProperty, value);
    }

    /// <summary>
    /// Gets or sets the popup margin inside the anchor rectangle.
    /// </summary>
    public Thickness Margin {
        get => GetValue(MarginProperty);
        set => SetValue(MarginProperty, value);
    }

    // TODO: Changes in properties ^ should call this method
    /// <inheritdoc />
    public Rect Update(Size anchorRectangle, Size size) {
        var margin = GetValue(MarginProperty);

        var availableSpaceRect = new Rect(anchorRectangle);
        var constrainRect = availableSpaceRect.Deflate(margin);
        var rect = new Rect(size);
        if (GetValue(HorizontalAlignmentProperty) == HorizontalAlignment.Stretch) rect = rect.WithWidth(0);
        if (GetValue(VerticalAlignmentProperty) == VerticalAlignment.Stretch) rect = rect.WithHeight(0);
        var aligned = rect.Align(constrainRect, GetValue(HorizontalAlignmentProperty), GetValue(VerticalAlignmentProperty));
        return new Rect(margin.Left + aligned.Left, margin.Top + aligned.Top, aligned.Width, aligned.Height);
    }

    /// <inheritdoc />
    public Size Constrain(Size availableSize)
    {
        return availableSize.Deflate(Margin);
    }
}