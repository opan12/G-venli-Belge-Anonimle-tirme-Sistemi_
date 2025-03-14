using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

public class PythonNER
{
    public static string AnonymizeText(string text)
    {
        var pythonScriptPath = @"C:\Users\Casper\source\repos\Güvenli Belge Anonimleştirme Sistemi\Güvenli Belge Anonimleştirme Sistemi\anonymize.py";
        var psi = new ProcessStartInfo
        {
            FileName = "python",
            Arguments = pythonScriptPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8
        };

        using (var process = new Process { StartInfo = psi })
        {
            process.Start();

            // Python'a metni gönder
            using (var sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.Write(text);
                }
            }

            // Çıktıyı al
            string resultJson = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // JSON parse işlemi
            var result = JsonSerializer.Deserialize<Dictionary<string, string>>(resultJson);
            return result["anonymized_text"];
        }
    }
}
