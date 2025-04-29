using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Diagnostics;
using System.Text;
using PDFMergeTool.Services;
using PDFMergeTool.Resources.Strings;
using PDFMergeTool.Resources;
using PDFMergeTool.ViewModels;
using PDFMergeTool.Models;
using PDFMergeTool.Helpers;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.Platform;
#endif

namespace PDFMergeTool;

public partial class MainPage : ContentPage, INotifyPropertyChanged
{
    private readonly MainPageViewModel _viewModel;
    private readonly LocalizationService _localizationService;
    private readonly PDFService _pdfService;
    private readonly WindowsDragDropService _dragDropService;

    public MainPage()
    {
        InitializeComponent();
        
        _viewModel = new MainPageViewModel();
        _pdfService = PDFService.Instance;
        _localizationService = LocalizationService.Instance;
        _dragDropService = WindowsDragDropService.Instance;
        
        BindingContext = _viewModel;

        _localizationService.PropertyChanged += OnLocalizationServicePropertyChanged;

        Microsoft.Maui.Controls.Application.Current!.RequestedThemeChanged += (s, e) =>
        {
            _viewModel.ApplyAlternatingRowColors();
        };

        this.HandlerChanged += MainPage_HandlerChanged;
        
        UpdateLocalizedText();
    }
    
    private void OnLocalizationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
        {
            UpdateLocalizedText();
        }
    }
    
    private void UpdateLocalizedText()
    {
        try
        {
            Title = LocalizedString.GetString("AppTitle");
            
            if (this.AppTitleLabel != null)
                this.AppTitleLabel.Text = LocalizedString.GetString("AppTitle");
            
            if (this.InstructionsLabel != null)
                this.InstructionsLabel.Text = LocalizedString.GetString("Instructions");
            
            if (this.UploadFilesButton != null)
                this.UploadFilesButton.Text = LocalizedString.GetString("UploadFiles");
            
            if (this.ClearAllFilesButton != null)
                this.ClearAllFilesButton.Text = LocalizedString.GetString("ClearAllFiles");
            
            if (this.DragDropHereLabel != null)
                this.DragDropHereLabel.Text = LocalizedString.GetString("DragDropHere");
            
            if (this.OrUseUploadLabel != null)
                this.OrUseUploadLabel.Text = LocalizedString.GetString("OrUseUpload");
            
            if (this.FileNameLabel != null)
                this.FileNameLabel.Text = LocalizedString.GetString("FileName");
            
            if (this.InfoLabel != null)
                this.InfoLabel.Text = LocalizedString.GetString("Info");
            
            if (this.OrderLabel != null)
                this.OrderLabel.Text = LocalizedString.GetString("Order");
            
            if (this.ActionsLabel != null)
                this.ActionsLabel.Text = LocalizedString.GetString("Actions");
            
            if (this.NoFilesYetLabel != null)
                this.NoFilesYetLabel.Text = LocalizedString.GetString("NoFilesYet");
            
            if (this.UploadToStartLabel != null)
                this.UploadToStartLabel.Text = LocalizedString.GetString("UploadToStart");
            
            if (this.MergePDFsButton != null)
                this.MergePDFsButton.Text = LocalizedString.GetString("MergePDFs");
            
            _viewModel.UpdateStatusText();
            if (this.StatusLabel != null)
                this.StatusLabel.Text = _viewModel.StatusText;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UpdateLocalizedText: {ex.Message}");
        }
    }

    private void MainPage_HandlerChanged(object? sender, EventArgs e)
    {
#if WINDOWS
        if (Handler != null)
        {
            SetupWindowsDropHandler();
        }
#endif
    }

