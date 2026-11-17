# Especificación 009: Presupuestos y Pedidos

**Estado:** Activa e **implementada** en su alcance actual: modelos `Quote` y `Order` con tablas `Quotes` y `Orders` (secciones 2 y 6), `QuoteForm` y `OrderForm` completos con edición, filtro de año y totales (secciones 3 y 4), reglas de `QuoteService`/`OrderService` con `SpanishPrice` (sección 5), integración completa (sección 7) y búsqueda global ampliada (specs/004). El CRM no genera el documento del presupuesto ni del pedido (eso vive en el programa de administración, decisión del usuario): aquí se **registra** que un cliente o futuro cliente pidió un presupuesto, que un presupuesto se aceptó —y al aceptarse nace el pedido— y los pedidos y abonos de cada cliente, con sus totales anuales para saber lo que consume.

## 1. Objetivo

- **Presupuesto**: registro ligero (fecha, precio, referencia y descripción) vinculado a un **cliente o futuro cliente**, con estado Pendiente / Aceptado / Descartado. Un futuro cliente **solo puede tener presupuestos**: si tuviera un pedido, ya sería cliente.
- **Pedido**: registro (fecha, precio, referencia y descripción) **solo de clientes**; puede crearse directamente o nacer de un presupuesto aceptado, heredando fecha, precio, referencia y descripción.
- **Abono**: igual que un pedido pero resta en los totales (devoluciones, descuentos); se introduce con precio positivo.
- **Totales**: por año y por cliente (pedidos − abonos = consumo neto), con selector de año para comparar.

## 2. Modelo de datos

### Quote (presupuesto)

| Campo       | Tipo             | Descripción                                             |
|-------------|------------------|---------------------------------------------------------|
| Id          | Guid             | Identificador único (no se muestra)                     |
| QuoteDate   | DateTime         | Fecha del presupuesto (obligatoria, dd/MM/aaaa)          |
| Amount      | decimal          | Precio (obligatorio, **mayor que cero**)                 |
| Reference   | string           | Nº de presupuesto del programa de administración (opcional) |
| Description | string           | Descripción (opcional)                                   |
| Status      | QuoteStatus      | Pending / Accepted / Dismissed (Pendiente / Aceptado / Descartado) |
| RelatedId   | Guid             | Registro vinculado (obligatorio)                         |
| RelatedType | TaskRelatedType  | Client / Prospect (se reutiliza el tipo de las tareas)   |
| OrderId     | Guid?            | Pedido generado al aceptar; null = aún sin pedido        |

### Order (pedido o abono)

| Campo       | Tipo        | Descripción                                             |
|-------------|-------------|----------------------------------------------------------|
| Id          | Guid        | Identificador único (no se muestra)                      |
| Kind        | OrderKind   | Order / CreditNote (Pedido / Abono)                      |
| OrderDate   | DateTime    | Fecha (obligatoria, dd/MM/aaaa)                          |
| Amount      | decimal     | Precio (obligatorio, mayor que cero; los abonos restan en totales) |
| Reference   | string      | Nº de pedido/abono del programa de administración (opcional) |
| Description | string      | Descripción (opcional)                                   |
| ClientId    | Guid        | Cliente (obligatorio; **solo clientes**, nunca futuros)  |

- Enums en inglés (`QuoteStatus`, `OrderKind`) con etiquetas en español en la interfaz (constitución, punto 6). El vínculo de un presupuesto se resuelve contra la unión de `Clients` y `Contacts`; el de un pedido, solo contra `Clients`.

## 3. Diseño de los formularios

