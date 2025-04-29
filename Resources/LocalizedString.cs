using PDFMergeTool.Resources.Strings;
using System.Reflection;
using System.Globalization;
using System.Resources;

namespace PDFMergeTool.Resources
{
    public class LocalizedString
    {
        static LocalizedString()
        {
            var _ = AppResources.ResourceManager;
        }

        private static readonly object _resourceLock = new object();
        
        private static CultureInfo _lastUsedCulture = CultureInfo.CurrentUICulture;

        public static string GetString(string key)
        {
            try
            {
                if (string.IsNullOrEmpty(key))
                    return string.Empty;

                if (AppResources.Culture == null || AppResources.Culture.Name != CultureInfo.CurrentUICulture.Name)
                {
                    AppResources.Culture = CultureInfo.CurrentUICulture;
                    
                    if (_lastUsedCulture.Name != CultureInfo.CurrentUICulture.Name)
                    {
                        RefreshResources();
                        _lastUsedCulture = CultureInfo.CurrentUICulture;
                    }
                }
                
                string? result = GetResourceViaProperty(key);
                
                if (!string.IsNullOrEmpty(result))
                    return result;
                
                var resourceManager = AppResources.ResourceManager;
                if (resourceManager != null)
                {
                    result = resourceManager.GetString(key, CultureInfo.CurrentUICulture);
                    
                    if (string.IsNullOrEmpty(result))
                        result = resourceManager.GetString(key, new CultureInfo("en"));
                }
                
                return result ?? key;
            }
            catch
            {
                return key;
            }
        }

        private static string? GetResourceViaProperty(string key)
        {
            try
            {
                PropertyInfo? prop = typeof(AppResources).GetProperty(key, 
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                
                if (prop != null)
                {
                    return prop.GetValue(null) as string;
                }
            }
            catch
            {
            }
            
            return null;
        }

        public static string GetString(string key, params object[] args)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;
                
            string value = GetString(key);
            if (!string.IsNullOrEmpty(value) && args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(value, args);
                }
                catch
                {
                    return value;
                }
            }
            return value;
        }

        public static void RefreshResources()
        {
            lock (_resourceLock)
            {
                try
                {
                    var currentCulture = CultureInfo.CurrentUICulture;
                    
                    var resourceManagerField = typeof(AppResources).GetField("resourceMan", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (resourceManagerField != null)
                    {
                        resourceManagerField.SetValue(null, null);
                        
                        var newResourceManager = new ResourceManager(
                            "PDFMergeTool.Resources.Strings.AppResources", 
                            typeof(AppResources).Assembly);
                        
                        resourceManagerField.SetValue(null, newResourceManager);
                    }
                    
                    AppResources.Culture = currentCulture;
                    
                    try
                    {
                        _ = AppResources.AppTitle;
                        _ = AppResources.Language;
                    }
                    catch
                    {
                    }

                    var cultureField = typeof(AppResources).GetField("resourceCulture", 
                        BindingFlags.NonPublic | BindingFlags.Static);
                    
                    if (cultureField != null)
                    {
                        cultureField.SetValue(null, currentCulture);
                    }
                    
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error refreshing resources: {ex.Message}");
                }
            }
        }
    }
}