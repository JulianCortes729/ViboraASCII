using NUnit.Framework;
using Vibora.Infrastructure;
using System.IO;
using System;

namespace Vibora.Core.Tests
{
    [TestFixture]
    public sealed class JsonScoreRepositoryTests
    {
        private readonly string _path = Path.Combine(
                Path.GetTempPath(), $"vibora-highscore-test-{Guid.NewGuid():N}.json");
        private int _score = 37;

        [SetUp]
        public void SetUp()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }

        [Test]
        public void SiElArchivoNoExiste_DevuelveCero()
        {
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            int score = repo.Load();
            Assert.AreEqual(0, score);
        }


        [Test]
        public void SiSeGuardaUnValor_SeTieneQueDevolverEseMismoValor()
        {
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            repo.Save(_score);
            int bestScore = repo.Load();
            Assert.AreEqual(_score, bestScore);
        }


        [Test]
        public void ElRepositorioNoDecideQueEsRecord()
        {  
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            repo.Save(_score);
            repo.Save(5);
            int bestScore = repo.Load();
            Assert.AreEqual(5, bestScore);
        }

        [Test]
        public void JSONMalFormado_DevuelveCero()
        {  
            // Create a malformed JSON file
            File.WriteAllText(_path, "{ esto no cierra");
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            int score = repo.Load();
            Assert.AreEqual(0, score);
        }

        [Test]
        public void ArchivoVacio_DevulveCero()
        {
            File.WriteAllText(_path, "");
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            int score = repo.Load();
            Assert.AreEqual(0, score);
        }

        [Test]
        public void ArchivoRecordNegativo_DevuelveCero()
        {  
            File.WriteAllText(_path, "{ \"best\": -10 }");
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            int score = repo.Load();
            Assert.AreEqual(0, score);
        }

        [Test]
        public void ConstruirRepositorio_NoCreaArchivo()
        {
            JsonScoreRepository repo = new JsonScoreRepository(_path);
            Assert.IsFalse(File.Exists(_path));
        }

        [Test]
        public void ConstruirRepositorio_ConRutaVacia_LanzaExcepcion()
        {
            Assert.Throws<ArgumentException>(() => new JsonScoreRepository(""));
            Assert.Throws<ArgumentException>(() => new JsonScoreRepository("   "));
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_path))
                File.Delete(_path);
        }
    }
}