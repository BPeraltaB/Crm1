using Crm1.WindowsForms.Models;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de un presupuesto sobre un campo concreto.
/// </summary>
public sealed class QuoteValidationError
{
    /// <summary>
    /// Campo del modelo afectado (nombre de la propiedad, en inglés).
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Mensaje de error para el usuario, en español.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Error de validación asociado a una fila de la tabla de presupuestos.
/// </summary>
public sealed class QuoteRowValidationError
{
    /// <summary>
    /// Índice (base cero) del presupuesto dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public QuoteValidationError Error { get; init; } = new();
}

/// <summary>
/// Reglas de negocio de los presupuestos (specs/009): fecha obligatoria,
/// precio mayor que cero, estado del desplegable y vínculo obligatorio con un
/// cliente o futuro cliente existente. Vive fuera de los formularios
/// (constitución, punto 3).
/// </summary>
public sealed class QuoteService
{
    // Registros vinculables conocidos: Id → tipo (lo construye el formulario
    // con los clientes y futuros clientes de la base).
    private readonly IReadOnlyDictionary<Guid, TaskRelatedType> _relatedRecords;

    /// <summary>
    /// Crea el servicio con los registros vinculables existentes en la base.
    /// </summary>
    public QuoteService(IReadOnlyDictionary<Guid, TaskRelatedType> relatedRecords)
    {
        _relatedRecords = relatedRecords;
    }

    /// <summary>
    /// Valida un presupuesto concreto; devuelve el error del primer campo que
    /// falla o null si es correcto. La fecha no puede ser anterior a hoy
    /// SOLO al dar de alta (isNewCreation): modificar un registro existente
    /// nunca se bloquea por su fecha histórica, para poder aceptar
    /// presupuestos pendientes antiguos (decisión del usuario, 18/11).
    /// </summary>
    public QuoteValidationError? Validate(Quote quote, bool isNewCreation = false)
    {
        if (quote.QuoteDate == default)
        {
            return new QuoteValidationError
            {
                Field = nameof(Quote.QuoteDate),
                Message = "La celda está vacía."
            };
        }

        if (isNewCreation && quote.QuoteDate.Date < DateTime.Today)
        {
            return new QuoteValidationError
            {
                Field = nameof(Quote.QuoteDate),
                Message = "La fecha no puede ser anterior a hoy."
            };
        }

        if (quote.Amount <= 0)
        {
            return new QuoteValidationError
            {
                Field = nameof(Quote.Amount),
                Message = quote.Amount == 0
                    ? "La celda está vacía."
                    : "El precio debe ser mayor que cero."
            };
        }

        if (!_relatedRecords.TryGetValue(quote.RelatedId, out TaskRelatedType type)
            || type != quote.RelatedType)
        {
            return new QuoteValidationError
            {
                Field = nameof(Quote.RelatedId),
                Message = "Debes elegir un cliente o futuro cliente."
            };
        }

        return null;
    }

    /// <summary>
    /// Valida todos los presupuestos y devuelve un error por fila defectuosa.
    /// </summary>
    public List<QuoteRowValidationError> ValidateQuotes(List<Quote> quotes)
    {
        var errors = new List<QuoteRowValidationError>();
        for (int i = 0; i < quotes.Count; i++)
        {
            QuoteValidationError? error = Validate(quotes[i]);
            if (error is not null)
            {
                errors.Add(new QuoteRowValidationError { RowIndex = i, Error = error });
            }
        }
        return errors;
    }

    // ------------------------------------------------------------------
    // Caducidad y avisos automáticos (specs/009 §10, decisión del usuario
    // 18/11/2026): validez de 30 días, avisos derivados en los días 15, 23,
    // 27, 28, 29 y 30 mientras el presupuesto siga Pendiente, y paso a
    // Descartado en el día 31.
    // ------------------------------------------------------------------

    /// <summary>Días de validez de un presupuesto (el 31 pasa a Descartado).</summary>
    public const int ValidityDays = 30;

