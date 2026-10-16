# Estado de CRM1

## Sesión
- Fecha de actualización: 16 de octubre de 2026

## Clientes
- 0 clientes registrados.

## Proyectos
- No hay proyectos registrados todavía.

## Ventas
- No hay ventas registradas todavía.

## Pendiente de revisar
- Siguientes módulos según specs/000-fundamentos.md (sección 8): contactos, oportunidades, tareas, recordatorios, presupuestos, pedidos, servicios, ventas, historial, búsqueda global, formatos TXT/binario y SQL.

## Decisiones
- 16/10/2026: empezar de cero; se borró la solución y el proyecto anteriores (el repositorio de GitHub estaba vacío).
- 16/10/2026: estructura de 1 proyecto WinForms (carpetas Models/Services/Forms) + proyecto de pruebas Crm1.Tests con xUnit.
- 16/10/2026: código en inglés; comentarios, interfaz y mensajes en español (constitución, punto 6, actualizado en consecuencia).
- 16/10/2026: especificación activa = specs/000-fundamentos.md.
- 16/10/2026: almacenamiento JSON en Data/clients.json mediante IDataStore/JsonDataStore; validaciones en ClientService (nombre obligatorio, email válido, TaxId único).
- 16/10/2026: primera versión del módulo Clientes implementada (lista, alta, edición, baja, búsqueda y filtro por estado).

## Errores que se deben evitar
- No compilar cuando la tarea solo cambia documentación: genera carpetas bin/ y obj/ innecesarias.
- No usar identificadores en español en el código (el código va en inglés; solo comentarios e interfaz en español).
- Al crear proyectos de pruebas que referencien WinForms, el TargetFramework debe ser net8.0-windows, no net8.0.
