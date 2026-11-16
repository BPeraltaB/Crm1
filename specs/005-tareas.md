# Especificación 005: Tareas

**Estado:** Activa. **Implementados:** el diseño de `TaskForm` (sección 3), su item de menú «Tareas» en `MainForm` (sección 7), el modelo `Task` con su tabla SQL `Tasks` (secciones 2 y 6), la carga en solo lectura con el nombre del cliente/futuro cliente resuelto, la **edición completa con Nuevo, Modificar, Eliminar, Guardar y Cancelar** (sección 4) y las reglas de `TaskService` (sección 5). El módulo está completo en su alcance actual.

## 1. Objetivo

Módulo de tareas del CRM: seguimiento de tareas con título, descripción, fecha límite, estado y prioridad, cada una vinculada a un cliente o a un futuro cliente (decisión del usuario; `docs/02-modelos-datos.md` prevé también `OportunidadId`, pendiente hasta que exista ese módulo).

## 2. Modelo de datos (implementado)

| Campo      | Tipo           | Descripción                                              |
|------------|----------------|----------------------------------------------------------|
| Id         | Guid           | Identificador único (no se muestra en la tabla)          |
| Action     | string         | Acción: Llamar, Visitar, Enviar correo, Reunión u Otro (obligatoria, desplegable) |
| Description| string         | Descripción (opcional)                                   |
| DueDate    | DateTime       | Fecha límite obligatoria (dd/MM/aaaa, sin hora)          |
| Status     | TaskStatus     | Pendiente / En curso / Completada                        |
| Priority   | TaskPriority   | Baja / Media / Alta                                      |
| Vínculo    | Guid + tipo    | Cliente o futuro cliente existente (obligatorio)         |

- Enums en inglés (`TaskStatus`: Pending/InProgress/Completed; `TaskPriority`: Low/Medium/High) con etiquetas en español en la interfaz (constitución, punto 6).
- Campos del vínculo: `RelatedId` (Guid) y `RelatedType` (`TaskRelatedType`: Client / Prospect).
- El vínculo se resuelve contra la unión de las tablas `Clients` y `Contacts` de `Data/crm1.db` (specs/006); se muestra el nombre en la columna «Cliente/Futuro cliente».

## 3. Diseño del formulario (implementado)

