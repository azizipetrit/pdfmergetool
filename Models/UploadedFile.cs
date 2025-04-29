using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using Microsoft.Maui.Graphics;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace PDFMergeTool.Models;

public class UploadedFile : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(FileSize));
                OnPropertyChanged(nameof(FileInfo));
                
                LoadPageCount();
            }
        }
    }

    public string FileName => Path.GetFileName(FilePath);

    public string FileSize
    {
        get
        {
            try
            {
                var fileInfo = new FileInfo(FilePath);
                long bytes = fileInfo.Length;
                
                if (bytes < 1024)
                    return $"{bytes} B";
                else if (bytes < 1024 * 1024)
                    return $"{bytes / 1024.0:F1} KB";
                else
                    return $"{bytes / (1024.0 * 1024.0):F1} MB";
            }
            catch
            {
                return "Unknown";
            }
        }
    }

    public string FileInfo => $"{FileSize} • {PageCount} pages";

    private int _pageCount;
    public int PageCount
    {
        get => _pageCount;
        private set
        {
            if (_pageCount != value)
            {
                _pageCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileInfo));
            }
        }
    }

    private void LoadPageCount()
    {
        try
        {
            if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
            {
                using var document = PdfReader.Open(FilePath, PdfDocumentOpenMode.Import);
                PageCount = document.PageCount;
            }
            else
            {
                PageCount = 0;
            }
        }
        catch
        {
            PageCount = 0;
        }
    }

    private Color _rowColor = Colors.White;
    public Color RowColor
    {
        get => _rowColor;
        set
        {
            if (_rowColor != value)
            {
                _rowColor = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}