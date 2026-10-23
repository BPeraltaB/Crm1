# Estado de CRM1

## Sesión
- Fecha de actualización: 21 de octubre de 2026

## Clientes
- 0 clientes registrados.

## Proyectos
- No hay proyectos registrados todavía.

## Ventas
- No hay ventas registradas todavía.

## Pendiente de revisar
- Siguientes módulos según specs/000-fundamentos.md (sección 8): contactos, oportunidades, tareas, recordatorios, presupuestos, pedidos, servicios, ventas, historial, búsqueda global, formatos TXT/binario y SQL.

## Decisiones
- 16/10/2026: empezar de cero; estructura de 1 proyecto WinForms (Models/Services/Forms) + proyecto de pruebas Crm1.Tests con xUnit; código en inglés; especificación activa = specs/000-fundamentos.md; módulo Clientes implementado con validaciones y almacenamiento JSON.
- 20/10/2026: el usuario decidió volver al esqueleto vacío: se borraron Forms/, Models/, Services/ y el proyecto Crm1.Tests. El commit b2ac189 contiene todo ese código como copia de seguridad recuperable.
- 20/10/2026: se conservan los documentos .md (docs/, specs/, agents.md, constitution.md, memory.md), .gitignore, Crm1.sln y el proyecto WinForms vacío con Program.cs mínimo.
- 20/10/2026: el desarrollo futuro partirá de los documentos .md existentes; no crear formularios ni escribir código sin que el usuario lo pida y exista especificación activa.
- 20/10/2026: specs/000-fundamentos.md marcada como "pendiente de implementar" (diseño activo, sin código) para cumplir el punto 2 de la constitución.
- 20/10/2026: formulario principal con Designer editable y configurado como ventana inicial de la aplicación.
- 21/10/2026: diseño del formulario principal implementado: menú lateral oscuro con `MenuStrip` vertical (`VerticalStackWithOverflow`, 220 px, #2D3748) con items CRM1 (título), Clientes, Búsqueda, Futuros clientes y Salir; área central clara con encabezado "Clientes"; barra de estado con versión y reloj (Timer 30 s). Solo Salir tiene funcionalidad. Renderer oscuro `DarkMenuRenderer` en Form.cs. "Futuros clientes" es módulo nuevo pendiente de especificación; el resto de módulos (contactos, oportunidades, etc.) irá en otro formulario futuro.
- 21/10/2026: eliminado el encabezado del módulo activo del área central (no se quiere el texto "Clientes"); `panelContent` queda como área vacía única.
- 21/10/2026: clase del formulario principal renombrada de Form a MainForm (`Forms/MainForm.cs` + `Forms/MainForm.Designer.cs`, eliminado `Form.resx` vacío) porque el diseñador de VS no carga una clase llamada Form (colisión con System.Windows.Forms.Form).
- 23/10/2026: revisado MainForm con el diseño intacto; no abre ningún formulario (solo Salir) y compila por sí solo. Anulado el plan anterior de eliminar FormClients/editar FormMain: ya no existen.
- 23/10/2026: creada specs/001-formularios-base.md: formularios en blanco de uno en uno cuando el usuario lo pida, patrón XForm.cs + XForm.Designer.cs con InitializeComponent vacío, sin conexión al menú de MainForm (solo diseño) y MainForm siempre intacto. Nomenclatura con sufijo Form: ClientForm, SearchForm, ProspectForm, ContactForm, OpportunityForm, TaskForm, ReminderForm, QuoteForm, OrderForm, ServiceForm, SaleForm, HistoryForm (12 pendientes, ninguno creado).

## Errores que se deben evitar
- No escribir código ni formularios sin petición expresa del usuario y especificación activa que lo ampare.
- No compilar cuando la tarea solo cambia documentación: genera carpetas bin/ y obj/ innecesarias.
- No usar identificadores en español en el código (el código va en inglés; solo comentarios e interfaz en español).
- Cuidado con sesiones paralelas: verificar el estado real del disco y de git antes de tocar archivos.
- No nombrar `Form` a una clase de formulario: el diseñador de Visual Studio no la carga (por eso el principal se llama `MainForm`).