- `TaskForm` (`Forms/TaskForm.cs` + `Forms/TaskForm.Designer.cs`): 1200×650, título «Tareas», mismo patrón visual que `ClientForm`/`ContactForm`. **Sin eventos ni lógica**: puro diseño.
- Botonera superior (panel de 60 px, #F3F4F6) con cinco botones de 120×35 en las posiciones 12, 144, 276, 408 y 540: **Nuevo, Modificar, Eliminar, Guardar, Cancelar**.
- Panel del contador (40 px, #F3F4F6) con `counterLabel` alineado a la derecha; texto inicial «Mostrando 0 de 0» (la lógica lo actualizará).
- Tabla `tasksDataGridView`: FullRowSelect, MultiSelect=false, sin fila de alta automática (AllowUserToAddRows=false), columnas autoajustadas al contenido (AllCells), sin cabecera de filas y celdas siempre blancas (la selección no se resalta en azul).
- Columnas (6), en orden:

| # | Columna               | Tipo                              |
|---|-----------------------|-----------------------------------|
| 1 | Acción                | Desplegable: Llamar / Visitar / Enviar correo / Reunión / Otro |
| 2 | Descripción           | Texto                             |
| 3 | Cliente/Futuro cliente| Desplegable con la unión de clientes y futuros clientes: «(Cliente) Nombre» / «(Futuro cliente) Nombre» |
| 4 | Fecha límite          | Calendario (`CalendarColumn`: DateTimePicker incrustado que se despliega al editar; celda dd/MM/aaaa) |
| 5 | Estado                | Desplegable: Pendiente / En curso / Completada |
| 6 | Prioridad             | Desplegable: Baja / Media / Alta  |

- Un vínculo cuyo registro ya no existe se muestra como «—» (se añade a la lista del desplegable para poder pintarlo, como ClientForm con las provincias). Si dos registros se llaman igual, el segundo añade « (2)» al texto del desplegable.

- Conexión al menú: `TaskForm` se muestra desde el item «Tareas» de `MainForm` (sección 7).
- **Carga en solo lectura (implementada):** al abrirse, `TaskForm` lee las tareas de `Data/crm1.db` y rellena las columnas (fecha en dd/MM/aaaa; Estado y Prioridad con sus etiquetas en español). El vínculo se muestra como «(Cliente) Nombre» / «(Futuro cliente) Nombre», o «—» si el registro vinculado ya no existe. Contador «Mostrando X de Y», recarga al volver a mostrarse si cambió la base de datos (sello temporal, mismo criterio que ClientForm) y borde grueso (3 px, #64748B) en la fila seleccionada. La tabla es de solo lectura y los cinco botones están inactivos hasta que exista la edición (sección 4). Si la base falla, error visible y tabla vacía (constitución, punto 5).

## 4. Comportamiento

- **Implementado (skill «modificar», primera iteración):** tabla en solo lectura por defecto (cada fila `ReadOnly`, la activa se desbloquea); **Nuevo** inserta una fila vacía arriba y despliega la Acción; **Cancelar** elimina la fila nueva (con instantánea lista para Modificar); **Guardar** valida con `TaskService`, asigna los Id, escribe en `Tasks`, cierra la edición y retira las filas vacías — con errores muestra una línea por fila (el primer error de cada fila, como ClientForm), marca las celdas con borde rojo y **mantiene la edición abierta**. Durante la edición las demás filas quedan atenuadas y sin selección posible, la ordenación se bloquea y Nuevo/Modificar quedan inactivos; Cancelar activo. Contador «Mostrando X de Y» y borde grueso (3 px, #64748B) en la fila seleccionada.
- La fila nueva nace con Estado «Pendiente» y Prioridad «Media»; una fila que solo tiene esos valores por defecto se considera vacía (un Nuevo por accidente se descarta al Guardar, como el Pendiente de ContactForm).
- El desplegable «Cliente/Futuro cliente» se carga al abrir con la unión de clientes y futuros clientes, con prefijo para distinguirlos: «(Cliente) Nombre» / «(Futuro cliente) Nombre» (puede haber nombres repetidos entre ambas tablas). Si falla la carga de la base, todos los botones quedan desactivados (mismo criterio que `ContactForm`).
- **Modificar (implementado, skill «modificar»):** exige tarea marcada (aviso «Selecciona una tarea de la tabla.» si no la hay); activa sus celdas y coloca el cursor en la Acción; Cancelar restaura la instantánea de la fila modificada.
- **Eliminar (implementado, skill «borrar»):** actúa sobre la tarea marcada (mismo aviso sin selección); confirmación Sí/No identificándola **solo por su nombre**: acción y registro vinculado («¿Seguro que quieres eliminar la tarea Visitar de (Cliente) Ana…?»); la eliminación es en memoria y se hace permanente al pulsar Guardar; sin papelera ni deshacer. Tras eliminar o guardar, el contador «Mostrando X de Y» se refresca también sin edición abierta.
- **Registro actual (implementado, skill «seleccionar-cliente»):** clic en cualquier celda marca la tarea actual (`CurrentTask`/`CurrentTaskId`), rastreada por identificador (`FindRowById`/`EnsureCurrentTaskExists`); sin selección no hay marca; tras Guardar la marca pasa al Id asignado de la fila editada; el borde grueso de selección acompaña siempre a la fila marcada.

## 5. Reglas de negocio (implementado)

- `TaskService` (fuera del formulario, constitución punto 3): **acción obligatoria** y perteneciente a la lista del desplegable («La celda está vacía.» / «La acción no es válida.»); **vínculo obligatorio** con un cliente o futuro cliente existente y del tipo correcto («Debes elegir un cliente o futuro cliente.»); **fecha límite obligatoria** y en formato dd/MM/aaaa («La celda está vacía.» / «La fecha no es válida (dd/mm/aaaa).»); Estado y Prioridad pertenecientes a sus desplegables; descripción opcional. Errores por celda con mensajes cortos en español y borde rojo alrededor de la celda (patrón de `ClientService`, specs/002 §4.1); una línea por fila con el primer error.

## 6. Almacenamiento (implementado)

- Tabla `Tasks` en `Data/crm1.db` (specs/006): sin migración desde JSON (las tareas son un dato nuevo, sin histórico), escritura transaccional (borrado e inserciones en una transacción) e independiente de las tablas `Clients` y `Contacts`. Un fallo de lectura o escritura se muestra como error, nunca como éxito ni con pérdida silenciosa de datos. `LoadTasks`/`SaveTasks` viven en `SqlDataStore`, fuera de `IDataStore` (que queda para clientes y futuros clientes).
- **Conversión del futuro vinculado** (specs/003 §7): cuando un futuro cliente se convierte en cliente, sus tareas pasan a estar vinculadas al cliente nuevo automáticamente (`RelinkRelatedRecords`, specs/006); ninguna tarea queda huérfana ni bloquea Guardar.

## 7. Integración

- **Implementado:** item «Tareas» en el menú lateral de `MainForm` (tras «Futuros clientes»), mostrando `TaskForm` embebido en `panelContent` (instancia única, `ShowModule`), con la carga en solo lectura descrita en la sección 3, y **aviso al cerrar la aplicación** ampliado a los cambios sin guardar de `TaskForm` (`HasUnsavedChanges`).
- **Filtro por cliente/futuro cliente (implementado; botón «Tareas» de ClientForm y ContactForm, specs/002 y specs/003):** con un registro marcado, esos formularios lanzan `TasksRequested(id, nombre)` y `MainForm` abre este módulo embebido **filtrado** a sus tareas: aviso «Tareas de: (Cliente) Ana» y botón «Ver todas» que retira el filtro (`ShowTasksOf`/`ShowAllTasks`). Con el filtro activo funcionan **todos los botones, limitados al registro filtrado**: Nuevo crea la tarea **pre-vinculada** (el vínculo nace fijado y su celda queda bloqueada durante la edición), Modificar/Eliminar actúan sobre las visibles y **Guardar conserva las tareas ocultas** — se escriben las visibles más las de otros registros, nunca se pierden (constitución, punto 5). El contador refleja el filtro; la recarga al volver al módulo lo respeta. Con edición abierta o cambios pendientes no se cambia de vista (antes se guarda o se cancela). El item «Tareas» del menú abre siempre **sin filtro**.

## 8. Fuera de alcance

- Módulo de Recordatorios: formulario y avisos aparte (specs/007-recordatorios.md, creada y pendiente de implementar; decisión del usuario: aviso al abrir la app con activos vencidos o de hoy, y al cerrar con vencidos y del día siguiente).
- Vínculo de tareas con oportunidades (módulo inexistente).
- Ampliación de la búsqueda global a tareas (specs/004 §7, pendiente).

## 9. Pruebas

- **Implementadas:** `SqlDataStoreTests` para la tabla `Tasks` (recarga con todos los campos, independencia entre tablas, base nueva vacía), `TaskServiceTests` (una por regla: acciones válidas y desconocidas, vínculo ausente o de tipo equivocado, fecha vacía, índices de fila) y `TaskFormUiTests` (nombres de vínculo resueltos, «—» si el registro no existe, contador, botonera por estado, base ilegible → error visible, recarga al volver a mostrarse, Nuevo con valores por defecto, Cancelar, Guardar con errores manteniendo la edición, Guardar válido persistiendo en la base, marca del registro actual, Modificar sin selección, Modificar+Cancelar restaura la instantánea, Modificar+Guardar persiste, Eliminar sin selección, Eliminar rechazado y Eliminar aceptado sin tocar la base hasta Guardar).
