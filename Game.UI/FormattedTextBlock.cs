using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Game.UI;

public class FormattedTextBlock : TextBlock
{
    public static readonly StyledProperty<double?> ValueProperty =
        AvaloniaProperty.Register<FormattedTextBlock, double?>(nameof(Value));

    public static readonly StyledProperty<int?> MaxStringProperty =
        AvaloniaProperty.Register<FormattedTextBlock, int?>(nameof(MaxString));

    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int? MaxString
    {
        get => GetValue(MaxStringProperty);
        set => SetValue(MaxStringProperty, value);
    }
    
    public static double MeasureTextWidth(string text, Typeface typeface, double fontSize)
    {
        var runProps = new GenericTextRunProperties(typeface, fontSize);

        var paraProps = new GenericTextParagraphProperties(
            runProps,
            TextAlignment.Left,
            TextWrapping.NoWrap);

        var formatter = TextFormatter.Current;

        var line = formatter.FormatLine(
            new SimpleTextSource(text, runProps),
            0,
            double.PositiveInfinity,
            paraProps);

        return line.Width;
    }

    static FormattedTextBlock()
    {
        ValueProperty.Changed.AddClassHandler<FormattedTextBlock>((x, e) => x.UpdateText());
        MaxStringProperty.Changed.AddClassHandler<FormattedTextBlock>((x, e) => x.UpdateText());
    }

    public FormattedTextBlock()
    {
    }

    public override void ApplyTemplate()
    {
        Width = MaxString.HasValue ? MeasureTextWidth(new string(Enumerable.Repeat('1',MaxString.Value).ToArray()), new Typeface(FontFamily), FontSize) :
            MeasureTextWidth("999.99M", new Typeface(FontFamily), FontSize);
    }

    public static string FormatWithPrefix(double value)
    {
        string[] units = { "", "k", "M", "G", "T" };
        int unitIndex = 0;

        while (Math.Abs(value) >= 1000 && unitIndex < units.Length - 1)
        {
            value /= 1000;
            unitIndex++;
        }

        return $"{value:0.00}{units[unitIndex]}";
    }
    
    private void UpdateText()
    {
        if(Value.HasValue)
        Text = FormatWithPrefix(Value.Value);
        // if (Value != null)
        // {
        //     Text = FormatWithPrefix(Value.Value);
        // }
        // else
        // {
        //     Text = Value?.ToString() ?? string.Empty;
        // }
    }
}