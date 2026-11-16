# Especificación 004: Búsqueda global

**Estado:** Activa. Rige la búsqueda en clientes, futuros clientes, tareas y recordatorios guardados. Modelo y almacenamiento en specs/000-fundamentos.md y specs/006; los módulos buscados se definen en specs/002, specs/003, specs/005 y specs/007.

## 1. Objetivo

Desde el menú "Búsqueda" de `MainForm`, encontrar registros por texto libre con coincidencia total o parcial en cualquier campo visible de las tablas de Clientes, Futuros clientes, Tareas y Recordatorios, y navegar hasta el registro elegido.

## 2. Acceso

- El elemento "Búsqueda" del menú lateral de `MainForm` muestra `SearchForm` (`Forms/SearchForm.cs` + `Forms/SearchForm.Designer.cs`) embebido en el área central (`panelContent`), como los demás módulos. Instancia única. Implementado.

## 3. Diseño

- Panel superior (60 px, #F3F4F6) con etiqueta "Buscar:" y cuadro de texto ancho; la búsqueda se repite **en vivo** al escribir y el foco vuelve al cuadro cada vez que se muestra el módulo.
- Tabla de resultados `resultsDataGridView`: **la misma tabla que los módulos** — primera columna **Módulo** (Clientes / Futuros clientes) y las 13 columnas de specs/002 y specs/003 con los mismos nombres, cabeceras y orden. Solo lectura, selección por fila completa y única, sin cabecera de filas, columnas autoajustadas, celdas siempre blancas y **borde grueso** (3 px, #64748B) en la fila seleccionada, igual que en ClientForm/ContactForm. Sin resaltado de coincidencias: todas las celdas iguales.
- **Tabla inferior de tareas y recordatorios** (implementado, specs/004 §7): como no encajan en las 13 columnas de clientes, debajo aparece —solo cuando hay coincidencias— una segunda tabla `tasksDataGridView` (180 px, Dock=Bottom) con la etiqueta «Tareas y recordatorios» (28 px) sobre ella y cinco columnas: **Tipo** (Tarea / Recordatorio), **Título / Asunto**, **Cliente/Futuro cliente** («(Cliente) …» / «(Futuro cliente) …», «—» si el vínculo está roto), **Fecha** (límite dd/MM/aaaa o aviso dd/MM/aaaa HH:mm) y **Estado**. Mismas reglas visuales y de navegación que la tabla superior.
- Término vacío o en blanco = lista vacía (y la tabla inferior oculta).

## 4. Coincidencia (SearchService)

- Origen de los datos: **la base de datos guardada en disco** (`Data/crm1.db`, tablas `Clients`, `Contacts`, `Tasks` y `Reminders`, specs/006), leída en cada búsqueda. Los cambios sin Guardar no aparecen hasta guardarse.
- Coincidencia **total o parcial** (subcadena), **sin mayúsculas/minúsculas** y **sin tildes** ("malaga" encuentra "Málaga", "GARCIA" encuentra "García").
- Campos buscados, en el orden de las columnas de los módulos: Nombre, Primer apellido, Segundo apellido, Documento, Teléfono, Correo electrónico, Dirección, Código postal, Ciudad, Provincia, Estado (Activo/Inactivo; Pendiente/Descartado), Fecha de alta (dd/MM/aaaa; un futuro cliente sin fecha no busca por fecha) y Observaciones. **Tareas** (specs/005): Título, Descripción, registro vinculado (por su nombre resuelto), Fecha límite, Estado y Prioridad. **Recordatorios** (specs/007): Asunto, registro vinculado, Fecha y hora y Estado.
- El «—» de un vínculo roto se muestra pero no se busca.
- Un registro ocupa **una fila**. `SearchMatch` expone el registro completo (`Client`/`Contact`/`Task`/`Reminder`) y conserva internamente los campos coincidentes (`MatchedText`); para tareas y recordatorios añade `RelatedName` con el registro vinculado resuelto. El nombre para mostrar es nombre + apellidos no vacíos (razón social en empresas), o el título/asunto en tareas y recordatorios.
- Orden: primero clientes, luego futuros clientes, después tareas y por último recordatorios, en el orden de las tablas.

## 5. Navegación al resultado

- Doble clic o **Enter** sobre un resultado (en cualquiera de las dos tablas): `SearchForm` lanza `NavigateRequested(módulo, Id)` y `MainForm` muestra el módulo correspondiente y marca la fila con la selección existente (borde grueso), desplazando la tabla hasta hacerla visible (`SelectClientById` / `SelectContactById` / `SelectTaskById` / `SelectReminderById`).
- Si el módulo destino tiene una **edición en curso** (`IsEditing`): aviso y no se navega.
- Si el registro encontrado **no está en la tabla** en pantalla (posibles cambios sin guardar): aviso y no se marca nada. Al navegar a una tarea, antes se vuelve a la vista completa (`ShowAllTasks`) por si estaba filtrada a otro registro; si el cambio de vista no procede (edición o cambios pendientes), la selección falla y se avisa.

## 6. Protección de datos

- Un fallo de lectura de la base se muestra como error (MessageBox) y deja los resultados vacíos; nunca como una búsqueda correcta sin filas (constitución, punto 5).
- La búsqueda **nunca escribe** en disco.

## 7. Fuera de alcance

- Filtro por columna dentro de cada módulo.
- Módulos futuros (oportunidades, presupuestos, etc.): se añadirán a la búsqueda cuando existan.

## 8. Pruebas (xUnit)

`SearchServiceTests` (17): término vacío; parcial sin mayúsculas ni tildes; coincidencia total por documento; coincidencia por cualquier campo (observaciones); futuro cliente sin fecha no coincide con 01/01/0001; estado en español; fecha en dd/MM/aaaa; varios campos coincidentes en una sola fila; clientes antes que futuros clientes; sin resultados; nombre para mostrar con apellidos; registro completo expuesto; tarea por título con vínculo resuelto (`RelatedName`); tarea por descripción/estado/prioridad en español; recordatorio por asunto y por fecha con hora; vínculo roto mostrado como «—» y no buscable; orden clientes → futuros → tareas → recordatorios. Además `SearchFormUiTests` (2): las dos tablas se rellenan según el módulo (con Tag correcto y tabla inferior vacía sin coincidencias) y el doble clic en la tabla inferior pide navegar con módulo e identificador.
