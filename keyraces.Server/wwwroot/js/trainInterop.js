// Typing training interop functions
window.trainInterop = {
    currentWordIndex: 0,
    totalErrors: 0,
    totalCharacters: 0,
    currentPosition: 0,
    isInitialized: false,
    textContent: "",
    words: [],

    // Initialize the typing area
    initTypingArea: function (inputId, textAreaId) {
        try {
            // Reset state
            this.currentWordIndex = 0
            this.totalErrors = 0
            this.totalCharacters = 0
            this.currentPosition = 0

            // Get elements
            const inputElement = document.getElementById(inputId)
            const textArea = document.getElementById(textAreaId)

            if (!inputElement || !textArea) {
                console.error("Could not find input or text area elements")
                return false
            }

            // Store text content
            this.textContent = textArea.textContent.trim()
            this.totalCharacters = this.textContent.length

            // Get all word elements
            const wordElements = textArea.querySelectorAll(".word")
            this.words = Array.from(wordElements)

            // Highlight first word
            if (this.words.length > 0) {
                this.words[0].classList.add("current-word")
            }

            // Focus input
            inputElement.focus()

            // Set up progress bar
            const progressBar = document.getElementById("typing-progress-bar")
            if (progressBar) {
                progressBar.style.width = "0%"
                progressBar.setAttribute("aria-valuenow", "0")
            }

            // Mark as initialized
            this.isInitialized = true

            return true
        } catch (error) {
            console.error("Error initializing typing area:", error)
            return false
        }
    },

    // Update highlighting based on user input
    updateHighlighting: function (userInput) {
        try {
            if (!this.isInitialized) {
                console.warn("Typing area not initialized")
                return false
            }

            // Get current word element
            const currentWordElement = this.words[this.currentWordIndex]
            if (!currentWordElement) return false

            // Get expected word text
            const expectedWord = currentWordElement.getAttribute("data-word")

            // Check if input matches the current word
            const isCorrect = userInput === expectedWord
            const isPartiallyCorrect = expectedWord.startsWith(userInput)

            // Update word highlighting
            if (isCorrect) {
                // Word is correct - mark as completed
                currentWordElement.classList.remove("current-word")
                currentWordElement.classList.add("completed-word")

                // Move to next word
                this.currentWordIndex++
                if (this.currentWordIndex < this.words.length) {
                    this.words[this.currentWordIndex].classList.add("current-word")
                }

                // Update progress
                this.updateProgress()

                // Clear input for next word
                document.getElementById("typing-input").value = ""

                return true
            } else if (isPartiallyCorrect) {
                // Word is partially correct - highlight characters
                this.highlightCharacters(currentWordElement, userInput)
                return true
            } else {
                // Word has errors - highlight errors and increment error count
                const errorCount = this.highlightErrors(currentWordElement, userInput)
                this.totalErrors += errorCount

                // Update error display
                const errorsDisplay = document.getElementById("errors-display")
                if (errorsDisplay) {
                    errorsDisplay.textContent = this.totalErrors.toString()
                }

                // Calculate and update accuracy
                this.updateAccuracy()

                return false
            }
        } catch (error) {
            console.error("Error updating highlighting:", error)
            return false
        }
    },

    // Highlight characters in the current word
    highlightCharacters: (wordElement, userInput) => {
        try {
            const characters = wordElement.querySelectorAll(".character")

            // Reset all character classes
            characters.forEach((char) => {
                char.classList.remove("correct-char", "error-char", "current-char")
            })

            // Highlight characters based on user input
            for (let i = 0; i < userInput.length && i < characters.length; i++) {
                if (userInput[i] === characters[i].textContent) {
                    characters[i].classList.add("correct-char")
                } else {
                    characters[i].classList.add("error-char")
                }
            }

            // Highlight current character position
            if (userInput.length < characters.length) {
                characters[userInput.length].classList.add("current-char")
            }

            return true
        } catch (error) {
            console.error("Error highlighting characters:", error)
            return false
        }
    },

    // Highlight errors in the current word
    highlightErrors: (wordElement, userInput) => {
        try {
            const characters = wordElement.querySelectorAll(".character")
            let errorCount = 0

            // Reset all character classes
            characters.forEach((char) => {
                char.classList.remove("correct-char", "error-char", "current-char")
            })

            // Highlight characters based on user input
            for (let i = 0; i < userInput.length && i < characters.length; i++) {
                if (userInput[i] === characters[i].textContent) {
                    characters[i].classList.add("correct-char")
                } else {
                    characters[i].classList.add("error-char")
                    errorCount++
                }
            }

            // Highlight current character position
            if (userInput.length < characters.length) {
                characters[userInput.length].classList.add("current-char")
            }

            return errorCount
        } catch (error) {
            console.error("Error highlighting errors:", error)
            return 0
        }
    },

    // Update progress bar
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

    // Update accuracy calculation
    updateAccuracy: function () {
        try {
            const accuracyDisplay = document.getElementById("accuracy-display")
            if (!accuracyDisplay) return false

            // Calculate typed characters (current position in text)
            const typedCharacters = this.calculateTypedCharacters()

            // Calculate accuracy as percentage of correct characters
            const correctCharacters = Math.max(0, typedCharacters - this.totalErrors)
            const accuracy = typedCharacters > 0 ? (correctCharacters / typedCharacters) * 100 : 100

            // Update display with one decimal place
            accuracyDisplay.textContent = accuracy.toFixed(1) + "%"

            return true
        } catch (error) {
            console.error("Error updating accuracy:", error)
            return false
        }
    },

    // Calculate total typed characters
    calculateTypedCharacters: function () {
        try {
            let count = 0

            // Count characters in completed words
            for (let i = 0; i < this.currentWordIndex; i++) {
                count += this.words[i].getAttribute("data-word").length
            }

            // Add characters in current word
            const inputElement = document.getElementById("typing-input")
            if (inputElement && inputElement.value) {
                count += inputElement.value.length
            }

            // Add spaces between words (except after the last word)
            count += Math.max(0, this.currentWordIndex)

            return count
        } catch (error) {
            console.error("Error calculating typed characters:", error)
            return 0
        }
    },

    // Calculate WPM (words per minute)
    calculateWPM: function (elapsedTimeInSeconds) {
        try {
            // Calculate typed characters
            const typedCharacters = this.calculateTypedCharacters()

            // Standard word is 5 characters
            const standardWords = typedCharacters / 5

            // Calculate minutes elapsed
            const minutes = elapsedTimeInSeconds / 60

            // Calculate WPM
            const wpm = minutes > 0 ? standardWords / minutes : 0

            return wpm
        } catch (error) {
            console.error("Error calculating WPM:", error)
            return 0
        }
    },

    // Show completion animation
    showCompletionAnimation: () => {
        try {
            // Add animation class to completion modal
            const modal = document.querySelector(".completion-modal")
            if (modal) {
                modal.classList.add("animate")
            }

            return true
        } catch (error) {
            console.error("Error showing completion animation:", error)
            return false
        }
    },

    // Get current statistics
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

    // Calculate accuracy
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
}
