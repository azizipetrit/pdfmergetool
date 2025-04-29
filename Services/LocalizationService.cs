using Microsoft.Maui.Controls;
using System;
using System.ComponentModel;
using System.Globalization;
using PDFMergeTool.Resources;
using Microsoft.Maui.Controls.Compatibility;
using System.Threading.Tasks;
using PDFMergeTool.Resources.Strings;

namespace PDFMergeTool.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        private CultureInfo _currentCulture;
        private bool _isRefreshingResources = false;
        
        public static LocalizationService Instance { get; } = new LocalizationService();

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler<CultureInfo>? LanguageChanged;

        public CultureInfo CurrentCulture 
        { 
            get => _currentCulture; 
            set
            {
                if (_currentCulture?.Name != value?.Name)
                {
                    _currentCulture = value;
                    
                    CultureInfo.CurrentUICulture = value;
                    CultureInfo.CurrentCulture = value;
                    
                    AppResources.Culture = value;
                    
                    Preferences.Default.Set("Language", value.Name);
                    
                    RefreshResourcesAndNotify();
                }
            }
        }

        private LocalizationService()
        {
            var savedLanguage = Preferences.Default.Get("Language", string.Empty);
            
            if (!string.IsNullOrEmpty(savedLanguage))
            {
                _currentCulture = new CultureInfo(savedLanguage);
            }
            else
            {
                _currentCulture = new CultureInfo("de");
            }
            
            CultureInfo.CurrentUICulture = _currentCulture;
            CultureInfo.CurrentCulture = _currentCulture;
            
            AppResources.Culture = _currentCulture;
            
            LocalizedString.RefreshResources();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        public void SwitchLanguage(string languageCode)
        {
            try
            {
                CultureInfo newCulture = new CultureInfo(languageCode);
                
                if (!IsCurrentLanguage(languageCode))
                {
                    CurrentCulture = newCulture;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Invalid language code: {languageCode}. Error: {ex.Message}");
            }
        }
        
        public bool IsCurrentLanguage(string languageCode)
        {
            return CurrentCulture.Name.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase);
        }

        private async void RefreshResourcesAndNotify()
        {
            if (_isRefreshingResources) return;
            
            _isRefreshingResources = true;
            
            try
            {
                await Task.Delay(10);
                
                LocalizedString.RefreshResources();
                
                await Task.Delay(50);
                LocalizedString.RefreshResources();
                
                OnPropertyChanged(nameof(CurrentCulture));
                LanguageChanged?.Invoke(this, CurrentCulture);
                
                await Task.Delay(50);
                ForceUIRefresh();
            }
            finally
            {
                _isRefreshingResources = false;
            }
        }
        
        private void ForceUIRefresh()
        {
            if (Application.Current?.MainPage != null)
            {
                Application.Current.MainPage.Dispatcher.Dispatch(() => {
                    ForceApplicationRefresh();
                    
                    RefreshPage(Application.Current.MainPage);
                });
            }
        }
        
        private void ForceApplicationRefresh()
        {
            try
            {
                if (Application.Current is INotifyPropertyChanged app)
                {
                    typeof(INotifyPropertyChanged).GetEvent("PropertyChanged")?
                        .GetRaiseMethod(true)?
                        .Invoke(app, new object[] { app, new PropertyChangedEventArgs(string.Empty) });
                }
                
                var mainPage = Application.Current.MainPage;
                if (mainPage != null)
                {
                    bool wasVisible = mainPage.IsVisible;
                    mainPage.IsVisible = !wasVisible;
                    mainPage.IsVisible = wasVisible;
                }
                
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    var tempDict = dict;
                    Application.Current.Resources.MergedDictionaries.Remove(dict);
                    Application.Current.Resources.MergedDictionaries.Add(tempDict);
                    break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ForceApplicationRefresh: {ex.Message}");
            }
        }
        
        private void RefreshPage(Element element)
        {
            try
            {
                if (element is BindableObject bindable)
                {
                    var context = bindable.BindingContext;
                    bindable.BindingContext = null;
                    bindable.BindingContext = context;
                    
                    if (element is ContentPage cp)
                    {
                        cp.Title = cp.Title;
                    }
                    
                    if (element is Label label)
                    {
                        label.Text = label.Text;
                    }
                    
                    if (element is Button button)
                    {
                        button.Text = button.Text;
                    }
                    
                    if (element is Entry entry)
                    {
                        entry.Placeholder = entry.Placeholder;
                    }
                }
                
                if (element is Layout<View> layout)
                {
                    foreach (var child in layout.Children)
                    {
                        RefreshPage(child);
                    }
                }
                else if (element is ContentPage contentPage && contentPage.Content != null)
                {
                    RefreshPage(contentPage.Content);
                }
                else if (element is ContentView contentView && contentView.Content != null)
                {
                    RefreshPage(contentView.Content);
                }
                else if (element is Shell shell)
                {
                    foreach (var item in shell.Items)
                    {
                        RefreshPage(item);
                    }
                }
                else if (element is FlyoutItem flyoutItem)
                {
                    foreach (var item in flyoutItem.Items)
                    {
                        RefreshPage(item);
                    }
                }
                else if (element is TabBar tabBar)
                {
                    foreach (var item in tabBar.Items)
                    {
                        RefreshPage(item);
                    }
                }
                else if (element is Microsoft.Maui.Controls.Compatibility.Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        RefreshPage(child);
                    }
                }
                else if (element is Microsoft.Maui.Controls.Compatibility.StackLayout stack)
                {
                    foreach (var child in stack.Children)
                    {
                        RefreshPage(child);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing element {element?.GetType().Name}: {ex.Message}");
            }
        }
    }
}