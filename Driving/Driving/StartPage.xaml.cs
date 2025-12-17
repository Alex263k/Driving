namespace Driving;

public partial class StartPage : ContentPage
{
    public StartPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Update the high score label from local storage
        int high = Preferences.Get("HighScore", 0);
        HighScoreLabel.Text = $"HIGH SCORE: {high}";
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        // Navigate to the Game scene
        await Navigation.PushAsync(new GamePage());
    }
}