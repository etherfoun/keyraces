window.lobbyInterop = {
    initializeTypingGame: () => {
        console.log("Initializing typing game...")
    },

    startGame: function () {
        console.log("Starting game...")

        const typingInput = document.getElementById("typing-input")
        if (typingInput) {
            typingInput.focus()
        }

        this.startTime = new Date()
        this.updateTimer()
    },

    updateTimer: function () {
        const timeDisplay = document.getElementById("time-display")
        if (!timeDisplay) return

        const now = new Date()
        const elapsedTime = Math.floor((now - this.startTime) / 1000)
        const minutes = Math.floor(elapsedTime / 60)
        const seconds = elapsedTime % 60

        timeDisplay.textContent = `${minutes.toString().padStart(2, "0")}:${seconds.toString().padStart(2, "0")}`

        if (!this.gameFinished) {
            setTimeout(() => this.updateTimer(), 1000)
        }
    },

    handleKeyDown: (key, currentPosition) => {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return

        const characters = textDisplay.querySelectorAll(".character")

        const currentChar = textDisplay.querySelector(".character.current")
        if (currentChar) {
            currentChar.classList.remove("current")
        }

        if (currentPosition < characters.length) {
            characters[currentPosition].classList.add("current")
        }
    },

    handleKeyUp: (key, currentPosition) => {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return

        const characters = textDisplay.querySelectorAll(".character")
        const typingInput = document.getElementById("typing-input")

        if (!typingInput) return

        const inputText = typingInput.value

        for (let i = 0; i < characters.length; i++) {
            characters[i].classList.remove("correct", "incorrect")

            if (i < inputText.length) {
                if (inputText[i] === characters[i].textContent) {
                    characters[i].classList.add("correct")
                } else {
                    characters[i].classList.add("incorrect")
                }
            }
        }
    },

    updateStats: (wpm, accuracy, elapsedMinutes) => {
        const wpmDisplay = document.getElementById("wpm-display")
        const accuracyDisplay = document.getElementById("accuracy-display")

        if (wpmDisplay) wpmDisplay.textContent = wpm
        if (accuracyDisplay) accuracyDisplay.textContent = accuracy.toFixed(1)
    },

    finishGame: function () {
        console.log("Game finished!")
        this.gameFinished = true

        const typingInput = document.getElementById("typing-input")
        if (typingInput) {
            typingInput.disabled = true
        }
    },
}
function scrollToBottom(element) {
    if (element) {
        element.scrollTop = element.scrollHeight
    }
}
