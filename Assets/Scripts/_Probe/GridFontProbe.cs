using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// SCRIPT DESECHABLE. Valida tres cosas antes de escribir el juego:
/// 1) la fuente dibuja una grilla alineada, 2) las celdas se ven cuadradas,
/// 3) los tags de color no rompen el alineado.
/// Borrá la carpeta _Probe apenas arranque el Core real.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public sealed class GridFontProbe : MonoBehaviour
{
    // 📖 2 chars por celda: un glifo de Cascadia Mono es ~2x más alto que ancho,
    //    así que poniendo dos al lado del otro la celda queda cuadrada (ratio medido: 0.991).
    private const int CharsPerCell = 2;

    [Header("Grilla en CELDAS (decisión de juego, no de pantalla)")]
    [SerializeField] private int width = 40;
    [SerializeField] private int height = 25;

    private TMP_Text _label;

    // 📖 Un solo StringBuilder para toda la vida del objeto: en el juego real esto
    //    se re-arma cada tick, y crear un string nuevo cada vez le da trabajo al GC.
    private readonly StringBuilder _buffer = new StringBuilder();

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();

        // ⚠️API En TMP moderno (Unity 6) el no-wrap es 'textWrappingMode'.
        //       Si tu versión no lo tiene: destildá "Wrapping" en el Inspector.
        _label.textWrappingMode = TextWrappingModes.NoWrap;

        _label.richText = true;
        _label.enableAutoSizing = false; // 📖 el auto-size de TMP pelearía con nuestro cálculo
        _label.lineSpacing = 0f;         // 📖 con 2 chars/celda ya no hace falta apretar filas
        _label.characterSpacing = 0f;
        _label.alignment = TextAlignmentOptions.Center;
    }

    private void Start()
    {
        // 📖 En Start y no en Awake: el RectTransform recién tiene su tamaño real
        //    después de que el Canvas hizo su primer layout.
        FitFontSizeToRect();

        BuildProbeGrid();

        // ⚠️API Confirmá que existe el overload SetText(StringBuilder).
        //       Si no: _label.text = _buffer.ToString();
        _label.SetText(_buffer);

        Debug.Log($"[GridFontProbe] {width}x{height} celdas · fontSize={_label.fontSize:F1} · {_buffer.Length} chars en el buffer.");
    }

    /// <summary>
    /// Calcula el fontSize más grande con el que la grilla entra completa en el rect.
    /// Cero números mágicos: todo sale de las métricas del font asset.
    /// </summary>
    private void FitFontSizeToRect()
    {
        TMP_FontAsset font = _label.font;
        if (font == null)
        {
            Debug.LogError("[GridFontProbe] El TMP no tiene Font Asset asignado.");
            return;
        }

        // ⚠️API characterLookupTable es propiedad de TMP_FontAsset. Confirmá que resuelve.
        if (!font.characterLookupTable.TryGetValue('#', out TMP_Character sample) || sample.glyph == null)
        {
            Debug.LogError("[GridFontProbe] El font asset no contiene '#'. ¿Generaste el rango 32-126?");
            return;
        }

        float pointSize = font.faceInfo.pointSize;

        // 📖 Ratios normalizados: cuánto mide un char por cada punto de fontSize.
        float charWidthRatio = sample.glyph.metrics.horizontalAdvance / pointSize;
        float lineHeightRatio = font.faceInfo.lineHeight / pointSize;

        float cellWidthRatio = charWidthRatio * CharsPerCell;

        Rect rect = _label.rectTransform.rect;
        float sizeByWidth = rect.width / (width * cellWidthRatio);
        float sizeByHeight = rect.height / (height * lineHeightRatio);

        // 📖 El más chico manda: es el eje que se queda sin espacio primero.
        _label.fontSize = Mathf.Min(sizeByWidth, sizeByHeight);
    }

    private void BuildProbeGrid()
    {
        _buffer.Clear();

        int midX = width / 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 📖 ORDEN IMPORTANTE: lo específico va ANTES que lo genérico.
                //    Al revés, la columna '|' del medio se comía la manzana de la celda (20,10)
                //    y el bug parecía "los tags de color no funcionan". No funcionaba el orden.
                if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                {
                    AppendCell('#', null);
                }
                else if (y == 10 && x == 20)
                {
                    AppendCell('*', "#2ECC40"); // manzana verde, justo sobre la columna del medio
                }
                else if (y == 10 && x == 8)
                {
                    AppendCell('@', "#FF4136"); // cabeza roja
                }
                // 📖 Bloque de 5x5 CELDAS: si se ve cuadrado en pantalla, CharsPerCell es correcto.
                else if (y >= 3 && y <= 7 && x >= 4 && x <= 8)
                {
                    AppendCell('O', "#0074D9");
                }
                else if (x == midX)
                {
                    AppendCell('|', null); // columna recta: delata si la fuente no fuera mono
                }
                else
                {
                    AppendCell('.', null);
                }
            }

            if (y < height - 1)
            {
                _buffer.Append('\n');
            }
        }
    }

    /// <summary>Escribe una celda: el glifo + relleno hasta completar CharsPerCell.</summary>
    private void AppendCell(char glyph, string hexColor)
    {
        bool colored = !string.IsNullOrEmpty(hexColor);

        if (colored)
        {
            _buffer.Append("<color=").Append(hexColor).Append('>');
        }

        _buffer.Append(glyph);

        if (colored)
        {
            _buffer.Append("</color>");
        }

        // 📖 El relleno va fuera del tag: no tiene sentido colorear un espacio.
        for (int i = 1; i < CharsPerCell; i++)
        {
            _buffer.Append(' ');
        }
    }
}
