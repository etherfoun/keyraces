window.lobbyInterop = {
    initializeTypingGame: () => {
        console.log("Initializing typing game...")
    },

    startGame: function () {
        console.log("Starting game...")

        const typingInput = document.getElementById("typing-input")
        if (typingInput) {
            typingInput.focus()

            typingInput.addEventListener("paste", (e) => {
                e.preventDefault()
                console.log("Paste prevented")
            })

            typingInput.addEventListener("contextmenu", (e) => {
                e.preventDefault()
                return false
            })
        }

        this.startTime = new Date()
        this.updateTimer()

        this.currentWordIndex = 0
        this.wordsCompleted = 0
        this.correctWords = 0
        this.totalChars = 0
        this.errorChars = 0
        this.currentCharIndex = 0
        this.highlightCurrentWord()
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

    highlightCurrentWord: function () {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return

        const words = textDisplay.querySelectorAll(".word")
        words.forEach((word) => {
            word.classList.remove("current-word")
        })

        if (this.currentWordIndex < words.length) {
            words[this.currentWordIndex].classList.add("current-word")

            this.currentCharIndex = 0

            const characters = words[this.currentWordIndex].querySelectorAll(".character")
            if (characters.length > 0) {
                characters.forEach((char) => char.classList.remove("current"))
                characters[0].classList.add("current")
            }
        }
    },

    handleKeyDown: function (e, currentInput) {
        if (e.ctrlKey || e.altKey || e.metaKey) return true

        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return true

        const words = textDisplay.querySelectorAll(".word")
        if (this.currentWordIndex >= words.length) return true

        const currentWord = words[this.currentWordIndex]
        const targetWord = currentWord.getAttribute("data-word")
        const characters = currentWord.querySelectorAll(".character")

        if (e.key === " ") {
            if (currentInput === targetWord) {
                this.completeCurrentWord(true)
                return false
            }
            return false
        }

        if (e.key === "Backspace") {
            if (this.currentCharIndex > 0) {
                this.currentCharIndex--

                characters.forEach((char) => char.classList.remove("current"))
                if (this.currentCharIndex < characters.length) {
                    characters[this.currentCharIndex].classList.remove("correct", "incorrect")
                    characters[this.currentCharIndex].classList.add("current")
                }

                this.calculateProgress()
            }
            return true
        }

        if (e.key.length === 1) {
            if (this.currentCharIndex >= targetWord.length) {
                return false
            }

            const expectedChar = targetWord[this.currentCharIndex]
            const isCorrect = e.key === expectedChar

            this.totalChars++
            if (!isCorrect) {
                this.errorChars++
            }

            if (this.currentCharIndex < characters.length) {
                characters[this.currentCharIndex].classList.remove("current")
                characters[this.currentCharIndex].classList.add(isCorrect ? "correct" : "incorrect")
            }

            if (isCorrect) {
                this.currentCharIndex++

                this.calculateProgress()

                if (this.currentCharIndex === targetWord.length) {
                    this.completeCurrentWord(true)
                    return false
                }

                if (this.currentCharIndex < characters.length) {
                    characters[this.currentCharIndex].classList.add("current")
                }
            }

            return isCorrect
        }

        return true
    },

    completeCurrentWord: function (isCorrect) {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return 0

        const words = textDisplay.querySelectorAll(".word")
        if (this.currentWordIndex >= words.length) return 100

        const currentWord = words[this.currentWordIndex]

        currentWord.classList.add(isCorrect ? "correct-word" : "incorrect-word")
        this.wordsCompleted++
        if (isCorrect) {
            this.correctWords++
        }

        this.currentWordIndex++
        this.currentCharIndex = 0

        const typingInput = document.getElementById("typing-input")
        if (typingInput) {
            typingInput.value = ""
        }

        this.highlightCurrentWord()

        const progress = this.calculateProgress()

        if (this.currentWordIndex >= words.length) {
            this.finishGame()
        }

        return progress
    },

    calculateProgress: function () {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return 0

        const words = textDisplay.querySelectorAll(".word")
        if (words.length === 0) return 0

        let totalChars = 0
        let completedChars = 0

        for (let i = 0; i < words.length; i++) {
            const word = words[i]
            const wordText = word.getAttribute("data-word")
            const wordLength = wordText.length + (i < words.length - 1 ? 1 : 0)

            totalChars += wordLength

            if (i < this.currentWordIndex) {
                completedChars += wordLength
            } else if (i === this.currentWordIndex) {
                completedChars += this.currentCharIndex
            }
        }

        const progress = Math.floor((completedChars / totalChars) * 100)

        const currentUserId = document.querySelector("[data-current-user-id]")?.getAttribute("data-current-user-id")

        console.log("Current user ID:", currentUserId, "Progress:", progress)

        if (currentUserId) {
            const progressItem = document.querySelector(`.progress-item[data-user-id="${currentUserId}"]`)
            if (progressItem) {
                const progressBar = progressItem.querySelector(".progress-bar")
                if (progressBar) {
                    console.log("Updating progress bar to", progress + "%")
                    progressBar.style.width = `${progress}%`
                } else {
                    console.log("Progress bar element not found")
                }
            } else {
                console.log("Progress item not found for user ID:", currentUserId)
            }
        } else {
            console.log("Current user ID not found")
        }

        return progress
    },

    updateStats: function (elapsedMinutes) {
        if (!this.wordsCompleted) this.wordsCompleted = 0
        if (!this.correctWords) this.correctWords = 0
        if (!this.currentWordIndex) this.currentWordIndex = 0
        if (!this.totalChars) this.totalChars = 0
        if (!this.errorChars) this.errorChars = 0

        if (this.gameFinished && this.finalWPM && this.finalAccuracy) {
            const wpmDisplay = document.getElementById("wpm-display")
            const accuracyDisplay = document.getElementById("accuracy-display")

            if (wpmDisplay) wpmDisplay.textContent = this.finalWPM.toString()
            if (accuracyDisplay) accuracyDisplay.textContent = this.finalAccuracy.toFixed(1)

            return { wpm: this.finalWPM, accuracy: this.finalAccuracy }
        }

        if (elapsedMinutes <= 0) {
            const wpmDisplay = document.getElementById("wpm-display")
            const accuracyDisplay = document.getElementById("accuracy-display")

            if (wpmDisplay) wpmDisplay.textContent = "0"
            if (accuracyDisplay) accuracyDisplay.textContent = "100.0"

            return { wpm: 0, accuracy: 100.0 }
        }

        const wpm = Math.round(this.wordsCompleted / elapsedMinutes)

        let accuracy = 100.0
        if (this.totalChars > 0) {
            accuracy = ((this.totalChars - this.errorChars) / this.totalChars) * 100
        }

        const wpmDisplay = document.getElementById("wpm-display")
        const accuracyDisplay = document.getElementById("accuracy-display")

        if (wpmDisplay) wpmDisplay.textContent = wpm.toString()
        if (accuracyDisplay) accuracyDisplay.textContent = accuracy.toFixed(1)

        return { wpm: wpm, accuracy: accuracy }
    },

    finishGame: function () {
        console.log("Game finished!")
        this.gameFinished = true

        const elapsedMinutes = (new Date() - this.startTime) / 60000
        const stats = this.updateStats(elapsedMinutes)
        this.finalWPM = stats.wpm
        this.finalAccuracy = stats.accuracy

        const finalWpmElement = document.getElementById("final-wpm")
        const finalAccuracyElement = document.getElementById("final-accuracy")

        if (finalWpmElement) finalWpmElement.textContent = this.finalWPM
        if (finalAccuracyElement) finalAccuracyElement.textContent = this.finalAccuracy.toFixed(1) + "%"

        const completionModal = document.getElementById("completion-notification")
        if (completionModal) {
            completionModal.classList.add("show")
        }

        const typingInput = document.getElementById("typing-input")
        if (typingInput) {
            typingInput.disabled = true
        }

        if (completionModal) {
            completionModal.setAttribute("data-final-wpm", this.finalWPM)
            completionModal.setAttribute("data-final-accuracy", this.finalAccuracy)
        }
    },

    filterInput: (input, currentWordIndex) => {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return input

        const words = textDisplay.querySelectorAll(".word")
        if (currentWordIndex >= words.length) return input

        const targetWord = words[currentWordIndex].getAttribute("data-word")

        for (let i = 0; i < input.length; i++) {
            if (i >= targetWord.length || input[i] !== targetWord[i]) {
                return input.substring(0, i)
            }
        }

        return input
    },

    isGameComplete: function () {
        const textDisplay = document.getElementById("text-display")
        if (!textDisplay) return false

        const words = textDisplay.querySelectorAll(".word")
        if (!words.length) return false

        return this.currentWordIndex >= words.length
    },

    hideCompletionModal: () => {
        const completionModal = document.getElementById("completion-notification")
        if (completionModal) {
            completionModal.classList.remove("show")
        }
    },

    scrollToResults: () => {
        const resultsArea = document.querySelector(".results-area")
        if (resultsArea) {
            resultsArea.scrollIntoView({ behavior: "smooth", block: "start" })
        }
    },

    getFinalResults: function () {
        return {
            wpm: this.finalWPM || 0,
            accuracy: this.finalAccuracy || 0,
        }
    },
}

function scrollToBottom(element) {
    if (element) {
        element.scrollTop = element.scrollHeight
    }
}

window.preventDefault = (event) => {
    console.log("preventDefault called, but it's not needed in this context")
}
