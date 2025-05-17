// Определяем объект themeInterop глобально
window.themeInterop = {
    // Ключ для хранения темы в localStorage
    THEME_KEY: "keyraces-theme",

    // Получить текущую тему
    getTheme: function () {
        const theme = localStorage.getItem(this.THEME_KEY) || "dark"
        console.log("Getting theme:", theme)
        return theme
    },

    // Установить тему
    setTheme: function (theme) {
        console.log("Setting theme to:", theme)
        localStorage.setItem(this.THEME_KEY, theme)
        document.documentElement.setAttribute("data-theme", theme)
        this.fixSelectStyles()
        return theme
    },

    // Инициализация темы
    initTheme: function () {
        console.log("Initializing theme")
        const theme = this.getTheme()
        document.documentElement.setAttribute("data-theme", theme)
        this.fixSelectStyles()
        return theme
    },

    // Переключение темы
    toggleTheme: function () {
        console.log("Toggling theme")
        const currentTheme = this.getTheme()
        console.log("Current theme before toggle:", currentTheme)

        // Переключаем тему
        const newTheme = currentTheme === "dark" ? "light" : "dark"
        console.log("New theme after toggle:", newTheme)

        return this.setTheme(newTheme)
    },

    // Исправление стилей для select-элементов
    fixSelectStyles: function () {
        const theme = this.getTheme()
        const selects = document.querySelectorAll("select")

        selects.forEach((select) => {
            try {
                select.style.backgroundColor = getComputedStyle(document.documentElement).getPropertyValue(
                    theme === "dark" ? "--dark-bg-input" : "--light-bg-input",
                )
                select.style.color = getComputedStyle(document.documentElement).getPropertyValue(
                    theme === "dark" ? "--dark-text-primary" : "--light-text-primary",
                )
                select.style.borderColor = getComputedStyle(document.documentElement).getPropertyValue(
                    theme === "dark" ? "--dark-border-color" : "--light-border-color",
                )
            } catch (e) {
                console.error("Error styling select:", e)
            }
        })
    },
}

// Сообщаем, что скрипт загружен
console.log("themeInterop.js loaded, object defined:", typeof window.themeInterop !== "undefined")

// Инициализация темы при загрузке DOM
document.addEventListener("DOMContentLoaded", () => {
    console.log("DOM loaded, initializing theme")
    window.themeInterop.initTheme()

    // Обработка динамически добавленных селектов
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.addedNodes.length) {
                setTimeout(() => window.themeInterop.fixSelectStyles(), 100)
            }
        })
    })

    // Запускаем наблюдение за изменениями в DOM
    observer.observe(document.body, { childList: true, subtree: true })
})
