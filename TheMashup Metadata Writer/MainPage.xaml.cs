using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using TheMashup_Metadata_Writer.Helpers;
using TheMashup_Metadata_Writer.Popups;
using TheMashup_Metadata_Writer.ViewModels;

namespace TheMashup_Metadata_Writer;

public partial class MainPage : ContentPage
{
    private readonly SongViewModel viewModel;
    private CancellationTokenSource _cancellationTokenSource = new();

    public MainPage()
    {
        InitializeComponent();


        if (BindingContext is SongViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }
        else
        {
            // Handle the case where BindingContext is not a SongViewModel
            // For example, you could log an error or throw an exception
            throw new InvalidOperationException("BindingContext is not of type SongViewModel");
        }
    }

    private async void OnBrowseFolderClicked(object sender, EventArgs e)
    {
        CancellationTokenSource source = new();
        CancellationToken token = source.Token;

#pragma warning disable CA1416 // Validate platform compatibility
        var folder = await FolderPicker.Default.PickAsync(token);
#pragma warning restore CA1416 // Validate platform compatibility

        if (folder.IsSuccessful)
        {
            viewModel.IsSearching = true;
            viewModel.SearchActivityMessage = "Getting folder contents";
            viewModel.SelectedFolderPath = folder.Folder.Path;
            viewModel.GetMp3FilesAsync();
            viewModel.IsSearching = false;
            viewModel.SearchActivityMessage = "";
        }
    }

    private async void OnBatchWriteClicked(object sender, EventArgs e)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        btnCancel.IsEnabled = true;
        btnCancel.Text = "Cancel";
        await viewModel.BatchSearchAndSaveSongsAsync(_cancellationTokenSource.Token);
    }

    private async void OnSearchButtonClicked(object sender, EventArgs e)
    {
        string artist = ArtistEntry.Text;
        string title = TitleEntry.Text;

        await viewModel.SearchSongsAsync(artist, title, 0, new CancellationToken());
    }

    private void HeaderView_SettingsButtonClicked(object sender, EventArgs e)
    {

    }

    private async void HeaderView_LoginButtonClicked(object sender, EventArgs e)
    {
        var loginPopup = new LoginPopup();

        if (TheMashup.IsLoggedIn())
        {
            loginPopup.LoggedIn = true;
            loginPopup.EmailAddress = TheMashup.RetrieveStoredEmail();
        }
        else
        {
            loginPopup.LoggedIn = false;
            loginPopup.EmailAddress = "";
        }

        await this.ShowPopupAsync(loginPopup, CancellationToken.None);

        viewModel.RefreshLoginStatus();
    }

    private void BtnCancel_Clicked(object sender, EventArgs e)
    {
        btnCancel.IsEnabled = false;
        btnCancel.Text = "Cancelling...";
        _cancellationTokenSource?.Cancel();
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SongViewModel.SelectedMp3File))
        {
            if (sender is SongViewModel viewModel)
            {
                var collectionView = this.FindByName<CollectionView>("colviewMp3Files");
                collectionView.ScrollTo(viewModel.SelectedMp3File, position: ScrollToPosition.Center, animate: true);
            }
        }
    }
}
