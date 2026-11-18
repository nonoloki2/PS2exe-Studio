using System.Text.RegularExpressions;

namespace PS2EXE.Studio.Core;

public static class BuildValidation
{
    public static IReadOnlyList<string> Validate(BuildProfile p)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(p.InputFile) || !File.Exists(p.InputFile))
            errors.Add("Selecione um script .ps1 existente.");
        else if (!string.Equals(Path.GetExtension(p.InputFile), ".ps1", StringComparison.OrdinalIgnoreCase))
            errors.Add("O arquivo de origem deve ter extensão .ps1.");
        if (string.IsNullOrWhiteSpace(p.OutputFile) || !string.Equals(Path.GetExtension(p.OutputFile), ".exe", StringComparison.OrdinalIgnoreCase))
            errors.Add("Defina um destino com extensão .exe.");
        if (!string.IsNullOrWhiteSpace(p.InputFile) && !string.IsNullOrWhiteSpace(p.OutputFile)
            && string.Equals(Path.GetFullPath(p.InputFile), Path.GetFullPath(p.OutputFile), StringComparison.OrdinalIgnoreCase))
            errors.Add("Origem e destino não podem ser o mesmo arquivo.");
        if (!string.IsNullOrWhiteSpace(p.IconFile) && (!File.Exists(p.IconFile) ||
            !string.Equals(Path.GetExtension(p.IconFile), ".ico", StringComparison.OrdinalIgnoreCase)))
            errors.Add("O ícone deve ser um arquivo .ico existente.");
        if (!Regex.IsMatch(p.Version, @"^\d{1,5}(\.\d{1,5}){1,3}$"))
            errors.Add("Versão inválida: utilize formato como 1.0.0.0.");
        if (p.WinFormsDpiAware && !p.NoConsole)
            errors.Add("WinForms DPI Aware exige uma aplicação gráfica (sem console).");
        if (p.EmbeddedFiles.Any(x => !File.Exists(x.Value)))
            errors.Add("Existe arquivo incorporado de origem que não foi encontrado.");
        return errors;
    }
}
