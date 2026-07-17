// Mic energy source for the web preview. Unity WebGL has no Microphone API; this uses the browser's.

window.vrMic = {
  analyser: null,
  buffer: null,

  async start() {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const ctx = new (window.AudioContext || window.webkitAudioContext)();

      // Some browsers create the context suspended until a user gesture. start() is called from a
      // button click, so resume() is allowed here.
      if (ctx.state === "suspended") await ctx.resume();

      const source = ctx.createMediaStreamSource(stream);
      this.analyser = ctx.createAnalyser();
      this.analyser.fftSize = 512;
      source.connect(this.analyser);

      this.buffer = new Float32Array(this.analyser.fftSize);
      return true;
    } catch (e) {
      console.warn("[VirtualRoom] microphone unavailable:", e);
      return false;
    }
  },

  // Returns 0..1. RMS less a 0.005 noise floor, scaled by 12. Matches MicrophoneEnergySource.
  level() {
    if (!this.analyser) return 0;

    this.analyser.getFloatTimeDomainData(this.buffer);

    let sum = 0;
    for (let i = 0; i < this.buffer.length; i++) sum += this.buffer[i] * this.buffer[i];

    const rms = Math.sqrt(sum / this.buffer.length);
    return Math.min(1, Math.max(0, (rms - 0.005) * 12));
  },
};
