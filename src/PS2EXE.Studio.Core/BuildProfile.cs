namespace PS2EXE.Studio.Core;

public enum TargetArchitecture { Auto, X86, X64 }
public enum ApartmentMode { Default, STA, MTA }

public sealed record BuildProfile
{
    public string InputFile { get; init; } = "";
    public string OutputFile { get; init; } = "";
    public string? IconFile { get; init; }
    public bool NoConsole { get; init; } = true;
    public TargetArchitecture Architecture { get; init; } = TargetArchitecture.X64;
    public ApartmentMode Apartment { get; init; } = ApartmentMode.STA;
    public bool RequireAdmin { get; init; }
    public bool DpiAware { get; init; }
    public bool WinFormsDpiAware { get; init; }
    public string Title { get; init; } = "";
    public string Product { get; init; } = "";
    public string Company { get; init; } = "";
    public string Description { get; init; } = "";
    public string Version { get; init; } = "1.0.0.0";
    public string Copyright { get; init; } = "";
    public Dictionary<string, string> EmbeddedFiles { get; init; } = new();
}

public sealed record BuildResult(bool Success, int ExitCode, string Output, string Error, string? Sha256, string? Message = null);

public sealed record ModuleStatus(bool Available, string Version, string Details);
