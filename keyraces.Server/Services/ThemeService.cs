using Microsoft.JSInterop;

namespace keyraces.Server.Services
{
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _defaultTheme = "dark"; // Тема по умолчанию

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string DefaultTheme => _defaultTheme;

        public async Task<string> GetCurrentThemeAsync()
        {
            try
            {
                // Проверяем, доступен ли themeInterop
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
                // Если JS не доступен, возвращаем тему по умолчанию
                return _defaultTheme;
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            try
            {
                // Проверяем, доступен ли themeInterop
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
                // Игнорируем ошибки при предварительном рендеринге
            }
        }

        public async Task<string> ToggleThemeAsync()
        {
            try
            {
                // Проверяем, доступен ли themeInterop
                bool isAvailable = await _jsRuntime.InvokeAsync<bool>("eval", "typeof window.themeInterop !== 'undefined'");

                if (!isAvailable)
                {
                    Console.WriteLine("themeInterop is not available when toggling theme");
                    return _defaultTheme;
                }

                // Получаем новую тему после переключения
                string newTheme = await _jsRuntime.InvokeAsync<string>("window.themeInterop.toggleTheme");
                Console.WriteLine($"Theme toggled to: {newTheme}");
                return newTheme;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling theme: {ex.Message}");
                // В случае ошибки возвращаем текущую тему
                return await GetCurrentThemeAsync();
            }
        }
    }
}
