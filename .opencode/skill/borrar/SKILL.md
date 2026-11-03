---
name: borrar
description: Patrón de CRM1 para eliminar el registro actual de un DataGridView con confirmación previa (identificación solo por nombre, borrado en memoria y definitivo únicamente al pulsar Guardar). Usar al implementar el botón Eliminar en cualquier formulario con tabla de registros (ContactForm y futuros; ClientForm ya lo usa).
---

# Patrón «borrar»: eliminación del registro actual

## Cuándo usarlo

Cuando un formulario con un `DataGridView` de registros deba permitir eliminar
uno de ellos mediante su botón Eliminar. Aplica a cualquier entidad cuya fila
lleve su `Guid` en `Tag`. Antes de tocar código: leer `docs/constitution.md` y
las specs activas; después, sincronizar las specs y `memory.md` (reglas del
proyecto).

## Reglas del patrón

1. **Actúa sobre el registro actual** (skill «seleccionar-cliente»): la fila
   marcada con el clic; sin selección → aviso «Selecciona …». El botón está
   inactivo durante una edición abierta (skill «modificar») y con error de
   carga de datos.
2. **Confirmación Sí/No ANTES de eliminar**, identificando el registro **solo
   por su nombre** (nombre y apellidos; en empresas, la razón social, porque
   los apellidos están vacíos). El mensaje aclara que se quita de la tabla y se
   elimina por completo del archivo al pulsar Guardar.
3. **Eliminación en memoria**: se quita la fila del `DataGridView`; **solo
   Guardar escribe en disco** (constitución, punto 5). Salir sin guardar deja
   el archivo intacto, con la eliminación descartada.
4. **Tras eliminar**: limpiar la marca del registro actual si era esa fila
   (`EnsureCurrent…Exists`) y marcar que hay cambios sin guardar (el aviso al
   cerrar los detecta).
5. **Sin papelera ni deshacer por defecto**: recuperar lo borrado exigiría
   actualizar la spec antes de implementarlo.

## Cómo implantarlo en un formulario nuevo

1. Conectar el evento `Click` del botón Eliminar en el Designer y activarlo
   solo en estado normal (sin edición y sin error de carga) desde
   `UpdateButtonStates`.
2. Localizar la fila por identificador (`FindRowById` del patrón
   «seleccionar-cliente»); sin marca → aviso informativo.
3. Construir el nombre visible de la fila (nombre + apellidos no vacíos) y
   pedir confirmación Sí/No con icono de advertencia.
4. Al confirmar: `Rows.Remove(fila)`, `EnsureCurrent…Exists()` y marcar los
   cambios pendientes.

## Ejemplo de referencia

- `Crm1.WindowsForms/Forms/ClientForm.cs`: `DeleteButton_Click` y
  `RowDisplayName`.
- `specs/002-formulario-clientes.md`: sección 4 (entrada «Eliminar»).
