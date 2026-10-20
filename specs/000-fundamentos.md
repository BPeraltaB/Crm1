# Especificación 000: Fundamentos y módulo Clientes

**Estado:** Activa como diseño, **pendiente de implementar** (el esqueleto del proyecto está vacío; nada de lo descrito aquí existe aún en código). Sustituye a la documentación previa de `docs/` como referencia de implementación; los documentos de `docs/` quedan como diseño general con módulos pendientes.

## 1. Alcance

Primera versión funcional de CRM1 centrada en el módulo de Clientes: alta, edición, baja, listado, búsqueda y filtro. El resto de módulos del diseño general queda **pendiente** (ver sección 8).

## 2. Solución y proyectos

- `Crm1.sln` con dos proyectos:
  - `Crm1.WindowsForms`: aplicación WinForms (.NET 8) con carpetas `Models/`, `Services/` y `Forms/`.
  - `Crm1.Tests`: proyecto de pruebas xUnit que referencia al principal.
- Compilación y pruebas mediante Visual Studio 2022 o `dotnet build` / `dotnet test`.

## 3. Modelo de datos: Client

| Campo             | Tipo          | Descripción                      |
|-------------------|---------------|----------------------------------|
| Id                | Guid          | Identificador único              |
| Name              | string        | Nombre / razón social (obligatorio) |
| TaxId             | string        | CIF/NIF (único si se informa)    |
| Address           | string        | Dirección                        |
| PostalCode        | string        | Código postal                    |
| City              | string        | Ciudad                           |
| Province          | string        | Provincia                        |
| Phone             | string        | Teléfono                         |
| Email             | string        | Correo electrónico (formato válido si se informa) |
| RegistrationDate  | DateTime      | Fecha de alta                    |
| Notes             | string        | Observaciones                    |
| Status            | ClientStatus  | Active / Inactive                |

## 4. Reglas de negocio (ClientService)

- El nombre es obligatorio.
- El correo electrónico, si se informa, debe tener un formato válido.
- El TaxId, si se informa, no puede estar repetido (comparación sin mayúsculas/minúsculas ni espacios).
- Las reglas viven fuera de los formularios (constitución, punto 3).

## 5. Almacenamiento (JSON)

- Interfaz `IDataStore` con implementación `JsonDataStore`.
- Los clientes se guardan en `Data/clients.json` junto al ejecutable (carpeta `Data/` creada automáticamente).
- Un fallo de lectura o escritura debe provocar un error visible, nunca presentarse como éxito ni descartar datos silenciosamente (constitución, punto 5). Un archivo inexistente se interpreta como lista vacía (primera ejecución).
- Formato de archivo: JSON (System.Text.Json). Los formatos TXT y binario quedan pendientes; SQL queda pendiente hasta especificación futura.

## 6. Idioma

- Código (clases, métodos, variables) en inglés.
- Comentarios, interfaz y mensajes al usuario en español (constitución, punto 6).

## 7. Pruebas (xUnit)

- `JsonDataStoreTests`: guardar y recargar colección; archivo inexistente devuelve lista vacía; archivo dañado lanza error.
- `ClientServiceTests`: nombre vacío, email inválido, TaxId duplicado, alta válida persiste entre instancias, filtro por estado y actualización de cliente inexistente.

## 8. Fuera de alcance (pendiente)

Contactos, oportunidades, tareas, recordatorios, presupuestos, pedidos, servicios, ventas, historial por cliente, búsqueda global, formatos TXT y binario, SQL y exportación de datos.
