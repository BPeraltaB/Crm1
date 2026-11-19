using Crm1.WindowsForms.Models;
using Task = Crm1.WindowsForms.Models.Task;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Tipo de línea del historial (specs/010): el alta del registro, las tareas
/// y recordatorios que siguen pendientes, y los resúmenes por año de lo que
/// ya es historial (consumo, presupuestos, tareas completadas y recordatorios
/// descartados). El orden de los valores es también el de desempate cuando
/// dos líneas caen el mismo día: lo resumido y el dinero quedan arriba.
/// </summary>
public enum HistoryEntryKind
{
    /// <summary>Alta del cliente o del futuro cliente.</summary>
    Registration = 0,

    /// <summary>Recordatorio activo (todavía tiene que avisar).</summary>
    Reminder = 1,

    /// <summary>Tarea pendiente o en curso.</summary>
    Task = 2,

    /// <summary>Resumen anual de recordatorios descartados.</summary>
    ReminderSummary = 3,

    /// <summary>Resumen anual de tareas completadas.</summary>
    TaskSummary = 4,

    /// <summary>Resumen anual de presupuestos por estado.</summary>
    QuoteSummary = 5,

    /// <summary>Resumen anual del consumo (pedidos, abonos y neto).</summary>
    ConsumptionSummary = 6
}

/// <summary>
/// Una línea del historial de un registro (specs/010 §2): fecha, tipo,
/// detalle y estado ya listos para la interfaz (constitución, punto 6).
/// </summary>
public sealed class HistoryEntry
{
    /// <summary>Fecha (y hora en los recordatorios) de la línea; las de
    /// resumen anual llevan el 31/12 del año a las 23:59:59 para encabezar
    /// su bloque, aunque se muestran solo con el año.</summary>
    public DateTime Date { get; init; }

    /// <summary>Tipo de la línea.</summary>
    public HistoryEntryKind Kind { get; init; }

    /// <summary>Texto de la columna Tipo («Presupuestos 2026», «Alta del cliente»…).</summary>
    public string TypeText { get; init; } = string.Empty;

    /// <summary>Texto de la columna Detalle (resumen del año, asunto…).</summary>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Texto de la columna Estado; vacío cuando el tipo no lo tiene.</summary>
    public string StatusText { get; init; } = string.Empty;
}

/// <summary>
/// Totales del historial de un registro (specs/010 §1): el dinero de pedidos
/// y abonos, el consumo neto y los presupuestos por estado, de toda su
/// historia (la barra del formulario; los desgloses por año van en la tabla).
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

    /// <summary>Totales globales para la barra del formulario.</summary>
    public HistoryTotals Totals { get; init; } = new();
}

/// <summary>
/// Agregación del historial de un registro (specs/010): junta tareas,
/// recordatorios, presupuestos y pedidos/abonos del cliente o futuro cliente
/// indicado. Se detallan solo las tareas y recordatorios que faltan (decision
/// del usuario, 19/11): el resto se resume por año — consumo, presupuestos
/// por estado, tareas completadas y recordatorios descartados — con una
/// línea por año a la cabeza de cada bloque. Servicio estático y puro
/// (constitución, punto 3): recibe las listas ya cargadas y no toca la base
/// de datos — nada nuevo que guardar.
/// </summary>
public static class HistoryService
{
    /// <summary>
    /// Construye el historial del registro indicado: exactamente uno de
    /// cliente o futuro cliente no nulo (el otro, null). Devuelve las líneas
    /// ordenadas por fecha descendente (empate: lo resumido y el dinero
    /// arriba) y los totales globales.
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

        // Recordatorios: los Activos se detallan (aunque estén vencidos:
        // siguen faltando); los Descartados se cuentan por año.
        var dismissedRemindersByYear = new Dictionary<int, int>();
        foreach (Reminder reminder in reminders)
        {
            if (reminder.RelatedId != relatedId || reminder.RelatedType != relatedType)
            {
                continue;
            }

            if (reminder.Status == ReminderStatus.Dismissed)
            {
                CountByYear(dismissedRemindersByYear, reminder.RemindAt.Year);
                continue;
            }

            entries.Add(new HistoryEntry
            {
                Date = reminder.RemindAt,
                Kind = HistoryEntryKind.Reminder,
                TypeText = "Recordatorio",
                Detail = reminder.Subject,
                StatusText = "Activo"
            });
        }

        // Tareas: las Pendientes y En curso se detallan; las Completadas se
        // cuentan por año.
        var completedTasksByYear = new Dictionary<int, int>();
        foreach (Task task in tasks)
        {
            if (task.RelatedId != relatedId || task.RelatedType != relatedType)
            {
                continue;
            }

            if (task.Status == Models.TaskStatus.Completed)
            {
                CountByYear(completedTasksByYear, task.DueDate.Year);
                continue;
            }

            entries.Add(new HistoryEntry
            {
                Date = task.DueDate,
                Kind = HistoryEntryKind.Task,
                TypeText = "Tarea",
                Detail = task.Description.Length > 0 ? $"{task.Action}: {task.Description}" : task.Action,
                StatusText = task.Status == Models.TaskStatus.InProgress ? "En curso" : "Pendiente"
            });
        }

        // Presupuestos: todos a su resumen anual por estado (aceptados,
        // descartados y pendientes; las partes a cero no se muestran).
        var quotesByYear = new Dictionary<int, (int Accepted, int Dismissed, int Pending)>();
        foreach (Quote quote in quotes)
        {
            if (quote.RelatedId != relatedId || quote.RelatedType != relatedType)
            {
                continue;
            }

            (int accepted, int dismissed, int pending) = quotesByYear.GetValueOrDefault(quote.QuoteDate.Year);
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
            quotesByYear[quote.QuoteDate.Year] = (accepted, dismissed, pending);
        }

