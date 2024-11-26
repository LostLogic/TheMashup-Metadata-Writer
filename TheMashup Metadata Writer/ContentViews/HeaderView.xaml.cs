namespace TheMashup_Metadata_Writer.ContentViews;

public partial class HeaderView : ContentView
{
    public HeaderView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty EmailProperty = BindableProperty.Create(nameof(Email), typeof(string), typeof(HeaderView), string.Empty);
    public string Email
    {
        get => (string)GetValue(EmailProperty);
        set => SetValue(EmailProperty, value);
    }

    public static readonly BindableProperty LoggedInProperty = BindableProperty.Create(nameof(LoggedIn), typeof(bool), typeof(HeaderView), false);
    public bool LoggedIn
    {
        get => (bool)GetValue(LoggedInProperty);
        set => SetValue(LoggedInProperty, value);
    }

    public event EventHandler LoginButtonClicked = delegate { };
    public event EventHandler SettingsButtonClicked = delegate { };

    private void LoginButton_Clicked(object sender, EventArgs e)
    {
        LoginButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void SettingsButton_Clicked(object sender, EventArgs e)
    {
        SettingsButtonClicked?.Invoke(this, EventArgs.Empty);
    }
}