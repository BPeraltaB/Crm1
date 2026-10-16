# Modelos de Datos - CRM1

## Cliente

| # | Campo        | Tipo       | Descripción                        |
|---|-------------|------------|------------------------------------|
| 1 | Id          | Guid       | Identificador único                |
| 2 | Nombre      | string     | Nombre/Razón social                |
| 3 | CIF_NIF     | string     | Documento fiscal                   |
| 4 | Direccion   | string     | Dirección completa                 |
| 5 | CodigoPostal| string     | Código postal                      |
| 6 | Ciudad      | string     | Ciudad                             |
| 7 | Provincia   | string     | Provincia                          |
| 8 | Telefono    | string     | Teléfono principal                 |
| 9 | Email       | string     | Correo electrónico                 |
| 10| FechaAlta   | DateTime   | Fecha de alta en el sistema        |
| 11| Observaciones| string    | Notas adicionales                  |
| 12| Estado      | enum       | Activo / Inactivo                  |

### Enum EstadoCliente
```csharp
public enum EstadoCliente
{
    Activo,
    Inactivo
}
```

---

## Contacto

| # | Campo        | Tipo       | Descripción                        |
|---|-------------|------------|------------------------------------|
| 1 | Id          | Guid       | Identificador único                |
| 2 | ClienteId   | Guid       | FK → Cliente                       |
| 3 | Nombre      | string     | Nombre completo                    |
| 4 | Cargo       | string     | Cargo / Puesto                     |
| 5 | Telefono    | string     | Teléfono de contacto               |
| 6 | Email       | string     | Correo electrónico                 |
| 7 | EsPrincipal | bool       | ¿Es el contacto principal?         |

---

## Oportunidad

| # | Campo            | Tipo       | Descripción                    |
|---|-----------------|------------|--------------------------------|
| 1 | Id              | Guid       | Identificador único            |
| 2 | ClienteId       | Guid       | FK → Cliente                   |
| 3 | Titulo          | string     | Nombre de la oportunidad       |
| 4 | Descripcion     | string     | Detalles                       |
| 5 | ValorEstimado   | decimal    | Valor económico estimado       |
| 6 | Estado          | enum       | Nueva / EnProgreso / Ganada / Perdida |
| 7 | FechaCreacion   | DateTime   | Fecha de creación              |
| 8 | FechaCierre     | DateTime?  | Fecha de cierre prevista       |

### Enum EstadoOportunidad
```csharp
public enum EstadoOportunidad
{
    Nueva,
    EnProgreso,
    Ganada,
    Perdida
}
```

---

## Tarea

| # | Campo          | Tipo       | Descripción                    |
|---|---------------|------------|--------------------------------|
| 1 | Id            | Guid       | Identificador único            |
| 2 | ClienteId     | Guid       | FK → Cliente                   |
| 3 | OportunidadId | Guid?      | FK → Oportunidad (opcional)    |
| 4 | Titulo        | string     | Nombre de la tarea             |
| 5 | Descripcion   | string     | Detalles                       |
| 6 | FechaLimite   | DateTime   | Fecha límite                   |
| 7 | Estado        | enum       | Pendiente / EnCurso / Completada |
| 8 | Prioridad     | enum       | Baja / Media / Alta            |

### Enum EstadoTarea
```csharp
public enum EstadoTarea
{
    Pendiente,
    EnCurso,
    Completada
}
```

### Enum PrioridadTarea
```csharp
public enum PrioridadTarea
{
    Baja,
    Media,
    Alta
}
```

---

## Recordatorio

| # | Campo      | Tipo       | Descripción                    |
|---|-----------|------------|--------------------------------|
| 1 | Id        | Guid       | Identificador único            |
| 2 | ClienteId | Guid       | FK → Cliente                   |
| 3 | Titulo    | string     | Asunto del recordatorio        |
| 4 | Mensaje   | string     | Contenido del recordatorio     |
| 5 | FechaHora | DateTime   | Fecha y hora programada        |
| 6 | Activo    | bool       | ¿Está activo?                  |

---

## Presupuesto

| # | Campo          | Tipo             | Descripción                    |
|---|---------------|------------------|--------------------------------|
| 1 | Id            | Guid             | Identificador único            |
| 2 | ClienteId     | Guid             | FK → Cliente                   |
| 3 | OportunidadId | Guid?            | FK → Oportunidad (opcional)    |
| 4 | Numero        | string           | Número secuencial              |
| 5 | Lineas        | List<LineaDoc>   | Líneas del presupuesto         |
| 6 | Total         | decimal          | Total calculado                |
| 7 | Fecha         | DateTime         | Fecha de emisión               |
| 8 | Estado        | enum             | Borrador / Enviado / Aceptado / Rechazado |

### Enum EstadoPresupuesto
```csharp
public enum EstadoPresupuesto
{
    Borrador,
    Enviado,
    Aceptado,
    Rechazado
}
```

---

## Pedido

| # | Campo          | Tipo             | Descripción                    |
|---|---------------|------------------|--------------------------------|
| 1 | Id            | Guid             | Identificador único            |
| 2 | ClienteId     | Guid             | FK → Cliente                   |
| 3 | PresupuestoId | Guid?            | FK → Presupuesto (opcional)    |
| 4 | Numero        | string           | Número secuencial              |
| 5 | Lineas        | List<LineaDoc>   | Líneas del pedido              |
| 6 | Total         | decimal          | Total calculado                |
| 7 | Fecha         | DateTime         | Fecha del pedido               |
| 8 | Estado        | enum             | Pendiente / Entregado / Cancelado |

### Enum EstadoPedido
```csharp
public enum EstadoPedido
{
    Pendiente,
    Entregado,
    Cancelado
}
```

---

## Servicio

| # | Campo          | Tipo    | Descripción                    |
|---|---------------|---------|--------------------------------|
| 1 | Id            | Guid    | Identificador único            |
| 2 | Nombre        | string  | Nombre del servicio/producto   |
| 3 | Descripcion   | string  | Detalles                       |
| 4 | PrecioUnitario| decimal | Precio por unidad              |
| 5 | Tipo          | enum    | Producto / Servicio            |

### Enum TipoServicio
```csharp
public enum TipoServicio
{
    Producto,
    Servicio
}
```

---

## Venta

| # | Campo      | Tipo             | Descripción                    |
|---|-----------|------------------|--------------------------------|
| 1 | Id        | Guid             | Identificador único            |
| 2 | ClienteId | Guid             | FK → Cliente                   |
| 3 | PedidoId  | Guid?            | FK → Pedido (opcional)         |
| 4 | Numero    | string           | Número secuencial              |
| 5 | Lineas    | List<LineaDoc>   | Líneas de la venta             |
| 6 | Total     | decimal          | Total calculado                |
| 7 | Fecha     | DateTime         | Fecha de la venta              |
| 8 | Estado    | enum             | Completada / Pendiente / Anulada |

### Enum EstadoVenta
```csharp
public enum EstadoVenta
{
    Completada,
    Pendiente,
    Anulada
}
```

---

## LineaDoc (compartida)

| # | Campo        | Tipo    | Descripción                    |
|---|-------------|---------|--------------------------------|
| 1 | ServicioId  | Guid    | FK → Servicio                  |
| 2 | Descripcion | string  | Descripción de la línea        |
| 3 | Cantidad    | int     | Cantidad                       |
| 4 | PrecioUnitario| decimal| Precio unitario                |
| 5 | Total       | decimal | Cantidad × PrecioUnitario      |
