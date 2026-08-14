using System.Text;
using TMPro;
using UnityEditor.Build.Content;
using UnityEngine;
using Vibora.Core;

namespace Vibora.Presentation
{
    /// <summary>
    /// Dibuja el tablero como un único bloque de texto con tags de color,
    /// con el score escrito sobre el marco de arriba.
    /// </summary>
    /// <remarks>
    /// Solo LEE el estado del juego, por eso recibe <see cref="IBoardView"/> y no el
    /// GameLoop: desde acá es imposible hacer avanzar la partida. ⚠️SOLID
    /// </remarks>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class AsciiRenderer : MonoBehaviour
    {
        // 📖 Medido en Fase 0: un glifo de Cascadia Mono da una celda de relación
        //    alto/ancho 1.98; dos glifos la dejan en 0.99, o sea cuadrada.
        private const int CharsPerCell = 2;

        private const string ScoreLabel = "SCORE ";
        private const string BestScoreLabel = " BEST ";  
        private const string LostLabel = "FIN";
        private const string WonLabel = "GANASTE";

        

        [SerializeField] private AsciiPalette? _palette;

        [Tooltip("Marco alrededor del tablero. Va POR FUERA de la grilla jugable, y arriba lleva el score.")]
        [SerializeField] private bool _drawBorder = true;

        private TMP_Text _label = null!;
        private StringBuilder _buffer = null!;
        private GridModel? _grid;

        // 📖 Estado del armado: qué color está abierto y si ya se escribió alguna fila.
        //    Son campos y no parámetros para no arrastrarlos por seis métodos.
        private string? _openHex;
        private bool _anyRowWritten;

        // 📖 Buffer fijo para pasar un int a dígitos sin crear strings. Un int no pasa
        //    de 10 dígitos. Sin esto, cada tick generaría basura solo por dibujar el score. 🔴GC
        private readonly char[] _digits = new char[10];

        private int TotalColumns => (_grid!.Width + (_drawBorder ? 2 : 0)) * CharsPerCell;

        private int TotalRows => _grid!.Height + (_drawBorder ? 2 : 0);

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            // ⚠️API En TMP de Unity 6 el no-wrap es 'textWrappingMode'. En versiones
            //       viejas era enableWordWrapping = false.
            _label.textWrappingMode = TextWrappingModes.NoWrap;
            _label.richText = true;
            _label.enableAutoSizing = false;
            _label.lineSpacing = 0f;
            _label.characterSpacing = 0f;
            _label.alignment = TextAlignmentOptions.Center;

            if (_palette == null)
                Debug.LogError($"[{nameof(AsciiRenderer)}] Falta la paleta en el Inspector.", this);
        }

        /// <summary>
        /// Ata el renderer a un tablero y calcula el tamaño de fuente que lo hace entrar.
        /// Lo llama el bootstrap antes del primer Render.
        /// </summary>
        public void Initialize(GridModel grid)
        {
            _grid = grid;

            int estimated = (TotalColumns + 24) * TotalRows + 256;
            _buffer = new StringBuilder(estimated);

            FitFontSize();
        }

        /// <summary>Vuelca el estado actual del tablero a la pantalla.</summary>
        public void Render(IBoardView board, int highScore)
        {
            if (_grid == null)
            {
                Debug.LogError($"[{nameof(AsciiRenderer)}] Render sin Initialize.", this);
                return;
            }

            if (_palette == null)
                return;

            BuildFrame(board, highScore);

            // ⚠️API Confirmá el overload SetText(StringBuilder) en tu TMP.
            _label.SetText(_buffer);
        }

        // ---------------- armado del cuadro ----------------

        private void BuildFrame(IBoardView board, int highScore)
        {
            _buffer.Clear();
            _openHex = null;
            _anyRowWritten = false;

            bool hasFood = board.TryGetFood(out GridPos food);
            int border = _drawBorder ? 1 : 0;

            if (_drawBorder)
                AppendHudRow(board, highScore);

            for (int y = 0; y < _grid!.Height; y++)
            {
                StartRow();

                for (int x = -border; x < _grid.Width + border; x++)
                    AppendCell(KindAt(new GridPos(x, y), board, hasFood, food));
            }

            if (_drawBorder)
                AppendWallRow();

            CloseColor();
        }

        /// <summary>El marco de arriba, con el score centrado encima.</summary>
        private void AppendHudRow(IBoardView board, int highScore)
        {
            StartRow();

            int labelWidth = HudWidth(board, highScore);
            int left = (TotalColumns - labelWidth) / 2;

            // 📖 Si el tablero fuera tan angosto que el texto no entra, se prioriza el
            //    texto y el marco se pierde: mejor un score legible que un marco prolijo.
            if (left < 0)
                left = 0;

            AppendWallRun(0, left);
            AppendHudLabel(board, highScore);
            AppendWallRun(left + labelWidth, TotalColumns);

        }

        private void AppendWallRow()
        {
            StartRow();
            AppendWallRun(0, TotalColumns);
        }

        /// <summary>Escribe columnas de marco entre dos índices absolutos.</summary>
        // 📖 El patrón depende de la columna ABSOLUTA, no de cuántas van escritas: así
        //    el glifo del marco cae siempre en la misma mitad de cada celda y el borde
        //    no se desfasa al lado del texto del score.
        private void AppendWallRun(int fromColumn, int toColumn)
        {
            string hex = _palette!.HexOf(CellKind.Wall);
            char glyph = _palette.GlyphOf(CellKind.Wall);

            for (int column = fromColumn; column < toColumn; column++)
                Write(hex, column % CharsPerCell == 0 ? glyph : ' ');
        }

