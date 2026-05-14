using UnityEngine;

namespace KejarSetoran.Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource source;
        private AudioClip pickupClip;
        private AudioClip dropoffClip;
        private AudioClip timeoutClip;
        private AudioClip gameoverClip;
        private AudioClip winClip;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.volume = 0.45f;

            pickupClip = MakeBeep(880f, 0.10f);
            dropoffClip = MakeChord(new[] { 660f, 880f, 1320f }, 0.22f);
            timeoutClip = MakeBeep(220f, 0.30f);
            gameoverClip = MakeChord(new[] { 196f, 165f, 131f }, 0.6f);
            winClip = MakeChord(new[] { 523f, 659f, 784f, 1047f }, 0.5f);
        }

        public void PlayPickup() => source.PlayOneShot(pickupClip);
        public void PlayDropoff() => source.PlayOneShot(dropoffClip);
        public void PlayTimeout() => source.PlayOneShot(timeoutClip);
        public void PlayGameOver() => source.PlayOneShot(gameoverClip);
        public void PlayWin() => source.PlayOneShot(winClip);

        private AudioClip MakeBeep(float freq, float duration)
        {
            int sampleRate = 44100;
            int len = Mathf.RoundToInt(duration * sampleRate);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Min(1f, (1f - (float)i / len) * 4f);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f * env;
            }
            var clip = AudioClip.Create("beep", len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip MakeChord(float[] freqs, float duration)
        {
            int sampleRate = 44100;
            int len = Mathf.RoundToInt(duration * sampleRate);
            var data = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = (float)i / sampleRate;
                float env = Mathf.Min(1f, (1f - (float)i / len) * 3f);
                float sum = 0f;
                foreach (var f in freqs) sum += Mathf.Sin(2f * Mathf.PI * f * t);
                data[i] = (sum / freqs.Length) * 0.5f * env;
            }
            var clip = AudioClip.Create("chord", len, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
