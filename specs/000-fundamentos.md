# Especificación 000: Fundamentos y módulo Clientes

**Estado:** Activa y **implementada** en su alcance de Clientes: `MainForm` (menú "Clientes" muestra `ClientForm` embebido en su área central), `ClientForm` con carga, alta, edición, baja y guardado (specs/002), modelo `Client`, reglas de negocio en `ClientService`, catálogo de códigos postales, `JsonDataStore` y pruebas en `Crm1.Tests`. Pendiente del módulo: búsqueda y filtro. El módulo de Futuros clientes (`ContactForm`) está especificado e implementado en specs/003-formulario-futuros-clientes.md. El resto de módulos del diseño general queda pendiente (sección 8).

## 1. Alcance

Primera versión funcional de CRM1 centrada en el módulo de Clientes: alta, edición, baja, listado, búsqueda y filtro. La búsqueda y el filtro quedan **pendientes** (ver sección 8).

## 2. Solución y proyectos

- `Crm1.sln` con dos proyectos:
  - `Crm1.WindowsForms`: aplicación WinForms (.NET 8) con carpetas `Models/`, `Services/` y `Forms/`.
  - `Crm1.Tests`: proyecto de pruebas xUnit que referencia al principal.
- Compilación y pruebas mediante Visual Studio 2022 o `dotnet build` / `dotnet test`.

## 2.1 Formulario principal

