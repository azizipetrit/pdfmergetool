using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Diagnostics;

namespace PDFMergeTool.Services;

public class PDFService
{
    private static PDFService? _instance;
    public static PDFService Instance => _instance ??= new PDFService();

    private PDFService() { }

    public bool IsPdfFile(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        if (!Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return false;

        try
        {
            using var document = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void MergePdfFiles(List<string> pdfFiles, string outputPath)
    {
        using (var outputDocument = new PdfDocument())
        {
            foreach (var file in pdfFiles)
            {
                using (var inputDocument = PdfReader.Open(file, PdfDocumentOpenMode.Import))
                {
                    for (int i = 0; i < inputDocument.PageCount; i++)
                    {
                        var page = inputDocument.Pages[i];
                        outputDocument.AddPage(page);
                    }
                }
            }
            outputDocument.Save(outputPath);
        }
    }

    public void OpenFile(string filePath)
    {
        try
        {
#if WINDOWS
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
#endif

#if MACCATALYST
            if (DeviceInfo.Platform == DevicePlatform.macOS)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "open",
                    Arguments = $"\"{filePath}\"",
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(startInfo);
            }
#endif

#if IOS
            if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("File Created", 
                        $"File saved at: {filePath}", "OK");
                });
            }
#endif

#if ANDROID
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await Application.Current.MainPage.DisplayAlert("File Created", 
                        $"File saved at: {filePath}\n\nUse your file manager to open it.", 
                        "OK");
                });
            }
#endif

            if (DeviceInfo.Platform != DevicePlatform.WinUI && 
                DeviceInfo.Platform != DevicePlatform.macOS &&
                DeviceInfo.Platform != DevicePlatform.iOS &&
                DeviceInfo.Platform != DevicePlatform.Android)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () => {
                    await Application.Current.MainPage.DisplayAlert("File Created", 
                        $"PDF merged and saved at: {filePath}", "OK");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error opening file: {ex.Message}");
            throw;
        }
    }
}