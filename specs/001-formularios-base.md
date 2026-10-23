# Especificación 001: Formularios en blanco

**Estado:** Activa. Rige la creación de formularios vacíos para que el usuario los diseñe después en el diseñador de Visual Studio. La especificación `specs/000-fundamentos.md` sigue vigente para el diseño general del proyecto.

## 1. Objetivo

Crear formularios Windows Forms **en blanco**: sin lógica, sin controles y sin eventos. Cada formulario se entrega vacío para que el usuario diseñe su interfaz (menús, etiquetas, botones, etc.) con el diseñador de Visual Studio.

## 2. Proceso

- Los formularios se crean **de uno en uno**, únicamente cuando el usuario lo pida.
- El formulario principal `MainForm` **queda siempre intacto**: no se modifica ni se le añade código.
- Ningún formulario nuevo se conecta al menú lateral de `MainForm` (el menú es solo diseño; no abre nada).

## 3. Patrón de cada formulario

- Ubicación: `Crm1.WindowsForms/Forms/`.
- Dos archivos por formulario, patrón estándar de Visual Studio:
  - `XForm.cs`: clase parcial con el constructor llamando a `InitializeComponent()`.
  - `XForm.Designer.cs`: `InitializeComponent()` vacío y `Dispose` estándar del diseñador.
- Nomenclatura en inglés con sufijo `Form` (mismo patrón que `MainForm`).
- Código en inglés; comentarios en español (constitución, punto 6).
- Sin lógica de negocio ni acceso a datos (la constitución los exige fuera de los formularios y se especificarán en specs posteriores).

## 4. Formularios pendientes (12)

| # | Módulo (texto en español) | Archivo           | Estado    |
|---|---------------------------|-------------------|-----------|
| 1 | Clientes                  | `ClientForm`      | Pendiente |
| 2 | Búsqueda                  | `SearchForm`      | Pendiente |
| 3 | Futuros clientes          | `ProspectForm`    | Pendiente |
| 4 | Contactos                 | `ContactForm`     | Pendiente |
| 5 | Oportunidades             | `OpportunityForm` | Pendiente |
| 6 | Tareas                    | `TaskForm`        | Pendiente |
| 7 | Recordatorios             | `ReminderForm`    | Pendiente |
| 8 | Presupuestos              | `QuoteForm`       | Pendiente |
| 9 | Pedidos                   | `OrderForm`       | Pendiente |
| 10| Servicios                 | `ServiceForm`     | Pendiente |
| 11| Ventas                    | `SaleForm`        | Pendiente |
| 12| Historial                 | `HistoryForm`     | Pendiente |

Ninguno está creado todavía. Cada uno se marcará como creado en esta tabla al entregarse.

## 5. Fuera de alcance

- Cualquier lógica, evento o acceso a datos dentro de los formularios.
- Modificar `MainForm` o su menú.
- Conectar formularios entre sí.
- Pruebas automatizadas (no hay lógica que probar; la constitución las exige para negocio y persistencia, que se especificarán aparte).