- La aplicación inicia con el formulario principal `MainForm` (clase parcial `Crm1.WindowsForms.Forms.MainForm`), dividido en `Crm1.WindowsForms/Forms/MainForm.cs` y `Forms/MainForm.Designer.cs` para poder editarlo con el diseñador de Visual Studio. La clase no se llama `Form` a propósito: colisiona con `System.Windows.Forms.Form` y el diseñador de VS no la cargaría.
- Diseño: ventana de 1200×750 (mínimo 900×600), título "CRM1", centrada en pantalla.
  - Menú lateral (`MenuStrip` vertical, 220 px, tema oscuro #2D3748 con renderer propio) con: título "CRM1", Clientes, Búsqueda, Futuros clientes y Salir. Salir cierra la aplicación; Clientes muestra `ClientForm`, Futuros clientes muestra `ContactForm` y Búsqueda muestra `SearchForm` (specs/004-busqueda-global.md), embebidos en el área central (`panelContent`) de MainForm — **implementados**.
  - Área central clara (#F3F4F6) vacía, reservada para el contenido de los módulos futuros.
  - Barra de estado inferior blanca con "CRM1 — v0.1" y fecha/hora actualizada cada 30 segundos.
- "Futuros clientes" es el módulo de seguimiento previo a ser cliente: `ContactForm`, especificado e implementado en specs/003-formulario-futuros-clientes.md. El módulo "Contactos" original (personas de contacto de cada cliente) queda por decidir; el resto de módulos del diseño general (oportunidades, tareas, etc.) se ubicarán en otro formulario en el futuro, no en este menú.
- `ClientForm` (`Crm1.WindowsForms/Forms/ClientForm.cs` + `Forms/ClientForm.Designer.cs`): formulario del módulo de clientes; su diseño y comportamiento se definen en specs/002-formulario-clientes.md.

## 3. Modelo de datos: Client

| Campo             | Tipo          | Descripción                      |
|-------------------|---------------|----------------------------------|
| Id                | Guid          | Identificador único (no se muestra en la tabla) |
| FirstName         | string        | Nombre / razón social (obligatorio) |
| LastName1         | string        | Primer apellido (vacío en empresas) |
| LastName2         | string        | Segundo apellido (vacío en empresas y extranjeros con NIE) |
| TaxId             | string        | Documento: DNI, NIE o CIF (único entre clientes) |
| Address           | string        | Dirección (obligatorio) |
| PostalCode        | string        | Código postal (5 dígitos, texto) |
| City              | string        | Ciudad (obligatorio) |
| Province          | string        | Provincia (desplegable con las 52 oficiales) |
| Phone             | string        | Teléfono móvil (obligatorio) |
| Email             | string        | Correo electrónico (obligatorio, formato válido) |
| RegistrationDate  | DateTime      | Fecha de alta (dd/MM/aaaa) |
| Notes             | string        | Observaciones (único campo opcional) |
| Status            | ClientStatus  | Active / Inactive (Activo / Inactivo) |

## 4. Reglas de negocio (ClientService)

Todas las reglas viven fuera de los formularios (constitución, punto 3). El documento se normaliza (mayúsculas, sin espacios, DNI de 7 dígitos rellenado con 0) y el teléfono sin espacios antes de validar:

1. **Documento**: obligatorio; se distingue DNI, NIE y CIF por su forma y se comprueba el carácter de control (letra mod 23 para DNI/NIE; dígito o letra para CIF según la organización). Formato o control incorrectos → error.
2. **Teléfono**: obligatorio; móvil español: 9 dígitos empezando por 6 o 7, con `+34` opcional.
3. **Correo electrónico**: obligatorio y con formato válido.
4. **Código postal**: obligatorio; 5 dígitos y existente en el catálogo; además debe corresponder a la ciudad indicada.
5. **Ciudad**: obligatoria; debe pertenecer a la provincia seleccionada (comparación sin mayúsculas ni tildes; los nombres oficiales dobles tipo "Vitoria-Gasteiz" o "Agurain/Salvatierra" admiten cualquiera de sus formas).
6. **Primer apellido**: obligatorio salvo si el documento es un CIF.
7. **Segundo apellido**: obligatorio salvo si el documento es un CIF o un NIE.
8. **Resto de columnas**: obligatorias (Nombre, Dirección, Provincia, Estado ∈ {Activo, Inactivo}, Fecha de alta válida); Observaciones es el único campo opcional.
9. **Documento único**: no puede repetirse entre los clientes guardados ni entre filas de la tabla (comparación normalizada).
10. **Errores**: antes de guardar nada, cada celda con error se marca con un **borde rojo alrededor** (las celdas permanecen siempre blancas) y se muestra un mensaje corto por error, dirigido a la celda (p. ej. «La celda está vacía.»). El borde desaparece en cuanto se corrige la celda. La tabla de mensajes está en specs/002, sección 4.1.

## 5. Almacenamiento (JSON)

- Interfaz `IDataStore` con implementación `JsonDataStore`.
- Los clientes se guardan en `Data/clients.json` junto al ejecutable (carpeta `Data/` creada automáticamente). Escritura **atómica**: archivo temporal + reemplazo, para no perder el archivo anterior si la escritura falla.
- Los futuros clientes se guardan en `Data/contacts.json`, archivo distinto e independiente de `clients.json`, con la misma escritura atómica (specs/003).
- Un fallo de lectura o escritura debe provocar un error visible, nunca presentarse como éxito ni descartar datos silenciosamente (constitución, punto 5). Un archivo inexistente se interpreta como lista vacía (primera ejecución). Salir del formulario sin guardar nunca modifica el archivo.
- Formato de archivo: JSON (System.Text.Json, UTF-8 legible). Los formatos TXT y binario quedan pendientes; SQL queda pendiente hasta especificación futura.

### 5.1 Catálogo de códigos postales

- `Services/PostalCodeCatalog` carga `Data/postal-codes-es.json` (archivo local copiado junto al ejecutable): 10.874 códigos postales con sus municipios (correspondencia a nivel de municipio).
- Provincias: los dos primeros dígitos del CP identifican la provincia; los nombres oficiales (INE) están embebidos en el catálogo (52 provincias, formas oficiales dobles incluidas).
- Origen del dataset: Code for Spain (`codeforspain/ds-codigos-postales` y `codeforspain/ds-organizacion-administrativa`), licencia ODC-PDDL-1.0 (dominio público). Generado en octubre de 2026.
- Si el catálogo no se puede cargar, `ClientForm` desactiva Guardar (no se valida ni guarda sin él).

## 6. Idioma

- Código (clases, métodos, variables) en inglés.
- Comentarios, interfaz y mensajes al usuario en español (constitución, punto 6).

## 7. Pruebas (xUnit, `Crm1.Tests`)

- `SpanishDocumentsTests`: DNI/NIE/CIF válidos y con control incorrecto; detección del tipo; normalización (espacios, minúsculas, DNI de 7 dígitos).
- `PostalCodeCatalogTests`: con un archivo de datos reducido propio: existencia de CPs, municipios por CP, ciudad↔provincia, nombres oficiales dobles, archivo ilegible.
- `ClientServiceTests`: una prueba por regla (1-9) más un cliente válido completo.
- `JsonDataStoreTests`: guardar y recargar; archivo inexistente → lista vacía; archivo dañado → error; salir sin guardar no modifica el archivo; escritura atómica (sin .tmp residual).

## 8. Fuera de alcance (pendiente)

Búsqueda y filtro del módulo de clientes, contactos, oportunidades, tareas, recordatorios, presupuestos, pedidos, servicios, ventas, historial por cliente, formatos TXT y binario, SQL y exportación de datos.
