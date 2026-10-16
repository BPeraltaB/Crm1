# Resumen del Proyecto CRM1

## Descripción
Aplicación de escritorio Windows Forms en C# para gestión de clientes CRM.

## Características Principales
- Gestión de clientes con datos completos
- Personas de contacto por cliente
- Búsqueda y filtrado en todos los módulos
- Historial de actividades por cliente
- Tareas y seguimiento
- Recordatorios
- Estado de oportunidades comerciales
- Presupuestos, pedidos, servicios y ventas

## Almacenamiento
- **Formatos soportados:** JSON, TXT, Binario (futuro: SQL)
- **Ubicación:** Carpeta `Data/` junto al ejecutable
- **Abstracción:** Interfaz `IDataStore` con múltiples implementaciones

## Entorno
- Ejecución en un solo PC
- Datos locales en archivos
- Sin dependencias de red

## Tecnologías
- C# / .NET 8
- Windows Forms
- System.Text.Json

## Archivos de Especificación
1. [Estructura de la solución](01-estructura-solucion.md)
2. [Modelos de datos](02-modelos-datos.md)
3. [Capa de almacenamiento](03-almacenamiento.md)
4. [Formularios y funcionalidades](04-formularios.md)
5. [Orden de implementación](05-orden-implantacion.md)
