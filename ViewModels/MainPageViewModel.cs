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
using PDFMergeTool.Models;

#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Microsoft.Maui.Platform;
#endif
namespace PDFMergeTool.ViewModels;

public class MainPageViewModel : INotifyPropertyChanged
{
    private readonly PDFService _pdfService;
    private readonly LocalizationService _localizationService;

    public ObservableCollection<UploadedFile> Files { get; private set; } = new ObservableCollection<UploadedFile>();

    private bool _hasFiles;
    public bool HasFiles
    {
        get => _hasFiles;
        set
        {
            if (_hasFiles != value)
            {
                _hasFiles = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _canMerge;
    public bool CanMerge
    {
        get => _canMerge;
        set
        {
            if (_canMerge != value)
            {
                _canMerge = value;
                OnPropertyChanged();
            }
        }
    }

    private string _statusText = string.Empty;
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }
    }

    // Tooltip properties for buttons
    public string UploadFilesToolTip => LocalizedString.GetString("UploadFiles");
    public string ClearAllFilesToolTip => LocalizedString.GetString("ClearAllFiles");
    public string MoveToTopToolTip => LocalizedString.GetString("MoveToTop");
    public string MoveUpToolTip => LocalizedString.GetString("MoveUp");
    public string MoveDownToolTip => LocalizedString.GetString("MoveDown");
    public string MoveToBottomToolTip => LocalizedString.GetString("MoveToBottom");
    public string PreviewPDFToolTip => LocalizedString.GetString("PreviewPDF");
    public string RemoveFileToolTip => LocalizedString.GetString("RemoveFile");
    public string MergePDFsToolTip => LocalizedString.GetString("MergePDFs");

    public MainPageViewModel()
    {
        _pdfService = PDFService.Instance;
        _localizationService = LocalizationService.Instance;
        
        _localizationService.PropertyChanged += OnLocalizationServicePropertyChanged;

        Files.CollectionChanged += Files_CollectionChanged!;
        
        UpdateUIState();
    }

    public void UpdateUIState()
    {
        HasFiles = Files.Count > 0;
        CanMerge = Files.Count >= 2;
        
        UpdateStatusText();
    }

    public void UpdateStatusText()
    {
        if (Files.Count == 0)
        {
            StatusText = LocalizedString.GetString("AddToBegin");
        }
        else if (Files.Count == 1)
        {
            StatusText = LocalizedString.GetString("AddMoreToEnable");
        }
        else
        {
            StatusText = string.Format(
                LocalizedString.GetString("ReadyToMerge"), 
                Files.Count);
        }
    }

    public void ApplyAlternatingRowColors()
    {
        bool isDarkTheme = Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;
        
        for (int i = 0; i < Files.Count; i++)
        {
            if (isDarkTheme)
            {
                Files[i].RowColor = i % 2 == 0 ? Color.FromArgb("#2E2E2E") : Color.FromArgb("#383838");
            }
            else
            {
                Files[i].RowColor = i % 2 == 0 ? Colors.White : Color.FromArgb("#F5F5F5");
            }
        }
    }

    private void OnLocalizationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
        {
            UpdateStatusText();
            
            OnPropertyChanged(nameof(UploadFilesToolTip));
            OnPropertyChanged(nameof(ClearAllFilesToolTip));
            OnPropertyChanged(nameof(MoveToTopToolTip));
            OnPropertyChanged(nameof(MoveUpToolTip));
            OnPropertyChanged(nameof(MoveDownToolTip));
            OnPropertyChanged(nameof(MoveToBottomToolTip));
            OnPropertyChanged(nameof(PreviewPDFToolTip));
            OnPropertyChanged(nameof(RemoveFileToolTip));
            OnPropertyChanged(nameof(MergePDFsToolTip));
        }
    }

