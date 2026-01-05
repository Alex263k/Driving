using Android;
using Android.App;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;

[Activity(...)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestPermissions();
    }

    private void RequestPermissions()
    {
        var permissions = new List<string>();

        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != Permission.Granted)
            permissions.Add(Manifest.Permission.ReadExternalStorage);

        if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != Permission.Granted)
            permissions.Add(Manifest.Permission.WriteExternalStorage);

        if (permissions.Any())
        {
            ActivityCompat.RequestPermissions(this, permissions.ToArray(), 0);
        }
    }
}