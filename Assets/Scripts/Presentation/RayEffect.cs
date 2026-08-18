using System;
using Vibora.Core;

namespace Vibora.Presentation
{
    /// <summary>
    /// Un destello que sale de una celda y viaja hacia afuera por su fila y su columna,
    /// como cuatro puntas que se alejan del origen y se apagan.
    /// </summary>
    /// <remarks>
    /// C# puro a propósito: recibe el tiempo por parámetro en vez de leer
    /// <c>Time.deltaTime</c>. Así la duración del efecto no depende de la velocidad del
    /// juego —que acelera con el score— y la clase se puede testear sin abrir Unity.
    /// </remarks>
    public sealed class RayEffect
    {
        private int _distanceMax;

        /// <summary>A qué velocidad se aleja el frente, en celdas por segundo.</summary>
        // 📖 En celdas y no en píxeles: como cada celda es cuadrada (2 caracteres de
        //    ancho, decisión de Fase 0), el rayo se ve igual de rápido en horizontal
        //    que en vertical sin tener que compensar nada.
        private const float Speed = 30f;

        /// <summary>Cuántas celdas de ancho tiene el frente que se pinta.</summary>
        // 📖 No es decoración: a 30 celdas/s y 60 fps el frente avanza media celda por
        //    frame. Con un frente de una sola celda habría frames en los que no cae
        //    justo sobre ninguna y el rayo se vería entrecortado.
        private const int Thickness = 2;

        private float _timer;
        private GridPos _posRay;

        public bool IsActive { get; private set; }

        /// <summary>A qué distancia del origen llegó el frente en este instante.</summary>
        private float Front => _timer * Speed;

        /// <summary>Arranca un destello nuevo en <paramref name="origin"/>, pisando el anterior.</summary>
        public void Trigger(GridPos origin, int distance)
        {
            _timer = 0f;
            _posRay = origin;
            IsActive = true;
            _distanceMax = distance;
        }

        /// <summary>Le pasa el tiempo al destello. Se apaga solo al llegar al final del tablero </summary>
        public void Advance(float deltaTime)
        {
            // 📖 La guarda vive acá y no en el que llama: "pasó tiempo" es una frase
            //    válida siempre, aunque no haya nada que hacer con ella.
            if (!IsActive)
                return;

            _timer += deltaTime;

            if (Front-Thickness > _distanceMax)
                IsActive = false;
        }

        /// <summary>¿Esta celda es parte del frente del destello ahora mismo?</summary>
        /// <remarks>
        /// 🟡PERF Se llama para cada celda vacía del tablero, hasta 60 veces por segundo.
        /// Tiene que quedar en aritmética sobre structs: nada que aloque acá adentro.
        /// </remarks>
        public bool Covers(GridPos pos)
        {
            if (!IsActive)
                return false;

            // 📖 El rayo solo viaja por la fila y la columna del origen. Fuera de esa
            //    cruz no hay nada que calcular.
            if (pos.X != _posRay.X && pos.Y != _posRay.Y)
                return false;

            // 📖 Como la celda ya está sobre la cruz, una de las dos diferencias es cero:
            //    la suma de los dos valores absolutos es la distancia sobre el brazo.
            int distance = Math.Abs(pos.X - _posRay.X) + Math.Abs(pos.Y - _posRay.Y);

            float front = Front;

            // 📖 Las dos condiciones juntas son las que hacen que esto sea una ONDA que
            //    viaja y no un segmento que crece: la celda tiene que estar detrás del
            //    frente (ya le pasó por encima) pero todavía adentro del grosor.
            //    Sin la segunda, todo el recorrido quedaría encendido para siempre.
            return distance <= front && distance > front - Thickness;
        }
    }
}
