using System;
using System.Collections.Generic;
using System.IO;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests
{
    // Pruebas del almacén JSON
    public class JsonDataStoreTests : IDisposable
    {
        private readonly string _tempFolder;

        public JsonDataStoreTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "Crm1StoreTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempFolder))
                Directory.Delete(_tempFolder, true);
        }

        [Fact]
        public void Load_MissingFile_ReturnsEmptyList()
        {
            var store = new JsonDataStore(_tempFolder);

            List<Client> clients = store.Load<Client>("clients");

            Assert.Empty(clients);
        }

        [Fact]
        public void SaveAndLoad_RoundTripsClients()
        {
            var store = new JsonDataStore(_tempFolder);
            var clients = new List<Client>
            {
                new Client { Name = "Clientes SA", TaxId = "B12345678", City = "Valencia", Province = "Valencia", Status = ClientStatus.Inactive },
                new Client { Name = "Comercial SL", TaxId = "B87654321", City = "Madrid", Province = "Madrid" }
            };

            store.Save("clients", clients);
            List<Client> loaded = store.Load<Client>("clients");

            Assert.Equal(2, loaded.Count);
            Assert.Equal("Clientes SA", loaded[0].Name);
            Assert.Equal("B12345678", loaded[0].TaxId);
            Assert.Equal(ClientStatus.Inactive, loaded[0].Status);
            Assert.Equal("Comercial SL", loaded[1].Name);
        }

        [Fact]
        public void Load_CorruptFile_Throws()
        {
            string path = Path.Combine(_tempFolder, "clients.json");
            File.WriteAllText(path, "{ contenido no válido");

            var store = new JsonDataStore(_tempFolder);

            // Un archivo dañado nunca debe devolverse como una carga correcta
            Assert.ThrowsAny<Exception>(() => store.Load<Client>("clients"));
        }
    }
}
