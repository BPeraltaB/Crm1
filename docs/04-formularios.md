# Formularios y Funcionalidades - CRM1

## FormPrincipal
- Menú lateral con acceso a todos los módulos
- Barra de estado con información del sistema
- Acceso rápido: Clientes, Búsqueda

## FormClientes
- **Lista:** DataGridView con todos los clientes
- **Búsqueda:** Por nombre, CIF/NIF, ciudad
- **Filtros:** Por estado (Activo/Inactivo), provincia
- **CRUD:** Alta, Baja lógica, Modificación
- **Campos:** Todos los del modelo Cliente
- **Validaciones:** Nombre obligatorio, CIF/NIF único, email válido

## FormContactos
- Lista filtrada por cliente seleccionado
- Marcar contacto principal
- CRUD completo

## FormOportunidades
- Lista con estados visuales (colores por estado)
- Valor estimado total
- Filtrar por cliente/estado
- CRUD completo

## FormTareas
- Lista de tareas pendientes
- Asignar a cliente/oportunidad
- Cambiar estado con un clic
- Prioridad visual (colores)

## FormRecordatorios
- Lista con alertas visuales
- Fecha/hora de vencimiento
- Marcar como leído/activo

## FormPresupuestos
- Crear/editar con líneas de detalle
- Seleccionar servicios como líneas
- Calcular total automáticamente
- Cambiar estado

## FormPedidos
- Similar a presupuestos
- Vincular a presupuesto aceptado
- Estados: Pendiente, Entregado, Cancelado

## FormServicios
- Catálogo de servicios/productos con precio
- CRUD completo
- Filtrar por tipo

## FormVentas
- Registro de ventas
- Vincular a pedido
- Estados: Completada, Pendiente, Anulada

## FormHistorial
- Vista consolidada de todas las actividades de un cliente
- Cronología: contactos, oportunidades, tareas, ventas

## FormBuscador
- Búsqueda global por texto en todos los módulos
- Resultados agrupados por tipo
