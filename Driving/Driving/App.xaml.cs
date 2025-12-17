namespace Driving;

// The 'partial' keyword is essential for XAML linking
public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    // Modern .NET 9 way to set the starting page
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new NavigationPage(new StartPage()));
    }
}