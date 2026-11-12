using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;
using Task = Crm1.WindowsForms.Models.Task;
using TaskStatus = Crm1.WindowsForms.Models.TaskStatus;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de las reglas de negocio de las tareas (specs/005, sección 5):
/// una por regla más una tarea válida completa.
/// </summary>
public sealed class TaskServiceTests
{
    // Registros vinculables de prueba: un cliente y un futuro cliente.
    private static TaskService Service(params (Guid Id, TaskRelatedType Type)[] related)
    {
        var records = related.ToDictionary(item => item.Id, item => item.Type);
        return new TaskService(records);
    }

    private static Guid ClientId { get; } = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

    private static Guid ProspectId { get; } = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

    private static Task ValidTask() => new()
    {
        Id = Guid.NewGuid(),
        Action = "Llamar",
        Description = "Seguimiento comercial",
        DueDate = new DateTime(2026, 12, 1),
        Status = TaskStatus.Pending,
        Priority = TaskPriority.Medium,
        RelatedId = ClientId,
        RelatedType = TaskRelatedType.Client
    };

    [Fact]
    public void Valid_task_passes()
    {
        TaskValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(ValidTask());

        Assert.Null(error);
    }

    [Fact]
    public void Empty_action_is_rejected()
    {
        Task task = ValidTask();
        task.Action = " ";

        TaskValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(task);

        Assert.NotNull(error);
        Assert.Equal(nameof(Task.Action), error!.Field);
        Assert.Equal("La celda está vacía.", error.Message);
    }

    [Theory]
    [InlineData("Llamar")]
    [InlineData("Visitar")]
    [InlineData("Enviar correo")]
    [InlineData("Reunión")]
    [InlineData("Otro")]
    public void Known_actions_are_allowed(string action)
    {
        Task task = ValidTask();
        task.Action = action;

        Assert.Null(Service((ClientId, TaskRelatedType.Client)).Validate(task));
    }

    [Fact]
    public void Unknown_action_is_rejected()
    {
        Task task = ValidTask();
        task.Action = "Enviar fax";

        TaskValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(task);

        Assert.NotNull(error);
        Assert.Equal(nameof(Task.Action), error!.Field);
        Assert.Equal("La acción no es válida.", error.Message);
    }

    [Fact]
    public void Missing_related_record_is_rejected()
    {
        Task task = ValidTask();
        task.RelatedId = Guid.NewGuid();

        TaskValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(task);

        Assert.NotNull(error);
        Assert.Equal(nameof(Task.RelatedId), error!.Field);
        Assert.Equal("Debes elegir un cliente o futuro cliente.", error.Message);
    }

    [Fact]
    public void Related_type_mismatch_is_rejected()
    {
        // El Id existe pero es de un futuro cliente y la tarea dice cliente.
        Task task = ValidTask();
        task.RelatedId = ProspectId;
        task.RelatedType = TaskRelatedType.Client;

        TaskValidationError? error =
            Service((ClientId, TaskRelatedType.Client), (ProspectId, TaskRelatedType.Prospect)).Validate(task);

        Assert.NotNull(error);
        Assert.Equal(nameof(Task.RelatedId), error!.Field);
    }

    [Fact]
    public void Missing_due_date_is_rejected()
    {
        Task task = ValidTask();
        task.DueDate = default;

        TaskValidationError? error = Service((ClientId, TaskRelatedType.Client)).Validate(task);

        Assert.NotNull(error);
        Assert.Equal(nameof(Task.DueDate), error!.Field);
        Assert.Equal("La celda está vacía.", error.Message);
    }

    [Fact]
    public void ValidateTasks_reports_row_indexes()
    {
        Task valid = ValidTask();

        Task emptyAction = ValidTask();
        emptyAction.Action = string.Empty;

        Task noDate = ValidTask();
        noDate.DueDate = default;

        List<TaskRowValidationError> errors =
            Service((ClientId, TaskRelatedType.Client)).ValidateTasks(new List<Task> { valid, emptyAction, noDate });

        Assert.Equal(2, errors.Count);
        Assert.Equal(1, errors[0].RowIndex);
        Assert.Equal(nameof(Task.Action), errors[0].Error.Field);
        Assert.Equal(2, errors[1].RowIndex);
        Assert.Equal(nameof(Task.DueDate), errors[1].Error.Field);
    }
}
