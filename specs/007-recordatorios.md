# Especificación 007: Recordatorios

**Estado:** Activa. **Implementada en su alcance actual:** el diseño de `ReminderForm` con su carga en solo lectura (sección 3), el modelo `Reminder` con su tabla SQL `Reminders` (secciones 2 y 6), el item de menú «Recordatorios» (specs/001), las reglas de `ReminderService` incluidas las de aviso (sección 5), la edición completa con Nuevo, Modificar, Eliminar, Guardar y Cancelar y el registro actual (sección 4, skills «modificar», «borrar» y «seleccionar-cliente») y los avisos al abrir y al cerrar la aplicación (sección 7). Pendiente solo lo anotado en la sección 8 (repetición, posponer, aviso anticipado, aviso en vivo, descartar desde el aviso y ampliación de la búsqueda global).

## 1. Objetivo

Avisos puntuales con **fecha y hora** que saltan solos: al abrir la aplicación avisan los activos vencidos o de hoy, y al cerrarla los vencidos y los del día siguiente (decisión del usuario, 08/11/2026). La diferencia con una tarea es de función, no de campos:

| | Tarea (specs/005) | Recordatorio |
|---|---|---|
| Esencia | Trabajo a realizar | Aviso puntual |
| Momento | Fecha límite (día) | Fecha **y hora** |
| Estados | Pendiente / En curso / Completada | Activo / Descartado |
| Prioridad | Baja / Media / Alta | — |
| Avisos | — | Al abrir y al cerrar la app |

Un recordatorio no se «completa»: o todavía tiene que avisar (Activo) o ya avisó (Descartado). El aviso es informativo; descartar o eliminar es gestión manual en el módulo.

## 2. Modelo de datos (implementado)

| Campo      | Tipo             | Descripción                                             |
|------------|------------------|---------------------------------------------------------|
| Id         | Guid             | Identificador único (no se muestra en la tabla)         |
| Subject    | string           | Asunto: lo que dice el aviso (obligatorio)              |
| RemindAt   | DateTime         | Fecha y hora del aviso (obligatoria, dd/MM/aaaa HH:mm)  |
| Status     | ReminderStatus   | Active / Dismissed (Activo / Descartado)                |
| RelatedId  | Guid             | Registro vinculado (obligatorio)                        |
| RelatedType| TaskRelatedType  | Client / Prospect (se reutiliza el tipo de las tareas)  |

- Enums en inglés (`ReminderStatus`: Active/Dismissed) con etiquetas en español en la interfaz (constitución, punto 6).
- El vínculo se resuelve contra la unión de `Clients` y `Contacts` (specs/006), igual que las tareas; no se vincula a una tarea.
- **Fechas pasadas permitidas**: los recordatorios vencidos quedan como historial; no se valida antelación mínima.

## 3. Diseño del formulario (implementado, con carga en solo lectura)

