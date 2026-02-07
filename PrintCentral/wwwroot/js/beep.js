// El navegador limitará el número de contextos de audio concurrentes
// Así que asegúrate de reutilizarlos siempre que puedas
const myAudioContext = new AudioContext();

/**
 * Función auxiliar para emitir un pitido en el navegador mediante la API de Web Audio.
 * 
 * https://caniuse.com/?search=AudioContext
 * @param {number} duration - La duración del pitido en milisegundos.
 * @param {number} frequency - La frecuencia del pitido.
 * @param {number} volume - El volumen del pitido.
 * 
 * @returns {Promise} - Una promesa que se resuelve cuando termina el pitido.
 * 
 * Implementation
 * create property
 * this.AlertSound = null;
 * 
 * Assign function
 * AppContext.LoadJS('/js/beep.js').then(() => {
     self.AlertSound = beep;
   });
 */
function beep(duration, frequency, volume) {
    return new Promise((resolve, reject) => {
        // Establecer la duración predeterminada si no se proporciona
        duration = duration || 200;
        frequency = frequency || 440;
        volume = volume || 100;

        try {
            let oscillatorNode = myAudioContext.createOscillator();
            let gainNode = myAudioContext.createGain();
            oscillatorNode.connect(gainNode);

            // Establecer la frecuencia del oscilador en hercios
            oscillatorNode.frequency.value = frequency;

            // Establecer el tipo de oscilador
            oscillatorNode.type = "square";
            gainNode.connect(myAudioContext.destination);

            // Establecer la ganancia al volumen
            gainNode.gain.value = volume * 0.01;

            // Inicie el audio con la duración deseada
            oscillatorNode.start(myAudioContext.currentTime);
            oscillatorNode.stop(myAudioContext.currentTime + duration * 0.001);

            // Resuelve la promesa cuando se acabe el sonido.
            oscillatorNode.onended = () => {
                resolve();
            };
        } catch (error) {
            reject(error);
        }
    });
}