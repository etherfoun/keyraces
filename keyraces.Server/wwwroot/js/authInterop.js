// Функции для работы с JWT токенами
window.authInterop = {
    // Обновляем функцию isTokenExpired для более надежной проверки
    isTokenExpired: () => {
        const token = localStorage.getItem("auth_token")
        if (!token) return true

        try {
            // Декодируем JWT токен
            const base64Url = token.split(".")[1]
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/")
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split("")
                    .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                    .join(""),
            )

            const payload = JSON.parse(jsonPayload)
            const expiry = payload.exp

            // Добавляем буфер в 5 минут для надежности
            const bufferTime = 5 * 60 * 1000 // 5 минут в миллисекундах

            // Проверяем, истек ли токен с учетом буфера
            const isExpired = expiry * 1000 < Date.now() - bufferTime

            console.log("Token expiry check:", {
                expiryTimestamp: expiry * 1000,
                currentTime: Date.now(),
                isExpired: isExpired,
            })

            return isExpired
        } catch (e) {
            console.error("Error checking token expiry:", e)
            return true
        }
    },

    // Получает текущий токен
    getToken: () => localStorage.getItem("auth_token"),

    // Получает имя пользователя
    getUserName: () => localStorage.getItem("user_name"),

    // Сохраняет токены в localStorage
    saveTokens: (data) => {
        if (data.token) localStorage.setItem("auth_token", data.token)
        if (data.refreshToken) localStorage.setItem("refresh_token", data.refreshToken)
        if (data.userId) localStorage.setItem("user_id", data.userId)
        if (data.userName) localStorage.setItem("user_name", data.userName)
        console.log("Tokens saved to localStorage:", data)
    },

    // Очищает токены
    clearTokens: () => {
        localStorage.removeItem("auth_token")
        localStorage.removeItem("refresh_token")
        localStorage.removeItem("user_id")
        localStorage.removeItem("user_name")
        console.log("Tokens cleared from localStorage")
    },

    // Обновить функцию login для более подробного логирования
    login: (email, password) => {
        console.log("Login attempt for:", email)
        return fetch("/api/auth/login", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                email: email,
                password: password,
            }),
            credentials: "include",
        })
            .then((response) => {
                console.log("Login response status:", response.status)

                return response.json().then((data) => {
                    if (response.status === 200) {
                        console.log("Login successful, saving tokens:", data)
                        window.authInterop.saveTokens(data)
                        return { success: true }
                    } else {
                        console.log("Login failed:", data)
                        return {
                            success: false,
                            message: data.message || "Invalid email or password",
                        }
                    }
                })
            })
            .catch((error) => {
                console.error("Error during login:", error)
                return {
                    success: false,
                    message: error.message || "An error occurred during login",
                }
            })
    },

    // Обновить функцию register для более подробного логирования
    register: (name, email, password) => {
        console.log("Register attempt for:", email)
        return fetch("/api/auth/register", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                name: name,
                email: email,
                password: password,
            }),
            credentials: "include",
        })
            .then((response) => {
                console.log("Register response status:", response.status)

                return response.json().then((data) => {
                    if (response.status === 200) {
                        console.log("Registration successful, saving tokens:", data)
                        window.authInterop.saveTokens(data)
                        return { success: true }
                    } else {
                        console.log("Registration failed:", data)
                        return {
                            success: false,
                            message: data.message || "Registration failed",
                        }
                    }
                })
            })
            .catch((error) => {
                console.error("Error during registration:", error)
                return {
                    success: false,
                    message: error.message || "An error occurred during registration",
                }
            })
    },

    // Проверяет статус аутентификации
    checkAuthStatus: () =>
        fetch("/api/auth/status")
            .then((response) => {
                if (response.ok) {
                    return response.json()
                }
                return { isAuthenticated: false }
            })
            .catch((error) => {
                console.error("Error checking auth status:", error)
                return { isAuthenticated: false }
            }),

    // Выполняет выход
    logout: function () {
        // Очищаем токены
        this.clearTokens()

        // Выполняем запрос на выход
        return fetch("/api/auth/logout").catch((error) => {
            console.error("Error during logout:", error)
        })
    },

    // Выполняет запрос с учетом аутентификации
    fetchWithAuth: function (url, options) {
        options = options || {}

        // Добавляем заголовок Authorization
        const token = this.getToken()
        if (token) {
            if (!options.headers) options.headers = {}
            options.headers["Authorization"] = "Bearer " + token
        }

        // Выполняем запрос
        return fetch(url, options)
    },

    // Обновляет токен
    refreshToken: () => {
        const refreshToken = localStorage.getItem("refresh_token")
        if (!refreshToken) return Promise.resolve(false)

        return fetch("/api/auth/refresh", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({ refreshToken: refreshToken }),
        })
            .then((response) => {
                if (response.ok) {
                    return response.json().then((data) => {
                        window.authInterop.saveTokens(data)
                        return true
                    })
                }
                return false
            })
            .catch((error) => {
                console.error("Error refreshing token:", error)
                return false
            })
    },

    // Добавляем новую функцию для проверки аутентификации
    checkAuthentication: () => {
        const token = localStorage.getItem("auth_token")
        const userId = localStorage.getItem("user_id")
        const userName = localStorage.getItem("user_name")

        // Проверяем наличие всех необходимых данных
        if (!token || !userId || !userName) {
            console.log("Authentication check failed: missing token, userId, or userName")
            return false
        }

        // Проверяем срок действия токена
        try {
            const base64Url = token.split(".")[1]
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/")
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split("")
                    .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                    .join(""),
            )

            const payload = JSON.parse(jsonPayload)
            const expiry = payload.exp

            // Проверяем, истек ли токен
            if (expiry * 1000 < Date.now()) {
                console.log("Authentication check failed: token expired", {
                    expiryTimestamp: expiry * 1000,
                    currentTime: Date.now(),
                })
                return false
            }

            console.log("Authentication check passed")
            return true
        } catch (e) {
            console.error("Error during authentication check:", e)
            return false
        }
    },
}

// Добавляем инициализацию для проверки, что authInterop доступен
document.addEventListener("DOMContentLoaded", () => {
    console.log("authInterop initialized:", !!window.authInterop)
})
