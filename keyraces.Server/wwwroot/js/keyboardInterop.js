window.keyboardInterop = {
    focusTypingArea: (elementId) => {
        const element = document.getElementById(elementId)
        if (element) {
            element.focus()
        }
    },

    toggleFullscreen: () => {
        if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen().catch((err) => {
                console.error(`Error attempting to enable fullscreen: ${err.message}`)
            })
        } else {
            if (document.exitFullscreen) {
                document.exitFullscreen()
            }
        }
    },

    disableScroll: () => {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop
        const scrollLeft = window.pageXOffset || document.documentElement.scrollLeft

        document.body.style.overflow = "hidden"
        document.body.style.position = "fixed"
        document.body.style.top = `-${scrollTop}px`
        document.body.style.left = `-${scrollLeft}px`
        document.body.style.width = "100%"

        window.addEventListener("wheel", window.keyboardInterop.preventDefault, { passive: false })
        window.addEventListener("touchmove", window.keyboardInterop.preventDefault, { passive: false })

        window.addEventListener(
            "keydown",
            (e) => {
                const keys = {
                    33: 1, // Page Up
                    34: 1, // Page Down
                    35: 1, // End
                    36: 1, // Home
                    37: 1, // Left
                    38: 1, // Up
                    39: 1, // Right
                    40: 1, // Down
                }

                if (e.keyCode === 32) {
                    const activeElement = document.activeElement
                    const isInputElement =
                        activeElement.tagName === "INPUT" ||
                        activeElement.tagName === "TEXTAREA" ||
                        activeElement.classList.contains("typing-area")

                    if (!isInputElement) {
                        e.preventDefault()
                        return false
                    }
                    return true
                }

                if (keys[e.keyCode]) {
                    e.preventDefault()
                    return false
                }
            },
            { passive: false },
        )
    },

    enableScroll: () => {
        document.body.style.overflow = ""
        document.body.style.position = ""
        document.body.style.top = ""
        document.body.style.left = ""
        document.body.style.width = ""

        window.removeEventListener("wheel", window.keyboardInterop.preventDefault)
        window.removeEventListener("touchmove", window.keyboardInterop.preventDefault)
        window.removeEventListener("keydown", window.keyboardInterop.preventScrollKeys)
    },

    preventDefault: (e) => {
        e.preventDefault()
    },
}

document.addEventListener("DOMContentLoaded", () => {
    if (window.location.pathname.includes("/practice")) {
        window.keyboardInterop.disableScroll()

        document.addEventListener("keydown", (e) => {
            if (e.key === "F11") {
                e.preventDefault()
                window.keyboardInterop.toggleFullscreen()
            }
        })

        console.log("Scroll prevention enabled for Practice page")
    }
})
