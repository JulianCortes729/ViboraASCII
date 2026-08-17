using Vibora.Core;

namespace Vibora.Presentation
{
    /// <summary>
    /// Representa un efecto visual de rayo que se activa en una posición específica y dura un tiempo determinado.
    /// </summary>
    /// <remarks>
    /// El efecto se activa en la posición especificada y permanece visible durante un tiempo limitado.
    /// </remarks>
    public sealed class RayEffect
    {
        private float _timer;
        private GridPos _posRay;

        private const float Duration = 0.4f;
        public bool IsActive { get; private set; } 
        public void Trigger(GridPos origin)
        {
            _timer = 0f;
            _posRay = origin;
            IsActive = true;
            
        }

        public void Advance(float deltaTime)
        {
            if(!IsActive)
                return;

            _timer += deltaTime;

            if( _timer > Duration )
            {
                IsActive = false;
                _timer = 0f;
            }
        }

        public bool Covers(GridPos pos)
        {
            if (!IsActive) return false;

            return _posRay.Y == pos.Y || _posRay.X == pos.X;
        }

    }
}