        // Pedidos y abonos: solo de clientes (un futuro con pedido ya sería
        // cliente, specs/009); se agrupan por año para el resumen de consumo.
        var ordersByYear = new Dictionary<int, List<Order>>();
        if (client is not null)
        {
            foreach (Order order in orders)
            {
                if (order.ClientId != relatedId)
                {
                    continue;
                }

                if (!ordersByYear.TryGetValue(order.OrderDate.Year, out List<Order>? yearOrders))
                {
                    yearOrders = new List<Order>();
                    ordersByYear[order.OrderDate.Year] = yearOrders;
                }
                yearOrders.Add(order);
            }
        }

        // Líneas de resumen anual: encabezan su bloque con el 31/12 a última
        // hora (se muestran solo con el año) — consumo, presupuestos, tareas
        // completadas y recordatorios descartados, solo los años con contenido.
        foreach ((int year, List<Order> yearOrders) in ordersByYear)
        {
            (int orderCount, decimal orderTotal, int creditNoteCount, decimal creditNoteTotal, decimal net) =
                OrderService.Totals(yearOrders);
            entries.Add(new HistoryEntry
            {
                Date = EndOfYear(year),
                Kind = HistoryEntryKind.ConsumptionSummary,
                TypeText = $"Consumo {year}",
                Detail = ConsumptionText(orderCount, orderTotal, creditNoteCount, creditNoteTotal, net)
            });
        }
        foreach ((int year, (int accepted, int dismissed, int pending)) in quotesByYear)
        {
            entries.Add(new HistoryEntry
            {
                Date = EndOfYear(year),
                Kind = HistoryEntryKind.QuoteSummary,
                TypeText = $"Presupuestos {year}",
                Detail = QuoteSummaryText(accepted + dismissed + pending, accepted, dismissed, pending)
            });
        }
        foreach ((int year, int completed) in completedTasksByYear)
        {
            entries.Add(new HistoryEntry
            {
                Date = EndOfYear(year),
                Kind = HistoryEntryKind.TaskSummary,
                TypeText = $"Tareas {year}",
                Detail = $"{completed} {(completed == 1 ? "completada" : "completadas")}"
            });
        }
        foreach ((int year, int dismissed) in dismissedRemindersByYear)
        {
            entries.Add(new HistoryEntry
            {
                Date = EndOfYear(year),
                Kind = HistoryEntryKind.ReminderSummary,
                TypeText = $"Recordatorios {year}",
                Detail = $"{dismissed} {(dismissed == 1 ? "descartado" : "descartados")}"
            });
        }

        // Lo más reciente arriba; el mismo día, lo resumido y el dinero
        // arriba (el orden del enum es el de desempate).
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

    // Totales globales de presupuestos (por estado) y de movimientos; el
    // neto reutiliza OrderService.Totals (specs/009 §4.2) para no duplicar
    // la aritmética.
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

    // Detalle del resumen anual de consumo: «2 pedidos — 1.000,00 € ·
    // 1 abono — −150,00 € · Consumido: 850,00 €», sin las partes a cero.
    private static string ConsumptionText(int orderCount, decimal orderTotal,
        int creditNoteCount, decimal creditNoteTotal, decimal net)
    {
        var parts = new List<string>();
        if (orderCount > 0)
        {
            parts.Add($"{orderCount} {(orderCount == 1 ? "pedido" : "pedidos")} — {SpanishPrice.Format(orderTotal)} €");
        }
        if (creditNoteCount > 0)
        {
            parts.Add($"{creditNoteCount} {(creditNoteCount == 1 ? "abono" : "abonos")} — −{SpanishPrice.Format(creditNoteTotal)} €");
        }
        parts.Add($"Consumido: {FormatNet(net)} €");
        return string.Join(" · ", parts);
    }

    // Detalle del resumen anual de presupuestos: «4 — 2 aceptados ·
    // 1 descartado · 1 pendiente», sin las partes a cero.
    private static string QuoteSummaryText(int total, int accepted, int dismissed, int pending)
    {
        var parts = new List<string>();
        if (accepted > 0)
        {
            parts.Add($"{accepted} {(accepted == 1 ? "aceptado" : "aceptados")}");
        }
        if (dismissed > 0)
        {
            parts.Add($"{dismissed} {(dismissed == 1 ? "descartado" : "descartados")}");
        }
        if (pending > 0)
        {
            parts.Add($"{pending} {(pending == 1 ? "pendiente" : "pendientes")}");
        }
        return parts.Count > 0 ? $"{total} — {string.Join(" · ", parts)}" : total.ToString();
    }

    /// <summary>
    /// Cifra neta con el menos tipográfico «−» cuando es negativo, igual que
    /// en Pedidos (specs/009 §4.2); lo usan los resúmenes anuales y la barra
    /// de totales del formulario.
    /// </summary>
    internal static string FormatNet(decimal net) => net < 0
        ? $"−{SpanishPrice.Format(-net)}"
        : SpanishPrice.Format(net);

    // Marca de cierre de año para las líneas de resumen: encabezan su bloque.
    private static DateTime EndOfYear(int year) => new(year, 12, 31, 23, 59, 59);

    // Cuenta una unidad más para un año del diccionario de resúmenes.
    private static void CountByYear(Dictionary<int, int> counts, int year)
    {
        counts[year] = counts.GetValueOrDefault(year) + 1;
    }
}