        /// <summary>Cuántos caracteres ocupa el texto del score. Debe coincidir con lo que escribe <see cref="AppendHudLabel"/>.</summary>
        private int HudWidth(IBoardView board, int highScore)
        {
            // Un espacio a cada lado para que el texto no toque el marco.
            int width = 1 + ScoreLabel.Length + DigitCount(board.Score) + 1 + BestScoreLabel.Length + DigitCount(highScore) + 1;

            if (board.IsOver)
                width += 1 + (board.IsWon ? WonLabel.Length : LostLabel.Length);

            return width;
        }

        private void AppendHudLabel(IBoardView board, int highScore)
        {
            string hex = _palette!.HexOf(CellKind.Hud);

            Write(hex, ' ');
            WriteText(hex, ScoreLabel);
            WriteInt(hex, board.Score);

            Write(hex, ' ');
            WriteText(hex, BestScoreLabel);
            WriteInt(hex, highScore);

            if (board.IsOver)
            {
                Write(hex, ' ');
                WriteText(hex, board.IsWon ? WonLabel : LostLabel);
            }

            Write(hex, ' ');
        }

        private void AppendCell(CellKind kind)
        {
            string hex = _palette!.HexOf(kind);

            Write(hex, _palette.GlyphOf(kind));

            // 📖 El relleno que hace la celda cuadrada: un espacio, del color que sea.
            for (int pad = 1; pad < CharsPerCell; pad++)
                Write(hex, ' ');
        }

        private CellKind KindAt(GridPos position, IBoardView board, bool hasFood, GridPos food)
        {
            // 📖 Fuera de la grilla = marco. La pared no ocupa celdas jugables, así que
            //    la víbora no puede caminar sobre ella: choca al intentar salir.
            if (!_grid!.Contains(position))
                return CellKind.Wall;

            if (board.SnakeOccupies(position))
            {
                // 📖 Toda la víbora cambia de color al perder. No es una decisión de juego:
                //    el "está muerta" ya lo decidió el Core, acá solo se elige cómo se ve.
                if (board.IsOver)
                    return CellKind.SnakeDead;

                return position == board.SnakeHead ? CellKind.SnakeHead : CellKind.SnakeBody;
            }

            if (hasFood && position == food)
                return CellKind.Food;

            return CellKind.Empty;
        }

        // ---------------- escritura con color arrastrado ----------------

        private void StartRow()
        {
            if (_anyRowWritten)
                _buffer.Append('\n');

            _anyRowWritten = true;
        }

        // 📖 El tag de color solo se emite cuando CAMBIA. Uno por celda serían ~23 KB
        //    de string por tick, casi todo repetido. Así una fila de fondo cuesta un tag. 🟡PERF
        private void Write(string hex, char character)
        {
            if (hex != _openHex)
            {
                CloseColor();
                _buffer.Append("<color=").Append(hex).Append('>');
                _openHex = hex;
            }

            _buffer.Append(character);
        }

        private void WriteText(string hex, string text)
        {
            for (int i = 0; i < text.Length; i++)
                Write(hex, text[i]);
        }

        /// <summary>Escribe un entero dígito por dígito, sin crear ni un string.</summary>
        private void WriteInt(string hex, int value)
        {
            if (value < 0)
            {
                Write(hex, '-');
                value = -value;
            }

            if (value == 0)
            {
                Write(hex, '0');
                return;
            }

            int count = 0;

            // 📖 Los dígitos salen al revés (unidades primero), por eso se guardan y
            //    después se escriben de atrás para adelante.
            while (value > 0 && count < _digits.Length)
            {
                _digits[count++] = (char)('0' + (value % 10));
                value /= 10;
            }

            for (int i = count - 1; i >= 0; i--)
                Write(hex, _digits[i]);
        }

        private static int DigitCount(int value)
        {
            if (value < 0)
                return 1 + DigitCount(-value);

            int digits = 1;

            while (value >= 10)
            {
                value /= 10;
                digits++;
            }

            return digits;
        }

        private void CloseColor()
        {
            if (_openHex == null)
                return;

            _buffer.Append("</color>");
            _openHex = null;
        }

        // ---------------- ajuste de tamaño ----------------

        /// <summary>El fontSize más grande con el que el tablero entra completo en el rect.</summary>
        private void FitFontSize()
        {
            TMP_FontAsset? font = _label.font;

            if (font == null)
            {
                Debug.LogError($"[{nameof(AsciiRenderer)}] El TMP no tiene Font Asset.", this);
                return;
            }

            if (!font.characterLookupTable.TryGetValue('#', out TMP_Character sample) || sample.glyph == null)
            {
                Debug.LogError($"[{nameof(AsciiRenderer)}] La fuente no tiene '#'. ¿Generaste el rango 32-126?", this);
                return;
            }

            float pointSize = font.faceInfo.pointSize;
            float charWidthRatio = sample.glyph.metrics.horizontalAdvance / pointSize;
            float lineHeightRatio = font.faceInfo.lineHeight / pointSize;

            Rect rect = _label.rectTransform.rect;
            float byWidth = rect.width / (TotalColumns * charWidthRatio);
            float byHeight = rect.height / (TotalRows * lineHeightRatio);

            _label.fontSize = Mathf.Min(byWidth, byHeight);
        }
    }
}
