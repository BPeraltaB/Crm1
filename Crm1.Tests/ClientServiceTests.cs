using System;
using System.IO;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests
{
    // Pruebas de las reglas de negocio de clientes
    public class ClientServiceTests : IDisposable
    {
        private readonly string _tempFolder;
        private readonly ClientService _service;

        public ClientServiceTests()
        {
            _tempFolder = Path.Combine(Path.GetTempPath(), "Crm1ServiceTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempFolder);
            _service = CreateService();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempFolder))
                Directory.Delete(_tempFolder, true);
        }

        // Crea un servicio sobre la misma carpeta de datos para comprobar la persistencia
        private ClientService CreateService() => new ClientService(new JsonDataStore(_tempFolder));

        // Cliente válido de referencia para las pruebas
        private static Client MakeValidClient() => new Client
        {
            Name = "Cliente de prueba",
            TaxId = "B12345678",
            Email = "correo@ejemplo.com"
        };

        [Fact]
        public void Add_ValidClient_Succeeds()
        {
            ValidationResult result = _service.Add(MakeValidClient());

            Assert.True(result.IsValid);
            Assert.Single(_service.GetAll());
        }

        [Fact]
        public void Add_EmptyName_ReturnsError()
        {
            var client = MakeValidClient();
            client.Name = "   ";

            ValidationResult result = _service.Add(client);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("nombre"));
        }

        [Fact]
        public void Add_InvalidEmail_ReturnsError()
        {
            var client = MakeValidClient();
            client.Email = "correo-no-valido";

            ValidationResult result = _service.Add(client);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("correo"));
        }

        [Fact]
        public void Add_DuplicateTaxId_ReturnsError()
        {
            _service.Add(MakeValidClient());

            var duplicate = new Client { Name = "Otro cliente", TaxId = "b12345678" };

            ValidationResult result = _service.Add(duplicate);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("CIF/NIF"));
        }

        [Fact]
        public void Add_PersistsAcrossServiceInstances()
        {
            _service.Add(MakeValidClient());

            List<Client> reloaded = CreateService().GetAll();

            Assert.Single(reloaded);
            Assert.Equal("Cliente de prueba", reloaded[0].Name);
        }

        [Fact]
        public void Search_ByStatus_FiltersCorrectly()
        {
            Client active = MakeValidClient();
            Client inactive = new Client { Name = "Inactivo SL", Status = ClientStatus.Inactive };
            _service.Add(active);
            _service.Add(inactive);

            List<Client> result = _service.Search(string.Empty, ClientStatus.Inactive);

            Assert.Single(result);
            Assert.Equal("Inactivo SL", result[0].Name);
        }

        [Fact]
        public void Search_ByText_FindsByName()
        {
            _service.Add(MakeValidClient());

            List<Client> result = _service.Search("prueba");

            Assert.Single(result);
        }

        [Fact]
        public void Update_MissingClient_ReturnsError()
        {
            Client client = MakeValidClient();

            ValidationResult result = _service.Update(client);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("ya no existe"));
        }

        [Fact]
        public void Delete_ExistingClient_RemovesIt()
        {
            Client client = MakeValidClient();
            _service.Add(client);

            bool deleted = _service.Delete(client.Id);

            Assert.True(deleted);
            Assert.Empty(_service.GetAll());
        }

        [Fact]
        public void Delete_MissingClient_ReturnsFalse()
        {
            Assert.False(_service.Delete(Guid.NewGuid()));
        }
    }
}
