using CommunityToolkit.Maui.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheMashup_Metadata_Writer.Helpers;

namespace TheMashup_Metadata_Writer.Popups;

public partial class LoginPopup : Popup, INotifyPropertyChanged
{
    public LoginPopup()
    {
        InitializeComponent();
        BindingContext = this;

        if (LoggedIn)
        {
            LoginResponse = "Logged in as " + EmailAddress;
        }
        else
        {
            LoginResponse = "Enter your email address and password to login";
        }
    }

    private bool loggedIn;
    public bool LoggedIn
    {
        get => loggedIn;
        set
        {
            loggedIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EmailAddress));

            if (LoggedIn)
            {
                LoginResponse = "Logged in as " + EmailAddress;
            }
            else
            {
                LoginResponse = "Enter your email address and password to login";
            }
        }
    }

    private string emailAddress = string.Empty;
    public string EmailAddress
    {
        get => emailAddress;
        set
        {
            emailAddress = value;
            OnPropertyChanged();
        }
    }

    private string password = string.Empty;
    public string Password
    {
        get => password;
        set
        {
            password = value;
            OnPropertyChanged();
        }
    }

    private string loginResponse = string.Empty;
    public string LoginResponse
    {
        get => loginResponse;
        set
        {
            loginResponse = value;
            OnPropertyChanged();
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void ButtonLogin_Clicked(object sender, EventArgs e)
    {
        LoginSpinner.IsVisible = true;
        var result = await TheMashup.GetLoginCookieAsync(EmailAddress, Password);
        LoginSpinner.IsVisible = false;

        if (result == "Success")
        {
            LoggedIn = true;
            LoginResponse = "User logged in successfully";
            LabelLoginResponse.TextColor = Colors.Black;
        }
        else
        {
            LoginResponse = result;
            LabelLoginResponse.TextColor = Colors.Red;
        }
    }

    private void ButtonLogout_Clicked(object sender, EventArgs e)
    {
        TheMashup.ClearStoredCookie();
        LoggedIn = false;
    }
}