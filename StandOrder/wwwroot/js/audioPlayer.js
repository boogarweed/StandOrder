window.audioPlayer = {
    play: (soundFile) => {
        // Create a new Audio object
        // The soundFile path will be relative to the wwwroot folder, e.g., 'sounds/error.mp3'
        var audio = new Audio(soundFile);

        // Set volume to maximum
        audio.volume = 1.0;

        // Play the sound
        audio.play().catch(e => console.error("Error playing sound:", e));
    }
};