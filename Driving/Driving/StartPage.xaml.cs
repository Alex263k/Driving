using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Plugin.Maui.Audio; // Необходим установленный NuGet пакет Plugin.Maui.Audio

namespace Driving;

public partial class StartPage : ContentPage
{
    // Ключи для хранения данных игры
    private const string HighScoreKey = "HighScore";
    private const string TotalCoinsKey = "TotalCoins";
    private const string GamesPlayedKey = "GamesPlayed";
    private const string SelectedCarKey = "SelectedCar";
    private const string DurabilityLevelKey = "DurabilityLevel";
    private const string SpeedLevelKey = "SpeedLevel";
    private const string CustomSkinPathKey = "CustomSkinPath";

    // Класс для хранения данных автомобиля
    public class CarInfo
    {
        public string Name { get; set; } = string.Empty;
        public Color Color { get; set; } = Colors.LimeGreen;
        public string Emoji { get; set; } = "🚗";
        public string? CustomImagePath { get; set; }
    }

    private List<CarInfo> _cars = new List<CarInfo>();
    private int _currentCarIndex = 0;

    // Поля для работы со звуком
    private IAudioPlayer _buttonClickSound;
    private IAudioPlayer _carSwitchSound;
    private IAudioPlayer _startGameSound;

    public StartPage()
    {
        InitializeComponent();
        InitializeCars();
        LoadSounds(); // Инициализация звуков при запуске
    }

