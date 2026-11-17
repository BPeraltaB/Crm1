# Especificación 006: Almacenamiento SQL (SQLite)

**Estado:** Activa. Rige el almacenamiento de los datos de la aplicación en una base de datos SQL local (SQLite). Sustituye a los archivos JSON como almacén activo (specs/000, sección 5); los JSON quedan como copia histórica, legibles solo para la migración. El catálogo de códigos postales sigue siendo un archivo de solo lectura (specs/000, sección 5.1).

## 1. Motor y dependencia

- **SQLite** local mediante el paquete NuGet `Microsoft.Data.Sqlite` (única dependencia nueva; necesidad concreta: almacenamiento durable y transaccional sin servidor, decidió el usuario).
- Archivo de base de datos: `Data/crm1.db` junto al ejecutable (carpeta `Data/` creada automáticamente), conexiones sin agrupación para no bloquear el archivo.
- Decisión de alcance: **todo dato nuevo de módulos futuros se guarda en esta base de datos**; los formatos TXT y binario quedan pendientes y JSON pasa a formato histórico.

## 2. Esquema

- Tablas `Clients` y `Contacts`, espejo de los modelos (specs/000 sección 3 y specs/003 sección 4): `Id` TEXT (GUID, clave primaria), `FirstName`, `LastName1`, `LastName2`, `TaxId`, `Phone`, `Email`, `Address`, `PostalCode`, `City`, `Province`, `Notes` (TEXT), `RegistrationDate` TEXT (ISO `yyyy-MM-dd`; vacío = sin fecha) y `Status` TEXT (nombres del enumerado: Active/Inactive y Pending/Discarded).
- Tabla `Tasks` (specs/005), espejo del modelo de tareas: `Id` TEXT (GUID, clave primaria), `Action`, `Description` (TEXT), `DueDate` TEXT (ISO `yyyy-MM-dd`), `Status` TEXT (Pending/InProgress/Completed), `Priority` TEXT (Low/Medium/High), `RelatedId` TEXT (GUID del registro vinculado) y `RelatedType` TEXT (Client/Prospect). Sin migración desde JSON: las tareas son un dato nuevo, sin histórico. Migración de esquema menor: la columna `Title` original se renombró a `Action` automáticamente (`ALTER TABLE … RENAME COLUMN` si aún existe la antigua), sin pérdida de datos.
- Tabla `Reminders` (specs/007), espejo del modelo de recordatorios: `Id` TEXT (GUID, clave primaria), `Subject` TEXT, `RemindAt` TEXT (ISO `yyyy-MM-dd HH:mm:ss`, con hora), `Status` TEXT (Active/Dismissed), `RelatedId` TEXT (GUID) y `RelatedType` TEXT (Client/Prospect). Sin migración desde JSON: dato nuevo, sin histórico.
- Tablas `Quotes` y `Orders` (specs/009), espejo de los modelos de presupuestos y pedidos: fechas ISO `yyyy-MM-dd`, enums como nombre del valor, `Amount` TEXT en cultura invariable (exacto para decimal) y `OrderId` TEXT nulable en `Quotes` (pedido generado al aceptar). Sin migración: dato nuevo.
- Tabla `SchemaInfo` (clave/valor) para marcas internas, como la de migración.
- Las tablas se crean automáticamente al abrir la base de datos (`CREATE TABLE IF NOT EXISTS`).

## 3. Operaciones

- `Services/SqlDataStore` implementa `IDataStore` (mismos métodos que hoy): conexiones cortas y consultas siempre parametrizadas. Las tareas añaden `LoadTasks`/`SaveTasks` propios de `SqlDataStore`, fuera de `IDataStore` (specs/005); los recordatorios, igual, con `LoadReminders`/`SaveReminders` (specs/007); los presupuestos y pedidos, con `LoadQuotes`/`SaveQuotes` y `LoadOrders`/`SaveOrders` (specs/009). La conversión de un futuro cliente en cliente añade `RelinkRelatedRecords(prospectId, clientId)`: reasigna en una transacción las tablas `Tasks`, `Reminders` y `Quotes` cuyo vínculo apuntaba al futuro para que apunten al cliente nuevo (specs/003 §7 y specs/009 §4.3); devuelve cuántas filas se han reasignado y no toca los vínculos de otros registros.
- Guardar reemplaza el contenido de la tabla correspondiente en **una transacción** (borrado e inserciones): atómico por naturaleza; un fallo no deja cambios a medias (constitución, punto 5).
- Las tablas `Clients`, `Contacts`, `Tasks` y `Reminders` son independientes: guardar una no toca las demás.
- Un fallo de lectura o escritura se muestra como error visible, nunca como éxito (constitución, punto 5). Una base de datos ilegible deja los botones de gestión inactivos (mismo criterio que con los JSON).

## 4. Migración desde los JSON

- Al abrir una base de datos sin la marca de migración (tabla `SchemaInfo`), si existen `Data/clients.json` y/o `Data/contacts.json`, sus registros se importan a las tablas en una transacción y se asienta la marca: es **idempotente** (no duplica en aperturas siguientes).
- Los archivos JSON se quedan en disco **intactos** como copia de seguridad; la aplicación no vuelve a escribirlos ni los borra.
- Un fallo de migración (JSON ilegible) es un error visible: no se asienta la marca y la base de datos queda consistente y sin datos.
- Sin JSON existentes: primera ejecución limpia, tablas vacías, marca asentada igualmente.

## 5. Pruebas (xUnit, `Crm1.Tests`)

- `SqlDataStoreTests`: creación automática de tablas y primera ejecución vacía; guardar/recargar clientes y contactas con todos los campos (acentos, fechas ISO, estados, fecha vacía de contacto); independencia entre tablas; base de datos ilegible → error visible; migración con JSON previos → tablas pobladas una sola vez (idempotente); la marca impide reimportar si los JSON reaparecen tras el primer arranque; guardar/recargar tareas con todos los campos, incluidos los enums y el vínculo (specs/005); guardar/recargar recordatorios con fecha y hora (specs/007).
- `JsonDataStoreTests` se conservan: `JsonDataStore` sigue usándose como lector de la migración.

## 6. Fuera de alcance

- Otros motores (SQL Server, PostgreSQL...), acceso en red o multiusuario.
- Exportación o importación manual de la base de datos.