- `ReminderForm` (`Forms/ReminderForm.cs` + `Forms/ReminderForm.Designer.cs`): 1200×650, título «Recordatorios», mismo patrón visual que `ClientForm`/`ContactForm`/`TaskForm`.
- Botonera superior (panel de 60 px, #F3F4F6) con cinco botones de 120×35 en las posiciones 12, 144, 276, 408 y 540: **Nuevo, Modificar, Eliminar, Guardar, Cancelar** — inactivos (y sin eventos) hasta que exista la edición (sección 4).
- Panel del contador (40 px, #F3F4F6) con `counterLabel` alineado a la derecha; texto «Mostrando X de Y».
- Tabla `remindersDataGridView`: FullRowSelect, MultiSelect=false, sin fila de alta automática, columnas autoajustadas (AllCells), sin cabecera de filas, celdas siempre blancas y borde grueso (3 px, #64748B) en la fila seleccionada.
- Columnas (4), en orden:

| # | Columna                | Tipo                                                        |
|---|------------------------|-------------------------------------------------------------|
| 1 | Asunto                 | Texto                                                       |
| 2 | Cliente/Futuro cliente | Desplegable con la unión: «(Cliente) Nombre» / «(Futuro cliente) Nombre» |
| 3 | Fecha y hora           | Calendario con hora: control combinado — DateTimePicker de calendario dd/MM/aaaa + selector de hora HH:mm con flechas y escritura (horas libres); la celda guarda dd/MM/aaaa HH:mm (`CalendarColumn` con formato propio y `DateTimeEditingControl`) |
| 4 | Estado                 | Desplegable: Activo / Descartado                            |

- Vínculo roto → «—» en la celda (añadido a la lista del desplegable, como las provincias de ClientForm); nombres repetidos → sufijo « (2)».
- Orden inicial: por **fecha y hora ascendente** (el más antiguo arriba); ordenable por cualquier cabecera como los demás módulos.
- La fila nueva nace con Estado «Activo»; una fila que solo tiene ese valor por defecto se considera vacía (un Nuevo por accidente se descarta al Guardar).
- **Carga en solo lectura (implementada):** al abrirse, `ReminderForm` lee los recordatorios de `Data/crm1.db` y rellena las columnas (fecha y hora en dd/MM/aaaa HH:mm; Estado con sus etiquetas en español), ordenados por fecha y hora ascendente. El vínculo se muestra como «(Cliente) Nombre» / «(Futuro cliente) Nombre», o «—» si el registro vinculado ya no existe. Contador «Mostrando X de Y», recarga al volver a mostrarse si cambió la base de datos (sello temporal, mismo criterio que ClientForm) y borde grueso (3 px, #64748B) en la fila seleccionada. Si la base falla, error visible y tabla vacía (constitución, punto 5).

## 4. Comportamiento

- **Implementado (skills «modificar», «borrar» y «seleccionar-cliente»):** tabla en solo lectura por defecto (cada fila `ReadOnly`, la activa se desbloquea); **Nuevo** inserta una fila vacía arriba que nace con Estado «Activo» y despliega el Asunto; **Modificar** exige recordatorio marcado (aviso «Selecciona un recordatorio de la tabla.»), activa sus celdas y coloca el cursor en el Asunto; **Cancelar** elimina la fila nueva o restaura la instantánea de la modificada (la marca persiste); **Eliminar** confirma Sí/No identificándolo por **asunto entrecomillado y registro vinculado** («¿Seguro que quieres eliminar el recordatorio "Llamada de Ana" de (Cliente) Ana García Pérez?»), en memoria hasta Guardar, sin papelera ni deshacer; **Guardar** recorta, valida con `ReminderService`, asigna los Id, escribe en `Reminders`, cierra la edición, **reordena la tabla por fecha y hora ascendente** (decisión del usuario: la vista queda siempre de agenda) y muestra «Se han guardado X recordatorio(s) correctamente.» — con errores muestra una línea por fila (el primer error de cada fila), marca las celdas con borde rojo y **mantiene la edición abierta**. Durante la edición las demás filas quedan atenuadas y sin selección posible, la ordenación se bloquea y Nuevo/Modificar/Eliminar quedan inactivos; Cancelar activo. Contador «Mostrando X de Y» (las filas con solo el «Activo» por defecto no cuentan) y borde grueso (3 px, #64748B) en la fila seleccionada. `HasUnsavedChanges` (`_dirty` comparando por Id con la última copia guardada) entra en el aviso de cierre de `MainForm`.
- **Registro actual (implementado, skill «seleccionar-cliente»):** clic en cualquier celda de la fila (o teclado, vía `SelectionChanged`) marca el recordatorio actual (`CurrentReminder`/`CurrentReminderId`), rastreado por identificador (`FindRowById`/`EnsureCurrentReminderExists`); sin selección no hay marca, nunca un valor inventado; durante la edición la marca es la fila editada y tras Guardar pasa al Id asignado **y sobrevive al reordenado** (la fila editada vuelve a seleccionarse donde haya quedado).
- Recarga por sello temporal al volver a mostrarse si la base cambió y no hay edición ni cambios pendientes; contador refrescado tras Eliminar y Guardar también sin edición abierta.

## 5. Reglas de negocio (ReminderService)

Validación al Guardar **implementada**, con mensajes cortos y borde rojo (patrón specs/002 §4.1):

1. **Asunto obligatorio** (tras recortar espacios): «La celda está vacía.»
2. **Vínculo obligatorio** y existente en la unión de clientes y futuros clientes: «Debes elegir un cliente o futuro cliente.»
3. **Fecha y hora obligatoria** y válida en dd/MM/aaaa HH:mm: «La celda está vacía.» / «La fecha no es válida (dd/mm/aaaa hh:mm).»
4. **Estado** perteneciente al desplegable: «El estado no es válido.»

Reglas de aviso (**implementadas** en `ReminderService.GetDueOnOpen`/`GetDueOnClose`; decisión original del 08/11/2026 con la **ventana de antigüedad de 5 días añadida el 14/11 por decisión del usuario**):

- **Al abrir la aplicación**: recordatorios con Estado Activo cuya fecha y hora es de **hoy** o de los **últimos 5 días** (vencidos con cinco días de antigüedad como mucho, límite incluido). Los vencidos más antiguos no avisan: quedan como historial en la tabla, sin descartarse solos. La comparación es por fecha: uno de hoy a las 23:00 avisa también a las 08:00.
- **Al cerrar la aplicación**: recordatorios con Estado Activo cuya fecha y hora es de **mañana** o de los **últimos 5 días** (mismo límite de antigüedad que la apertura).
- Ambas consultan la base de datos en el momento: los cambios sin guardar no cuentan.
- Los avisos **solo informan** (lista de asunto, registro vinculado y fecha/hora); no descartan ni modifican nada.
- Los Descartados nunca avisan; las listas salen ordenadas por fecha y hora ascendente.

## 6. Almacenamiento (implementado)

- Tabla `Reminders` en `Data/crm1.db` (specs/006), espejo del modelo: `Id` TEXT (GUID, clave primaria), `Subject` TEXT, `RemindAt` TEXT (ISO `yyyy-MM-dd HH:mm:ss`), `Status` TEXT (Active/Dismissed), `RelatedId` TEXT (GUID) y `RelatedType` TEXT (Client/Prospect). Sin migración desde JSON (dato nuevo, sin histórico).
- `LoadReminders`/`SaveReminders` en `SqlDataStore`, fuera de `IDataStore` (como `Tasks`): escritura transaccional, independiente del resto de tablas. Fallo de lectura/escritura = error visible, nunca éxito (constitución, punto 5).

## 7. Integración

- **Implementado:** item «Recordatorios» en el menú lateral de `MainForm` (tras «Tareas») mostrando `ReminderForm` embebido (specs/001).
- **Aviso al abrir (implementado):** en `OnShown` de `MainForm`, consulta `ReminderService.GetDueOnOpen` (con un `SqlDataStore` puntual); si hay recordatorios, diálogo OK con la lista («Tienes N recordatorio(s) vencidos o para hoy:» seguido de una línea por recordatorio: asunto entrecomillado, fecha y hora y registro vinculado con su prefijo); si no hay, nada. Un fallo de lectura es un error visible y la aplicación sigue.
- **Aviso al cerrar (implementado):** al final de `OnFormClosing`, solo si la salida no se canceló (con cambios sin guardar y respuesta No no se muestra), diálogo OK con vencidos, de hoy y de mañana («Antes de cerrar: N recordatorio(s) vencidos, de hoy o de mañana:» + la lista); **nunca bloquea el cierre**. Fallo de lectura = error visible y cierre igualmente.
- La lógica de los avisos vive en `ReminderService` (pura y probada); `MainForm` solo la invoca y presenta el diálogo.
- `ReminderForm.HasUnsavedChanges` se añade al aviso de cierre por cambios sin guardar de `MainForm.OnFormClosing` (**implementado**).

## 8. Fuera de alcance (anotado como pendiente futuro)

- **Repetición** (diaria/semanal/mensual), **posponer** (snooze), **aviso anticipado** («avísame 2 días antes») y **aviso en vivo** a la hora exacta con la app abierta (timer): la v1 comprueba solo al abrir y al cerrar.
- Descartar recordatorios desde el propio aviso.
- Vínculo con oportunidades (módulo inexistente) y ampliación de la búsqueda global (specs/004 §7).

## 9. Pruebas

- **Implementadas:** `SqlDataStoreTests` de la tabla `Reminders` (recarga con todos los campos —asunto con acentos, fecha y hora, enums, vínculo— e independencia del resto de tablas); `ReminderServiceTests` (una por regla de validación: asunto vacío, vínculo ausente o de tipo equivocado, fecha ausente, estado inválido, ambos estados permitidos y índices de fila; más las reglas de aviso: al abrir entran vencidos y los de hoy sin importar la hora, mañana y posteriores no; al cerrar entran vencidos, hoy y mañana, pero no pasado mañana; los vencidos de exactamente 5 días avisan y los de 6 o más no (ventana del 14/11); los Descartados nunca avisan; orden ascendente por fecha); y `ReminderFormUiTests` de la carga en solo lectura (orden por fecha y hora, nombres de vínculo resueltos, «—» si el registro no existe, contador, base ilegible → error visible y recarga al volver a mostrarse), de la edición (Nuevo con el Estado por defecto y bloqueo del resto, Cancelar, Guardar con errores manteniendo la edición, Guardar válido persistiendo en la base, **reordenando por fecha** y confirmando) y de Modificar/Eliminar/marca (marca con clic y limpieza sin selección, Modificar sin selección, Modificar+Cancelar restaura la instantánea, Modificar+Guardar persiste, Eliminar sin selección, Eliminar rechazado y Eliminar aceptado sin tocar la base hasta Guardar).
- Los diálogos de aviso de `MainForm` no llevan pruebas automatizadas (la lógica que deciden qué avisar está probada en `ReminderServiceTests`; mismo criterio que el resto de wiring de `MainForm`).
