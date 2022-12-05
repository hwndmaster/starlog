using System.Windows.Controls;
using System.Windows.Documents;

namespace Genius.Starlog.UI.Behaviors;

public class RichTextDocumentBehavior
{
    public static readonly DependencyProperty BindableDocumentProperty = DependencyProperty.RegisterAttached(
        "BindableDocument",
        typeof(FlowDocument),
        typeof(RichTextDocumentBehavior),
        new PropertyMetadata(OnDocumentChanged));

    public static FlowDocument GetBindableDocument(DependencyObject element)
    {
        return (FlowDocument)element.GetValue(BindableDocumentProperty);
    }

    public static void SetBindableDocument(DependencyObject element, FlowDocument value)
    {
        element.SetValue(BindableDocumentProperty, value);
    }

    private static void OnDocumentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
    {
        var rtb = (RichTextBox)obj;
        rtb.Document = (FlowDocument)(args.NewValue ?? new FlowDocument());
    }
}
