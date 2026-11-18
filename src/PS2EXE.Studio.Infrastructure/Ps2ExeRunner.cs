using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PS2EXE.Studio.Core;

namespace PS2EXE.Studio.Infrastructure;

/// <summary>Invoke the installed PS2EXE module in an isolated Windows PowerShell process.
/// The runner sends a JSON profile to a fixed PowerShell script; user paths are never interpolated into code.</summary>
public sealed class Ps2ExeRunner
{
    private const string Probe = """
        $ErrorActionPreference = 'Stop'
        Import-Module PS2EXE -ErrorAction Stop
        $m = Get-Module PS2EXE
        [Console]::Out.WriteLine($m.Version.ToString())
        """;

    private const string BuildScript = """
        param([Parameter(Mandatory=$true)][string]$ManifestPath)
        $ErrorActionPreference = 'Stop'
        Import-Module PS2EXE -ErrorAction Stop
        $p = Get-Content -LiteralPath $ManifestPath -Raw -Encoding UTF8 | ConvertFrom-Json
        $args = @{ inputFile = [string]$p.InputFile; outputFile = [string]$p.OutputFile }
        if ($p.NoConsole) { $args.noConsole = $true }
        switch ([string]$p.Architecture) {
          'X86' { $args.x86 = $true }
          'X64' { $args.x64 = $true }
        }
        switch ([string]$p.Apartment) {
          'STA' { $args.STA = $true }
          'MTA' { $args.MTA = $true }
        }
        if ($p.RequireAdmin) { $args.requireAdmin = $true }
        if ($p.DpiAware) { $args.DPIAware = $true }
        if ($p.WinFormsDpiAware) { $args.winFormsDPIAware = $true }
        foreach ($key in @('Title','Product','Company','Description','Version','Copyright')) {
          $value = [string]$p.$key
          if (-not [string]::IsNullOrWhiteSpace($value)) {
            $parameter = $key.ToLowerInvariant()
            $args[$parameter] = $value
          }
        }
        if (-not [string]::IsNullOrWhiteSpace([string]$p.IconFile)) {
          $args.iconFile = [string]$p.IconFile
        }
        if ($p.EmbeddedFiles -and @($p.EmbeddedFiles.PSObject.Properties).Count -gt 0) {
          $files = @{}
          foreach ($item in $p.EmbeddedFiles.PSObject.Properties) {
            $files[[string]$item.Name] = [string]$item.Value
          }
          $args.embedFiles = $files
        }
        Invoke-ps2exe @args
        if (-not (Test-Path -LiteralPath ([string]$p.OutputFile) -PathType Leaf)) {
          throw 'PS2EXE did not produce the requested executable.'
        }
        """;

    public async Task<ModuleStatus> ProbeAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await RunAsync(Probe, null, ct);
            return result.ExitCode == 0
                ? new ModuleStatus(true, result.Stdout.Trim(), "PS2EXE localizado em Windows PowerShell.")
                : new ModuleStatus(false, "", result.Stderr.Length > 0 ? result.Stderr : result.Stdout);
        }
        catch (Exception ex) { return new ModuleStatus(false, "", ex.Message); }
    }

    public async Task<BuildResult> BuildAsync(BuildProfile profile, CancellationToken ct = default)
    {
        var issues = BuildValidation.Validate(profile);
        if (issues.Count > 0) return new BuildResult(false, -1, "", "", null, string.Join(Environment.NewLine, issues));

        var output = Path.GetFullPath(profile.OutputFile);
        var directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        var work = Path.Combine(Path.GetTempPath(), "PS2EXEStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(work);
        try
        {
            var manifest = Path.Combine(work, "manifest.json");
            await File.WriteAllTextAsync(manifest,
                JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false), ct);
            var result = await RunAsync(BuildScript, manifest, ct);
            var exists = File.Exists(output);
            var success = result.ExitCode == 0 && exists && new FileInfo(output).Length > 0;
            string? hash = null;
            if (success)
            {
                await using var stream = File.OpenRead(output);
                hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
            }
            return new BuildResult(success, result.ExitCode, result.Stdout, result.Stderr, hash,
                success ? "Arquivo EXE criado; execução e estrutura PE ainda não verificadas." :
                "Falha de compilação ou executável de saída ausente.");
        }
        catch (OperationCanceledException)
        {
            return new BuildResult(false, -2, "", "Compilação cancelada.", null, "Cancelado");
        }
        catch (Exception ex)
        {
            return new BuildResult(false, -1, "", ex.ToString(), null, "Erro interno de compilação.");
        }
        finally
        {
            try { Directory.Delete(work, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunAsync(
        string script, string? manifestPath, CancellationToken ct)
    {
        var working = Path.Combine(Path.GetTempPath(), "PS2EXEStudio", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(working);
        try
        {
            var path = Path.Combine(working, "runner.ps1");
            await File.WriteAllTextAsync(path, script, new UTF8Encoding(false), ct);
            var start = new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "WindowsPowerShell", "v1.0", "powershell.exe"),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            start.ArgumentList.Add("-NoLogo");
            start.ArgumentList.Add("-NoProfile");
            start.ArgumentList.Add("-NonInteractive");
            start.ArgumentList.Add("-File");
            start.ArgumentList.Add(path);
            if (manifestPath is not null)
            {
                start.ArgumentList.Add("-ManifestPath");
                start.ArgumentList.Add(manifestPath);
            }
            using var process = new Process { StartInfo = start };
            if (!process.Start()) throw new InvalidOperationException("Não foi possível iniciar o PowerShell.");
            using var registration = ct.Register(() => { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } });
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync(ct);
            return (process.ExitCode, await stdout, await stderr);
        }
        finally
        {
            try { Directory.Delete(working, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
