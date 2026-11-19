# Especificación 010: Historial

**Estado:** Activa e **implementada**. El historial es una sola vista cronológica por registro (cliente o futuro cliente) que reúne lo que hoy está repartido entre Tareas, Recordatorios, Presupuestos y Pedidos: «la historia de Ana» de un vistazo, con lo más reciente arriba. Es **solo lectura**: no hay tablas, reglas ni validaciones nuevas — pura agregación de lo que ya vive en `Data/crm1.db` (constitución, punto 5: nada que guardar, nada que perder).

## 1. Objetivo y decisiones

- **Una sola tabla cronológica** con columna Tipo (decisión del usuario): fecha descendente, lo más reciente arriba; una lectura = la relación completa con el registro.
- **La primera línea es el «Alta del cliente»** (o «Alta del futuro cliente»): solo si el registro tiene fecha (en futuros es opcional; sin fecha no hay fila de alta).
- **Barra de totales** (decisión del usuario): dinero de pedidos, dinero de abonos **y** el neto («Consumido»), más los presupuestos con aceptados, descartados y pendientes.
- **Sin botón «Ver todas/os»** y **sin item propio en el menú lateral**: el historial siempre está filtrado a un registro concreto; se abre desde el botón «Historial» de ClientForm o ContactForm.
- **Navegación con doble clic** al registro en su módulo (como la búsqueda global): **aplazada a una segunda ronda** (decisión del usuario).

## 2. Agregación (HistoryService, en Services/)

Servicio **estático y puro** (sin acceso a datos: recibe las listas ya cargadas y el registro objetivo; constitución, punto 3). `Build(client, contact, tasks, reminders, quotes, orders)` — exactamente uno de cliente/futuro no nulo — devuelve un `HistoryResult` con:

- **Entries**: lista de `HistoryEntry { Date, Kind, TypeText, Detail, StatusText }`, una por:
  - **Alta** (`Registration`): `Date` = fecha de alta; `Detail` y `Status` vacíos. Solo si la fecha existe.
  - **Recordatorio** (`Reminder`): `Date` = `RemindAt` (con hora), `Detail` = asunto, estado Activo/Descartado.
  - **Tarea** (`Task`): `Date` = `DueDate`, `Detail` = acción (y «acción: descripción» si hay descripción), estado Pendiente/En curso/Completada.
  - **Presupuesto** (`Quote`): `Date` = `QuoteDate`, `Detail` = «precio · referencia · descripción» en formato español, saltando los vacíos (p. ej. «1.000,00 € · PRE-003 · Reforma cocina»), estado Pendiente/Aceptado/Descartado. Se muestran **todos**, también los aceptados (que ya solo son historial).
  - **Pedido / Abono** (`Order`/`CreditNote`): `Date` = `OrderDate`, `Detail` = «precio · referencia · descripción» (el abono con el menos tipográfico «−» delante del precio, como en Pedidos), `Status` vacío. **Solo de clientes**: los futuros no pueden tener pedidos.
  - Filtrado por vínculo: tareas/recordatorios/presupuestos del registro (`RelatedId` + `RelatedType`); pedidos por `ClientId`.
- **Orden**: `Date` descendente; **empate** → el dinero arriba (abono, pedido, presupuesto, tarea, recordatorio y alta, en ese orden), mismo criterio que la vista de ejemplo.
- **Totals** (`HistoryTotals`): recuento y suma de presupuestos por estado (aceptados/descartados/pendientes) y los totales de movimientos reutilizando `OrderService.Totals` (pedidos, abonos y **Consumido = neto** = pedidos − abonos, que puede ser negativo).

Etiquetas de tipo, detalle y estado generadas en español (constitución, punto 6). Los estados se leen tal cual de la base: la caducidad de presupuestos la gestiona `QuoteService` al abrir/cerrar la aplicación; el historial no recalcula nada.

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
- Tabla `historyDataGridView` con 4 columnas de texto: **Fecha · Tipo · Detalle · Estado**. La fecha se muestra `dd/MM/aaaa`, y `dd/MM/aaaa HH:mm` en las líneas de recordatorio (su hora importa). Ordenable por cabeceras (con la misma limitación asumida de siempre al ordenar fechas como texto).
- **Carga**: `ShowHistoryOf(relatedType, relatedId, displayName)` fija el registro objetivo y rellena la tabla; el formulario se crea una sola vez y cada apertura reutiliza la instancia (patrón de TaskForm filtrado, pero sin botón «Ver todas»).
- **Recarga por sello temporal** al volver a mostrarse (si `Data/crm1.db` cambió en disco), para reflejar lo guardado en otros módulos; base ilegible → error visible y tabla vacía (constitución, punto 5).

## 4. Integración

- Botón **«Historial»** en la botonera de ClientForm y ContactForm (105×35, el último de la fila), con el mismo comportamiento que el botón Tareas: requiere registro actual marcado (sin selección → aviso «Selecciona un cliente/futuro cliente de la tabla.», caption «CRM1 — Historial»); inactivo durante la edición o con error de carga; lanza el evento `HistoryRequested(id, nombre)` que atiende MainForm.
- `MainForm.ShowFilteredHistory(relatedType, relatedId, displayName)`: crea `HistoryForm` una sola vez, aplica el filtro y lo muestra embebido en `panelContent` (como Tareas/Presupuestos/Pedidos filtrados).
- **Sin** item de menú, **sin** «Ver todas/os», **sin** ampliación de la búsqueda global (specs/004) y **sin** doble clic de navegación (segunda ronda).
- En ContactForm el arreglo aprovecha para **corregir un solapamiento existente**: el botón «Presupuestos» (x=802) pisaba al botón «Convertir en cliente» (x=795–945); pasa a x=957 y las posiciones de la botonera quedan sin solapes (Tareas 667 · Convertir 795 · Presupuestos 957 · Historial 1119).

## 5. Pruebas

- **HistoryServiceTests** (servicio puro): mezcla de tipos y orden descendente; empate de fechas con el dinero arriba; alta condicionada a la fecha (cliente siempre, futuro sin fecha no); futuro sin pedidos (ni líneas ni totales); textos de detalle (presupuesto con referencia y descripción, abono con «−», tarea con y sin descripción); etiquetas de estado; totales con abono (consumido = neto, incluso negativo); registro sin actividad; la actividad de otros registros no aparece.
- **HistoryFormUiTests** (formularios reales, colección «Formularios UI»): vista de un cliente con todo (filas, aviso, contador, barra de totales completa); vista de un futuro (solo presupuestos); recarga al volver a mostrarse tras un cambio en disco; base ilegible → error y tabla vacía.
- **Botón Historial** en ClientFormUiTests y ContactFormUiTests: sin selección → aviso; con registro marcado → evento con identificador y nombre.

## 6. Fuera de alcance (segunda ronda)

- Navegación con doble clic desde una línea del historial hasta su registro en el módulo correspondiente (reutilizar `SelectClientById`/`SelectTaskById`/`SelectQuoteById`/… de la búsqueda global).
- Filtrado por fechas, exportación del historial y cualquier edición de las líneas.
