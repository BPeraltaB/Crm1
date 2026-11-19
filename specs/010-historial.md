# Especificación 010: Historial

**Estado:** Activa e **implementada**. El historial es una sola vista cronológica por registro (cliente o futuro cliente) que reúne lo que hoy está repartido entre Tareas, Recordatorios, Presupuestos y Pedidos: «la historia de Ana» de un vistazo, con lo más reciente arriba. Es **solo lectura**: no hay tablas, reglas ni validaciones nuevas — pura agregación de lo que ya vive en `Data/crm1.db` (constitución, punto 5: nada que guardar, nada que perder).

## 1. Objetivo y decisiones

- **Una sola tabla cronológica** con columna Tipo (decisión del usuario): fecha descendente, lo más reciente arriba; una lectura = la relación completa con el registro.
- **Se detallan solo las tareas y recordatorios que faltan** (Pendientes, En curso y Activos, aunque estén vencidos) y el alta; **todo lo demás se resume por año** (decisión del usuario, 19/11, revisada el mismo día): una línea de «Consumo AÑO», una de «Presupuestos AÑO», una de «Tareas AÑO» (completadas) y una de «Recordatorios AÑO» (descartados), solo en los años con contenido de cada clase.
- **La línea más antigua es el «Alta del cliente»** (o «Alta del futuro cliente»): solo si el registro tiene fecha (en futuros es opcional; sin fecha no hay fila de alta).
- **Barra de totales globales** (decisión del usuario): dinero de pedidos, dinero de abonos **y** el neto («Consumido»), más los presupuestos con aceptados, descartados y pendientes — de toda la historia; los desgloses por año viven en la tabla.
- **Sin botón «Ver todas/os»** y **sin item propio en el menú lateral**: el historial siempre está filtrado a un registro concreto; se abre desde el botón «Historial» de ClientForm o ContactForm.
- **Navegación con doble clic** al registro en su módulo (como la búsqueda global): **aplazada a una segunda ronda** (decisión del usuario).

## 2. Agregación (HistoryService, en Services/)

Servicio **estático y puro** (sin acceso a datos: recibe las listas ya cargadas y el registro objetivo; constitución, punto 3). `Build(client, contact, tasks, reminders, quotes, orders)` — exactamente uno de cliente/futuro no nulo — devuelve un `HistoryResult` con:

- **Entries**: lista de `HistoryEntry { Date, Kind, TypeText, Detail, StatusText }`:

  | Línea | Kind | Fecha | Detalle | Estado |
  |---|---|---|---|---|
  | Alta del cliente/futuro | `Registration` | fecha de alta | vacío | vacío |
  | Recordatorio **activo** | `Reminder` | `RemindAt` (con hora) | asunto | Activo |
  | Tarea **pendiente o en curso** | `Task` | `DueDate` | acción (y «acción: descripción» si la hay) | Pendiente / En curso |
  | Consumo AÑO | `ConsumptionSummary` | 31/12 del año | «2 pedidos — 1.000,00 € · 1 abono — −150,00 € · Consumido: 850,00 €» (partes a cero omitidas; solo clientes) | vacío |
  | Presupuestos AÑO | `QuoteSummary` | 31/12 del año | «4 — 2 aceptados · 1 descartado · 1 pendiente» (partes a cero omitidas) | vacío |
  | Tareas AÑO | `TaskSummary` | 31/12 del año | «3 completadas» | vacío |
  | Recordatorios AÑO | `ReminderSummary` | 31/12 del año | «2 descartados» | vacío |

  - Filtrado por vínculo: tareas/recordatorios/presupuestos del registro (`RelatedId` + `RelatedType`); pedidos por `ClientId` (**solo clientes**: los futuros no pueden tener pedidos).
  - Las líneas de resumen llevan internamente el 31/12 a las 23:59:59 de su año para **encabezar su bloque anual**, aunque se muestran solo con el año.
- **Orden**: `Date` descendente; **empate** → el orden del enum: consumo, presupuestos, tareas completadas, recordatorios descartados y luego el detalle (el dinero y lo resumido quedan arriba).
- **Totals** (`HistoryTotals`): recuento y suma globales de presupuestos por estado (aceptados/descartados/pendientes) y los totales de movimientos reutilizando `OrderService.Totals` (pedidos, abonos y **Consumido = neto** = pedidos − abonos, que puede ser negativo).

Etiquetas de tipo, detalle y estado generadas en español, con singular y plural correctos («1 aceptado» / «2 aceptados»; constitución, punto 6). Los estados se leen tal cual de la base: la caducidad de presupuestos la gestiona `QuoteService` al abrir/cerrar la aplicación; el historial no recalcula nada.

## 3. HistoryForm (en Forms/)

