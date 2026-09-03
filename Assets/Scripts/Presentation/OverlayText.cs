using Vibora.Core;

namespace Vibora.Presentation
{
    public static class OverlayText
    {
        private static readonly string[] MenuLines = new string[]
        {
            "V I B O R A S C I I",
            "",
            "ENTER PARA JUGAR"
        };

        private static readonly string[] PausedLines = new string[]
        {
            "P A U S A",
            "",
            "ENTER PARA SEGUIR"
        };


        /// <summary>
        /// Qué línea de cartel va en la fila <paramref name="y"/>, o <c>null</c> si esa fila
        /// se dibuja normal.
        /// </summary>
        public static string? LineFor(int y, GameState state, int gridHeight)
        {
            // 📖 Elegir el array primero deja una sola copia de la aritmética de centrado.
            //    Con un case por estado, agregar un cartel nuevo duplicaría las tres líneas.
            string[]? lines = state switch
            {
                GameState.MainMenu => MenuLines,
                GameState.Paused => PausedLines,
                _ => null
            };

            if (lines == null)
                return null;

            // 📖 Primera fila del bloque, para que quede centrado vertical.
            int first = (gridHeight - lines.Length) / 2;
            int index = y - first;

            // 📖 null = "esta fila no lleva cartel". Distinto de "", que es una línea
            //    de cartel en blanco — la del medio de los arrays de arriba.
            return index >= 0 && index < lines.Length ? lines[index] : null;
        }
    }
}
