using CommunityToolkit.Maui.Views;
using TheMashup_Metadata_Writer.Helpers;
using TheMashup_Metadata_Writer.Popups;
using TheMashup_Metadata_Writer.ViewModels;

namespace TheMashup_Metadata_Writer;

public partial class AppShell : Shell
{
    private readonly AppShellViewModel viewModel;

    public AppShell()
    {
        InitializeComponent();

        if (BindingContext is AppShellViewModel viewModel)
        {
            this.viewModel = viewModel;
        }
    }
}