- `QuoteForm` («Presupuestos») y `OrderForm` («Pedidos»), cada uno `Forms/XForm.cs` + `XForm.Designer.cs`, 1200×650, mismo patrón visual que los módulos existentes (celdas siempre blancas, borde grueso 3 px #64748B en la selección, columnas autoajustadas, sin cabecera de filas).
- Botonera de 60 px (#F3F4F6): **Nuevo, Modificar, Eliminar, Guardar, Cancelar** (120×35 en 12, 144, 276, 408 y 540). `QuoteForm` añade un sexto botón en 672: **«Aceptar»**, cuyo texto y acción dependen del presupuesto marcado (sección 4.3).
- Barra de 40 px con: etiqueta **«Año:» + desplegable** (`yearFilterComboBox`: «Todos» —por defecto— y los años presentes en los datos), **etiqueta de totales** a la derecha y **contador «Mostrando X de Y»** en el extremo derecho.
- Tabla de `QuoteForm` (6 columnas): **Fecha · Precio (€) · Referencia · Descripción · Estado · Cliente/Futuro cliente** (desplegable con la unión, «(Cliente) …» / «(Futuro cliente) …», «—» si el vínculo está roto; nombres repetidos con sufijo « (2)»).
- Tabla de `OrderForm` (6 columnas): **Tipo · Fecha · Precio (€) · Referencia · Descripción · Cliente** (desplegable **solo con clientes**, con prefijo «(Cliente) …»).
- Fecha con `CalendarColumn` (dd/MM/aaaa, como las tareas). Precio como texto en formato español: se muestra «1.234,56» (separador de miles y dos decimales). Estado y Tipo como desplegables.
- **Orden por defecto: fecha descendente** (lo más reciente arriba, para ver qué hay que perseguir); ordenable por cabeceras; tras Guardar se reordena igual (la vista queda siempre cronológica inversa).
- La fila nueva de `QuoteForm` nace con Estado «Pendiente»; una fila que solo tiene ese valor por defecto se considera vacía. La fila nueva de `OrderForm` nace con Tipo «Pedido» (mismo criterio de fila vacía).
- Carga en solo lectura al abrir; recarga por sello temporal al volver a mostrarse (si no hay edición ni cambios); base ilegible → error visible, botones inactivos (constitución, punto 5).

## 4. Comportamiento

### 4.1 Edición (skills «modificar», «borrar» y «seleccionar-cliente»)

- Tabla en solo lectura por defecto; Nuevo inserta fila vacía arriba y despliega la Fecha; Modificar exige registro marcado; Cancelar restaura/elimina; Eliminar confirma por nombre («¿Seguro que quieres eliminar el pedido de 1.200,00 € de (Cliente) Ana…?», presupuesto por descripción o referencia); Guardar valida, asigna Id, escribe, cierra la edición y reordena; con errores marca celdas con borde rojo, una línea por fila y mantiene la edición. Registro actual con marca por Id (`CurrentQuote`/`CurrentOrder`), contador, filas atenuadas durante la edición y ordenación bloqueada: todo como los demás módulos.

### 4.2 Selector de año y totales

- El desplegable de año lista «Todos» + los años con fecha en la tabla del módulo; al elegir uno, la tabla muestra solo las filas de ese año (y el contador refleja el filtro). Durante la edición queda bloqueado, como la botonera.
- **Totales de `OrderForm`** (de las filas visibles, es decir, según año y cliente filtrados): «2026: 12 pedidos — 34.560,50 € · 2 abonos — −450,00 € · **Neto: 34.110,50 €**». Con «Todos», los números agregan todos los años.
- **Totales de `QuoteForm`**: «5 presupuestos — 2 aceptados» (según el año y el cliente filtrados).

### 4.3 Aceptar un presupuesto (botón «Aceptar»)

1. Activo solo con un presupuesto **Pendiente** marcado; confirmación con el resumen (fecha, precio, referencia y registro vinculado).
2. El presupuesto pasa a **Aceptado** y **queda en el historial** (para ver cuántos acepta cada cliente); el **pedido se crea aparte**, heredando fecha, precio, referencia y descripción, con Tipo «Pedido» y `OrderId` del presupuesto enlazado.
3. Si el presupuesto es de un **cliente**: el pedido se crea directamente (en memoria; al Guardar).
4. Si es de un **futuro cliente**: aviso — «Para crear el pedido, [nombre] debe ser cliente con todos sus datos. ¿Convertirlo ahora?»:
   - **Sí** y el futuro cumple las reglas de clientes → conversión con sus **dos confirmaciones** (specs/003 §7): cliente nuevo Activo; su presupuesto, tareas y recordatorios se revinculan; el pedido se crea a nombre del cliente nuevo.
   - **Sí** pero faltan datos → lista de carencias; el presupuesto queda **Aceptado sin pedido**.
   - **No** → Aceptado sin pedido.
5. **Recuperación**: un presupuesto Aceptado con `OrderId` vacío cambia el botón a **«Crear pedido»**, que repite el punto 3 o 4 (sin re-marcar el estado, que ya es Aceptado).
6. Todo en memoria hasta **Guardar**; Cancelar sin edición deshace las aceptaciones pendientes (como las conversiones de ContactForm).
7. **Orden de escritura al Guardar** (aceptación con conversión): alta del cliente en `Clients` → revinculación de tareas, recordatorios y presupuestos al cliente nuevo → `Contacts` sin el futuro → pedidos nuevos en `Orders` → `Quotes` con el estado y el `OrderId`. Cada fallo se muestra tal cual ocurrió y no se guarda nada más (constitución, punto 5). Sin conversión: `Orders` primero y `Quotes` después.

## 5. Reglas de negocio (QuoteService / OrderService)

- **Fecha obligatoria**, válida en dd/MM/aaaa y **nunca anterior a hoy** (decisión del usuario, 18/11/2026): al Guardar una fila nueva o modificada con fecha pasada salta el mensaje «La fecha no puede ser anterior a hoy.», la celda queda marcada con borde rojo y la edición sigue abierta. Los registros históricos guardados **sin cambios** se reescriben tal cual sin revalidar su fecha: el historial previo a la regla permanece (y sigue alimentando los totales por año) («La celda está vacía.» / «La fecha no es válida (dd/mm/aaaa).» / «La fecha no puede ser anterior a hoy.»). **Aceptar** también respeta la regla: un presupuesto con fecha pasada no se acepta hasta corregirla, así el pedido heredado nace siempre con fecha válida.
- **Precio obligatorio**, en formato español — se admite coma o punto como decimal y puntos de miles con coma («1.234,56», «1234,56», «1234.56») — y **mayor que cero** («La celda está vacía.» / «El precio no es válido (usa el formato 1234,56).» / «El precio debe ser mayor que cero.»).
- **Estado / Tipo** pertenecientes a sus desplegables («El estado no es válido.» / «El tipo no es válido.»).
- **Vínculo obligatorio**: presupuesto → cliente o futuro existente y del tipo correcto; pedido/abono → **cliente existente** («Debes elegir un cliente o futuro cliente.» / «Debes elegir un cliente.»).
- Referencia y descripción opcionales (se recortan al guardar; sin normalización de mayúsculas).
- Servicios puros con validación por fila (patrón specs/002 §4.1), más helpers de totales por año y de análisis del precio.

## 6. Almacenamiento (specs/006)

- Tablas **`Quotes`** e **`Orders`** en `Data/crm1.db`, espejo de los modelos: fechas ISO `yyyy-MM-dd`, enums como nombre del valor, `Amount` como texto en cultura invariable («1234.56», exacto para decimal), `OrderId` TEXT nulable.
- `LoadQuotes`/`SaveQuotes` y `LoadOrders`/`SaveOrders` en `SqlDataStore`, fuera de `IDataStore`, escritura transaccional e independiente del resto de tablas. Sin migración (dato nuevo).
- **`RelinkRelatedRecords` se extiende a `Quotes`**: la conversión de un futuro (desde ContactForm o desde el propio QuoteForm) también reasigna sus presupuestos; los pedidos nunca están en futuros.

## 7. Integración

- Menú lateral de `MainForm`: items **«Presupuestos»** y **«Pedidos»** tras «Recordatorios», embebidos e instancia única (specs/001).
- Botón **«Presupuestos»** en ClientForm y ContactForm, y **«Pedidos»** en ClientForm (un futuro no puede tener pedidos): eventos `QuotesRequested`/`OrdersRequested` y vistas filtradas con aviso («Presupuestos de: (Cliente) Ana» / «Pedidos de: (Cliente) Ana») y botón «Ver todas/os», igual que Tareas (specs/005 §7): Nuevo pre-vinculado con la celda bloqueada, Modificar/Eliminar sobre las visibles, **Guardar conserva las ocultas**, contador según filtro, sin cambio de vista con edición o cambios pendientes.
- **Aviso al cerrar** ampliado a `QuoteForm`/`OrderForm` (`HasUnsavedChanges`).
- **Búsqueda global** (specs/004): la tabla inferior admite tres valores nuevos de «Tipo»: Presupuesto, Pedido y Abono — busca en descripción, referencia, registro vinculado, fecha, precio (formato español) y estado; la etiqueta pasa a «Tareas, recordatorios, presupuestos y pedidos». Navegación a los módulos con `SelectQuoteById`/`SelectOrderById` + `IsEditing` (respetando el filtro de año).

## 8. Fuera de alcance

- Generar el documento del presupuesto o del pedido (vive en el programa de administración); líneas de detalle/artículos; presupuestos caducados por antigüedad; vínculo con oportunidades (módulo inexistente); exportación.
- Descuento automático del stock, facturación y IVA.

## 9. Pruebas (planificadas)

- `QuoteServiceTests`/`OrderServiceTests`: una por regla (fecha, precio vacío/inválido/cero, formatos admitidos incluidos miles con coma, estado/tipo, vínculo y tipo de vínculo, pedido solo a cliente) y totales por año con abonos (neto).
- `SqlDataStoreTests`: recarga de Quotes/Orders con decimales exactos, referencias y OrderId; independencia; `RelinkRelatedRecords` moviendo también presupuestos.
- `QuoteFormUiTests`: carga descendente, aceptar de cliente (pedido en memoria → Guardar escribe Orders+Quotes), aceptar de futuro con conversión completa (revinculación incluida), carencias → Aceptado sin pedido, «No» → Aceptado sin pedido, «Crear pedido» de recuperación, Cancelar deshace, selector de año y totales.
- `OrderFormUiTests`: carga, abonos que restan en el neto, totales por año y por cliente filtrado, CRUD completo y fila nueva «Pedido».
- `SearchServiceTests`/`SearchFormUiTests`: los nuevos tipos en la tabla inferior y su navegación.
