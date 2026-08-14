using System;
using System.IO;
using UnityEngine;
using Vibora.Core;

namespace Vibora.Infrastructure
{
    /// <summary>
    /// Guarda el récord en un archivo JSON. Es el adaptador del puerto
    /// <see cref="IScoreRepository"/> contra el disco de verdad.
    /// </summary>
    /// <remarks>
    /// 🧩 Todo lo que sabe de archivos, rutas y JSON vive acá adentro. El Core solo ve
    /// "un int que entra y un int que sale": cambiar esto por PlayerPrefs, por la nube o
    /// por un mock no obliga a tocar ni una línea de la lógica del juego.
    /// </remarks>
    public sealed class JsonScoreRepository : IScoreRepository
    {
        // 📖 JsonUtility no serializa un int suelto ni propiedades: necesita una clase
        //    [Serializable] con campos públicos. Por eso este envoltorio de un solo dato.
        [Serializable]
        private sealed class Data
        {
            public int best;
        }

        private readonly string _savePath;

        /// <param name="savePath">Ruta completa del archivo. La decide el composition root, no esta clase.</param>
        public JsonScoreRepository(string savePath)
        {
            // 📖 El constructor SOLO guarda la ruta. No lee y sobre todo no escribe:
            //    construir un repositorio no puede tener el efecto de borrar los datos.
            if (string.IsNullOrWhiteSpace(savePath))
                throw new ArgumentException("La ruta del archivo de récord no puede estar vacía.", nameof(savePath));

            _savePath = savePath;
        }

        public int Load()
        {
            // 📖 Que no exista no es un error: es la primera vez que alguien juega.
            if (!File.Exists(_savePath))
                return 0;

            try
            {
                Data? data = JsonUtility.FromJson<Data>(File.ReadAllText(_savePath));

                // 📖 FromJson devuelve null si el texto es "null" o está vacío. Sin este
                //    chequeo sería un NullReferenceException disfrazado de error de disco.
                if (data == null)
                    return 0;

                // 📖 Cada capa se defiende sola: el archivo lo puede editar cualquiera a
                //    mano, y un récord negativo haría que un score de 0 pareciera récord.
                return Math.Max(0, data.best);
            }
            catch (Exception e)
            {
                // 📖 El repositorio no puede arreglar un disco roto ni un JSON corrupto.
                //    Registra, arranca de cero y deja jugar. Colgar el juego sería peor.
                Debug.LogWarning($"[{nameof(JsonScoreRepository)}] No se pudo leer el récord ({_savePath}): {e.Message}");
                return 0;
            }
        }

        public void Save(int score)
        {
            try
            {
                // 📖 Sin guarda de existencia: WriteAllText crea el archivo si falta y lo
                //    reemplaza si ya está. Escribir es justamente lo que lo hace existir.
                File.WriteAllText(_savePath, JsonUtility.ToJson(new Data { best = score }));
            }
            catch (Exception e)
            {
                // 📖 Esto corre en el frame exacto en que el jugador muere. Un récord que
                //    no se guardó molesta; una excepción en el game over rompe la partida.
                Debug.LogWarning($"[{nameof(JsonScoreRepository)}] No se pudo guardar el récord ({_savePath}): {e.Message}");
            }
        }
    }
}
