using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using PDFMergeTool.Services;
using System.Globalization;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Dispatching;
#endif

namespace PDFMergeTool
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            InitializeLocalization();

            builder.ConfigureLifecycleEvents(events => {
#if WINDOWS
                events.AddWindows(windows => windows
                    .OnWindowCreated((window) => {
                        window.ExtendsContentIntoTitleBar = false;
                        
                        try
                        {
                            var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
                            dispatcherQueue.TryEnqueue(() => {
                                if (window.Content is FrameworkElement frameworkElement)
                                {
                                    frameworkElement.AllowDrop = true;
                                }
                            });
                        }
                        catch
                        {
                        }
                    })
                );
#endif
            });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void InitializeLocalization()
        {
            var localizationService = LocalizationService.Instance;
            
            CultureInfo.CurrentUICulture = localizationService.CurrentCulture;
            CultureInfo.CurrentCulture = localizationService.CurrentCulture;
        }
    }
}
