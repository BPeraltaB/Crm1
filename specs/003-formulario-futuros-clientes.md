# Especificación 003: Formulario de futuros clientes

**Estado:** Activa. Rige la interfaz y el comportamiento de `ContactForm` (módulo "Futuros clientes"), el modelo `Contact` y su almacenamiento. Estructura general del proyecto en specs/000-fundamentos.md; el módulo de Clientes le sirve de referencia (specs/002).

## 1. Objetivo

Formulario del módulo de futuros clientes: personas o empresas en seguimiento antes de convertirse en clientes. Como no siempre se conocen todos los datos, **las celdas pueden quedar vacías**; lo que sí está relleno debe ser correcto. El formulario solo presenta la interfaz y llama a los servicios; las reglas viven fuera (constitución, punto 3).

## 2. Acceso

- El elemento "Futuros clientes" del menú lateral de `MainForm` muestra `ContactForm` **embebido en el área central** (`panelContent`): `TopLevel = false`, sin borde y `Dock = Fill`. Instancia única: los clics repetidos no acumulan copias ni recargan el archivo. Implementado.

## 3. Diseño

- Idéntico al de `ClientForm` (specs/002): panel superior con `newButton`/`modifyButton`/`deleteButton`/`saveButton`/`cancelButton` (Nuevo/Modificar/Eliminar/Guardar/Cancelar) y tabla `contactsDataGridView` con las mismas 13 columnas y en el mismo orden (Nombre, Primer apellido, Segundo apellido, Documento, Teléfono, Correo electrónico, Dirección, Código postal, Ciudad, Provincia, Estado, Fecha de alta, Observaciones).
- Diferencias con Clientes:
  - Título "Futuros clientes".
  - La columna Estado es un desplegable con **Pendiente / Descartado** (en vez de Activo / Inactivo).
  - La fila nueva de "Nuevo" nace con Estado = Pendiente.
- Botonera por estado (patrón de la skill «modificar»):

| Control (código)  | Texto     | Estado       |
|-------------------|-----------|--------------|
| `newButton`       | Nuevo     | Implementado (sección 6) |
| `modifyButton`    | Modificar | Implementado (sección 6) |
| `deleteButton`    | Eliminar  | Implementado (sección 6) |
| `saveButton`      | Guardar   | Implementado (sección 6) |
| `cancelButton`    | Cancelar  | Implementado (sección 6) |
| `convertButton`   | Convertir en cliente | Implementado (sección 6) |

