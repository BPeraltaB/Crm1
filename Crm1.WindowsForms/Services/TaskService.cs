using Crm1.WindowsForms.Models;
using Task = Crm1.WindowsForms.Models.Task;

namespace Crm1.WindowsForms.Services;

/// <summary>
/// Error de validación de una tarea sobre un campo concreto.
/// </summary>
public sealed class TaskValidationError
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
/// Error de validación asociado a una fila de la tabla de tareas.
/// </summary>
public sealed class TaskRowValidationError
{
    /// <summary>
    /// Índice (base cero) de la tarea dentro de la lista validada.
    /// </summary>
    public int RowIndex { get; init; }

    /// <summary>
    /// Error del campo concreto.
    /// </summary>
    public TaskValidationError Error { get; init; } = new();
}

/// <summary>
/// Reglas de negocio de las tareas (specs/005): la acción debe ser una de la
/// lista del desplegable, el vínculo con un cliente o futuro cliente
/// existente es obligatorio y la fecha límite debe ser válida. Vive fuera de
/// los formularios (constitución, punto 3).
/// </summary>
public sealed class TaskService
{
    /// <summary>
    /// Acciones permitidas en el desplegable de la tabla (specs/005, sección 3).
    /// </summary>
    public static readonly string[] Actions = { "Llamar", "Visitar", "Enviar correo", "Reunión", "Otro" };

    // Registros vinculables conocidos: Id → tipo (lo construye el formulario
    // con los clientes y futuros clientes de la base).
    private readonly IReadOnlyDictionary<Guid, TaskRelatedType> _relatedRecords;

    /// <summary>
    /// Crea el servicio con los registros vinculables existentes en la base.
    /// </summary>
    public TaskService(IReadOnlyDictionary<Guid, TaskRelatedType> relatedRecords)
    {
        _relatedRecords = relatedRecords;
    }

    /// <summary>
    /// Valida una tarea concreta; devuelve el error del primer campo que falla
    /// o null si es correcta.
    /// </summary>
    public TaskValidationError? Validate(Task task)
    {
        if (string.IsNullOrWhiteSpace(task.Action))
        {
            return new TaskValidationError
            {
                Field = nameof(Task.Action),
                Message = "La celda está vacía."
            };
        }

        if (!Actions.Contains(task.Action))
        {
            return new TaskValidationError
            {
                Field = nameof(Task.Action),
                Message = "La acción no es válida."
            };
        }

        if (!_relatedRecords.TryGetValue(task.RelatedId, out TaskRelatedType type)
            || type != task.RelatedType)
        {
            return new TaskValidationError
            {
                Field = nameof(Task.RelatedId),
                Message = "Debes elegir un cliente o futuro cliente."
            };
        }

        if (task.DueDate == default)
        {
            return new TaskValidationError
            {
                Field = nameof(Task.DueDate),
                Message = "La celda está vacía."
            };
        }

        return null;
    }

    /// <summary>
    /// Valida todas las tareas y devuelve un error por fila defectuosa.
    /// </summary>
    public List<TaskRowValidationError> ValidateTasks(List<Task> tasks)
    {
        var errors = new List<TaskRowValidationError>();
        for (int i = 0; i < tasks.Count; i++)
        {
            TaskValidationError? error = Validate(tasks[i]);
            if (error is not null)
            {
                errors.Add(new TaskRowValidationError { RowIndex = i, Error = error });
            }
        }
        return errors;
    }
}
