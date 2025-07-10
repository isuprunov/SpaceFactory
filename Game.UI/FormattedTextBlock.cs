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

    public static readonly StyledProperty<string?> StringFormatProperty =
        AvaloniaProperty.Register<FormattedTextBlock, string?>(nameof(StringFormat));

    public double? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string? StringFormat
    {
        get => GetValue(StringFormatProperty);
        set => SetValue(StringFormatProperty, value);
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
        StringFormatProperty.Changed.AddClassHandler<FormattedTextBlock>((x, e) => x.UpdateText());
    }

    public FormattedTextBlock()
    {
        Width = MeasureTextWidth("999.99M", new Typeface(FontFamily), FontSize);    
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
        
        if (Value != null && StringFormat != null)
        {
            Text = FormatWithPrefix(Value.Value);
        }
        else
        {
            Text = Value?.ToString() ?? string.Empty;
        }
    }
}