    private async void LoadSounds()
    {
        try
        {
            // Загружаем аудиофайлы из ресурсов MauiAsset
            _buttonClickSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("button_click.wav"));
            _carSwitchSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("car_switch.wav"));
            _startGameSound = AudioManager.Current.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("start_game.wav"));

            // Устанавливаем базовую громкость
            if (_buttonClickSound != null) _buttonClickSound.Volume = 0.5;
            if (_carSwitchSound != null) _carSwitchSound.Volume = 0.5;
            if (_startGameSound != null) _startGameSound.Volume = 0.7;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"!!! ОШИБКА ЗАГРУЗКИ ЗВУКА НА STARTPAGE: {ex.Message}");
        }
    }

    private void PlaySound(IAudioPlayer player)
    {
        if (player != null)
        {
            if (player.IsPlaying) player.Stop();
            player.Play();
        }
    }

    private void InitializeCars()
    {
        // Загрузка пути кастомного скина, если он существует
        string? customSkinPath = null;
        if (Preferences.ContainsKey(CustomSkinPathKey))
        {
            customSkinPath = Preferences.Get(CustomSkinPathKey, string.Empty);
            if (!File.Exists(customSkinPath))
                customSkinPath = null;
        }

        // Инициализация коллекции автомобилей
        _cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" },
            new CarInfo {
                Name = "CUSTOM",
                Color = Colors.Purple,
                Emoji = "🎨",
                CustomImagePath = customSkinPath
            }
        };

        // Загрузка последнего выбранного авто
        _currentCarIndex = Preferences.Get(SelectedCarKey, 0);
        if (_currentCarIndex >= _cars.Count)
        {
            _currentCarIndex = 0;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadAndDisplayStats();
        UpdateCarDisplay();
    }

    private void LoadAndDisplayStats()
    {
        int highScore = Preferences.Get(HighScoreKey, 0);
        int totalCoins = Preferences.Get(TotalCoinsKey, 0);
        int gamesPlayed = Preferences.Get(GamesPlayedKey, 0);

        HighScoreLabel.Text = highScore.ToString();
        CoinsLabel.Text = totalCoins.ToString();
        GamesPlayedLabel.Text = gamesPlayed.ToString();
    }

    private void UpdateCarDisplay()
    {
        if (_cars.Count == 0 || _currentCarIndex >= _cars.Count) return;

        var currentCar = _cars[_currentCarIndex];

        CarBody.Color = currentCar.Color;
        CarNameLabel.Text = currentCar.Name;
        CarEmojiLabel.Text = currentCar.Emoji;

        bool isCustomCar = currentCar.Name == "CUSTOM";
        CustomCarImage.IsVisible = isCustomCar && !string.IsNullOrEmpty(currentCar.CustomImagePath);
        CarBody.IsVisible = !CustomCarImage.IsVisible;

        if (isCustomCar && !string.IsNullOrEmpty(currentCar.CustomImagePath) && File.Exists(currentCar.CustomImagePath))
        {
            try
            {
                CustomCarImage.Source = ImageSource.FromFile(currentCar.CustomImagePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading custom image: {ex.Message}");
                CustomCarImage.IsVisible = false;
                CarBody.IsVisible = true;
            }
        }

        UploadCustomButton.IsVisible = isCustomCar;

        PrevCarButton.IsEnabled = _currentCarIndex > 0;
        NextCarButton.IsEnabled = _currentCarIndex < _cars.Count - 1;

        PrevCarButton.Opacity = PrevCarButton.IsEnabled ? 1.0 : 0.5;
        NextCarButton.Opacity = NextCarButton.IsEnabled ? 1.0 : 0.5;
    }

    private async void OnPrevCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex > 0)
        {
            PlaySound(_carSwitchSound); // Звук переключения
            _currentCarIndex--;
            UpdateCarDisplay();

            var button = (Button)sender;
            await button.ScaleTo(0.8, 100, Easing.CubicInOut);
            await button.ScaleTo(1.0, 100, Easing.CubicInOut);

            await CarDisplayGrid.TranslateTo(-30, 0, 100);
            CarDisplayGrid.TranslationX = 30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnNextCarClicked(object sender, EventArgs e)
    {
        if (_currentCarIndex < _cars.Count - 1)
        {
            PlaySound(_carSwitchSound); // Звук переключения
            _currentCarIndex++;
            UpdateCarDisplay();

            var button = (Button)sender;
            await button.ScaleTo(0.8, 100, Easing.CubicInOut);
            await button.ScaleTo(1.0, 100, Easing.CubicInOut);

            await CarDisplayGrid.TranslateTo(30, 0, 100);
            CarDisplayGrid.TranslationX = -30;
            await CarDisplayGrid.TranslateTo(0, 0, 100);
        }
    }

    private async void OnUploadCustomClicked(object sender, EventArgs e)
    {
        PlaySound(_buttonClickSound); // Звук нажатия
        try
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Выберите изображение для скина",
                FileTypes = FilePickerFileType.Images
            });

            if (result == null) return;

            var targetPath = Path.Combine(FileSystem.AppDataDirectory, "custom_car.png");

            using (var sourceStream = await result.OpenReadAsync())
            using (var targetStream = File.Create(targetPath))
            {
                await sourceStream.CopyToAsync(targetStream);
            }

            Preferences.Set(CustomSkinPathKey, targetPath);
            _cars[_currentCarIndex].CustomImagePath = targetPath;

            UpdateCarDisplay();
            await DisplayAlert("Успех!", "Скин успешно загружен!", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось загрузить изображение: {ex.Message}", "OK");
        }
    }

    private async void OnUpgradesClicked(object sender, EventArgs e)
    {
        PlaySound(_buttonClickSound); // Звук нажатия
        var button = (Button)sender;
        await button.ScaleTo(0.95, 100, Easing.CubicInOut);
        await button.ScaleTo(1.0, 100, Easing.CubicInOut);

        await Navigation.PushAsync(new UpgradePage());
    }

    private async void OnStartClicked(object sender, EventArgs e)
    {
        PlaySound(_startGameSound); // Звук начала игры

        var button = (Button)sender;
        await button.ScaleTo(0.95, 100, Easing.CubicInOut);
        await button.ScaleTo(1.0, 100, Easing.CubicInOut);

        // Увеличиваем счетчик сыгранных игр
        int gamesPlayed = Preferences.Get(GamesPlayedKey, 0);
        Preferences.Set(GamesPlayedKey, gamesPlayed + 1);

        // Сохраняем выбранную машину
        Preferences.Set(SelectedCarKey, _currentCarIndex);

        if (_currentCarIndex < _cars.Count)
        {
            var selectedCar = _cars[_currentCarIndex];
            await Navigation.PushAsync(new GamePage(selectedCar));
        }
        else
        {
            var defaultCar = new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" };
            await Navigation.PushAsync(new GamePage(defaultCar));
        }
    }

    public static CarInfo GetSelectedCar()
    {
        int selectedIndex = Preferences.Get(SelectedCarKey, 0);

        string customPath = Preferences.Get(CustomSkinPathKey, string.Empty);

        var cars = new List<CarInfo>
        {
            new CarInfo { Name = "BASIC", Color = Colors.LimeGreen, Emoji = "🚗" },
            new CarInfo { Name = "SPORTS", Color = Colors.Red, Emoji = "🏎️" },
            new CarInfo { Name = "POLICE", Color = Colors.Blue, Emoji = "🚓" },
            new CarInfo { Name = "TAXI", Color = Colors.Yellow, Emoji = "🚖" },
            new CarInfo { Name = "RACING", Color = Colors.Magenta, Emoji = "🏁" },
            new CarInfo { Name = "VIP", Color = Colors.Gold, Emoji = "⭐" },
            new CarInfo {
                Name = "CUSTOM",
                Color = Colors.Purple,
                Emoji = "🎨",
                CustomImagePath = customPath
            }
        };

        if (selectedIndex >= 0 && selectedIndex < cars.Count)
        {
            return cars[selectedIndex];
        }

        return cars[0];
    }
}