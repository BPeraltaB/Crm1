# Estado de CRM1

## Sesión
- Fecha de actualización: 20 de octubre de 2026

## Clientes
- 0 clientes registrados.

## Proyectos
- No hay proyectos registrados todavía.

## Ventas
- No hay ventas registradas todavía.

## Pendiente de revisar
- specs/000-fundamentos.md quedó desactualizada tras el borrado: describe el módulo Clientes como implementado. Actualizar su estado (marcar como pendiente) cuando se retome el desarrollo.
- Siguientes módulos según specs/000-fundamentos.md (sección 8): contactos, oportunidades, tareas, recordatorios, presupuestos, pedidos, servicios, ventas, historial, búsqueda global, formatos TXT/binario y SQL.

## Decisiones
- 16/10/2026: empezar de cero; estructura de 1 proyecto WinForms (Models/Services/Forms) + proyecto de pruebas Crm1.Tests con xUnit; código en inglés; especificación activa = specs/000-fundamentos.md; módulo Clientes implementado con validaciones y almacenamiento JSON.
- 20/10/2026: el usuario decidió volver al esqueleto vacío: se borraron Forms/, Models/, Services/ y el proyecto Crm1.Tests. El commit b2ac189 contiene todo ese código como copia de seguridad recuperable.
- 20/10/2026: se conservan los documentos .md (docs/, specs/, agents.md, constitution.md, memory.md), .gitignore, Crm1.sln y el proyecto WinForms vacío con Program.cs mínimo.
- 20/10/2026: el desarrollo futuro partirá de los documentos .md existentes; no crear formularios ni escribir código sin que el usuario lo pida y exista especificación activa.

## Errores que se deben evitar
- No escribir código ni formularios sin petición expresa del usuario y especificación activa que lo ampare.
- No compilar cuando la tarea solo cambia documentación: genera carpetas bin/ y obj/ innecesarias.
- No usar identificadores en español en el código (el código va en inglés; solo comentarios e interfaz en español).
- Cuidado con sesiones paralelas: verificar el estado real del disco y de git antes de tocar archivos.
