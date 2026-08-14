using System;
using NUnit.Framework;
using Vibora.Core;

namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class HighScoreTrackerTests
    {
        /// <summary>
        /// Un repositorio de mentira: guarda en memoria y además cuenta cómo lo usaron.
        /// </summary>
        /// <remarks>
        /// 🧩 Los contadores son lo que un repositorio de verdad no puede darte. Que
        /// Submit devuelva false no prueba que NO escribió al disco; solo SaveCount
        /// en cero lo prueba. El fake convierte "no pasó nada" en algo verificable.
        /// </remarks>
        private sealed class FakeScoreRepository : IScoreRepository
        {
            private int _stored;

            public FakeScoreRepository(int stored = 0)
            {
                _stored = stored;
            }

            public int LoadCount { get; private set; }

            public int SaveCount { get; private set; }

            /// <summary>El último valor que le pidieron guardar.</summary>
            public int LastSaved { get; private set; }

            public int Load()
            {
                LoadCount++;
                return _stored;
            }

            public void Save(int score)
            {
                SaveCount++;
                LastSaved = score;
                _stored = score;
            }
        }

        // ---------------- carga inicial ----------------

        [Test]
        public void SinRecordPrevio_BestEsCero()
        {
            var tracker = new HighScoreTracker(new FakeScoreRepository());

            Assert.AreEqual(0, tracker.Best);
        }

        [Test]
        public void ConRecordGuardado_LoCargaAlConstruirse()
        {
            var tracker = new HighScoreTracker(new FakeScoreRepository(40));

            Assert.AreEqual(40, tracker.Best);
        }

        [Test]
        public void RecordNegativoEnDisco_SeIgnora()
        {
            var tracker = new HighScoreTracker(new FakeScoreRepository(-5));

            Assert.AreEqual(0, tracker.Best, "un archivo editado a mano no debe romper la regla");
        }

        [Test]
        public void ElDiscoSeLeeUnaSolaVez()
        {
            var repo = new FakeScoreRepository(10);
            var tracker = new HighScoreTracker(repo);

            Assert.AreEqual(1, repo.LoadCount, "el constructor lee una vez");

            tracker.Submit(50);
            tracker.Submit(3);

            Assert.AreEqual(1, repo.LoadCount, "jugar no vuelve a leer el disco");
        }

        // ---------------- la regla ----------------

        [Test]
        public void ScoreMayor_EsRecordYSeGuarda()
        {
            var repo = new FakeScoreRepository(50);
            var tracker = new HighScoreTracker(repo);

            bool esRecord = tracker.Submit(100);

            Assert.IsTrue(esRecord);
            Assert.AreEqual(100, tracker.Best);
            Assert.AreEqual(1, repo.SaveCount);
            Assert.AreEqual(100, repo.LastSaved);
        }

        [Test]
        public void ScoreMenor_NoEsRecordNiTocaElDisco()
        {
            var repo = new FakeScoreRepository(50);
            var tracker = new HighScoreTracker(repo);

            bool esRecord = tracker.Submit(30);

            Assert.IsFalse(esRecord);
            Assert.AreEqual(50, tracker.Best);
            Assert.AreEqual(0, repo.SaveCount, "una partida perdedora no escribe nada");
        }

        [Test]
        public void ScoreIgual_NoEsRecord()
        {
            var repo = new FakeScoreRepository(50);
            var tracker = new HighScoreTracker(repo);

            bool esRecord = tracker.Submit(50);

            Assert.IsFalse(esRecord, "empatar el récord no es superarlo");
            Assert.AreEqual(0, repo.SaveCount);
        }

        [Test]
        public void ScoreNegativo_NoEsRecord()
        {
            var repo = new FakeScoreRepository();
            var tracker = new HighScoreTracker(repo);

            Assert.IsFalse(tracker.Submit(-3));
            Assert.AreEqual(0, tracker.Best);
            Assert.AreEqual(0, repo.SaveCount);
        }

        // 📖 Este es el test que atrapa el bug de asignar al parámetro en vez de al
        //    campo: con Best congelado en 0, el segundo Submit también daría true.
        [Test]
        public void RecordSeMantieneEntreSubmits()
        {
            var repo = new FakeScoreRepository();
            var tracker = new HighScoreTracker(repo);

            Assert.IsTrue(tracker.Submit(20), "20 supera al récord inicial");
            Assert.IsFalse(tracker.Submit(5), "5 no supera a 20");

            Assert.AreEqual(20, tracker.Best, "un score peor nunca baja el récord");
            Assert.AreEqual(20, repo.LastSaved, "y no pisa el disco con el valor peor");
            Assert.AreEqual(1, repo.SaveCount);
        }

        [Test]
        public void RecordsSucesivos_SeVanActualizando()
        {
            var repo = new FakeScoreRepository();
            var tracker = new HighScoreTracker(repo);

            tracker.Submit(5);
            tracker.Submit(12);
            tracker.Submit(30);

            Assert.AreEqual(30, tracker.Best);
            Assert.AreEqual(3, repo.SaveCount, "cada récord real se persiste");
        }

        // ---------------- construcción ----------------

        [Test]
        public void RepoNulo_Explota()
        {
            // 📖 El null! le dice al compilador "sé lo que hago": con nullable activado,
            //    pasar null a un parámetro no anulable es warning. Acá es a propósito.
            Assert.Throws<ArgumentNullException>(() => new HighScoreTracker(null!));
        }
    }
}
