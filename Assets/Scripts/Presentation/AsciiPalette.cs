using System;
using UnityEngine;

namespace Vibora.Presentation
{
    /// <summary>Qué puede haber en una celda, desde el punto de vista de quien dibuja.</summary>
    public enum CellKind : byte
    {
        Empty = 0,
        Wall = 1,
        SnakeBody = 2,
        SnakeHead = 3,
        Food = 4,

        /// <summary>La víbora después de perder. Es el único feedback visual del game over.</summary>
        SnakeDead = 5,

        /// <summary>El texto del score escrito sobre el marco.</summary>
        Hud = 6
    }

    /// <summary>Un carácter y su color.</summary>
    [Serializable]
    public struct GlyphStyle
    {
        // 📖 Unity NO serializa char. Por eso esto es string y se usa el primer carácter:
        //    es una limitación real del serializador, no una elección de diseño.
        [SerializeField] private string _glyph;
        [SerializeField] private Color _color;

        public GlyphStyle(string glyph, Color color)
        {
            _glyph = glyph;
            _color = color;
        }

        public char Glyph => string.IsNullOrEmpty(_glyph) ? ' ' : _glyph[0];

        public Color Color => _color;
    }

    /// <summary>
    /// La paleta del juego: qué carácter y qué color le toca a cada cosa.
    /// </summary>
    /// <remarks>
    /// ScriptableObject y no constantes en el código: podés cambiar la estética entera
    /// desde el Inspector, en pleno Play, sin recompilar ni reiniciar la partida.
    /// </remarks>
    [CreateAssetMenu(fileName = "AsciiPalette", menuName = "Vibora/Paleta ASCII")]
    public sealed class AsciiPalette : ScriptableObject
    {
        [SerializeField] private GlyphStyle _empty = new GlyphStyle(".", new Color(0.16f, 0.18f, 0.22f));
        [SerializeField] private GlyphStyle _wall = new GlyphStyle("#", new Color(0.35f, 0.38f, 0.45f));
        [SerializeField] private GlyphStyle _snakeBody = new GlyphStyle("o", new Color(0.18f, 0.80f, 0.44f));
        [SerializeField] private GlyphStyle _snakeHead = new GlyphStyle("@", new Color(0.60f, 1.00f, 0.60f));
        [SerializeField] private GlyphStyle _food = new GlyphStyle("*", new Color(1.00f, 0.25f, 0.21f));
        [SerializeField] private GlyphStyle _snakeDead = new GlyphStyle("x", new Color(0.55f, 0.14f, 0.14f));
        [SerializeField] private GlyphStyle _hud = new GlyphStyle(" ", new Color(1.00f, 0.90f, 0.40f));

        private const int KindCount = 7;

        // 📖 Los strings de color se calculan UNA vez y se guardan. ColorUtility genera
        //    un string nuevo en cada llamada: hacerlo por celda serían 1000 strings por
        //    tick tirados a la basura. 🔴GC
        private string[]? _hexCache;

        public char GlyphOf(CellKind kind) => StyleOf(kind).Glyph;

        /// <summary>El color como "#RRGGBB", listo para meter en un tag de TMP.</summary>
        public string HexOf(CellKind kind)
        {
            // 📖 Se chequea el LARGO, no solo si es null: si algún día se agrega un
            //    CellKind, un cache viejo del tamaño anterior explotaría al pedir el
            //    último. Pasó exactamente eso al agregar SnakeDead.
            if (_hexCache == null || _hexCache.Length != KindCount)
                RebuildCache();

            return _hexCache![(int)kind];
        }

        private GlyphStyle StyleOf(CellKind kind) => kind switch
        {
            CellKind.Wall => _wall,
            CellKind.SnakeBody => _snakeBody,
            CellKind.SnakeHead => _snakeHead,
            CellKind.Food => _food,
            CellKind.SnakeDead => _snakeDead,
            CellKind.Hud => _hud,
            _ => _empty
        };

        private void OnEnable() => RebuildCache();

        // 📖 Al tocar un color en el Inspector hay que rearmar el cache, o seguirías
        //    viendo el color viejo hasta reiniciar.
        private void OnValidate() => RebuildCache();

        private void RebuildCache()
        {
            // 📖 Mismo motivo: reasignar si el largo no corresponde, no solo si es null.
            if (_hexCache == null || _hexCache.Length != KindCount)
                _hexCache = new string[KindCount];

            for (int i = 0; i < KindCount; i++)
                _hexCache[i] = "#" + ColorUtility.ToHtmlStringRGB(StyleOf((CellKind)i).Color);
        }
    }
}
