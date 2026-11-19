using Crm1.WindowsForms.Models;
using Task = Crm1.WindowsForms.Models.Task;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Tipo de línea del historial (specs/010): el alta del registro, sus
/// recordatorios, sus tareas, sus presupuestos y sus pedidos y abonos. El
/// orden de los valores es también el de desempate cuando dos líneas caen el
/// mismo día: el dinero queda arriba.
/// </summary>
public enum HistoryEntryKind
{
    /// <summary>Alta del cliente o del futuro cliente.</summary>
    Registration = 0,

    /// <summary>Recordatorio vinculado.</summary>
    Reminder = 1,

    /// <summary>Tarea vinculada.</summary>
    Task = 2,

    /// <summary>Presupuesto vinculado.</summary>
    Quote = 3,

    /// <summary>Pedido del cliente.</summary>
    Order = 4,

    /// <summary>Abono del cliente.</summary>
    CreditNote = 5
}

/// <summary>
/// Una línea del historial de un registro (specs/010 §2): fecha, tipo,
/// detalle y estado ya listos para la interfaz (constitución, punto 6).
/// </summary>
public sealed class HistoryEntry
{
    /// <summary>Fecha (y hora en los recordatorios) de la línea.</summary>
    public DateTime Date { get; init; }

    /// <summary>Tipo de la línea.</summary>
    public HistoryEntryKind Kind { get; init; }

    /// <summary>Texto de la columna Tipo («Presupuesto», «Alta del cliente»…).</summary>
    public string TypeText { get; init; } = string.Empty;

    /// <summary>Texto de la columna Detalle (precio · referencia · descripción…).</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Texto de la columna Estado; vacío cuando el tipo no lo tiene.</summary>
    public string StatusText { get; init; } = string.Empty;
}

/// <summary>
/// Totales del historial de un registro (specs/010 §1): el dinero de pedidos
/// y abonos, el consumo neto y los presupuestos por estado.
/// </summary>
public sealed class HistoryTotals
{
    /// <summary>Presupuestos del registro, en total.</summary>
    public int QuoteCount { get; init; }

    /// <summary>Presupuestos aceptados.</summary>
    public int AcceptedQuotes { get; init; }

    /// <summary>Presupuestos descartados (incluidos los caducados).</summary>
    public int DismissedQuotes { get; init; }

    /// <summary>Presupuestos pendientes.</summary>
    public int PendingQuotes { get; init; }

    /// <summary>Cuántos pedidos tiene el cliente.</summary>
    public int OrderCount { get; init; }

    /// <summary>Suma del dinero de los pedidos.</summary>
    public decimal OrderTotal { get; init; }

    /// <summary>Cuántos abonos tiene el cliente.</summary>
    public int CreditNoteCount { get; init; }

    /// <summary>Suma del dinero de los abonos.</summary>
    public decimal CreditNoteTotal { get; init; }

    /// <summary>
    /// Consumido: el neto de pedidos menos abonos (decisión del usuario:
    /// restando los abonos, pero mostrando también su dinero). Puede ser
    /// negativo si los abonos superan los pedidos.
    /// </summary>
    public decimal Consumed { get; init; }
}

/// <summary>
/// Resultado de construir el historial de un registro: las líneas ordenadas
/// y sus totales (specs/010 §2).
/// </summary>
public sealed class HistoryResult
{
    /// <summary>Líneas del historial, lo más reciente arriba.</summary>
    public List<HistoryEntry> Entries { get; init; } = new();

    /// <summary>Totales para la barra del formulario.</summary>
    public HistoryTotals Totals { get; init; } = new();
}

