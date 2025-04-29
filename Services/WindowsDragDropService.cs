using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Maui.Graphics;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
#endif

namespace PDFMergeTool.Services;

public class WindowsDragDropService
{
    private static WindowsDragDropService? _instance;
    public static WindowsDragDropService Instance => _instance ??= new WindowsDragDropService();

    public delegate void FilesDroppedHandler(List<string> filePaths);
    private FilesDroppedHandler? _filesDroppedCallback;
    
    private WindowsDragDropService() { }

    public void SetupDragAndDrop(Microsoft.Maui.Controls.Element dropZone, FilesDroppedHandler callback)
    {
#if WINDOWS
        try
        {
            _filesDroppedCallback = callback;
            
            var dropZoneHandler = dropZone.Handler as Microsoft.Maui.Handlers.IBorderHandler;
            if (dropZoneHandler?.PlatformView == null)
            {
                var handler = dropZone.Handler as Microsoft.Maui.Handlers.PageHandler;
                if (handler?.PlatformView == null)
                {
                    Debug.WriteLine("Could not get native handler");
                    return;
                }

                var nativeView = handler.PlatformView as Microsoft.UI.Xaml.UIElement;
                if (nativeView == null)
                {
                    Debug.WriteLine("Could not get native UIElement from handler");
                    return;
                }

                nativeView.AllowDrop = true;
                
                nativeView.DragOver += Native_DragOver;
                nativeView.Drop += Native_Drop;
            }
            else
            {
                var nativeBorder = dropZoneHandler.PlatformView as Microsoft.UI.Xaml.UIElement;
                if (nativeBorder == null)
                {
                    Debug.WriteLine("Could not get native Border element");
                    return;
                }

                nativeBorder.AllowDrop = true;
                
                nativeBorder.DragOver += Native_DragOver;
                nativeBorder.Drop += Native_Drop;
            }
            
            Debug.WriteLine("Windows-specific drag and drop handlers set up successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error setting up Windows drop handler: {ex.Message}");
        }
#endif
    }

#if WINDOWS
    private void Native_DragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        try
        {
            bool hasFiles = e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems);
            
            if (hasFiles)
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
            else
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Native_DragOver: {ex.Message}");
        }
    }
    
    private async void Native_Drop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        try
        {
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                
                if (items?.Count > 0)
                {
                    var filePaths = new List<string>();
                    foreach (var item in items)
                    {
                        if (item is Windows.Storage.StorageFile file)
                        {
                            filePaths.Add(file.Path);
                            Debug.WriteLine($"Found file path: {file.Path}");
                        }
                    }
                    
                    if (filePaths.Count > 0 && _filesDroppedCallback != null)
                    {
                        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => {
                            _filesDroppedCallback(filePaths);
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in Native_Drop: {ex.Message}");
        }
    }
#endif
}