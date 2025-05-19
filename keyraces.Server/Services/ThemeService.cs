using Microsoft.JSInterop;

namespace keyraces.Server.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _defaultTheme = "dark";

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string DefaultTheme => _defaultTheme;

        public async Task<string> GetCurrentThemeAsync()
        {
            try
            {
                bool isAvailable = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.themeInterop !== 'undefined'");

                if (!isAvailable)
                {
                    Console.WriteLine("themeInterop is not available when getting theme");
                    return _defaultTheme;
                }

                return await _jsRuntime.InvokeAsync<string>("window.themeInterop.getTheme");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting theme: {ex.Message}");
                return _defaultTheme;
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            try
            {
                bool isAvailable = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.themeInterop !== 'undefined'");

                if (!isAvailable)
                {
                    Console.WriteLine("themeInterop is not available when setting theme");
                    return;
                }

                await _jsRuntime.InvokeVoidAsync("window.themeInterop.setTheme", theme);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting theme: {ex.Message}");
            }
        }

        public async Task<string> ToggleThemeAsync()
        {
            try
            {
                bool isAvailable = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.themeInterop !== 'undefined'");

                if (!isAvailable)
                {
                    Console.WriteLine("themeInterop is not available when toggling theme");
                    return _defaultTheme;
                }

                string newTheme = await _jsRuntime.InvokeAsync<string>("window.themeInterop.toggleTheme");
                Console.WriteLine($"Theme toggled to: {newTheme}");
                return newTheme;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling theme: {ex.Message}");
                return await GetCurrentThemeAsync();
            }
        }
    }
}
