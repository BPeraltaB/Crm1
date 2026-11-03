---
name: modificar
description: Patrón de CRM1 para la edición de filas en formularios con DataGridView (tabla en solo lectura por defecto, botones Nuevo/Modificar que activan una fila, Cancelar con instantánea y Guardar que cierra la edición). Usar al implementar formularios con tablas de registros (ProspectForm y futuros) o al modificar ClientForm.
---

# Patrón «modificar»: edición de filas en formularios con DataGridView

## Cuándo usarlo

Al crear o ampliar un formulario de CRM1 con una tabla de registros que el usuario
deba rellenar o corregir (ClientForm ya lo usa; servirá para ProspectForm y el
resto de módulos). Antes de tocar código: leer `docs/constitution.md` y las specs
activas; después, sincronizar las specs y `memory.md` (reglas del proyecto).

## Reglas del patrón

1. **Solo lectura por defecto**: el `DataGridView` es editable a nivel de control,
   pero **cada fila nace con `ReadOnly = true`**. El `ReadOnly` del control nunca
   se pone a `true`, porque bloquearía todas las celdas sin excepción.
2. **Una sola fila en edición**, rastreada **por referencia** a la fila (nunca por
   índice: la ordenación o inserciones lo invalidarían).
3. **Botonera por estado**:
   - Sin edición: Nuevo, Modificar y Guardar activos; Cancelar inactivo; los
     botones sin implementar (p. ej. Eliminar) visibles pero inactivos.
   - En edición: Nuevo y Modificar inactivos (evita estados con errores);
     Cancelar activo; Guardar sigue activo.
   - Error de carga de datos (catálogo o JSON ilegible): todo inactivo; el error
     se muestra y nunca se sobrescribe el archivo (constitución, punto 5).
4. **Nuevo**: inserta una fila vacía como primera fila, la activa y entra a editar
   la primera celda.
5. **Modificar**: exige fila seleccionada (aviso si no la hay); activa sus celdas
   y coloca el cursor en la primera.
6. **Cancelar**: al activar una fila se toma una **instantánea de los valores de
   sus celdas**. Cancelar elimina una fila nueva (sin `Tag` de identificador) o
   restaura la instantánea de una modificada. Después se recalcula el indicador
   de "cambios sin guardar" comparando la tabla con la última copia guardada
   (por identificador, sin depender del orden), para que el aviso al cerrar nunca
   mienta.
7. **Guardar**: valida con el servicio (las reglas viven fuera del formulario,
   constitución punto 3). Con éxito: escribe de forma atómica, asigna ids a las
   filas nuevas, retira de la vista las filas totalmente vacías, cierra la
   edición y vuelve a solo lectura. Con errores: la edición sigue abierta (solo
   se cierra con un guardado correcto o con Cancelar).
8. **Ordenación por cabeceras bloqueada durante la edición** (`SortMode =
   NotSortable` en las columnas de texto; las de desplegable nunca ordenan) y
   restaurada al terminar.
9. Celdas siempre blancas (`SelectionBackColor` blanco) y errores marcados con
   **borde rojo** dibujado en `CellPainting` sobre un conjunto de celdas con
   error; el borde desaparece al editar la celda. Mensajes de error cortos, uno
   por línea.
10. **Filas deshabilitadas durante la edición**: al abrir la sesión, las demás
    filas quedan deshabilitadas — texto atenuado en gris (#9CA3AF) y sin
    selección posible (ni clic ni teclado: `SelectionChanged` devuelve la
    selección y la celda actual a la fila activa, con guarda anti-reentrada).
    Se rehabilitan (color normal y selección) al cerrar la sesión: guardado
    correcto o Cancelar.

## Ejemplo de referencia

- `Crm1.WindowsForms/Forms/ClientForm.cs`: `UpdateButtonStates`,
  `ActivateRowEditing`, `EndEditingSession`, `SetColumnsSortable`,
  `NewButton_Click`, `ModifyButton_Click`, `CancelButton_Click`,
  `GridDiffersFromSaved`.
- `specs/002-formulario-clientes.md` (secciones 3 y 4).
