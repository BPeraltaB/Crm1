# Estructura de la Solución CRM1

## Proyecto

- **Tipo:** Windows Forms
- **Lenguaje:** C#
- **Framework:** .NET 8
- **Nombre:** Crm1.WindowsForms

## Árbol de Archivos

```
Crm1.sln
└── Crm1.WindowsForms/
    ├── Forms/
    │   ├── FormPrincipal.cs          (MDI - menú principal)
    │   ├── FormClientes.cs           (Gestión de clientes)
    │   ├── FormContactos.cs          (Personas de contacto)
    │   ├── FormOportunidades.cs      (Oportunidades comerciales)
    │   ├── FormTareas.cs             (Tareas y seguimiento)
    │   ├── FormRecordatorios.cs      (Recordatorios)
    │   ├── FormPresupuestos.cs       (Presupuestos)
    │   ├── FormPedidos.cs            (Pedidos)
    │   ├── FormServicios.cs          (Servicios)
    │   ├── FormVentas.cs             (Ventas)
    │   ├── FormHistorial.cs          (Historial por cliente)
    │   └── FormBuscador.cs           (Búsqueda global)
    ├── Models/
    │   ├── Cliente.cs
    │   ├── Contacto.cs
    │   ├── Oportunidad.cs
    │   ├── Tarea.cs
    │   ├── Recordatorio.cs
    │   ├── Presupuesto.cs
    │   ├── Pedido.cs
    │   ├── Servicio.cs
    │   └── Venta.cs
    ├── Services/
    │   ├── IDataStore.cs             (Interfaz abstracta)
    │   ├── JsonDataStore.cs          (Implementación JSON)
    │   ├── TxtDataStore.cs           (Implementación TXT)
    │   ├── BinaryDataStore.cs        (Implementación binaria)
    │   └── DataStoreFactory.cs       (Factory para elegir formato)
    ├── Helpers/
    │   ├── DateHelper.cs
    │   └── ValidacionHelper.cs
    └── Data/                         (Carpeta de datos)
        ├── clientes.json
        ├── contactos.json
        ├── oportunidades.json
        ├── tareas.json
        ├── recordatorios.json
        ├── presupuestos.json
        ├── pedidos.json
        ├── servicios.json
        └── ventas.json
```

## Decisiones Técnicas

- .NET 8 (último LTS)
- System.Text.Json para serialización JSON
- BinaryWriter/Reader para formato binario
- StringBuilder para formato TXT
- Sin dependencias externas NuGet (MVP puro)
- Un solo ejecutable portable con datos en subcarpeta `Data/`
