using Crm1.WindowsForms.Forms;
using Xunit;

namespace Crm1.Tests;

/// <summary>
/// Pruebas del control de edición combinado día + hora de los recordatorios
/// (specs/007): el texto «dd/MM/aaaa HH:mm» se reparte entre los dos
/// selectores y vuelve a combinarse al leerlo.
/// </summary>
public sealed class DateTimeEditingControlTests
{
    [Fact]
    public void Formatted_value_splits_into_day_and_time_and_joins_back()
    {
        WinFormsSta.Run(() =>
        {
            using var control = new DateTimeEditingControl();

            control.EditingControlFormattedValue = "18/11/2026 09:45";

            Assert.Equal(new DateTime(2026, 11, 18, 9, 45, 0), control.Value);
            Assert.Equal(new DateTime(2026, 11, 18), control.DatePicker.Value.Date);
            Assert.Equal(new TimeSpan(9, 45, 0), control.TimePicker.Value.TimeOfDay);
            Assert.Equal("18/11/2026 09:45", control.EditingControlFormattedValue);
        });
    }

    [Fact]
    public void Midnight_is_kept_when_splitting()
    {
        WinFormsSta.Run(() =>
        {
            using var control = new DateTimeEditingControl();

            control.EditingControlFormattedValue = "01/12/2026 00:00";

            Assert.Equal(new DateTime(2026, 12, 1, 0, 0, 0), control.Value);
            Assert.Equal("01/12/2026 00:00", control.EditingControlFormattedValue);
        });
    }

    [Fact]
    public void Unreadable_formatted_value_falls_back_to_today()
    {
        WinFormsSta.Run(() =>
        {
            using var control = new DateTimeEditingControl();

            control.EditingControlFormattedValue = "no es una fecha";

            Assert.Equal(DateTime.Today, control.Value.Date);
            Assert.Equal(TimeSpan.Zero, control.TimePicker.Value.TimeOfDay);
        });
    }

    [Fact]
    public void Changing_the_day_or_the_time_marks_the_control_dirty()
    {
        WinFormsSta.Run(() =>
        {
            using var control = new DateTimeEditingControl();
            Assert.False(control.EditingControlValueChanged);

            control.DatePicker.Value = new DateTime(2026, 11, 18);
            Assert.True(control.EditingControlValueChanged);

            control.EditingControlValueChanged = false;
            control.TimePicker.Value = new DateTime(2026, 11, 18, 10, 30, 0);
            Assert.True(control.EditingControlValueChanged);
        });
    }

    [Fact]
    public void Row_cell_of_time_column_uses_the_combined_editor()
    {
        // Regresión: la celda de una FILA es un clon de la plantilla; sin el
        // Clone() sobrescrito el clon perdía el formato y editaba con el
        // calendario simple (sin selector de hora).
        WinFormsSta.Run(() =>
        {
            using var grid = new DataGridView();
            grid.Columns.Add(new CalendarColumn("dd/MM/yyyy HH:mm"));
            grid.Rows.Add();

            Assert.IsType<CalendarCell>(grid.Rows[0].Cells[0]);
            Assert.Equal(typeof(DateTimeEditingControl), grid.Rows[0].Cells[0].EditType);
        });
    }

    [Fact]
    public void Row_cell_of_date_column_keeps_the_simple_calendar()
    {
        // Camino de TaskForm: la columna de solo fecha sigue con el calendario
        // simple aunque sus celdas también se clonen.
        WinFormsSta.Run(() =>
        {
            using var grid = new DataGridView();
            grid.Columns.Add(new CalendarColumn());
            grid.Rows.Add();

            Assert.Equal(typeof(CalendarEditingControl), grid.Rows[0].Cells[0].EditType);
        });
    }
}
