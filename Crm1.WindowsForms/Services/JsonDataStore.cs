using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Crm1.WindowsForms.Services
{
    // Almacenamiento de colecciones en archivos JSON dentro de una carpeta de datos
    public class JsonDataStore : IDataStore
    {
        private readonly string _dataFolder;

        // Permite inyectar la carpeta de datos (útil en pruebas); por defecto usa Data junto al ejecutable
        public JsonDataStore(string? dataFolder = null)
        {
            _dataFolder = dataFolder ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(_dataFolder))
                Directory.CreateDirectory(_dataFolder);
        }

        private string GetPath(string key) => Path.Combine(_dataFolder, $"{key}.json");

        public void Save<T>(string key, List<T> items)
        {
            string path = GetPath(key);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(items, options);
            File.WriteAllText(path, json);
        }

        public List<T> Load<T>(string key)
        {
            string path = GetPath(key);
            if (!File.Exists(path))
                return new List<T>();

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
                return new List<T>();

            // Un archivo dañado debe provocar un error visible, nunca un éxito con datos descartados
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
    }
}
