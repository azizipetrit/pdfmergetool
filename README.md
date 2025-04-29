# PDF Merge Tool

A cross-platform utility for merging multiple PDF files into a single document.

## Overview

PDF Merge Tool is a modern, user-friendly application that allows you to combine multiple PDF files into a single document. Built using .NET MAUI (Multi-platform App UI), it runs natively on Windows, macOS, iOS, and Android devices, providing a consistent experience across all platforms.

## Features

- **Intuitive User Interface**: Simple drag-and-drop functionality for adding PDF files
- **File Management**: Add, remove, and rearrange PDF files before merging
- **File Preview**: Preview PDF files before merging
- **Flexible Output Options**: Choose where to save the merged document
- **Multi-platform Support**: Works on Windows, macOS, iOS, and Android
- **Multilingual Interface**: Supports English and German, with a framework in place for additional languages
- **Dark Mode Support**: Adapts to system light/dark theme settings

## System Requirements

- **Windows**: Windows 10 version 1809 (build 17763) or later
- **macOS**: macOS 11 (Big Sur) or later
- **iOS**: iOS 11 or later
- **Android**: Android 5.0 (API 21) or later

## Installation

### Windows
1. Download the latest Windows release from the Releases page
2. Run the installer and follow the on-screen instructions

### macOS
1. Download the latest macOS release from the Releases page
2. Mount the DMG file and drag the application to your Applications folder

### iOS
- Download from the App Store (coming soon)

### Android
- Download from Google Play Store (coming soon)

### Creating a Standalone Windows Executable
To generate a standalone .exe file for Windows distribution:

```
dotnet publish -c Release -f net8.0-windows10.0.19041.0 --self-contained true /p:WindowsPackageType=None
```

The executable will be available in the `bin\Release\net8.0-windows10.0.19041.0\win10-x64\publish` folder.

## How to Use

1. **Launch** the PDF Merge Tool application
2. **Add PDF files** using one of these methods:
   - Drag and drop files onto the designated area
   - Click the "Upload Files" button and select files from your device
3. **Arrange your files** in the desired order using the arrow buttons
4. **Review your files** by clicking the preview button (eye icon) next to each file
5. **Click "Merge PDFs"** when you're ready to combine the files
6. **Choose where to save** the merged PDF file
7. **Open the merged file** directly from the success dialog or access it later from your chosen location

## Building from Source

### Prerequisites
- .NET 8.0 SDK or later
- Visual Studio 2022 (Windows) or Visual Studio for Mac (macOS) with MAUI workload installed

### Steps
1. Clone the repository
   ```
   git clone https://github.com/yourusername/PDFMergeTool.git
   ```
2. Open the solution file (`PDFMergeTool.sln`) in Visual Studio
3. Restore NuGet packages
4. Build the solution
5. Run the application for your target platform

## Technologies Used

- **.NET MAUI**: Cross-platform UI framework
- **PDFsharp**: Library for PDF document manipulation
- **C# / XAML**: Programming languages

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Acknowledgements

- [PDFsharp](http://www.pdfsharp.net/) for PDF document processing
- .NET MAUI framework for cross-platform capabilities

## Contact

For questions, feedback, or support, please create an issue or contact the developer.

---

*Built with .NET MAUI and PDFsharp*