- Mismo patrón visual que los demás módulos: 1200×650 de referencia, embebido, tabla con celdas siempre blancas, selección única por fila completa, borde grueso (3 px, gris pizarra) en la fila seleccionada, columnas autoajustadas (`AllCells`), sin cabecera de filas, **todo en solo lectura**.
- **Sin botonera de edición** (no hay Nuevo/Modificar/Eliminar/Guardar/Cancelar) y sin `HasUnsavedChanges`: el aviso al cerrar de MainForm no lo consulta.
- Barra única de 40 px (#F3F4F6, `filterPanel`) con:
  - **Aviso** `filterLabel` «Historial de: (Cliente) Ana» / «Historial de: (Futuro cliente) …» a la izquierda. Siempre visible tras la primera apertura filtrada.
  - **Totales** `totalsLabel` a la derecha (antes del contador, con puntos suspensivos si no cabe):
    - Con movimientos (clientes con pedidos o abonos): «2 pedido(s) — 1.000,00 € · 1 abono(s) — −150,00 € · Consumido: 850,00 € · Presupuestos: 5 — 2 aceptado(s) · 1 descartado(s) · 2 pendiente(s)» (mismo formato que Pedidos, specs/009 §4.2).
    - Sin movimientos (futuros siempre —no pueden tener pedidos— y clientes que aún no tienen): solo la parte de presupuestos («Presupuestos: 3 — 1 aceptado(s) · 1 descartado(s) · 1 pendiente(s)»).
    - El neto negativo lleva el menos tipográfico «−».
  - **Contador** «Mostrando X de X» en el extremo derecho (X = líneas de la tabla).
- Tabla `historyDataGridView` con 4 columnas de texto: **Fecha · Tipo · Detalle · Estado**. La fecha se muestra `dd/MM/aaaa`, `dd/MM/aaaa HH:mm` en los recordatorios (su hora importa) y **solo el año** («2026») en las líneas de resumen anual. Ordenable por cabeceras (con la misma limitación asumida de siempre al ordenar fechas como texto).
- **Carga**: `ShowHistoryOf(relatedType, relatedId, displayName)` fija el registro objetivo y rellena la tabla; el formulario se crea una sola vez y cada apertura reutiliza la instancia (patrón de TaskForm filtrado, pero sin botón «Ver todas»).
- **Recarga por sello temporal** al volver a mostrarse (si `Data/crm1.db` cambió en disco), para reflejar lo guardado en otros módulos; base ilegible → error visible y tabla vacía (constitución, punto 5).

## 4. Integración

- Botón **«Historial»** en la segunda fila de la botonera de ClientForm y ContactForm —junto al botón Tareas (specs/002/003 §3)—, con el mismo comportamiento que este: requiere registro actual marcado (sin selección → aviso «Selecciona un cliente/futuro cliente de la tabla.», caption «CRM1 — Historial»); inactivo durante la edición o con error de carga; lanza el evento `HistoryRequested(id, nombre)` que atiende MainForm.
- `MainForm.ShowFilteredHistory(relatedType, relatedId, displayName)`: crea `HistoryForm` una sola vez, aplica el filtro y lo muestra embebido en `panelContent` (como Tareas/Presupuestos/Pedidos filtrados).
- **Sin** item de menú, **sin** «Ver todas/os», **sin** ampliación de la búsqueda global (specs/004) y **sin** doble clic de navegación (segunda ronda).
- El estreno del botón destapó los límites de la fila única de la botonera —el «Presupuestos» de ContactForm pisaba a «Convertir en cliente» desde el 17/11 y el propio «Historial», colocado al final, quedaba fuera de pantalla en ventanas normales— y llevó la botonera de ambos formularios a **dos filas** (gestión arriba, registro abajo), con la prueba de regresión de layout de specs/002 §3 que lo vigila desde entonces.

## 5. Pruebas

- **HistoryServiceTests** (servicio puro): bloques anuales con su orden (consumo, presupuestos, tareas, recordatorios y detalle vivo entre medias); solo lo pendiente/activo se detalla y lo completado/descartado se resume; textos de los resúmenes (partes a cero omitidas, singular/plural, año de solo abonos sin «0 pedidos», neto negativo con «−»); tarea sin descripción y estados En curso/Pendiente; alta condicionada a la fecha (cliente siempre, futuro sin fecha no); futuro sin pedidos (ni líneas ni totales); la actividad de otros registros no aparece; totales globales con abono (consumido = neto, incluso negativo); registro sin actividad.
- **HistoryFormUiTests** (formularios reales, colección «Formularios UI»): vista de un cliente con todo (resúmenes por año, detalle, fecha de año, aviso, contador y barra de totales); vista de un futuro (solo presupuestos); recarga al volver a mostrarse tras un cambio en disco; base ilegible → error y tabla vacía.
- **Botón Historial** en ClientFormUiTests y ContactFormUiTests: sin selección → aviso; con registro marcado → evento con identificador y nombre.

## 6. Fuera de alcance (segunda ronda)

- Navegación con doble clic desde una línea del historial hasta su registro en el módulo correspondiente (reutilizar `SelectClientById`/`SelectTaskById`/`SelectQuoteById`/… de la búsqueda global).
- Filtrado por fechas, exportación del historial y cualquier edición de las líneas.
