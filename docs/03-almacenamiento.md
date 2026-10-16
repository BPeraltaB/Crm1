# Capa de Almacenamiento - CRM1

## Interfaz IDataStore

```csharp
public interface IDataStore
{
    void Guardar<T>(string clave, List<T> datos);
    List<T> Cargar<T>(string clave);
    void GuardarUno<T>(string clave, T dato);
    T CargarUno<T>(string clave);
}
```

## Implementaciones

### JsonDataStore
- Usa `System.Text.Json`
- Archivos con extensión `.json`
- Formateado con `JsonSerializerOptions { WriteIndented = true }`

### TxtDataStore
- Serializa a líneas de texto plano
- Usa `StringBuilder`
- Un registro por línea, campos separados por `|`

### BinaryDataStore
- Usa `BinaryWriter` / `BinaryReader`
- Formato compacto y rápido
- Archivos con extensión `.dat`

### DataStoreFactory
```csharp
public static IDataStore Create(FormatoDato formato)
{
    return formato switch
    {
        FormatoDato.Json => new JsonDataStore(),
        FormatoDato.Txt => new TxtDataStore(),
        FormatoDato.Binario => new BinaryDataStore(),
        _ => new JsonDataStore()
    };
}
```

### Enum FormatoDato
```csharp
public enum FormatoDato
{
    Json,
    Txt,
    Binario
}
```

## Ubicación de archivos
- Todos los datos se guardan en la carpeta `Data/` junto al ejecutable
- Cada entidad tiene su propio archivo
- Se crea automáticamente la carpeta si no existe
