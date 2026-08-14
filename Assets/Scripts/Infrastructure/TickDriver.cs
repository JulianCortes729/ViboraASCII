using System;
using UnityEngine;
using Vibora.Core;

namespace Vibora.Infrastructure
{
    /// <summary>
    /// El metrónomo del juego. Traduce el tiempo de Unity en pasos discretos
    /// y avisa a quien esté escuchando.
    /// </summary>
    /// <remarks>
    /// Es una cáscara fina a propósito: toda la cuenta vive en <see cref="TickAccumulator"/>,
    /// que es C# puro y testeable. Acá solo queda lo que obliga a existir Unity.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TickDriver : MonoBehaviour
    {
        [Header("Ritmo")]
        [Tooltip("Pasos de la víbora por segundo. 8 es un arranque cómodo.")]
        [SerializeField, Range(1f, 30f)] private float _ticksPerSecond = 8f;

        [Tooltip("Tope de pasos que se ejecutan en un solo frame. Protege contra la espiral de la muerte.")]
        [SerializeField, Range(1, 10)] private int _maxStepsPerFrame = 4;

        /// <summary>Se dispara una vez por paso de juego.</summary>
        // 📖 Event y no UnityEvent: el suscriptor es código (el game loop), no el Inspector.
        //    Invocarlo no aloca. 🔴GC
        public event Action? Ticked;

        // 📖 null! = "yo garantizo que en Awake queda asignado". Es la forma de callar
        //    al compilador de nullable sin volver el campo nullable de verdad.
        private TickAccumulator _accumulator = null!;

        /// <summary>Cambiar esto en runtime acelera el juego. Lo va a usar la dificultad progresiva.</summary>
        public float TicksPerSecond
        {
            get => _ticksPerSecond;
            set
            {
                _ticksPerSecond = Mathf.Max(0.01f, value);
                _accumulator?.SetTicksPerSecond(_ticksPerSecond);
            }
        }

        public bool IsRunning { get; private set; } = true;

        // 📖 Awake: referencias y construcción. Ver el estándar de ciclo de vida.
        private void Awake()
        {
            _accumulator = new TickAccumulator(_ticksPerSecond, _maxStepsPerFrame);
        }

        private void Update()
        {
            if (!IsRunning)
                return;

            int steps = _accumulator.Advance(Time.deltaTime);

            for (int i = 0; i < steps; i++)
            {
                // 📖 Se re-chequea adentro del loop: si un suscriptor llama a Pause()
                //    (game over a mitad de un frame con varios pasos), los pasos que
                //    quedaban no deben ejecutarse.
                if (!IsRunning)
                    return;

                Ticked?.Invoke();
            }
        }

        /// <summary>Frena el metrónomo y descarta el tiempo acumulado.</summary>
        public void Pause()
        {
            IsRunning = false;
            _accumulator.Reset();
        }

        /// <summary>Arranca de nuevo desde cero, sin arrastrar el tiempo de la pausa.</summary>
        public void Resume()
        {
            _accumulator.Reset();
            IsRunning = true;
        }

        // 📖 Para poder tocar la velocidad desde el Inspector en pleno Play y ver el efecto.
        private void OnValidate()
        {
            if (Application.isPlaying && _accumulator != null)
                _accumulator.SetTicksPerSecond(Mathf.Max(0.01f, _ticksPerSecond));
        }
    }
}