#if WINDOWS
    private void SetupWindowsDropHandler()
    {
        try
        {
            _dragDropService.SetupDragAndDrop(this.DropZone, (filePaths) => {
                _viewModel.ProcessDroppedFiles(filePaths);
                
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() => {
                    bool isDarkTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
                    this.DropZone.BackgroundColor = isDarkTheme ? 
                        Color.FromArgb("#2E2E2E") : 
                        Color.FromArgb("#F0F0F0");
                });
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up Windows drop handler: {ex.Message}");
        }
    }
#endif

    private async void OnUploadFilesClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.PickMultipleAsync(new PickOptions
            {
                PickerTitle = AppResources.UploadFiles,
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".pdf" } },
                    { DevicePlatform.macOS, new[] { "pdf" } },
                    { DevicePlatform.iOS, new[] { "public.pdf" } },
                    { DevicePlatform.Android, new[] { "application/pdf" } },
                    { DevicePlatform.Unknown, new[] { ".pdf" } }
                })
            });

            if (result != null && result.Count() > 0)
            {
                int filesAdded = 0;
                List<string> duplicateFileNames = new List<string>();
                
                foreach (var file in result)
                {
                    if (_pdfService.IsPdfFile(file.FullPath))
                    {
                        if (_viewModel.Files.Any(f => string.Equals(f.FilePath, file.FullPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            continue;
                        }
                        
                        var duplicateFile = _viewModel.CheckForDuplicateFileName(file.FullPath);
                        if (duplicateFile != null)
                        {
                            bool addAnyway = true;
                            
                            if (!duplicateFileNames.Contains(Path.GetFileName(file.FullPath)))
                            {
                                addAnyway = await DisplayAlert(
                                    LocalizedString.GetString("DuplicateFileName"), 
                                    string.Format(LocalizedString.GetString("FileWithSameNameExists"), Path.GetFileName(file.FullPath)),
                                    LocalizedString.GetString("AddAnyway"),
                                    LocalizedString.GetString("Skip"));
                                
                                duplicateFileNames.Add(Path.GetFileName(file.FullPath));
                            }
                            
                            if (!addAnyway)
                            {
                                continue;
                            }
                        }
                        
                        _viewModel.Files.Add(new UploadedFile { FilePath = file.FullPath });
                        filesAdded++;
                    }
                    else
                    {
                        await DisplayAlert(AppResources.InvalidFile, string.Format(AppResources.NotValidPDF, Path.GetFileName(file.FullPath)), AppResources.Close);
                    }
                }
                
                if (filesAdded > 0 && duplicateFileNames.Count > 0)
                {
                    _viewModel.StatusText = string.Format(
                        LocalizedString.GetString("AddedFilesWithDuplicates"), 
                        filesAdded, 
                        duplicateFileNames.Count);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Error, $"{ex.Message}", AppResources.Close);
        }
    }

    private void OnDrop(object sender, Microsoft.Maui.Controls.DropEventArgs e)
    {
        try
        {
            if (e.Data?.Properties == null)
            {
                this.StatusLabel.Text = "Unable to process the dropped items";
                this.StatusLabel.IsVisible = true;
                return;
            }

            System.Diagnostics.Debug.WriteLine($"Drop data received with {e.Data.Properties.Count} properties");
            
            foreach (var key in e.Data.Properties.Keys)
            {
                var value = e.Data.Properties[key];
                System.Diagnostics.Debug.WriteLine($"Property: {key}, Type: {(value != null ? value.GetType().Name : "null")}");
            }

            if (e.Data.Properties.ContainsKey("StorageItems") && 
                e.Data.Properties["StorageItems"] is IEnumerable<object> storageItems)
            {
                var filePaths = new List<string>();
                foreach (var item in storageItems)
                {
                    var pathProperty = item.GetType().GetProperty("Path");
                    if (pathProperty != null)
                    {
                        string? path = pathProperty.GetValue(item) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            filePaths.Add(path);
                        }
                    }
                }
                
                if (filePaths.Count > 0)
                {
                    _viewModel.ProcessDroppedFiles(filePaths);
                    return;
                }
            }
            
            if (e.Data.Properties.ContainsKey("Files") && 
                e.Data.Properties["Files"] is IEnumerable<string> files)
            {
                _viewModel.ProcessDroppedFiles(files);
                return;
            }
            
            if (e.Data.Properties.ContainsKey("FileNames") && 
                e.Data.Properties["FileNames"] is IEnumerable<string> fileNames)
            {
                _viewModel.ProcessDroppedFiles(fileNames);
                return;
            }
            
            foreach (var key in e.Data.Properties.Keys)
            {
                var value = e.Data.Properties[key];
                
                if (value is IEnumerable<string> stringCollection)
                {
                    var fileList = stringCollection.Where(s => !string.IsNullOrEmpty(s) && File.Exists(s)).ToList();
                    if (fileList.Count > 0)
                    {
                        _viewModel.ProcessDroppedFiles(fileList);
                        return;
                    }
                }
                else if (value is string singleString && File.Exists(singleString))
                {
                    _viewModel.ProcessDroppedFiles(new[] { singleString });
                    return;
                }
                else if (value is IEnumerable<object> objectCollection)
                {
                    var paths = new List<string>();
                    foreach (var obj in objectCollection)
                    {
                        if (obj is string path && File.Exists(path))
                        {
                            paths.Add(path);
                        }
                        else if (obj != null)
                        {
                            var pathProperty = obj.GetType().GetProperty("Path");
                            if (pathProperty != null)
                            {
                                string? objPath = pathProperty.GetValue(obj) as string;
                                if (!string.IsNullOrEmpty(objPath) && File.Exists(objPath))
                                {
                                    paths.Add(objPath);
                                }
                            }
                        }
                    }
                    
                    if (paths.Count > 0)
                    {
                        _viewModel.ProcessDroppedFiles(paths);
                        return;
                    }
                }
            }

            this.StatusLabel.Text = "Couldn't process the dropped items. Please try using the Upload button instead.";
            this.StatusLabel.IsVisible = true;
            System.Diagnostics.Debug.WriteLine("No valid files found in dropped data");
        }
        catch (Exception ex)
        {
            this.StatusLabel.Text = $"Drop error: {ex.Message}";
            this.StatusLabel.IsVisible = true;
            System.Diagnostics.Debug.WriteLine($"Exception in OnDrop: {ex}");
        }
        finally
        {
            bool isDarkTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
            this.DropZone.BackgroundColor = isDarkTheme ? 
                Color.FromArgb("#2E2E2E") : 
                Color.FromArgb("#F0F0F0");
        }
    }

    private void OnDragOver(object sender, Microsoft.Maui.Controls.DragEventArgs e)
    {
        try
        {
            if (e.Data?.Properties == null)
            {
                e.AcceptedOperation = Microsoft.Maui.Controls.DataPackageOperation.None;
                return;
            }

            bool hasFiles = false;
            
            if (e.Data.Properties.ContainsKey("StorageItems"))
            {
                hasFiles = true;
            }
            else if (e.Data.Properties.ContainsKey("Files"))
            {
                hasFiles = true;
            }
            else if (e.Data.Properties.ContainsKey("FileNames"))
            {
                hasFiles = true;
            }
            else
            {
                foreach (var key in e.Data.Properties.Keys)
                {
                    var value = e.Data.Properties[key];
                    if (value is IEnumerable<string> || 
                        value is IEnumerable<object> ||
                        (value is string stringValue && 
                         stringValue.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                    {
                        hasFiles = true;
                        break;
                    }
                }
            }
            
            bool isDarkTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
            
            if (hasFiles)
            {
                this.DropZone.BackgroundColor = isDarkTheme ? 
                    Color.FromArgb("#3E5F8A") : 
                    Color.FromArgb("#ADD8E6");
                e.AcceptedOperation = Microsoft.Maui.Controls.DataPackageOperation.Copy;
            }
            else
            {
                this.DropZone.BackgroundColor = isDarkTheme ? 
                    Color.FromArgb("#4A4A4A") : 
                    Color.FromArgb("#EBEBEB");
                e.AcceptedOperation = Microsoft.Maui.Controls.DataPackageOperation.None;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Drag error: {ex.Message}");
            
            e.AcceptedOperation = Microsoft.Maui.Controls.DataPackageOperation.Copy;
        }
    }

    private async void OnMergeClicked(object sender, EventArgs e)
    {
        if (_viewModel.Files.Count < 2)
        {
            await DisplayAlert(AppResources.NotEnoughFiles, AppResources.NeedTwoFiles, AppResources.Close);
            return;
        }

        try
        {
            this.StatusLabel.Text = AppResources.PreparingToMerge;
            this.StatusLabel.IsVisible = true;

            string firstFilePath = _viewModel.Files.First().FilePath;
            string baseFileName = Path.GetFileNameWithoutExtension(firstFilePath);
            string defaultOutputFileName = $"{baseFileName}_merged.pdf";
            
            string? defaultDirectory = Path.GetDirectoryName(firstFilePath);
            if (string.IsNullOrEmpty(defaultDirectory))
            {
                defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            bool customLocation = await DisplayAlert(
                AppResources.OutputLocation, 
                AppResources.WhereToSave, 
                AppResources.ChooseLocation, AppResources.UseDefault);

            string outputPath;
            
            if (customLocation)
            {
                var folderResult = await FolderPicker.PickAsync(defaultDirectory);
                
                if (folderResult == null || string.IsNullOrEmpty(folderResult.Folder))
                {
                    this.StatusLabel.Text = AppResources.MergeFailed;
                    return;
                }
                
                string outputDir = folderResult.Folder;
                string outputFileName = defaultOutputFileName;
                
                string userFileName = await DisplayPromptAsync(
                    AppResources.FileName_Prompt, 
                    AppResources.EnterNameForMerged,
                    initialValue: defaultOutputFileName,
                    maxLength: 100);
                
                if (string.IsNullOrEmpty(userFileName))
                {
                    this.StatusLabel.Text = AppResources.MergeFailed;
                    return;
                }
                
                if (!userFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    userFileName += ".pdf";
                }
                
                outputPath = Path.Combine(outputDir, userFileName);
            }
            else
            {
                outputPath = Path.Combine(defaultDirectory, defaultOutputFileName);
            }
            
            if (File.Exists(outputPath))
            {
                bool overwrite = await DisplayAlert(AppResources.FileAlreadyExists, 
                    string.Format(AppResources.ReplaceQuestion, Path.GetFileName(outputPath)), 
                    AppResources.Yes, AppResources.No);
                
                if (!overwrite)
                {
                    string dir = Path.GetDirectoryName(outputPath) ?? defaultDirectory;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(outputPath);
                    string uniqueFileName = $"{fileNameWithoutExt}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                    outputPath = Path.Combine(dir, uniqueFileName);
                }
            }

            IsBusy = true;
            this.StatusLabel.Text = AppResources.MergingPDFs;

            await _viewModel.MergePDFFiles(outputPath);

            this.StatusLabel.Text = AppResources.MergeSuccessful;
            IsBusy = false;
            
            bool openFile = await DisplayAlert(AppResources.MergeComplete, 
                string.Format(AppResources.MergeSuccessMessage, Path.GetFileName(outputPath)), 
                AppResources.OpenFile, AppResources.Close);
            
            if (openFile)
            {
                _viewModel.OpenFile(outputPath);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert(AppResources.Error, string.Format(AppResources.MergeFailedMessage, ex.Message), AppResources.Close);
            this.StatusLabel.Text = AppResources.MergeFailed;
            IsBusy = false;
        }
    }

    private void OnMoveUpClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            _viewModel.MoveItemUp(item);
        }
    }

    private void OnMoveDownClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            _viewModel.MoveItemDown(item);
        }
    }

    private void OnMoveToTopClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            _viewModel.MoveItemToTop(item);
        }
    }

    private void OnMoveToBottomClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            _viewModel.MoveItemToBottom(item);
        }
    }

    private async void OnRemoveFileClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            bool confirm = await DisplayAlert(AppResources.RemoveFile, 
                string.Format(AppResources.RemoveFileConfirm, item.FileName), 
                AppResources.Yes, AppResources.No);
            
            if (confirm)
            {
                _viewModel.RemoveFile(item);
            }
        }
    }

    private async void OnClearFilesClicked(object sender, EventArgs e)
    {
        if (_viewModel.Files.Count > 0)
        {
            bool confirm = await DisplayAlert(AppResources.ClearAllFiles, 
                AppResources.ClearFilesConfirm, 
                AppResources.Yes, AppResources.No);
            
            if (confirm)
            {
                _viewModel.ClearFiles();
            }
        }
    }

    private void OnPreviewFileClicked(object sender, EventArgs e)
    {
        var button = sender as Microsoft.Maui.Controls.Button;
        var item = button?.CommandParameter as UploadedFile;
        if (item != null)
        {
            _viewModel.PreviewFile(item);
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        _localizationService.PropertyChanged -= OnLocalizationServicePropertyChanged;
        _viewModel.Cleanup();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChangedCustom([CallerMemberName] string? propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
