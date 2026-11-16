# Especificación 008: Copia de seguridad de la base de datos

**Estado:** Activa e **implementada** en su alcance actual: copia diaria automática al arrancar, una por día y rotación de las últimas 7 (decisión del usuario, 16/11/2026).

## 1. Objetivo

Proteger los datos concentrados en `Data/crm1.db` (clientes, futuros clientes, tareas y recordatorios, specs/006): un único archivo dañado o borrado no debe significar la pérdida de todo el historial.

## 2. Comportamiento (implementado)

- **Cuándo**: al arrancar la aplicación, en `Program.Main` **antes** de crear ningún formulario: la base aún no está abierta y la copia es una imagen limpia.
- **Qué**: `Services/BackupService.RunDailyBackup(base, carpeta, hoy)` copia `Data/crm1.db` a `Data/backup/crm1-AAAA-MM-DD.db`.
- **Una copia por día**: la copia del día se refresca (sobreescribe) en cada arranque; los días anteriores quedan intactos.
- **Rotación**: se conservan las **últimas 7 copias** (`BackupService.KeepCount`), contando la del día; la más antigua se retira al crearse una nueva. Los ajenos a `crm1-*.db` no se tocan.
- **Primera ejecución**: si `crm1.db` no existe, no hay copia ni error (nada que copiar).
- **Fallos**: cualquier fallo de copia o rotación se muestra al usuario (diálogo de advertencia al arrancar) **pero nunca impide trabajar**; tampoco se presenta como éxito (constitución, punto 5).
- Restaurar una copia es manual: cerrar la aplicación, copiar el archivo deseado sobre `Data/crm1.db` y volver a arrancar.

## 3. Fuera de alcance

- Copias a otras ubicaciones (nube, otra unidad), copia al cerrar y restauración asistida desde la interfaz.

## 4. Pruebas

- `BackupServiceTests`: crea la copia del día; sin base no copia ni falla; dos ejecuciones el mismo día dejan una sola copia refrescada; la rotación conserva exactamente las últimas 7 (la del 10 retirada al llegar la del 17; la del 11 queda) y los archivos ajenos quedan intactos.
