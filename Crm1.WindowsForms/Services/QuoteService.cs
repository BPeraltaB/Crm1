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
    /// falla o null si es correcto.
    /// </summary>
    public QuoteValidationError? Validate(Quote quote)
    {
        if (quote.QuoteDate == default)
        {
            return new QuoteValidationError
            {
                Field = nameof(Quote.QuoteDate),
                Message = "La celda está vacía."
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
}
