using Microsoft.Maui;
using Microsoft.Maui.Controls;
using PDFMergeTool.Resources;
using PDFMergeTool.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PDFMergeTool.Controls
{
    public partial class LanguageSwitcher : ContentView, INotifyPropertyChanged
    {
        private readonly LocalizationService _localizationService;
        private bool _isInitialized = false;
        private bool _isUpdatingText = false;
        private bool _isChangingLanguage = false;

        public new event PropertyChangedEventHandler? PropertyChanged;

        public class LanguageItem
        {
            public string DisplayName { get; set; }
            public string Code { get; set; }

            public LanguageItem(string displayName, string code)
            {
                DisplayName = displayName;
                Code = code;
            }

            public override string ToString() => DisplayName;
        }

        public LanguageSwitcher()
        {
            InitializeComponent();
            _localizationService = LocalizationService.Instance;
            this.BindingContext = this;
            
            InitializeLanguagePicker();
            
            _localizationService.PropertyChanged += OnLocalizationServicePropertyChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
            
            UpdateLocalizedText();
            
            _isInitialized = true;
        }

        private void InitializeLanguagePicker()
        {
            try
            {
                var languages = new List<LanguageItem>
                {
                    new LanguageItem(LocalizedString.GetString("English"), "en"),
                    new LanguageItem(LocalizedString.GetString("German"), "de")
                };

                LanguagePicker.ItemsSource = languages;
                
                SetLanguagePickerSelection();
                
                UpdatePickerText();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing language picker: {ex.Message}");
            }
        }
        
        private void UpdatePickerText()
        {
            try
            {
                if (LanguagePicker.SelectedItem is LanguageItem selectedItem)
                {
                    var temp = selectedItem.DisplayName;
                    selectedItem.DisplayName = LocalizedString.GetString(selectedItem.Code == "en" ? "English" : "German");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating picker text: {ex.Message}");
            }
        }

        private void SetLanguagePickerSelection()
        {
            try
            {
                var languages = LanguagePicker.ItemsSource as List<LanguageItem>;
                if (languages == null) return;

                var currentCulture = _localizationService.CurrentCulture.Name.ToLower();
                
                for (int i = 0; i < languages.Count; i++)
                {
                    if (currentCulture.StartsWith(languages[i].Code.ToLower()))
                    {
                        languages[i].DisplayName = LocalizedString.GetString(languages[i].Code == "en" ? "English" : "German");
                        LanguagePicker.SelectedIndex = i;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting language picker selection: {ex.Message}");
            }
        }
        
        private void UpdateLocalizedText()
        {
            if (_isUpdatingText) return;
            
            _isUpdatingText = true;
            
            try
            {
                LocalizedString.RefreshResources();
                
                LanguagePicker.Title = LocalizedString.GetString("SelectLanguage");
                
                var languages = LanguagePicker.ItemsSource as List<LanguageItem>;
                if (languages != null)
                {
                    foreach (var lang in languages)
                    {
                        if (lang.Code == "en")
                            lang.DisplayName = LocalizedString.GetString("English");
                        else if (lang.Code == "de")
                            lang.DisplayName = LocalizedString.GetString("German");
                    }
                }
                
                var selectedItem = LanguagePicker.SelectedItem;
                LanguagePicker.SelectedItem = null;
                LanguagePicker.SelectedItem = selectedItem;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating localized text: {ex.Message}");
            }
            finally
            {
                _isUpdatingText = false;
            }
        }
        
        private void OnLocalizationServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalizationService.CurrentCulture))
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    UpdateLocalizedText();
                    SetLanguagePickerSelection();
                });
            }
        }
        
        private void OnLanguageChanged(object? sender, CultureInfo cultureInfo)
        {
            MainThread.BeginInvokeOnMainThread(() => {
                if (!_isChangingLanguage)
                {
                    UpdateLocalizedText();
                    SetLanguagePickerSelection();
                }
            });
        }

        private void LanguagePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_isInitialized || LanguagePicker.SelectedItem == null) return;
            
            try
            {
                var selectedLanguage = LanguagePicker.SelectedItem as LanguageItem;
                if (selectedLanguage != null)
                {
                    if (!_localizationService.IsCurrentLanguage(selectedLanguage.Code))
                    {
                        _isChangingLanguage = true;
                        
                        try
                        {
                            _localizationService.SwitchLanguage(selectedLanguage.Code);
                            
                            MainThread.BeginInvokeOnMainThread(async () => {
                                await System.Threading.Tasks.Task.Delay(100);
                                UpdateLocalizedText();
                            });
                        }
                        finally
                        {
                            _isChangingLanguage = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in language selection: {ex.Message}");
            }
        }

        protected void OnPropertyChangedEvent([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected override void OnParentSet()
        {
            base.OnParentSet();
            
            if (Parent != null)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    UpdateLocalizedText();
                });
            }
        }
    }
}