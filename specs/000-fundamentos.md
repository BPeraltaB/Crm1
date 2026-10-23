# Especificación 000: Fundamentos y módulo Clientes

**Estado:** Activa como diseño, **pendiente de implementar** (solo existe el formulario principal `MainForm`; el resto descrito aquí no existe aún en código). Sustituye a la documentación previa de `docs/` como referencia de implementación; los documentos de `docs/` quedan como diseño general con módulos pendientes.

## 1. Alcance

Primera versión funcional de CRM1 centrada en el módulo de Clientes: alta, edición, baja, listado, búsqueda y filtro. El resto de módulos del diseño general queda **pendiente** (ver sección 8).

## 2. Solución y proyectos

- `Crm1.sln` con dos proyectos:
  - `Crm1.WindowsForms`: aplicación WinForms (.NET 8) con carpetas `Models/`, `Services/` y `Forms/`.
  - `Crm1.Tests`: proyecto de pruebas xUnit que referencia al principal.
- Compilación y pruebas mediante Visual Studio 2022 o `dotnet build` / `dotnet test`.

## 2.1 Formulario principal

- La aplicación inicia con el formulario principal `MainForm` (clase parcial `Crm1.WindowsForms.Forms.MainForm`), dividido en `Crm1.WindowsForms/Forms/MainForm.cs` y `Forms/MainForm.Designer.cs` para poder editarlo con el diseñador de Visual Studio. La clase no se llama `Form` a propósito: colisiona con `System.Windows.Forms.Form` y el diseñador de VS no la cargaría.
- Diseño: ventana de 1200×750 (mínimo 900×600), título "CRM1", centrada en pantalla.
  - Menú lateral (`MenuStrip` vertical, 220 px, tema oscuro #2D3748 con renderer propio) con: título "CRM1", Clientes, Búsqueda, Futuros clientes y Salir. Salir cierra la aplicación; el resto de items no abre nada todavía.
  - Área central clara (#F3F4F6) vacía, reservada para el contenido de los módulos futuros.
  - Barra de estado inferior blanca con "CRM1 — v0.1" y fecha/hora actualizada cada 30 segundos.
- "Futuros clientes" es un módulo nuevo pendiente de especificación. El resto de módulos del diseño general (contactos, oportunidades, tareas, etc.) se ubicarán en otro formulario en el futuro, no en este menú.

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
