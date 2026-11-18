using System.IO;
using System.Windows;
using Microsoft.Win32;
using PS2EXE.Studio.Core;
using PS2EXE.Studio.Infrastructure;

namespace PS2EXE.Studio.App;

public partial class MainWindow : Window
{
    private readonly Ps2ExeRunner _runner = new();
    private CancellationTokenSource? _active;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            var status = await _runner.ProbeAsync();
            ModuleText.Text = status.Available ? $"PS2EXE {status.Version} detectado" : "PS2EXE indisponível";
            LogText.Text = status.Details;
            CompileButton.IsEnabled = status.Available;
        };
    }

    private void SelectInput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Scripts PowerShell (*.ps1)|*.ps1" };
        if (dialog.ShowDialog(this) != true) return;
        InputPath.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(OutputPath.Text))
            OutputPath.Text = Path.ChangeExtension(dialog.FileName, ".exe");
    }

    private void SelectOutput_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Executáveis (*.exe)|*.exe", DefaultExt = ".exe" };
        if (dialog.ShowDialog(this) == true) OutputPath.Text = dialog.FileName;
    }

    private void SelectIcon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Ícones Windows (*.ico)|*.ico" };
        if (dialog.ShowDialog(this) == true) IconPath.Text = dialog.FileName;
    }

    private BuildProfile CreateProfile() => new()
    {
        InputFile = InputPath.Text.Trim(),
        OutputFile = OutputPath.Text.Trim(),
        IconFile = string.IsNullOrWhiteSpace(IconPath.Text) ? null : IconPath.Text.Trim(),
        NoConsole = NoConsole.IsChecked == true,
        RequireAdmin = RequireAdmin.IsChecked == true,
        DpiAware = DpiAware.IsChecked == true,
        WinFormsDpiAware = WinFormsDpiAware.IsChecked == true,
        Architecture = (TargetArchitecture)Architecture.SelectedIndex,
        Apartment = (ApartmentMode)Apartment.SelectedIndex,
        Title = AppTitle.Text,
        Product = Product.Text,
        Company = Company.Text,
        Version = Version.Text,
        Description = Description.Text
    };

    private async void Compile_Click(object sender, RoutedEventArgs e)
    {
        if (_active is not null) return;
        var profile = CreateProfile();
        var errors = BuildValidation.Validate(profile);
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _active = new CancellationTokenSource();
        CompileButton.IsEnabled = false;
        StatusText.Text = "Compilando...";
        LogText.Text = "Iniciando processo PS2EXE...";
        try
        {
            var result = await _runner.BuildAsync(profile, _active.Token);
            LogText.Text = $"{result.Output}{Environment.NewLine}{result.Error}{Environment.NewLine}{result.Message}";
            StatusText.Text = result.Success ? "EXE gerado" : "Falha na compilação";
            if (result.Success)
                MessageBox.Show(this, $"EXE gerado em:{Environment.NewLine}{profile.OutputFile}{Environment.NewLine}SHA-256: {result.Sha256}",
                    "PS2EXE Studio", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            _active.Dispose();
            _active = null;
            CompileButton.IsEnabled = true;
        }
    }
}
