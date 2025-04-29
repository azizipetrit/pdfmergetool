using System.Diagnostics;

#if WINDOWS
using Microsoft.UI.Xaml;
using Windows.Storage;
#endif

namespace PDFMergeTool.Helpers;

public static class FolderPicker
{
    public class FolderPickerResult
    {
        public string? Folder { get; set; }
    }
    
    public static async Task<FolderPickerResult?> PickAsync(string initialDirectory)
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return await PickFolderWindowsAsync(initialDirectory);
        }
        else if (DeviceInfo.Platform == DevicePlatform.macOS)
        {
            return await PickFolderMacOSAsync(initialDirectory);
        }
        else
        {
            return await FallbackPickFolderAsync(initialDirectory);
        }
    }
    
    private static async Task<FolderPickerResult?> PickFolderWindowsAsync(string initialDirectory)
    {
        try
        {
#if WINDOWS
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            
            folderPicker.FileTypeFilter.Add("*");
            
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            
            var window = Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView;
            if (window == null)
            {
                Debug.WriteLine("Could not get current window for folder picker");
                return null;
            }
            
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            
            var folder = await folderPicker.PickSingleFolderAsync();
            
            if (folder != null)
            {
                Debug.WriteLine($"Selected folder: {folder.Path}");
                return new FolderPickerResult { Folder = folder.Path };
            }
            else 
            {
                Debug.WriteLine("Folder selection was cancelled by user");
            }
            
            return null;
#else
            return await FallbackPickFolderAsync(initialDirectory);
#endif
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Windows folder picker: {ex}");
            return await FallbackPickFolderAsync(initialDirectory);
        }
    }
    
    private static async Task<FolderPickerResult?> PickFolderMacOSAsync(string initialDirectory)
    {
#if MACCATALYST
        try 
        {
            return await FallbackPickFolderAsync(initialDirectory);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in macOS folder picker: {ex}");
            return new FolderPickerResult { Folder = initialDirectory };
        }
#else
        return await FallbackPickFolderAsync(initialDirectory);
#endif
    }
    
    private static async Task<FolderPickerResult?> FallbackPickFolderAsync(string initialDirectory)
    {
        try
        {
            if (string.IsNullOrEmpty(initialDirectory))
            {
                initialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            
            string documentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (!string.IsNullOrEmpty(documentsFolder) && Directory.Exists(documentsFolder))
            {
                Debug.WriteLine($"Using documents folder: {documentsFolder}");
                return new FolderPickerResult { Folder = documentsFolder };
            }
            
            string downloadsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (Directory.Exists(downloadsFolder))
            {
                Debug.WriteLine($"Using downloads folder: {downloadsFolder}");
                return new FolderPickerResult { Folder = downloadsFolder };
            }
            
            Debug.WriteLine($"Using initial directory: {initialDirectory}");
            return new FolderPickerResult { Folder = initialDirectory };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in fallback folder picker: {ex}");
            return new FolderPickerResult { Folder = initialDirectory };
        }
    }
}