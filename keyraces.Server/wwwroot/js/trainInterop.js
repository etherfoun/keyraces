window.trainInterop = {
    currentWordIndex: 0,
    totalErrors: 0,
    totalCharacters: 0,
    currentPosition: 0,
    isInitialized: false,
    textContent: "",
    words: [],
    errorsByCharacter: {},

    initTypingArea: function (inputId, textAreaId) {
        try {
            this.currentWordIndex = 0
            this.totalErrors = 0
            this.totalCharacters = 0
            this.currentPosition = 0
            this.errorsByCharacter = {}

            const inputElement = document.getElementById(inputId)
            const textArea = document.getElementById(textAreaId)

            if (!inputElement || !textArea) {
                console.error("Could not find input or text area elements")
                return false
            }

            this.textContent = textArea.textContent.trim()
            this.totalCharacters = this.textContent.length

            const wordElements = textArea.querySelectorAll(".word")
            this.words = Array.from(wordElements)

            if (this.words.length > 0) {
                this.words[0].classList.add("current-word")
            }

            inputElement.focus()

            const progressBar = document.getElementById("typing-progress-bar")
            if (progressBar) {
                progressBar.style.width = "0%"
                progressBar.setAttribute("aria-valuenow", "0")
            }

            this.isInitialized = true

            return true
        } catch (error) {
            console.error("Error initializing typing area:", error)
            return false
        }
    },

    updateHighlighting: function (userInput) {
        try {
            if (!this.isInitialized) {
                console.warn("Typing area not initialized")
                return false
            }

            const currentWordElement = this.words[this.currentWordIndex]
            if (!currentWordElement) return false

            const expectedWord = currentWordElement.getAttribute("data-word")

            const isCorrect = userInput === expectedWord
            const isPartiallyCorrect = expectedWord.startsWith(userInput)

            if (isCorrect) {
                currentWordElement.classList.remove("current-word")
                currentWordElement.classList.add("completed-word")

                this.currentWordIndex++
                if (this.currentWordIndex < this.words.length) {
                    this.words[this.currentWordIndex].classList.add("current-word")
                }

                this.updateProgress()

                document.getElementById("typing-input").value = ""

                return true
            } else if (isPartiallyCorrect) {
                this.highlightCharacters(currentWordElement, userInput)
                return true
            } else {
                const errorCount = this.highlightErrors(currentWordElement, userInput)
                this.totalErrors += errorCount

                const errorsDisplay = document.getElementById("errors-display")
                if (errorsDisplay) {
                    errorsDisplay.textContent = this.totalErrors.toString()
                }

                this.updateAccuracy()

                return false
            }
        } catch (error) {
            console.error("Error updating highlighting:", error)
            return false
        }
    },

    highlightCharacters: (wordElement, userInput) => {
        try {
            const characters = wordElement.querySelectorAll(".character")

            characters.forEach((char) => {
                char.classList.remove("correct-char", "error-char", "current-char")
            })

            for (let i = 0; i < userInput.length && i < characters.length; i++) {
                if (userInput[i] === characters[i].textContent) {
                    characters[i].classList.add("correct-char")
                } else {
                    characters[i].classList.add("error-char")
                }
            }

            if (userInput.length < characters.length) {
                characters[userInput.length].classList.add("current-char")
            }

            return true
        } catch (error) {
            console.error("Error highlighting characters:", error)
            return false
        }
    },

    highlightErrors: (wordElement, userInput) => {
        try {
            const characters = wordElement.querySelectorAll(".character")
            let errorCount = 0

            characters.forEach((char) => {
                char.classList.remove("correct-char", "error-char", "current-char")
            })

            for (let i = 0; i < userInput.length && i < characters.length; i++) {
                if (userInput[i] === characters[i].textContent) {
                    characters[i].classList.add("correct-char")
                } else {
                    characters[i].classList.add("error-char")
                    errorCount++
                }
            }

            if (userInput.length < characters.length) {
                characters[userInput.length].classList.add("current-char")
            }

            return errorCount
        } catch (error) {
            console.error("Error highlighting errors:", error)
            return 0
        }
    },

    updateProgress: function () {
        try {
            const progressBar = document.getElementById("typing-progress-bar")
            if (!progressBar) return false

            const progress = (this.currentWordIndex / this.words.length) * 100
            progressBar.style.width = `${progress}%`
            progressBar.setAttribute("aria-valuenow", progress.toString())

            return true
        } catch (error) {
            console.error("Error updating progress:", error)
            return false
        }
    },

    updateAccuracy: function () {
        try {
            const accuracyDisplay = document.getElementById("accuracy-display")
            if (!accuracyDisplay) return false

            const typedCharacters = this.calculateTypedCharacters()

            const correctCharacters = Math.max(0, typedCharacters - this.totalErrors)
            const accuracy = typedCharacters > 0 ? (correctCharacters / typedCharacters) * 100 : 100

            accuracyDisplay.textContent = accuracy.toFixed(1) + "%"

            return true
        } catch (error) {
            console.error("Error updating accuracy:", error)
            return false
        }
    },

    calculateTypedCharacters: function () {
        try {
            let count = 0

            for (let i = 0; i < this.currentWordIndex; i++) {
                count += this.words[i].getAttribute("data-word").length
            }

            const inputElement = document.getElementById("typing-input")
            if (inputElement && inputElement.value) {
                count += inputElement.value.length
            }

            count += Math.max(0, this.currentWordIndex)

            return count
        } catch (error) {
            console.error("Error calculating typed characters:", error)
            return 0
        }
    },

    calculateWPM: function (elapsedTimeInSeconds) {
        try {
            const typedCharacters = this.calculateTypedCharacters()

            const standardWords = typedCharacters / 5

            const minutes = elapsedTimeInSeconds / 60

            const wpm = minutes > 0 ? standardWords / minutes : 0

            return wpm
        } catch (error) {
            console.error("Error calculating WPM:", error)
            return 0
        }
    },

    getStatistics: function (elapsedTimeInSeconds) {
        try {
            return {
                wpm: this.calculateWPM(elapsedTimeInSeconds),
                errors: this.totalErrors,
                accuracy: this.calculateAccuracy(),
                typedCharacters: this.calculateTypedCharacters(),
                totalCharacters: this.totalCharacters,
            }
        } catch (error) {
            console.error("Error getting statistics:", error)
            return {
                wpm: 0,
                errors: 0,
                accuracy: 100,
                typedCharacters: 0,
                totalCharacters: 0,
            }
        }
    },

    calculateAccuracy: function () {
        try {
            const typedCharacters = this.calculateTypedCharacters()

            if (typedCharacters === 0) return 100

            const correctCharacters = Math.max(0, typedCharacters - this.totalErrors)
            return (correctCharacters / typedCharacters) * 100
        } catch (error) {
            console.error("Error calculating accuracy:", error)
            return 100
        }
    },

    initWordByWord: function (startIndex) {
        try {
            console.log("Initializing word-by-word typing mode with startIndex:", startIndex)
            this.currentWordIndex = startIndex || 0
            this.totalErrors = 0
            this.errorsByCharacter = {}

            const words = document.querySelectorAll(".word")
            words.forEach((word) => {
                word.classList.remove("current-word", "completed", "error")

                const characters = word.querySelectorAll(".character")
                characters.forEach((char) => {
                    char.classList.remove("correct", "incorrect", "current")
                })
            })

            if (words[this.currentWordIndex]) {
                words[this.currentWordIndex].classList.add("current-word")

                const characters = words[this.currentWordIndex].querySelectorAll(".character")
                if (characters.length > 0) {
                    characters.forEach((char) => {
                        char.classList.remove("correct", "incorrect", "current")
                    })

                    characters[0].classList.add("current")
                }
            }

            const input = document.getElementById("typing-input")
            if (input) {
                input.value = ""
                input.focus()
            }

            return true
        } catch (error) {
            console.error("Error initializing word-by-word typing:", error)
            return false
        }
    },

    moveToNextWord: function (newIndex) {
        try {
            console.log("Moving to next word with index:", newIndex)
            const words = document.querySelectorAll(".word")

            if (words[this.currentWordIndex]) {
                words[this.currentWordIndex].classList.remove("current-word", "error")
                words[this.currentWordIndex].classList.add("completed")

                const characters = words[this.currentWordIndex].querySelectorAll(".character")
                characters.forEach((char) => {
                    char.classList.remove("correct", "incorrect", "current")
                })
            }

            this.currentWordIndex = newIndex

            if (words[this.currentWordIndex]) {
                words[this.currentWordIndex].classList.add("current-word")

                const characters = words[this.currentWordIndex].querySelectorAll(".character")
                if (characters.length > 0) {
                    characters.forEach((char) => {
                        char.classList.remove("correct", "incorrect", "current")
                    })

                    characters[0].classList.add("current")
                }

                const textDisplay = document.querySelector(".text-display")
                if (textDisplay) {
                    const wordRect = words[this.currentWordIndex].getBoundingClientRect()
                    const displayRect = textDisplay.getBoundingClientRect()

                    if (wordRect.bottom > displayRect.bottom || wordRect.top < displayRect.top) {
                        words[this.currentWordIndex].scrollIntoView({
                            behavior: "smooth",
                            block: "center",
                        })
                    }
                }
            }

            const input = document.getElementById("typing-input")
            if (input) {
                input.value = ""
                input.classList.remove("error")
                input.focus()
            }

            return true
        } catch (error) {
            console.error("Error moving to next word:", error)
            return false
        }
    },

    showWordError: function () {
        try {
            const words = document.querySelectorAll(".word")
            const input = document.getElementById("typing-input")

            if (words[this.currentWordIndex]) {
                words[this.currentWordIndex].classList.add("error")
            }

            if (input) {
                input.classList.add("error")

                setTimeout(() => {
                    input.classList.remove("error")
                    if (words[this.currentWordIndex]) {
                        words[this.currentWordIndex].classList.remove("error")
                    }
                }, 500)
            }

            if (navigator.vibrate) {
                navigator.vibrate(100)
            }

            return true
        } catch (error) {
            console.error("Error showing word error:", error)
            return false
        }
    },

    showCompletionAnimation: () => {
        try {
            const confettiContainer = document.createElement("div")
            confettiContainer.className = "confetti-container"
            document.body.appendChild(confettiContainer)

            const colors = ["#ff0000", "#00ff00", "#0000ff", "#ffff00", "#ff00ff", "#00ffff"]
            for (let i = 0; i < 50; i++) {
                const confetti = document.createElement("div")
                confetti.className = "confetti"
                confetti.style.left = Math.random() * 100 + "%"
                confetti.style.backgroundColor = colors[Math.floor(Math.random() * colors.length)]
                confetti.style.animationDelay = Math.random() * 2 + "s"
                confettiContainer.appendChild(confetti)
            }

            setTimeout(() => {
                confettiContainer.remove()
            }, 5000)

            return true
        } catch (error) {
            console.error("Error showing completion animation:", error)
            return false
        }
    },

    updateCharacterHighlighting: function (typedText, expectedWord) {
        try {
            console.log("Updating character highlighting with typedText:", typedText, "expectedWord:", expectedWord)
            const currentWord = document.querySelector(".current-word")
            if (!currentWord) {
                console.error("No current word found")
                return { completed: false, errors: 0 }
            }

            const characters = currentWord.querySelectorAll(".character")
            if (!characters.length) {
                console.error("No characters found in current word")
                return { completed: false, errors: 0 }
            }

            console.log("Found", characters.length, "characters in current word")

            characters.forEach((char) => {
                char.classList.remove("correct", "incorrect", "current")
            })

            let errorsInCurrentInput = 0
            let allCorrect = true

            for (let i = 0; i < characters.length; i++) {
                if (i < typedText.length) {
                    if (i < expectedWord.length && typedText[i] === expectedWord[i]) {
                        characters[i].classList.add("correct")
                    } else {
                        characters[i].classList.add("incorrect")
                        errorsInCurrentInput++
                        allCorrect = false

                        const charKey = `${this.currentWordIndex}_${i}`
                        if (!this.errorsByCharacter[charKey]) {
                            this.errorsByCharacter[charKey] = true
                            this.totalErrors++
                        }
                    }
                } else if (i === typedText.length) {
                    characters[i].classList.add("current")
                } else {
                    allCorrect = false
                }
            }

            if (typedText.length > expectedWord.length) {
                currentWord.classList.add("error")
                allCorrect = false

                for (let i = expectedWord.length; i < typedText.length; i++) {
                    const charKey = `${this.currentWordIndex}_extra_${i}`
                    if (!this.errorsByCharacter[charKey]) {
                        this.errorsByCharacter[charKey] = true
                        this.totalErrors++
                    }
                }
            } else {
                currentWord.classList.remove("error")
            }

            const isCompleted = typedText.length === expectedWord.length && allCorrect

            const errorsDisplay = document.getElementById("errors-display")
            if (errorsDisplay) {
                errorsDisplay.textContent = this.totalErrors.toString()
            }

            this.updateAccuracy()

            return {
                completed: isCompleted,
                errors: errorsInCurrentInput,
            }
        } catch (error) {
            console.error("Error updating character highlighting:", error)
            return { completed: false, errors: 0 }
        }
    },

    preventDefaultAction: () => {
        return true
    },

    handleInput: function (inputElement) {
        try {
            console.log("Handling input from element:", inputElement.value)

            const currentWord = document.querySelector(".current-word")
            if (!currentWord) {
                console.error("No current word found")
                return false
            }

            const expectedWord = currentWord.getAttribute("data-word")
            const typedText = inputElement.value

            const result = this.updateCharacterHighlighting(typedText, expectedWord)

            return result
        } catch (error) {
            console.error("Error handling input:", error)
            return { completed: false, errors: 0 }
        }
    },

    setInputValue: (value) => {
        try {
            const inputElement = document.getElementById("typing-input")
            if (inputElement) {
                inputElement.value = value
            }
            return true
        } catch (error) {
            console.error("Error setting input value:", error)
            return false
        }
    },

    getErrorCount: function () {
        return this.totalErrors
    },

    resetErrorCount: function () {
        this.totalErrors = 0
        this.errorsByCharacter = {}
        return true
    },
}

document.addEventListener("keydown", (e) => {
    if (e.key === " " || e.key === "Enter") {
        if (document.activeElement && document.activeElement.id === "typing-input") {
            e.preventDefault()
        }
    }
})
