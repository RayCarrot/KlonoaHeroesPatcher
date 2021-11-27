using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace KlonoaHeroesPatcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        AppViewModel = new AppViewModel();

        Startup += App_Startup;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    public new static App Current => (App)Application.Current;

    public AppViewModel AppViewModel { get; }

    private async void App_Startup(object sender, StartupEventArgs e)
    {
        AppViewModel.Init();

        if (e.Args.Any())
            await AppViewModel.LoadAsync(e.Args[0]);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Error: {e.Exception}");
    }
}