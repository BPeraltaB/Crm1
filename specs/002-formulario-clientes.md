# Especificación 002: Formulario de clientes

**Estado:** Activa. Rige la interfaz y el comportamiento de `ClientForm`. Modelo, reglas de negocio y almacenamiento en specs/000-fundamentos.md.

## 1. Objetivo

Formulario del módulo de clientes: tabla con todos los campos del cliente y botones de gestión. El formulario solo presenta la interfaz y llama a los servicios; las reglas viven fuera (constitución, punto 3).

## 2. Acceso

- El elemento "Clientes" del menú lateral de `MainForm` muestra `ClientForm` **embebido en el área central** (`panelContent`): `TopLevel = false`, sin borde (`FormBorderStyle.None`) y `Dock = Fill`. Instancia única: los clics repetidos no acumulan copias ni recargan el archivo. Implementado.

## 3. Diseño

- Embebido en el área central de `MainForm`: se adapta al espacio disponible (`Dock = Fill`), sin borde ni título propio. El diseño de referencia es de 1200×650.
- Panel superior (`buttonPanel`, 60 px, fondo #F3F4F6) con cuatro botones:

| Control (código)  | Texto     | Estado       |
|-------------------|-----------|--------------|
| `newButton`       | Nuevo     | Implementado (sección 4) |
| `modifyButton`    | Modificar | Implementado (sección 4) |
| `deleteButton`    | Eliminar  | Implementado (sección 4) |
| `saveButton`      | Guardar   | Implementado (sección 4) |
| `cancelButton`    | Cancelar  | Implementado (sección 4) |

- Tabla `clientsDataGridView` con las columnas **en este orden**:

| #  | Columna (HeaderText) | Campo del modelo | Tipo de columna |
|----|----------------------|------------------|-----------------|
| 1  | Nombre               | `FirstName` | Texto |
| 2  | Primer apellido      | `LastName1` | Texto |
| 3  | Segundo apellido     | `LastName2` | Texto |
| 4 | Documento            | `TaxId` | Texto |
| 5  | Teléfono             | `Phone` | Texto |
| 6  | Correo electrónico   | `Email` | Texto |
| 7  | Dirección            | `Address` | Texto |
| 8  | Código postal        | `PostalCode` | Texto |
| 9  | Ciudad               | `City` | Texto |
| 10 | Provincia            | `Province` | **Desplegable** (52 provincias oficiales del catálogo) |
| 11 | Estado               | `Status` | **Desplegable** (Activo / Inactivo) |
| 12 | Fecha de alta        | `RegistrationDate` | Texto (dd/MM/aaaa) |
| 13 | Observaciones        | `Notes` | Texto |

- La columna `Id` no se muestra; el identificador viaja en `Tag` de cada fila.
- Todas las filas están en **solo lectura** salvo la fila activa para edición (sección 4).
- Selección por fila completa, selección única, sin cabecera de filas, **columnas con ancho automático al contenido** (`AllCells`: crecen mientras se escribe, la cabecera marca el ancho mínimo) y **barra de desplazamiento horizontal** cuando el conjunto no cabe en la ventana. Las celdas se ven **siempre blancas** (`SelectionBackColor` blanco): la fila seleccionada se marca con un **borde grueso (3 px, gris pizarra) alrededor de toda la fila**, nunca con un tinte azul.
- Limitación asumida: las columnas desplegables no ordenan al pulsar su cabecera.

## 4. Comportamiento implementado

- **Solo lectura por defecto**: la tabla no se puede reescribir; la edición pasa por Nuevo o Modificar (patrón de la skill «modificar»).
- **Carga al abrir**: la tabla se rellena con los clientes de `Data/clients.json` (archivo inexistente = tabla vacía).
- **Nuevo**: inserta una fila vacía como primera fila, la activa para edición y entra en la celda Nombre.
- **Modificar**: activa las celdas de la **fila seleccionada** para reescribir las que se quiera (si no hay selección, aviso). Solo hay **una fila en edición a la vez**, rastreada por referencia a la fila.
- **Cancelar**: descarta la edición en curso — una fila nueva se elimina y una modificada vuelve a los valores de la instantánea tomada al activarla — y recalcula si quedan otros cambios pendientes comparando con la última copia guardada.
- **Eliminar** (skill «borrar»): pide confirmación Sí/No **solo con el nombre** («¿Seguro que quieres eliminar a Juan García López?») y quita la fila de la tabla. La eliminación es **en memoria**: se hace permanente al pulsar Guardar; salir sin guardar deja el archivo intacto. Actúa sobre el cliente actual (aviso si no hay selección), está inactivo durante la edición y, si la fila eliminada era la marcada, la marca se limpia.
- **Estados de la botonera**: sin edición → Nuevo/Modificar/Eliminar/Guardar activos y Cancelar inactivo; en edición → Nuevo/Modificar/Eliminar inactivos y Cancelar activo; error de carga (catálogo o JSON) → todo inactivo. La ordenación por cabeceras se bloquea durante la edición.
- **Filas deshabilitadas durante la edición**: mientras hay una fila activa, las demás se ven atenuadas en gris y no se pueden seleccionar (ni con clic ni con teclado: la selección y la celda actual vuelven a la fila en edición); se rehabilitan al guardar con éxito o al cancelar.
- **Cliente actual (skill «seleccionar-cliente»)**: un clic en cualquier celda de una fila (o el teclado) la marca como cliente actual, rastreado **por identificador** (nunca por índice) y expuesto como `CurrentClient`/`CurrentClientId` para acciones futuras. Sin selección no hay cliente actual; la fila marcada se ve rodeada por un **borde grueso** (la marca visual del patrón, también en la fila en edición); si la fila marcada desaparece, la marca se limpia; tras Guardar, la marca se mantiene sobre el mismo identificador. **Modificar actúa sobre el cliente actual** (aviso si no hay ninguno marcado).
- **Navegación desde la búsqueda global (specs/004)**: `IsEditing` expone si hay una edición en curso y `SelectClientById(Guid)` marca como cliente actual el indicado y desplaza la tabla hasta su fila; los usa `MainForm` al elegir un resultado de búsqueda. Con edición en curso o registro ausente, la búsqueda avisa y no navega.
- **Guardar**:
  1. Confirma la celda en edición (`EndEdit`).
  2. Ignora las filas totalmente vacías.
  3. Normaliza lo escrito (recortes, documento en mayúsculas y sin espacios, teléfono sin espacios) y lo refleja en las celdas.
  4. Valida todas las filas con `ClientService` (reglas 1-10 de specs/000).
  5. Si hay errores: **no se guarda nada**; cada celda con error recibe un **borde rojo alrededor** (dibujado con `CellPainting`; las celdas permanecen siempre blancas para escribir) y el MessageBox muestra una línea por error, **solo con el mensaje corto**. El borde rojo desaparece en cuanto se corrige la celda correspondiente.
  6. Si todo es correcto: asigna `Id` nuevo a las filas no guardadas y escribe `Data/clients.json` de forma atómica; confirma el número de clientes guardados. Un fallo de escritura se muestra como error, nunca como éxito (constitución, punto 5). Tras un guardado con éxito, todas las filas vuelven a solo lectura, las filas vacías se retiran de la vista y la edición se cierra; con errores, la edición sigue abierta (solo se cierra con un guardado correcto o con Cancelar).

### 4.1 Mensajes de error

| Situación | Mensaje |
|---|---|
| Celda obligatoria vacía | La celda está vacía. |
| Documento con formato o control incorrecto | El documento no es válido. |
| Documento repetido | El documento está repetido. |
| Teléfono que no es un móvil español | El teléfono no es un móvil válido. |
| Correo con formato incorrecto | El correo electrónico no es válido. |
| Código postal sin 5 dígitos | El código postal debe tener 5 dígitos. |
| Código postal inexistente | El código postal no existe. |
| Código postal que no corresponde a la ciudad | El código postal no corresponde a la ciudad. |
| Ciudad fuera de la provincia | La ciudad no pertenece a la provincia. |
| Fecha de alta mal escrita | La fecha no es válida (dd/mm/aaaa). |
| Estado no válido | El estado no es válido. |
- **Protecciones de datos**: si no se puede cargar el catálogo de CP o `clients.json` está dañado, se muestra el error y **Guardar queda deshabilitado** (nunca se sobrescribe un archivo ilegible). Salir del formulario no modifica el archivo en ningún caso: solo Guardar escribe.
- **Cierre con cambios sin guardar**: al cerrar la aplicación (o pulsar Salir) con cambios sin guardar en Clientes, `MainForm` pregunta "¿Seguro que quieres salir sin guardar?" (consulta `ClientForm.HasUnsavedChanges`); si se sale, el archivo conserva los datos de la última vez que se guardó.

## 5. Pendiente

- Búsqueda y filtro del listado.

## 6. Fuera de alcance

- Cualquier lógica de negocio dentro del formulario (vive en `Services/`).
- Otros módulos y formularios (specs/001-formularios-base.md).
