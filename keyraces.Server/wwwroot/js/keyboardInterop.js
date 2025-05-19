// Keyboard interaction helpers
window.keyboardInterop = {
    // Play key sound
    playKeySound: (keyType) => {
        const audioContext = new (window.AudioContext || window.webkitAudioContext)()
        const oscillator = audioContext.createOscillator()
        const gainNode = audioContext.createGain()

        // Different sound for different key types
        switch (keyType) {
            case "normal":
                oscillator.type = "sine"
                oscillator.frequency.value = 440 // A4 note
                break
            case "space":
                oscillator.type = "sine"
                oscillator.frequency.value = 330 // E4 note
                break
            case "enter":
                oscillator.type = "triangle"
                oscillator.frequency.value = 523.25 // C5 note
                break
            case "error":
                oscillator.type = "sawtooth"
                oscillator.frequency.value = 220 // A3 note
                break
            default:
                oscillator.type = "sine"
                oscillator.frequency.value = 440
        }

        // Connect nodes
        oscillator.connect(gainNode)
        gainNode.connect(audioContext.destination)

        // Set volume and duration
        gainNode.gain.value = 0.1
        gainNode.gain.exponentialRampToValueAtTime(0.001, audioContext.currentTime + 0.2)

        // Play sound
        oscillator.start()
        oscillator.stop(audioContext.currentTime + 0.2)
    },

    // Request focus on typing area
    focusTypingArea: (elementId) => {
        const element = document.getElementById(elementId)
        if (element) {
            element.focus()
        }
    },

    // Toggle fullscreen
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
}

// Listen for fullscreen toggle event
document.addEventListener("toggleFullscreen", () => {
    window.keyboardInterop.toggleFullscreen()
})