    private void Files_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateUIState();
        ApplyAlternatingRowColors();
    }

    public void ProcessDroppedFiles(IEnumerable<string> files)
    {
        if (files == null || !files.Any())
        {
            StatusText = "No files were provided in the drop operation";
            return;
        }

        int filesAdded = 0;
        int invalidFiles = 0;
        int duplicateFiles = 0;
        List<string> duplicateFileNames = new List<string>();
        
        foreach (var filePath in files)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"File doesn't exist: {filePath}");
                    continue;
                }
                
                if (_pdfService.IsPdfFile(filePath))
                {
                    if (Files.Any(f => string.Equals(f.FilePath, filePath, StringComparison.OrdinalIgnoreCase)))
                    {
                        System.Diagnostics.Debug.WriteLine($"File already in list (same path): {filePath}");
                        duplicateFiles++;
                        continue;
                    }
                    
                    var duplicateFile = CheckForDuplicateFileName(filePath);
                    if (duplicateFile != null)
                    {
                        string fileName = Path.GetFileName(filePath);
                        if (!duplicateFileNames.Contains(fileName))
                        {
                            duplicateFileNames.Add(fileName);
                        }
                    }
                    
                    Files.Add(new UploadedFile { FilePath = filePath });
                    filesAdded++;
                }
                else
                {
                    invalidFiles++;
                    System.Diagnostics.Debug.WriteLine($"Invalid PDF file: {filePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        if (filesAdded > 0)
        {
            string message = $"Added {filesAdded} PDF file{(filesAdded > 1 ? "s" : "")}";
            
            if (invalidFiles > 0)
            {
                message += $" (skipped {invalidFiles} non-PDF file{(invalidFiles > 1 ? "s" : "")})";
            }

            if (duplicateFiles > 0)
            {
                message += $" (skipped {duplicateFiles} duplicate file{(duplicateFiles > 1 ? "s" : "")})";
            }
            
            if (duplicateFileNames.Count > 0)
            {
                message += $"\nNotice: {duplicateFileNames.Count} file{(duplicateFileNames.Count > 1 ? "s" : "")} with duplicate name{(duplicateFileNames.Count > 1 ? "s" : "")} added";
                if (duplicateFileNames.Count <= 3)
                {
                    message += $": {string.Join(", ", duplicateFileNames)}";
                }
            }
            
            StatusText = message;
        }
        else if (invalidFiles > 0)
        {
            StatusText = $"No valid PDF files found. Skipped {invalidFiles} non-PDF file{(invalidFiles > 1 ? "s" : "")}";
        }
        else if (duplicateFiles > 0)
        {
            StatusText = $"No new files added. All {duplicateFiles} file{(duplicateFiles > 1 ? "s" : "")} were already in the list.";
        }
        else
        {
            StatusText = "No valid PDF files found in the dropped items";
        }
    }

    public void MoveItemUp(UploadedFile item)
    {
        int index = Files.IndexOf(item);
        if (index > 0)
        {
            Files.Move(index, index - 1);
        }
    }

    public void MoveItemDown(UploadedFile item)
    {
        int index = Files.IndexOf(item);
        if (index < Files.Count - 1)
        {
            Files.Move(index, index + 1);
        }
    }

    public void MoveItemToTop(UploadedFile item)
    {
        int index = Files.IndexOf(item);
        if (index > 0)
        {
            Files.Move(index, 0);
        }
    }

    public void MoveItemToBottom(UploadedFile item)
    {
        int index = Files.IndexOf(item);
        if (index < Files.Count - 1)
        {
            Files.Move(index, Files.Count - 1);
        }
    }

    public void RemoveFile(UploadedFile item)
    {
        Files.Remove(item);
    }

    public void ClearFiles()
    {
        Files.Clear();
    }
    
    public async Task<string> MergePDFFiles(string outputPath)
    {
        if (Files.Count < 2)
            throw new InvalidOperationException("Need at least two PDF files to merge");
            
        await Task.Run(() => _pdfService.MergePdfFiles(Files.Select(f => f.FilePath).ToList(), outputPath));
        
        return outputPath;
    }

    public void OpenFile(string filePath)
    {
        try
        {
            _pdfService.OpenFile(filePath);
        }
        catch (Exception ex)
        {
            StatusText = $"Could not open file. File saved at: {filePath}";
            System.Diagnostics.Debug.WriteLine($"Error opening file: {ex.Message}");
        }
    }

    public void PreviewFile(UploadedFile item)
    {
        if (item != null)
        {
            OpenFile(item.FilePath);
        }
    }

    /// <summary>
    /// Checks if a file with the same name already exists in the Files collection
    /// </summary>
    /// <param name="filePath">The path of the file to check</param>
    /// <returns>The existing file with the same name, or null if no duplicate found</returns>
    public UploadedFile? CheckForDuplicateFileName(string filePath)
    {
        string fileName = Path.GetFileName(filePath);
        return Files.FirstOrDefault(f => string.Equals(Path.GetFileName(f.FilePath), fileName, StringComparison.OrdinalIgnoreCase));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Cleanup()
    {
        _localizationService.PropertyChanged -= OnLocalizationServicePropertyChanged;
    }
}