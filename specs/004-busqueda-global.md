# Especificación 004: Búsqueda global

**Estado:** Activa. Rige la búsqueda en clientes y futuros clientes guardados. Modelo y almacenamiento en specs/000-fundamentos.md; los módulos buscados se definen en specs/002-formulario-clientes.md y specs/003-formulario-futuros-clientes.md.

## 1. Objetivo

Desde el menú "Búsqueda" de `MainForm`, encontrar registros por texto libre con coincidencia total o parcial en cualquier campo visible de las tablas de Clientes o Futuros clientes, y navegar hasta el registro elegido.

## 2. Acceso

- El elemento "Búsqueda" del menú lateral de `MainForm` muestra `SearchForm` (`Forms/SearchForm.cs` + `Forms/SearchForm.Designer.cs`) embebido en el área central (`panelContent`), como los demás módulos. Instancia única. Implementado.

## 3. Diseño

- Panel superior (60 px, #F3F4F6) con etiqueta "Buscar:" y cuadro de texto ancho; la búsqueda se repite **en vivo** al escribir y el foco vuelve al cuadro cada vez que se muestra el módulo.
- Tabla de resultados `resultsDataGridView`: **la misma tabla que los módulos** — primera columna **Módulo** (Clientes / Futuros clientes) y las 13 columnas de specs/002 y specs/003 con los mismos nombres, cabeceras y orden. Solo lectura, selección por fila completa y única, sin cabecera de filas, columnas autoajustadas, celdas siempre blancas y **borde grueso** (3 px, #64748B) en la fila seleccionada, igual que en ClientForm/ContactForm. Sin resaltado de coincidencias: todas las celdas iguales.
- Término vacío o en blanco = lista vacía.

## 4. Coincidencia (SearchService)

- Origen de los datos: **archivos guardados en disco** (`Data/clients.json` y `Data/contacts.json`), leídos en cada búsqueda. Los cambios sin Guardar no aparecen hasta guardarse.
- Coincidencia **total o parcial** (subcadena), **sin mayúsculas/minúsculas** y **sin tildes** ("malaga" encuentra "Málaga", "GARCIA" encuentra "García").
- Campos buscados, en el orden de las columnas de los módulos: Nombre, Primer apellido, Segundo apellido, Documento, Teléfono, Correo electrónico, Dirección, Código postal, Ciudad, Provincia, Estado (Activo/Inactivo; Pendiente/Descartado), Fecha de alta (dd/MM/aaaa; un futuro cliente sin fecha no busca por fecha) y Observaciones.
- Un registro ocupa **una fila**. `SearchMatch` expone el registro completo (`Client`/`Contact`) para rellenar la tabla y conserva internamente los campos coincidentes (`MatchedText`), aunque la tabla no los resalte; el nombre para mostrar es nombre + apellidos no vacíos (razón social en empresas).
- Orden: primero clientes y luego futuros clientes, en el orden de los archivos.

## 5. Navegación al resultado

- Doble clic o **Enter** sobre un resultado: `SearchForm` lanza `NavigateRequested(módulo, Id)` y `MainForm` muestra el módulo correspondiente y marca la fila con la selección existente (borde grueso), desplazando la tabla hasta hacerla visible (`SelectClientById` / `SelectContactById`).
- Si el módulo destino tiene una **edición en curso** (`IsEditing`): aviso y no se navega.
- Si el registro encontrado **no está en la tabla** en pantalla (posibles cambios sin guardar): aviso y no se marca nada.

## 6. Protección de datos

- Un fallo de lectura de los archivos se muestra como error (MessageBox) y deja los resultados vacíos; nunca como una búsqueda correcta sin filas (constitución, punto 5).
- La búsqueda **nunca escribe** en disco.

## 7. Fuera de alcance

- Filtro por columna dentro de cada módulo (specs/002 §5 y specs/003 §7, pendiente).
- Otros módulos futuros: se añadirán a la búsqueda cuando existan.

## 8. Pruebas (xUnit)

`SearchServiceTests` (12): término vacío; parcial sin mayúsculas ni tildes; coincidencia total por documento; coincidencia por cualquier campo (observaciones); futuro cliente sin fecha no coincide con 01/01/0001; estado en español; fecha en dd/MM/aaaa; varios campos coincidentes en una sola fila; clientes antes que futuros clientes; sin resultados; nombre para mostrar con apellidos; registro completo expuesto.
