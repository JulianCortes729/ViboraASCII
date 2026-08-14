using UnityEngine;

namespace Vibora.Presentation
{
    [RequireComponent(typeof(AudioSource))]
    [DisallowMultipleComponent]
    public sealed class SfxPlayer : MonoBehaviour
    {
        private AudioSource _audioSource = null!;
        private const int SampleRate = 44100;

        [Header("Frequency")]
        [Range(50f, 2000f)]
        [SerializeField] private float _frequencyClipEat = 880f;
        [Range(50f, 2000f)]
        [SerializeField] private float _frequencyClipRecord = 1320f;
        [Range(50f, 2000f)]
        [SerializeField] private float _frequencyClipDead = 160f;

        [Header("Duration")]
        [Range(0f, 1f)]
        [SerializeField] private float _durationClipEat = 0.06f;
        [Range(0f, 1f)]
        [SerializeField] private float _durationClipRecord = 0.25f;
        [Range(0f, 1f)]
        [SerializeField] private float _durationClipDead = 0.35f;

        private AudioClip _clipEat = null!;
        private AudioClip _clipRecord = null!;
        private AudioClip _clipDead = null!;

        [Header("Volume")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 1f;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            
            _clipEat = CrearTono("Eat", _frequencyClipEat, _durationClipEat);
            _clipRecord = CrearTono("Record", _frequencyClipRecord, _durationClipRecord);
            _clipDead = CrearTono("Dead", _frequencyClipDead, _durationClipDead);
        }

        [ContextMenu("Play Eat")]
        public void PlayEat()
        {
            PlayClip(_clipEat);
        }

        [ContextMenu("Play Record")]
        public void PlayRecord() {
            PlayClip(_clipRecord);
        }

        [ContextMenu("Play Dead")]
        public void PlayDead() {
            PlayClip(_clipDead);
        }

        private void PlayClip(AudioClip clip)
        {
            
            _audioSource.PlayOneShot(clip, _volume);

        }

        /// <summary>Fabrica un clip con una onda cuadrada de la nota y la duración pedidas.</summary>
        private AudioClip CrearTono(string nombre, float notaHz, float segundos)
        {
            // 📖 Cuántas "fotos" necesitamos. El Max con 1 evita que una duración muy chica
            //    redondee a 0 samples: AudioClip.Create con 0 falla.
            int totalSamples = Mathf.Max(1, (int)(SampleRate * segundos));

            AudioClip clip = AudioClip.Create(nombre, totalSamples, 1, SampleRate, false);

            float[] datos = new float[totalSamples];

            // 📖 Los últimos 10 ms se reservan para apagar el sonido de a poco.
            int samplesDeFade = Mathf.Min(totalSamples, (int)(SampleRate * 0.01f));

            for (int i = 0; i < totalSamples; i++)
            {
                // 📖 En qué punto del ciclo estamos, en radianes. Una vuelta entera son 2π.
                float fase = 2f * Mathf.PI * notaHz * i / SampleRate;

                // 📖 El seno oscila SUAVE entre -1 y 1; quedarse con el signo lo convierte
                //    en un salto seco entre -1 y +1. Eso es la onda cuadrada.
                float onda = Mathf.Sign(Mathf.Sin(fase));

                // 📖 Cuántos samples faltan para el final, llevado a un número entre 0 y 1.
                //    Vale 1 durante casi todo el clip y cae a 0 en los últimos milisegundos.
                //    Sin esto el parlante pasa de +1 a 0 de golpe y se oye un CLICK.
                float fade = Mathf.Min(1f, (totalSamples - i) / (float)samplesDeFade);

                datos[i] = onda * fade;
            }

            // 📖 Recién acá el sonido entra al clip. Sin esta línea, silencio.
            clip.SetData(datos, 0);

            return clip;
        }
    }
}
