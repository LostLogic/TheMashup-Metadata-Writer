using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheMashup_Metadata_Writer.Helpers;

namespace TheMashup_Metadata_Writer.ViewModels;

public class AppShellViewModel : INotifyPropertyChanged
{
    private bool isLoggedIn;
    public bool IsLoggedIn
    {
        get => isLoggedIn;
        set
        {
            isLoggedIn = value;
            OnPropertyChanged();
        }
    }

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

    public AppShellViewModel() { }

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
