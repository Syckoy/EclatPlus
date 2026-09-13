using System.Diagnostics;
using System.IO.Compression;

namespace EclatPlus;

internal static class SelfUpdate
{
    public static async Task ApplyAsync(UpdateInfo update, IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(update.DownloadUrl))
        {
            throw new InvalidOperationException("Aucun fichier de mise à jour n’est publié sur GitHub.");
        }

        string appDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string staging = Path.Combine(Path.GetTempPath(), "EclatPlus-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);

        progress?.Report("Téléchargement…");
        string downloaded = Path.Combine(staging, Path.GetFileName(new Uri(update.DownloadUrl).LocalPath));
        if (string.IsNullOrWhiteSpace(Path.GetFileName(downloaded)))
        {
            downloaded = Path.Combine(staging, "update.bin");
        }

        using (var response = await UpdateCheck.DownloadHttp.GetAsync(
                   update.DownloadUrl,
                   HttpCompletionOption.ResponseHeadersRead,
                   cancellationToken).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var output = File.Create(downloaded);
            await input.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
        }

        string payload = staging;
        if (downloaded.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            progress?.Report("Extraction…");
            string extracted = Path.Combine(staging, "payload");
            ZipFile.ExtractToDirectory(downloaded, extracted, overwriteFiles: true);
            payload = Flatten(extracted);
        }
        else
        {
            payload = staging;
        }

        string bat = Path.Combine(Path.GetTempPath(), "eclatplus-apply-update.bat");
        string exeName = Path.GetFileName(Environment.ProcessPath ?? "EclatPlus.exe");
        File.WriteAllText(bat, $$"""
            @echo off
            setlocal
            :wait
            timeout /t 1 /nobreak >nul
            tasklist /FI "IMAGENAME eq {{exeName}}" | find /I "{{exeName}}" >nul
            if not errorlevel 1 goto wait
            xcopy /Y /E /Q "{{payload}}\*" "{{appDir}}\" >nul
            start "" "{{appDir}}\{{exeName}}"
            rd /s /q "{{staging}}"
            del "%~f0"
            """);

        progress?.Report("Redémarrage…");
        Process.Start(new ProcessStartInfo
        {
            FileName = bat,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private static string Flatten(string extracted)
    {
        var dirs = Directory.GetDirectories(extracted);
        var files = Directory.GetFiles(extracted);
        if (files.Length == 0 && dirs.Length == 1)
        {
            return dirs[0];
        }

        return extracted;
    }
}
