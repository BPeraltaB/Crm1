using System.Reflection;
using Crm1.WindowsForms.Forms;
using Crm1.WindowsForms.Models;
using Crm1.WindowsForms.Services;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas de interfaz de la búsqueda global ampliada (specs/004 §7): la
/// tabla superior sigue mostrando clientes y futuros clientes y la inferior
/// aparece solo cuando hay tareas o recordatorios que coinciden; el doble
/// clic pide la navegación con el módulo y el identificador correctos.
/// </summary>
[Collection("Formularios UI")]
public sealed class SearchFormUiTests
{
    public SearchFormUiTests()
    {
        UiTestEnvironment.EnsureCatalog();
    }

    [Fact]
    public void Search_fills_both_tables_according_to_the_module()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Contact eva = Fixtures.Contact("Eva");
        Crm1.WindowsForms.Models.Task task = Fixtures.Task(ana.Id, TaskRelatedType.Client, "Llamar a Ana");
        Reminder reminder = Fixtures.Reminder(eva.Id, TaskRelatedType.Prospect, "Llamada de Eva");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts(eva);
        UiTestEnvironment.WriteTasks(task);
        UiTestEnvironment.WriteReminders(reminder);

        WinFormsSta.Run(() =>
        {
            using var form = new SearchForm();
            DataGridView results = FormAccess.Grid(form, "resultsDataGridView");
            DataGridView tasksGrid = FormAccess.Grid(form, "tasksDataGridView");
            TextBox search = FormAccess.TextBox(form, "searchTextBox");

            // "ana" cae en la cliente y en el registro vinculado de su tarea.
            search.Text = "ana";
            Assert.Equal(1, results.Rows.Count);
            Assert.Equal("Clientes", FormAccess.CellText(results.Rows[0], "moduleColumn"));
            Assert.Equal(1, tasksGrid.Rows.Count);
            Assert.Equal("Tarea", FormAccess.CellText(tasksGrid.Rows[0], "typeColumn"));
            Assert.Equal("Llamar a Ana", FormAccess.CellText(tasksGrid.Rows[0], "titleColumn"));
            Assert.Equal("(Cliente) Ana García Pérez", FormAccess.CellText(tasksGrid.Rows[0], "tasksRelatedColumn"));
            Assert.Equal((SearchModule.Task, task.Id), tasksGrid.Rows[0].Tag);

            // "eva" cae en la futura cliente y en el recordatorio vinculado a ella.
            search.Text = "eva";
            Assert.Equal(1, results.Rows.Count);
            Assert.Equal("Futuros clientes", FormAccess.CellText(results.Rows[0], "moduleColumn"));
            Assert.Equal(1, tasksGrid.Rows.Count);
            Assert.Equal("Recordatorio", FormAccess.CellText(tasksGrid.Rows[0], "typeColumn"));
            Assert.Equal("Llamada de Eva", FormAccess.CellText(tasksGrid.Rows[0], "titleColumn"));
            Assert.Equal("(Futuro cliente) Eva", FormAccess.CellText(tasksGrid.Rows[0], "tasksRelatedColumn"));
            Assert.Equal((SearchModule.Reminder, reminder.Id), tasksGrid.Rows[0].Tag);

            // Sin coincidencias, ambas tablas quedan vacías.
            search.Text = "zzz";
            Assert.Equal(0, results.Rows.Count);
            Assert.Equal(0, tasksGrid.Rows.Count);
        });
    }

    [Fact]
    public void Double_click_on_the_lower_table_requests_navigation_to_the_module()
    {
        Client ana = Fixtures.Client("Ana", "12345678Z");
        Crm1.WindowsForms.Models.Task task = Fixtures.Task(ana.Id, TaskRelatedType.Client, "Llamar");
        UiTestEnvironment.WriteClients(ana);
        UiTestEnvironment.WriteContacts();
        UiTestEnvironment.WriteTasks(task);

        WinFormsSta.Run(() =>
        {
            using var form = new SearchForm();
            DataGridView tasksGrid = FormAccess.Grid(form, "tasksDataGridView");
            FormAccess.TextBox(form, "searchTextBox").Text = "llamar";

            (SearchModule Module, Guid Id)? navigated = null;
            form.NavigateRequested += (module, id) => navigated = (module, id);

            // Doble clic simulado en la primera fila de la tabla inferior.
            MethodInfo handler = typeof(SearchForm).GetMethod(
                "ResultsDataGridView_CellDoubleClick",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            handler.Invoke(form, new object?[]
            {
                tasksGrid,
                new DataGridViewCellEventArgs(0, 0)
            });

            Assert.NotNull(navigated);
            Assert.Equal(SearchModule.Task, navigated!.Value.Module);
            Assert.Equal(task.Id, navigated.Value.Id);
        });
    }
}
