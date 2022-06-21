using CaptiveBrowser.ModelViews;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CaptiveBrowser.Pages;

class UnitOfWorkTemplateSelector : DataTemplateSelector
{
    public DataTemplate InProgressTemplate { get; set; } = null!;
    public DataTemplate FinishedTemplate { get; set; } = null!;
    public DataTemplate FailedTemplate { get; set; } = null!;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is UnitOfWorkModelView uof)
        {
            return uof.Status switch
            {
                Status.InProgress => InProgressTemplate,
                Status.Finished => FinishedTemplate,
                Status.Failed => FailedTemplate,
                _ => InProgressTemplate,
            };
        }

        return InProgressTemplate;
    }
}
