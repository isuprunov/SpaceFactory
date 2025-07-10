using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Game.UI;


public class ImageBoundColumn : DataGridBoundColumn
{
    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        var image = new Image
        {
            Width = 32,
            Height = 32,
            Stretch = Stretch.Uniform
        };

        if (Binding != null)
            image.Bind(Image.SourceProperty, Binding);

        return image;
    }

    protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
    {
        throw new NotImplementedException();
    }


    protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
    {
        throw new NotImplementedException();
    }
}