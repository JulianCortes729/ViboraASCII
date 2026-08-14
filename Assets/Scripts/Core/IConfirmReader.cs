namespace Vibora.Core
{
    /// <summary>
    /// "El jugador apretó confirmar". Un solo bit, consumible.
    /// </summary>
    /// <remarks>
    /// Separado de <see cref="IInputReader"/> a propósito: el movimiento es un flujo
    /// continuo de direcciones y esto es un evento puntual. Meterlos en la misma
    /// interfaz obligaría a todo el que solo quiere mover la víbora a implementar
    /// también el confirmar. ⚠️SOLID
    /// </remarks>
    public interface IConfirmReader
    {
        /// <summary>
        /// True una sola vez por pulsación: al leerlo, se consume.
        /// Devuelve false si no hay nada pendiente.
        /// </summary>
        bool ConsumeConfirm();
    }
}
