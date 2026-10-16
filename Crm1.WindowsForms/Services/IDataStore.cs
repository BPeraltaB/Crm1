using System.Collections.Generic;

namespace Crm1.WindowsForms.Services
{
    // Abstracción del almacenamiento de colecciones por clave
    public interface IDataStore
    {
        // Guarda la colección completa bajo una clave
        void Save<T>(string key, List<T> items);

        // Carga la colección guardada bajo una clave; si el archivo no existe devuelve lista vacía
        List<T> Load<T>(string key);
    }
}