    // Días transcurridos desde la fecha del presupuesto en los que salta el
    // aviso de caducidad, con los días que faltan para caducar.
    private static readonly Dictionary<int, int> ExpiryNoticeDays = new()
    {
        [15] = 15,
        [23] = 7,
        [27] = 3,
        [28] = 2,
        [29] = 1,
        [30] = 0
    };

    /// <summary>
    /// Indica si un presupuesto pasa el filtro del listado (specs/009 §3):
    /// coincidencia parcial, sin mayúsculas ni tildes, en las columnas
    /// visibles — relatedName es el texto del vínculo tal como se muestra.
    /// Un texto vacío deja pasar todo.
    /// </summary>
    public static bool MatchesFilter(Quote quote, string? text, string relatedName)
    {
        string normalized = SearchService.NormalizeText(text ?? string.Empty);
        if (normalized.Length == 0)
        {
            return true;
        }

        return SearchableValues(quote, relatedName).Any(value =>
            value.Length > 0 && SearchService.NormalizeText(value).Contains(normalized, StringComparison.Ordinal));
    }

    // Valores visibles de un presupuesto en el orden de las columnas.
    private static IEnumerable<string> SearchableValues(Quote quote, string relatedName)
    {
        yield return quote.QuoteDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture);
        yield return SpanishPrice.Format(quote.Amount);
        yield return quote.Reference;
        yield return quote.Description;
        yield return quote.Status switch
        {
            QuoteStatus.Accepted => "Aceptado",
            QuoteStatus.Dismissed => "Descartado",
            _ => "Pendiente"
        };
        yield return relatedName;
    }

    /// <summary>
    /// Presupuestos que están hoy en un día de aviso (15, 23, 27, 28, 29 o
    /// 30), con los días que les faltan para caducar (0 = último día).
    /// Aceptados, Descartados y fechas futuras nunca avisan; los Aceptados de
    /// un futuro cliente sin pedido avisan como «pendiente de convertir»
    /// (specs/009 §10); la lista sale ordenada por fecha.
    /// </summary>
    public static List<(Quote Quote, int DaysLeft)> GetExpiring(IEnumerable<Quote> quotes, DateTime today)
    {
        var expiring = new List<(Quote Quote, int DaysLeft)>();
        foreach (Quote quote in quotes)
        {
            if (!IsActiveForExpiry(quote))
            {
                continue;
            }

            int elapsed = (today.Date - quote.QuoteDate.Date).Days;
            if (ExpiryNoticeDays.TryGetValue(elapsed, out int daysLeft))
            {
                expiring.Add((quote, daysLeft));
            }
        }

        return expiring.OrderBy(item => item.Quote.QuoteDate).ToList();
    }

    /// <summary>
    /// Presupuestos con la validez agotada (día 31 o posterior): el programa
    /// los pasa a Descartado automáticamente (specs/009 §10), escribiendo en
    /// la base sin esperar a Guardar. Incluye los Pendientes y los Aceptados
    /// de un futuro cliente sin pedido («pendiente de convertir»: sin
    /// cliente a los 30 días, el presupuesto se descarta).
    /// </summary>
    public static List<Quote> GetExpired(IEnumerable<Quote> quotes, DateTime today) =>
        quotes.Where(quote => IsActiveForExpiry(quote)
                && (today.Date - quote.QuoteDate.Date).Days > ValidityDays)
            .OrderBy(quote => quote.QuoteDate)
            .ToList();

    // Un presupuesto está «activo para la caducidad» si es Pendiente o si es
    // un Aceptado de futuro cliente sin pedido: mientras no se convierta el
    // futuro, el presupuesto corre la misma validez (decisión del usuario,
    // 18/11, specs/009 §10).
    private static bool IsActiveForExpiry(Quote quote) =>
        quote.Status == QuoteStatus.Pending
        || (quote.Status == QuoteStatus.Accepted
            && quote.RelatedType == TaskRelatedType.Prospect
            && quote.OrderId is null);
}
