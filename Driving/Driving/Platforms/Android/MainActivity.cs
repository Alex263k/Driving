using Android;
using Android.App;
using Android.Content.PM; // Обязательно добавьте это для типа Permission
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System.Collections.Generic;
using System.Linq;

namespace Driving;

[Activity(Theme = "@style/Maui.MainTheme", MainLauncher = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState) // Добавили ? для Bundle
    {
        base.OnCreate(savedInstanceState);
        RequestExternalStoragePermissions();
    }

    private void RequestExternalStoragePermissions()
    {
        var permissions = new List<string>();

        // Используем полное имя Android.Content.PM.Permission, чтобы избежать путаницы
        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int)Permission.Granted)
            permissions.Add(Manifest.Permission.ReadExternalStorage);

        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
            permissions.Add(Manifest.Permission.WriteExternalStorage);

        if (permissions.Any())
        {
            ActivityCompat.RequestPermissions(this, permissions.ToArray(), 0);
        }
    }
}