- Barra de información (`filterPanel`, 40 px, fondo #F3F4F6) bajo la botonera con el contador `counterLabel` "Mostrando X de Y". Sin filtro local: **la búsqueda se realiza desde el formulario principal** (búsqueda global, specs/004).

- `Id` no se muestra; viaja en `Tag` de cada fila. Todas las filas están en **solo lectura** salvo la fila activa para edición (sección 6). Selección por fila completa, **columnas con ancho automático al contenido** (`AllCells`) y **barra de desplazamiento horizontal** cuando el conjunto no cabe. Las celdas se ven **siempre blancas** (`SelectionBackColor` blanco): la fila seleccionada se marca con un **borde grueso (3 px, gris pizarra) alrededor de toda la fila**, nunca con un tinte azul.

## 4. Modelo y reglas de negocio (ContactService)

Modelo `Contact` con los mismos campos que `Client` (specs/000), con `Status` de tipo `ContactStatus` (Pending/Discarded). Reglas (todas fuera del formulario):

1. **Nombre**: obligatorio. Único campo obligatorio individual.
2. **Medio de contacto**: al menos Teléfono o Correo; si faltan ambos, se marcan en rojo las dos celdas con el mensaje "Debes indicar un teléfono o un correo electrónico."
3. **Teléfono** (relleno): cualquier teléfono español, fijo o móvil: 9 dígitos empezando por 6, 7, 8 o 9, con `+34` opcional.
4. **Correo electrónico** (relleno): formato válido.
5. **Documento** (relleno): DNI, NIE o CIF con el carácter de control correcto.
6. **Documento único**: no repetido entre los futuros clientes de la tabla y **no registrado en clientes**: un documento de `clients.json` no puede ser futuro cliente.
7. **Código postal** (relleno): 5 dígitos y existente; además corresponde a la ciudad si esta está rellena.
8. **Ciudad y provincia** (ambas rellenas): la ciudad pertenece a la provincia.
9. **Fecha de alta** (rellena): dd/MM/aaaa; vacía = sin fecha.
10. **Resto de celdas** (apellidos, dirección, observaciones): opcionales.
11. **Estado**: Pendiente / Descartado; vacío se guarda como Pendiente.

## 5. Almacenamiento (JSON)

- Los futuros clientes se guardan en `Data/contacts.json` junto al ejecutable: **archivo distinto e independiente** de `Data/clients.json`. Escritura atómica (archivo temporal + reemplazo).
- Guardar en Futuros clientes nunca escribe en `clients.json`; solo lo **lee** al abrir el formulario para aplicar la regla 6 (documento de cliente).
- Un archivo inexistente es una primera ejecución (lista vacía); salir sin guardar nunca modifica el archivo. Un fallo de lectura o escritura se muestra como error, nunca como éxito (constitución, punto 5).

## 6. Comportamiento implementado

- **Solo lectura por defecto**: la tabla no se puede reescribir; la edición pasa por Nuevo o Modificar (patrón de la skill «modificar»).
- **Carga al abrir**: catálogo de CPs, documentos de clientes guardados y `Data/contacts.json` (archivo inexistente = tabla vacía).
- **Nuevo**: inserta una fila como primera fila con Estado = Pendiente, la activa para edición y entra en la celda Nombre.
- **Modificar**: activa las celdas de la **fila seleccionada** para reescribir las que se quiera (si no hay selección, aviso). Solo hay **una fila en edición a la vez**, rastreada por referencia a la fila.
- **Cancelar**: descarta la edición en curso — una fila nueva se elimina y una modificada vuelve a los valores de la instantánea tomada al activarla — y recalcula si quedan otros cambios pendientes comparando con la última copia guardada. Sin edición abierta, Cancelar **deshace las conversiones pendientes** (tercera capa de seguridad): pide confirmación y devuelve los futuros clientes a la tabla.
- **Conversión en cliente** (specs/003, sección 7): `convertButton` actúa sobre el futuro cliente actual (aviso si no hay selección; inactivo durante edición o error de carga) con **doble confirmación**:
  1. **Validación completa con las reglas de clientes** (`ContactService.ConvertToClient`): documento válido, apellidos según tipo de documento, móvil 6/7, correo, dirección, CP/ciudad/provincia coherentes y fecha de alta. Si falta algo, se muestra la lista de carencias ("Teléfono: los clientes exigen un móvil que empiece por 6 o 7.") y **no se convierte**.
  2. **Primera confirmación** con el resumen de los datos del alta (nombre, documento, teléfono, correo, ciudad y fecha).
  3. **Segunda confirmación** advirtiendo de que al guardar desaparecerá de Futuros clientes.
  4. Tras los dos «Sí», la conversión queda **en memoria** (la fila se retira de la tabla). Al pulsar **Guardar** se escribe primero el alta en `clients.json` (conservando los clientes existentes) y solo si funciona se guarda `contacts.json` sin el convertido; un fallo no toca nada y se muestra (constitución, punto 5). El cliente nace **Activo** con identificador nuevo. `ClientForm` lo mostrará al volver al módulo de Clientes (recarga de specs/002).
- **Eliminar** (skill «borrar»): pide confirmación Sí/No **solo con el nombre** («¿Seguro que quieres eliminar a Juan García López?») y quita la fila de la tabla. La eliminación es **en memoria**: se hace permanente al pulsar Guardar; salir sin guardar deja el archivo intacto. Actúa sobre el futuro cliente actual (aviso si no hay selección), está inactivo durante la edición y, si la fila eliminada era la marcada, la marca se limpia.
- **Futuro cliente actual (skill «seleccionar-cliente»)**: un clic en cualquier celda de una fila (o el teclado) la marca como futuro cliente actual, rastreado **por identificador** (nunca por índice) y expuesto como `CurrentContact`/`CurrentContactId` para acciones futuras. Sin selección no hay futuro cliente actual; la fila marcada se ve rodeada por un **borde grueso** (la marca visual del patrón, también en la fila en edición); si la fila marcada desaparece, la marca se limpia; tras Guardar, la marca se mantiene sobre el mismo identificador. **Modificar actúa sobre el futuro cliente actual** (aviso si no hay ninguno marcado).
- **Navegación desde la búsqueda global (specs/004)**: `IsEditing` expone si hay una edición en curso y `SelectContactById(Guid)` marca como futuro cliente actual el indicado y desplaza la tabla hasta su fila; los usa `MainForm` al elegir un resultado de búsqueda. Con edición en curso o registro ausente, la búsqueda avisa y no navega.
- **Filas deshabilitadas durante la edición**: mientras hay una fila activa, las demás se ven atenuadas en gris y no se pueden seleccionar (ni con clic ni con teclado: la selección y la celda actual vuelven a la fila en edición); se rehabilitan al guardar con éxito o al cancelar.
- **Estados de la botonera**: sin edición → Nuevo/Modificar/Eliminar/Guardar activos y Cancelar inactivo; en edición → Nuevo/Modificar/Eliminar inactivos y Cancelar activo; error de carga (catálogo o JSON) → todo inactivo. La ordenación por cabeceras se bloquea durante la edición.
- **Contador del listado**: la barra bajo la botonera muestra cuántas filas con datos hay en la tabla ("Mostrando X de Y", siempre iguales al no haber filtro). Sin búsqueda ni filtro local: la búsqueda se hace desde el formulario principal (specs/004).
- **Conversión en cliente** (`convertButton`, sección 7): sobre el futuro cliente actual, con validación completa y doble confirmación; la baja de Futuros clientes y el alta en Clientes se hacen al pulsar Guardar.
- **Guardar**:
  1. Confirma la celda en edición (`EndEdit`).
  2. Ignora las filas totalmente vacías (el Estado Pendiente de la fila nueva no cuenta como dato).
  3. Normaliza lo escrito —recortes, documento en mayúsculas y sin espacios, teléfono sin espacios, Estado vacío → Pendiente y **mayúsculas según las normas de la lengua**: inicial mayúscula en cada palabra de nombre, apellidos, dirección y ciudad (partículas como "de" o "la" en minúscula si no abren el texto y siglas SA/SL conservadas) y observaciones solo con la primera letra— y lo refleja en las celdas.
  4. Valida todas las filas con `ContactService` (reglas 1-8 de la sección 4).
  5. Si hay errores: **no se guarda nada**; cada celda con error recibe un borde rojo (dibujado con `CellPainting`) y el MessageBox muestra una línea por error con el mensaje corto. El borde desaparece al corregir la celda.
  6. Si todo es correcto: asigna `Id` nuevo a las filas no guardadas y escribe `Data/contacts.json` de forma atómica; confirma el número de registros. Tras un guardado con éxito, todas las filas vuelven a solo lectura, las filas vacías se retiran de la vista y la edición se cierra; con errores, la edición sigue abierta (solo se cierra con un guardado correcto o con Cancelar).
- **Protecciones de datos**: si no se puede cargar el catálogo de CPs, `clients.json` o `contacts.json` está dañado, se muestra el error y **los botones de gestión quedan inactivos** (no se sobrescribe un archivo ilegible ni se omite la regla cruzada).
- **Cierre con cambios sin guardar**: al cerrar la aplicación (o pulsar Salir) con cambios sin guardar en cualquier módulo embebido (Clientes y/o Futuros clientes), `MainForm` pregunta "¿Seguro que quieres salir sin guardar?".

### 6.1 Mensajes de error

| Situación | Mensaje |
|---|---|
| Nombre vacío | La celda está vacía. |
| Teléfono y correo ausentes (ambas celdas) | Debes indicar un teléfono o un correo electrónico. |
| Teléfono relleno sin formato español | El teléfono no es válido. |
| Documento con formato o control incorrecto | El documento no es válido. |
| Documento repetido entre futuros clientes | El documento está repetido. |
| Documento registrado en clientes | El documento ya está registrado en clientes. |
| Correo con formato incorrecto | El correo electrónico no es válido. |
| Código postal sin 5 dígitos | El código postal debe tener 5 dígitos. |
| Código postal inexistente | El código postal no existe. |
| Código postal que no corresponde a la ciudad | El código postal no corresponde a la ciudad. |
| Ciudad fuera de la provincia | La ciudad no pertenece a la provincia. |
| Fecha de alta mal escrita | La fecha no es válida (dd/mm/aaaa). |
| Estado no válido | El estado no es válido. |
| Modificar sin fila seleccionada | Selecciona un futuro cliente de la tabla. |

## 7. Pendiente

- Nada pendiente del módulo de Futuros clientes.

## 8. Fuera de alcance

- Cualquier lógica de negocio dentro del formulario (vive en `Services/`).
- Otros módulos y formularios (specs/001-formularios-base.md).

## 9. Pruebas (xUnit, `Crm1.Tests`)

- `ContactServiceTests`: contacto mínimo válido (nombre + teléfono), contacto completo, nombre obligatorio, teléfono o correo, teléfono fijo/móvil/inválido, documento opcional y válido, CP/ciudad/provincia condicionales, documento repetido y documento de cliente, y conversión a cliente (éxito, fijo en vez de móvil, sin documento, sin fecha, sin ubicación, apellidos según documento y empresa con CIF).
- `JsonDataStoreTests`: contacts.json guardar/recargar (campos vacíos conservados), archivo inexistente → lista vacía, archivo dañado → error, **independencia entre contacts.json y clients.json**, sin .tmp residual.
- `TextNormalizerTests`: corrección de mayúsculas (partículas, siglas SA/SL, tildes, espacios de más y observaciones con la primera letra).
