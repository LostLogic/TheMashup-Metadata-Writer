using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TheMashup_Metadata_Writer.Helpers;
using TheMashup_Metadata_Writer.Models;

namespace TheMashup_Metadata_Writer.ViewModels;

public class SongViewModel : INotifyPropertyChanged
{
    private ObservableCollection<SongModel> songs = [];
    public ObservableCollection<SongModel> Songs
    {
        get => songs;
        set
        {
            songs = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<MP3FileModel> mp3Files = [];
    public ObservableCollection<MP3FileModel> Mp3Files
    {
        get => mp3Files;
        set
        {
            mp3Files = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<MP3FileModel> filteredMp3Files = [];
    public ObservableCollection<MP3FileModel> FilteredMp3Files
    {
        get => filteredMp3Files;
        set
        {
            filteredMp3Files = value;
            OnPropertyChanged();
        }
    }

    private bool showOnlyUntagged;
    public bool ShowOnlyUntagged
    {
        get => showOnlyUntagged;
        set
        {
            showOnlyUntagged = value;
            OnPropertyChanged();
            UpdateFilteredMp3Files();
        }
    }

    private bool isLoggedIn;
    public bool IsLoggedIn
    {
        get => isLoggedIn;
        set
        {
            isLoggedIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchEnabledAndLoggedIn));
        }
    }

    public bool SearchEnabledAndLoggedIn => IsLoggedIn && IsSearchButtonEnabled;

    private string loggedInEmail = string.Empty;
    public string LoggedInEmail
    {
        get => loggedInEmail;
        set
        {
            loggedInEmail = value;
            OnPropertyChanged();
        }
    }

    private bool cancelButtonEnabled = false;
    public bool CancelButtonEnabled
    {
        get => cancelButtonEnabled;
        set
        {
            cancelButtonEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool retagBatch = false;
    public bool RetagBatch
    {
        get => retagBatch;
        set
        {
            retagBatch = value;
            OnPropertyChanged();
        }
    }

    private string searchActivityMessage = string.Empty;
    public string SearchActivityMessage
    {
        get => searchActivityMessage;
        set
        {
            searchActivityMessage = value;
            OnPropertyChanged();
        }
    }

    private bool isGetMp3FilesButtonEnabled;
    public bool IsGetMp3FilesButtonEnabled
    {
        get => isGetMp3FilesButtonEnabled;
        set
        {
            isGetMp3FilesButtonEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchEnabledAndLoggedIn));
        }
    }

    private string selectedFolderPath = string.Empty;
    public string SelectedFolderPath
    {
        get => selectedFolderPath;
        set
        {
            selectedFolderPath = value;
            OnPropertyChanged();
            IsGetMp3FilesButtonEnabled = !string.IsNullOrEmpty(value);
        }
    }

    private string searchResultMessage = string.Empty;
    public string SearchResultMessage
    {
        get => searchResultMessage;
        set
        {
            searchResultMessage = value;
            OnPropertyChanged();
        }
    }

    private MP3FileModel selectedMp3File = new();
    public MP3FileModel SelectedMp3File
    {
        get => selectedMp3File;
        set
        {

            selectedMp3File = value;
            OnPropertyChanged();
        }
    }

    public ICommand GetMetadataCommand
    {
        get;
    }

    public ICommand WriteMetadataCommand
    {
        get;
    }

    public SongViewModel()
    {
        Songs = [];
        Mp3Files = [];
        FilteredMp3Files = [];
        GetMetadataCommand = new Command<MP3FileModel>(async (mp3File) => await GetMetadataAsync(mp3File));
        WriteMetadataCommand = new Command<SongModel>((song) => SaveMetadata(song));

        Mp3Files.CollectionChanged += (s, e) => UpdateFilteredMp3Files();

        CheckIfLoggedIn();
    }

    private async void CheckIfLoggedIn()
    {
        IsLoggedIn = await TheMashup.CheckSessionValidityAsync();
        LoggedInEmail = IsLoggedIn ? TheMashup.RetrieveStoredEmail() : string.Empty;
    }
    
    private void UpdateFilteredMp3Files()
    {
        if (ShowOnlyUntagged)
        {
            FilteredMp3Files = new ObservableCollection<MP3FileModel>(Mp3Files.Where(file => !file.Tagged));
        }
        else
        {
            FilteredMp3Files = new ObservableCollection<MP3FileModel>(Mp3Files);
        }
    }

    public void RefreshLoginStatus()
    {
        IsLoggedIn = TheMashup.IsLoggedIn();

        if (IsLoggedIn)
        {
            LoggedInEmail = TheMashup.RetrieveStoredEmail();
        }
        else
        {
            LoggedInEmail = "";
        }
    }

    private async Task GetMetadataAsync(MP3FileModel mp3File)
    {
        SelectedMp3File = mp3File;
        SetActiveMp3(mp3File);
        await SearchSongsAsync(mp3File.Artist, mp3File.Title);
    }

    private void SetActiveMp3(MP3FileModel mp3File)
    {
        foreach (var file in Mp3Files)
        {
            file.Selected = file.FilePath == mp3File.FilePath;
        }
        SelectedMp3File = mp3File;
    }

    private void SaveMetadata(SongModel song)
    {
        var result = MetadataMaster.WriteMetadata(song, SelectedMp3File);
        SearchResultMessage = result;

        if (result.StartsWith("success", StringComparison.InvariantCultureIgnoreCase))
        {
            SelectedMp3File.Tagged = true;
        }
    }

    public void GetMp3FilesAsync()
    {
        if (string.IsNullOrEmpty(SelectedFolderPath))
        {
            return;
        }

        var mp3Files = Directory.GetFiles(SelectedFolderPath, "*.mp3", SearchOption.AllDirectories);

        var newMp3Files = new List<MP3FileModel>();

        foreach (var file in mp3Files)
        {
            // Skip Mac OSX metadata files
            if (file.StartsWith("._"))
            {
                continue;
            }

            var tagFile = TagLib.File.Create(file);
            var song = new MP3FileModel
            {
                Title = tagFile.Tag.Title,
                Artist = tagFile.Tag.FirstPerformer,
                FilePath = file
            };

            var mashupId = TagLib.Id3v2.UserTextInformationFrame.Get((TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, true), "TheMashupID", false);
            if (mashupId != null)
            {
                song.Tagged = true;
            }

            newMp3Files.Add(song);
        }

        Mp3Files = new ObservableCollection<MP3FileModel>(newMp3Files);
        UpdateFilteredMp3Files();
        Mp3Files.CollectionChanged += (s, e) => UpdateFilteredMp3Files();
    }


    public async Task SearchSongsAsync(string artist, string title, int waitMsAfterCompletion = 0, CancellationToken cancellationToken = new CancellationToken())
    {
        IsSearchButtonEnabled = false;
        IsSearching = true;
        SearchActivityMessage = "Searching for " + title + " by " + artist;
        var results = await TheMashup.SearchSongAsync(artist, title, cancellationToken);
        Songs.Clear();

        if (results.Count > 0)
        {
            foreach (var song in results)
            {
                Songs.Add(song);
            }

            if (results.Count == 1)
            {
                SearchResultMessage = $"{results.Count} result found.";
            }
            else
            {
                SearchResultMessage = $"{results.Count} results found.";
            }
        }
        else
        {
            SearchResultMessage = "No results found.";
        }

        await Task.Delay(waitMsAfterCompletion, cancellationToken);

        SearchActivityMessage = "";
        IsSearchButtonEnabled = true;
        IsSearching = false;
    }

    public async Task BatchSearchAndSaveSongsAsync(CancellationToken token)
    {
        CancelButtonEnabled = true;
        try
        {
            foreach (var file in Mp3Files)
            {
                SetActiveMp3(file);
                if (!RetagBatch && file.Tagged)
                {
                    continue;
                }

                await SearchSongsAsync(file.Artist, file.Title, 0, token);

                var matchingSongs = Songs.Where(s => string.Equals(s.Title, file.Title, StringComparison.InvariantCultureIgnoreCase)
                                                    && string.Equals(s.Artist, file.Artist, StringComparison.InvariantCultureIgnoreCase))
                                         .ToList();

                if (Songs.Count == 1)
                {
                    SearchActivityMessage = "Writing metadata to file";
                    SaveMetadata(Songs[0]);
                }
                else if (matchingSongs.Count == 1)
                {
                    SearchActivityMessage = "Multiple matches found, writing best match";
                    var bestMatch = matchingSongs.First(); // Customize this to select the best match
                    SaveMetadata(bestMatch);
                }

                token.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException)
        {
            SearchActivityMessage = "Operation cancelled";
        }
        finally
        {
            CancelButtonEnabled = false;
        }
    }

    private bool isSearchButtonEnabled = true;
    public bool IsSearchButtonEnabled
    {
        get => isSearchButtonEnabled;
        set
        {
            isSearchButtonEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool isSearching;
    public bool IsSearching
    {
        get => isSearching;
        set
        {
            isSearching = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