/// <summary>
/// Agregación del historial de un registro (specs/010): junta tareas,
/// recordatorios, presupuestos y pedidos/abonos del cliente o futuro cliente
/// indicado en una sola lista cronológica y calcula sus totales. Servicio
/// estático y puro (constitución, punto 3): recibe las listas ya cargadas y
/// no toca la base de datos — nada nuevo que guardar.
/// </summary>
public static class HistoryService
{
    /// <summary>
    /// Construye el historial del registro indicado: exactamente uno de
    /// cliente o futuro cliente no nulo (el otro, null). Devuelve las líneas
    /// ordenadas por fecha descendente (empate: el dinero arriba) y los
    /// totales de presupuestos y movimientos.
    /// </summary>
    public static HistoryResult Build(Client? client, Contact? contact,
        IEnumerable<Task> tasks, IEnumerable<Reminder> reminders,
        IEnumerable<Quote> quotes, IEnumerable<Order> orders)
    {
        TaskRelatedType relatedType = client is not null
            ? TaskRelatedType.Client
            : TaskRelatedType.Prospect;
        Guid relatedId = client?.Id ?? contact?.Id ?? Guid.Empty;

        var entries = new List<HistoryEntry>();

        // Alta del registro: solo si tiene fecha (en futuros es opcional).
        DateTime registrationDate = client?.RegistrationDate
            ?? contact?.RegistrationDate ?? default;
        if (registrationDate != default)
        {
            entries.Add(new HistoryEntry
            {
                Date = registrationDate,
                Kind = HistoryEntryKind.Registration,
                TypeText = client is not null ? "Alta del cliente" : "Alta del futuro cliente"
            });
        }

        foreach (Reminder reminder in reminders)
        {
            if (reminder.RelatedId != relatedId || reminder.RelatedType != relatedType)
            {
                continue;
            }

            entries.Add(new HistoryEntry
            {
                Date = reminder.RemindAt,
                Kind = HistoryEntryKind.Reminder,
                TypeText = "Recordatorio",
                Detail = reminder.Subject,
                StatusText = reminder.Status == ReminderStatus.Dismissed ? "Descartado" : "Activo"
            });
        }

        foreach (Task task in tasks)
        {
            if (task.RelatedId != relatedId || task.RelatedType != relatedType)
            {
                continue;
            }

            entries.Add(new HistoryEntry
            {
                Date = task.DueDate,
                Kind = HistoryEntryKind.Task,
                TypeText = "Tarea",
                Detail = task.Description.Length > 0 ? $"{task.Action}: {task.Description}" : task.Action,
                StatusText = TaskStatusText(task.Status)
            });
        }

        foreach (Quote quote in quotes)
        {
            if (quote.RelatedId != relatedId || quote.RelatedType != relatedType)
            {
                continue;
            }

            entries.Add(new HistoryEntry
            {
                Date = quote.QuoteDate,
                Kind = HistoryEntryKind.Quote,
                TypeText = "Presupuesto",
                Detail = QuoteDetail(quote),
                StatusText = quote.Status switch
                {
                    QuoteStatus.Accepted => "Aceptado",
                    QuoteStatus.Dismissed => "Descartado",
                    _ => "Pendiente"
                }
            });
        }

        // Pedidos y abonos: solo de clientes (un futuro con pedido ya sería
        // cliente, specs/009); se filtran por ClientId y nunca aparecen en
        // el historial de un futuro.
        if (client is not null)
        {
            foreach (Order order in orders)
            {
                if (order.ClientId != relatedId)
                {
                    continue;
                }

                bool isCreditNote = order.Kind == OrderKind.CreditNote;
                entries.Add(new HistoryEntry
                {
                    Date = order.OrderDate,
                    Kind = isCreditNote ? HistoryEntryKind.CreditNote : HistoryEntryKind.Order,
                    TypeText = isCreditNote ? "Abono" : "Pedido",
                    // El abono lleva el menos tipográfico «−», como en Pedidos.
                    Detail = (isCreditNote
                        ? $"−{SpanishPrice.Format(order.Amount)} €"
                        : $"{SpanishPrice.Format(order.Amount)} €")
                        + JoinOptional(order.Reference, order.Description)
                });
            }
        }

        // Lo más reciente arriba; el mismo día, el dinero arriba (el orden
        // del enum es el de desempate).
        entries.Sort((a, b) =>
        {
            int byDate = b.Date.CompareTo(a.Date);
            return byDate != 0 ? byDate : b.Kind.CompareTo(a.Kind);
        });

        return new HistoryResult
        {
            Entries = entries,
            Totals = BuildTotals(relatedType, relatedId, quotes, orders)
        };
    }

    // Totales de presupuestos (por estado) y de movimientos; el neto reutiliza
    // OrderService.Totals (specs/009 §4.2) para no duplicar la aritmética.
    private static HistoryTotals BuildTotals(TaskRelatedType relatedType, Guid relatedId,
        IEnumerable<Quote> quotes, IEnumerable<Order> orders)
    {
        int accepted = 0, dismissed = 0, pending = 0;
        foreach (Quote quote in quotes)
        {
            if (quote.RelatedId != relatedId || quote.RelatedType != relatedType)
            {
                continue;
            }

            if (quote.Status == QuoteStatus.Accepted)
            {
                accepted++;
            }
            else if (quote.Status == QuoteStatus.Dismissed)
            {
                dismissed++;
            }
            else
            {
                pending++;
            }
        }

        // Solo clientes tienen movimientos: para un futuro la lista entra
        // vacía porque se filtra antes por ClientId.
        List<Order> clientOrders = relatedType == TaskRelatedType.Client
            ? orders.Where(order => order.ClientId == relatedId).ToList()
            : new List<Order>();
        (int orderCount, decimal orderTotal, int creditNoteCount, decimal creditNoteTotal, decimal net) =
            OrderService.Totals(clientOrders);

        return new HistoryTotals
        {
            QuoteCount = accepted + dismissed + pending,
            AcceptedQuotes = accepted,
            DismissedQuotes = dismissed,
            PendingQuotes = pending,
            OrderCount = orderCount,
            OrderTotal = orderTotal,
            CreditNoteCount = creditNoteCount,
            CreditNoteTotal = creditNoteTotal,
            Consumed = net
        };
    }

    // Detalle de un presupuesto: «precio · referencia · descripción» en
    // formato español, saltando los vacíos.
    private static string QuoteDetail(Quote quote) =>
        $"{SpanishPrice.Format(quote.Amount)} €"
        + JoinOptional(quote.Reference, quote.Description);

    // Sufijo « · referencia · descripción» con solo los fragmentos rellenos.
    private static string JoinOptional(string reference, string description)
    {
        var parts = new[] { reference, description }
            .Where(part => part.Length > 0);
        string joined = string.Join(" · ", parts);
        return joined.Length > 0 ? $" · {joined}" : string.Empty;
    }

    // Etiqueta en español del estado de una tarea (specs/005).
    private static string TaskStatusText(Models.TaskStatus status) => status switch
    {
        Models.TaskStatus.InProgress => "En curso",
        Models.TaskStatus.Completed => "Completada",
        _ => "Pendiente"
    };
}
