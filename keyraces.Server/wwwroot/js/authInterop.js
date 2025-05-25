window.authInterop = {
    isTokenExpired: () => {
        const token = localStorage.getItem("auth_token")
        if (!token) return true

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

            const bufferTime = 5 * 60 * 1000

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

    getToken: () => localStorage.getItem("auth_token"),

    getUserName: () => localStorage.getItem("user_name"),

    saveTokens: (data) => {
        if (data.token) localStorage.setItem("auth_token", data.token)
        if (data.refreshToken) localStorage.setItem("refresh_token", data.refreshToken)
        if (data.userId) localStorage.setItem("user_id", data.userId)
        if (data.userName) localStorage.setItem("user_name", data.userName)
        console.log("Tokens saved to localStorage:", data)
    },

    clearTokens: () => {
        localStorage.removeItem("auth_token")
        localStorage.removeItem("refresh_token")
        localStorage.removeItem("user_id")
        localStorage.removeItem("user_name")
        console.log("Tokens cleared from localStorage")
    },

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

                const contentType = response.headers.get("content-type")

                if (contentType && contentType.includes("application/json")) {
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
                } else {
                    return response.text().then((text) => {
                        console.error("Server returned non-JSON response:", text)
                        return {
                            success: false,
                            message: "Server error occurred. Please try again later.",
                        }
                    })
                }
            })
            .catch((error) => {
                console.error("Error during login:", error)
                return {
                    success: false,
                    message: "An error occurred during login. Please try again.",
                }
            })
    },

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

                const contentType = response.headers.get("content-type")

                if (contentType && contentType.includes("application/json")) {
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
                } else {
                    return response.text().then((text) => {
                        console.error("Server returned non-JSON response:", text)
                        return {
                            success: false,
                            message: "Server error occurred. Please try again later.",
                        }
                    })
                }
            })
            .catch((error) => {
                console.error("Error during registration:", error)
                return {
                    success: false,
                    message: error.message || "An error occurred during registration",
                }
            })
    },

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

    logout: function () {
        this.clearTokens()

        return fetch("/api/auth/logout").catch((error) => {
            console.error("Error during logout:", error)
        })
    },

    fetchWithAuth: function (url, options) {
        options = options || {}

        const token = this.getToken()
        if (token) {
            if (!options.headers) options.headers = {}
            options.headers["Authorization"] = "Bearer " + token
        }

        return fetch(url, options)
    },

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

    checkAuthentication: () => {
        const token = localStorage.getItem("auth_token")
        const userId = localStorage.getItem("user_id")
        const userName = localStorage.getItem("user_name")

        if (!token || !userId || !userName) {
            console.log("Authentication check failed: missing token, userId, or userName")
            return false
        }

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

    getUserInfo: () => {
        try {
            const token = localStorage.getItem("auth_token")
            if (!token) {
                console.log("No auth token found")
                return null
            }

            const base64Url = token.split(".")[1]
            const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/")
            const jsonPayload = decodeURIComponent(
                atob(base64)
                    .split("")
                    .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
                    .join(""),
            )

            const payload = JSON.parse(jsonPayload)
            console.log("JWT payload:", payload)

            const userInfo = {
                userId:
                    payload.sub ||
                    payload.nameid ||
                    payload.id ||
                    payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
                username:
                    payload.unique_name ||
                    payload.name ||
                    payload.username ||
                    payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
                name:
                    payload.name ||
                    payload.given_name ||
                    payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"],
                email: payload.email || payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
                role: payload.role || payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
            }

            Object.keys(userInfo).forEach((key) => {
                if (userInfo[key] === undefined) {
                    delete userInfo[key]
                }
            })

            console.log("Extracted user info:", userInfo)
            return userInfo
        } catch (error) {
            console.error("Error getting user info:", error)
            return null
        }
    },
}

document.addEventListener("DOMContentLoaded", () => {
    console.log("authInterop initialized:", !!window.authInterop)
})
