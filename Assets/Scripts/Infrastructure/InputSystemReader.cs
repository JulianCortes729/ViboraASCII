using UnityEngine;
using UnityEngine.InputSystem;
using Vibora.Core;

namespace Vibora.Infrastructure
{
    /// <summary>
    /// Adaptador: traduce el Input System al puerto <see cref="IInputReader"/> del Core.
    /// </summary>
    /// <remarks>
    /// Se engancha a la acción <c>Player/Move</c> del asset que ya trae el proyecto,
    /// así que funciona con WASD, flechas y stick de gamepad sin configurar nada.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class InputSystemReader : MonoBehaviour, IInputReader, IConfirmReader
    {
        [Header("Acciones")]
        [Tooltip("Arrastrá acá Player/Move de Assets/Settings/InputSystem_Actions.")]
        [SerializeField] private InputActionReference? _moveAction;

        [Tooltip("Arrastrá acá UI/Submit. Es la tecla de reiniciar (Enter o Espacio).")]
        [SerializeField] private InputActionReference? _confirmAction;

        [Header("Ajustes")]
        [Tooltip("Cuántos giros se recuerdan entre paso y paso. 2 permite pre-programar una esquina.")]
        [SerializeField, Range(1, 4)] private int _bufferCapacity = 2;

        [Tooltip("Cuánto hay que mover el stick para que cuente. El teclado siempre da 1.")]
        [SerializeField, Range(0.1f, 0.9f)] private float _deadZone = 0.5f;

        private DirectionBuffer _buffer = null!;
        private InputAction? _action;
        private InputAction? _confirm;

        // 📖 Guardamos el vector anterior para desempatar diagonales (ver TryToDirection).
        private Vector2 _lastRaw;

        // 📖 Un solo bit: "hubo un confirmar sin consumir". No se acumula a propósito —
        //    aporrear Enter tres veces no debe encolar tres reinicios.
        private bool _confirmPending;

        /// <summary>Giros esperando. Útil para mirarlo en el Inspector mientras probás.</summary>
        public int PendingCount => _buffer?.Count ?? 0;

        private void Awake()
        {
            _buffer = new DirectionBuffer(_bufferCapacity);
            _action = _moveAction != null ? _moveAction.action : null;
            _confirm = _confirmAction != null ? _confirmAction.action : null;

            if (_action == null)
                Debug.LogError($"[{nameof(InputSystemReader)}] Falta asignar la acción de movimiento en el Inspector.", this);

            if (_confirm == null)
                Debug.LogWarning($"[{nameof(InputSystemReader)}] Sin acción de confirmar: no se va a poder reiniciar.", this);
        }

        // 📖 OnEnable suscribe, OnDisable desuscribe. Sin el par, el callback sigue vivo
        //    cuando el objeto ya no está y explota o dispara input fantasma. ⚠️SOLID
        private void OnEnable()
        {
            if (_confirm != null)
            {
                _confirm.performed += OnConfirmPerformed;
                _confirm.Enable();
            }

            if (_action == null)
                return;

            _action.performed += OnMovePerformed;
            _action.Enable();
        }

        private void OnDisable()
        {
            if (_confirm != null)
            {
                _confirm.performed -= OnConfirmPerformed;
                _confirm.Disable();
                _confirmPending = false;
            }

            if (_action == null)
                return;

            _action.performed -= OnMovePerformed;

            // ⚠️ El asset de acciones es compartido: si más adelante otro sistema usa
            //    la misma acción, este Disable lo va a apagar también. Cuando aparezca
            //    ese caso, hay que pasar a un InputActionMap propio.
            _action.Disable();

            _buffer.Clear();
            _lastRaw = Vector2.zero;
        }

        public bool TryConsumeDirection(out Direction direction) => _buffer.TryDequeue(out direction);

        public bool ConsumeConfirm()
        {
            if (!_confirmPending)
                return false;

            _confirmPending = false;
            return true;
        }

        public void Clear()
        {
            _buffer.Clear();
            _lastRaw = Vector2.zero;
            _confirmPending = false;
        }

        private void OnConfirmPerformed(InputAction.CallbackContext context) => _confirmPending = true;

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            Vector2 raw = context.ReadValue<Vector2>();

            if (TryToDirection(raw, _lastRaw, _deadZone, out Direction direction))
                _buffer.TryEnqueue(direction);

            _lastRaw = raw;
        }

        /// <summary>
        /// Vector2 -> Direction. <c>public static</c> y sin estado a propósito: es la regla
        /// con más casos borde de todo el input, así que tiene que poder testearse sola.
        /// </summary>
        // 📖 raw.y > 0 es "arriba en la pantalla" y mapea a Direction.Up, que en la grilla
        //    resta en Y. La inversión de ejes se resuelve acá, una sola vez.
        public static bool TryToDirection(Vector2 raw, Vector2 previous, float deadZone, out Direction direction)
        {
            direction = default;

            float ax = Mathf.Abs(raw.x);
            float ay = Mathf.Abs(raw.y);

            bool xActive = ax >= deadZone;
            bool yActive = ay >= deadZone;

            if (!xActive && !yActive)
                return false; // soltó todo

            bool horizontal;

            if (xActive && yActive)
            {
                // 📖 Diagonal. Pasa constantemente: el jugador aprieta Izquierda sin haber
                //    soltado Arriba. Descartarla sería comerse el giro. La regla: gana el eje
                //    que se ACABA de activar, porque es la tecla que el jugador acaba de tocar.
                bool xIsNew = Mathf.Abs(previous.x) < deadZone;
                bool yIsNew = Mathf.Abs(previous.y) < deadZone;

                if (xIsNew == yIsNew)
                    return false; // los dos a la vez, o ninguno nuevo: genuinamente ambiguo

                horizontal = xIsNew;
            }
            else
            {
                horizontal = xActive;
            }

            direction = horizontal
                ? (raw.x > 0f ? Direction.Right : Direction.Left)
                : (raw.y > 0f ? Direction.Up : Direction.Down);

            return true;
        }
    }
}
