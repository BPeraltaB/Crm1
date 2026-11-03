---
name: seleccionar-cliente
description: Patrón de CRM1 para marcar el «registro actual» en formularios con DataGridView (clic en cualquier celda de la fila, rastreo por identificador, borde grueso de selección y expuesto como Current... para acciones posteriores). Usar al añadir acciones que dependan del registro elegido en cualquier formulario con tabla de registros (ClientForm, ContactForm y futuros).
---

# Patrón «seleccionar registro»: marca del registro actual

## Cuándo usarlo

Cuando un formulario con un `DataGridView` de registros deba recordar qué
registro eligió el usuario para acciones posteriores (eliminar, ver sus
contactos, oportunidades, tareas…), o cuando otra parte de la aplicación
necesite saber el registro activo. Aplica a cualquier entidad cuya fila lleve
su `Guid` en `Tag`: clientes (`CurrentClient`), futuros clientes
(`CurrentContact`), y las que vengan. Antes de tocar código: leer
`docs/constitution.md` y las specs activas; después, sincronizar las specs y
`memory.md` (reglas del proyecto).

## Reglas del patrón

1. **Clic en una celda marca el registro**: al hacer clic en cualquier celda de
   una fila, el registro de esa fila pasa a ser el «registro actual». El
   seguimiento se hace con `SelectionChanged` (cubre también el teclado), no
   solo con `CellClick`. Sin fila seleccionada no hay registro actual
   (`Current… = null`), nunca un valor inventado.
2. **Marca visual = borde grueso, sin resaltado azul**: las celdas se ven
   siempre blancas (`SelectionBackColor` blanco, decisión del usuario) y la
   fila seleccionada se marca con un **borde grueso (3 px, gris pizarra
   #64748B) dibujado con `RowPostPaint` alrededor de toda la fila**; se
   distingue del borde rojo fino (2 px) de los errores de validación. La marca
   acompaña también a la fila en edición (la selección la sigue).
3. **Un solo registro actual**: `MultiSelect = false`.
4. **Rastreo por identificador, nunca por índice** (mismo criterio que la
   skill «modificar»): al filtrar, buscar, ordenar o recargar, la marca se
   restablece buscando la fila cuyo Id coincida.
5. **Si el registro marcado desaparece** (eliminado o no pasa el filtro), la
   marca se limpia de forma explícita; sin identificadores huérfanos.
6. **Durante la edición de una fila** (skill «modificar»), el registro actual
   es el de esa fila y la selección queda fijada en ella: las demás filas
   están deshabilitadas y cualquier clic o teclado devuelve la selección a la
   fila en edición; al guardar o cancelar se aplica la regla 4.
7. **Exposición hacia fuera**: `Current…` (o `Current…Id`) y, si hace falta,
   evento `Current…Changed`. La lógica de qué hacer con él vive fuera del
   formulario (constitución, punto 3).
8. **Sin persistencia por defecto**: la marca vive en memoria; recordarla
   entre sesiones exigiría actualizar la spec antes de implementarlo.
9. **Error de carga de datos**: sin marca y sin selección (constitución,
   punto 5).

## Cómo implantarlo en un formulario nuevo

1. Campo privado `Guid? _current…Id` y propiedades públicas `Current…`
   (entidad reconstruida de la fila) y `Current…Id`.
2. Manejador de `SelectionChanged` con guarda `_loading`: la fila seleccionada
   marca su registro (`Tag` como `Guid`); sin selección → `null`.
3. Helper `FindRowById(Guid?)`: busca la fila por identificador (nunca por
   índice) y helper `EnsureCurrent…Exists()`: limpia la marca si la fila ya
   no existe.
4. Manejador de `RowPostPaint`: borde grueso (3 px, #64748B) alrededor de la
   fila seleccionada; celdas siempre blancas.
5. Tras la carga inicial: `_current…Id = null` y `ClearSelection()` — no hay
   marca hasta que el usuario haga clic.
6. Al guardar con éxito: fijar la marca en el identificador recién asignado de
   la fila editada; al cancelar o eliminar filas: `EnsureCurrent…Exists()`.
7. Conectar los eventos en el Designer y usar el registro actual en los
   botones de acción (con aviso «Selecciona …» si no hay marca).

## Ejemplo de referencia

- `Crm1.WindowsForms/Forms/ClientForm.cs`: patrón implementado — `CurrentClient`/
  `CurrentClientId`, el manejador de `SelectionChanged`, `FindRowById` (búsqueda por
  identificador), `EnsureCurrentClientExists` (limpieza de marcas huérfanas) y
  `ClientsDataGridView_RowPostPaint` (borde grueso de la fila seleccionada);
  `ModifyButton_Click` actúa sobre el cliente actual.
- `specs/002-formulario-clientes.md`: sección 4 (entrada «Cliente actual»).
