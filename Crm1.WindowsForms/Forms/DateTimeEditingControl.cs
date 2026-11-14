using System.Globalization;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Control de edición combinado para fechas con hora (recordatorios,
/// specs/007): a la izquierda un DateTimePicker con calendario desplegable
/// para elegir el día (dd/MM/aaaa) y a la derecha un selector de hora con
/// flechas arriba/abajo y escritura directa (HH:mm, horas libres: cualquier
/// hora y minuto). La celda guarda el texto combinado «dd/MM/aaaa HH:mm».
/// Sustituye al calendario simple cuando el formato de la columna lleva hora.
/// </summary>
internal class DateTimeEditingControl : System.Windows.Forms.UserControl,
    System.Windows.Forms.IDataGridViewEditingControl
{
    // Formato combinado que guarda la celda (mismo que su columna).
    private const string CombinedFormat = "dd/MM/yyyy HH:mm";

    // Selector del día (calendario desplegable).
    internal readonly System.Windows.Forms.DateTimePicker DatePicker = new();

    // Selector de la hora: flechas o escritura, sin calendario.
    internal readonly System.Windows.Forms.DateTimePicker TimePicker = new();

    private System.Windows.Forms.DataGridView? _dataGridView;
    private bool _valueChanged;
    private int _rowIndex;

    /// <summary>
    /// Crea el control con el calendario a la izquierda y la hora a la
    /// derecha, ocupando todo el alto de la celda.
    /// </summary>
    public DateTimeEditingControl()
    {
        DatePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        DatePicker.CustomFormat = "dd/MM/yyyy";

        TimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        TimePicker.CustomFormat = "HH:mm";
        TimePicker.ShowUpDown = true;

        // El calendario se queda a la izquierda con su ancho justo y la hora
        // rellena el resto de la celda.
        DatePicker.Dock = System.Windows.Forms.DockStyle.Left;
        DatePicker.Width = 110;
        TimePicker.Dock = System.Windows.Forms.DockStyle.Fill;

        Controls.Add(TimePicker);
        Controls.Add(DatePicker);

        DatePicker.ValueChanged += (_, _) => NotifyChanged();
        TimePicker.ValueChanged += (_, _) => NotifyChanged();
    }

    /// <summary>
    /// Fecha y hora combinadas que muestra el control.
    /// </summary>
    public DateTime Value
    {
        get => DatePicker.Value.Date.Add(TimePicker.Value.TimeOfDay);
        set
        {
            DatePicker.Value = value.Date;
            TimePicker.Value = value.Date.Add(value.TimeOfDay);
        }
    }

    /// <summary>Texto combinado «dd/MM/aaaa HH:mm» de la celda.</summary>
    public object EditingControlFormattedValue
    {
        get => Value.ToString(CombinedFormat, CultureInfo.InvariantCulture);
        set => Value = DateTime.TryParseExact(
            value?.ToString(),
            CombinedFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date)
            ? date
            : DateTime.Today;
    }

    /// <summary>Texto combinado «dd/MM/aaaa HH:mm» de la celda.</summary>
    public object GetEditingControlFormattedValue(
        System.Windows.Forms.DataGridViewDataErrorContexts context) => EditingControlFormattedValue;

    /// <summary>
    /// Toma el tipo de letra y los colores de la celda para los dos selectores.
    /// </summary>
    public void ApplyCellStyleToEditingControl(System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle)
    {
        DatePicker.Font = dataGridViewCellStyle.Font;
        TimePicker.Font = dataGridViewCellStyle.Font;
        DatePicker.CalendarForeColor = dataGridViewCellStyle.ForeColor;
        DatePicker.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
    }

    /// <summary>Fila que se está editando.</summary>
    public int EditingControlRowIndex
    {
        get => _rowIndex;
        set => _rowIndex = value;
    }

    /// <summary>
    /// Los selectores necesitan las flechas (calendario y hora), Inicio/Fin y
    /// AvPág/RePág para moverse por meses y años, y Enter para cerrar.
    /// </summary>
    public bool EditingControlWantsInputKey(System.Windows.Forms.Keys keyData, bool dataGridViewWantsInputKey)
    {
        System.Windows.Forms.Keys key = keyData & System.Windows.Forms.Keys.KeyCode;
        return !dataGridViewWantsInputKey
            || key is System.Windows.Forms.Keys.Left or System.Windows.Forms.Keys.Right
                or System.Windows.Forms.Keys.Up or System.Windows.Forms.Keys.Down
                or System.Windows.Forms.Keys.Home or System.Windows.Forms.Keys.End
                or System.Windows.Forms.Keys.PageDown or System.Windows.Forms.Keys.PageUp
                or System.Windows.Forms.Keys.Enter;
    }

    /// <summary>Nada que preparar: los selectores se usan al pulsar.</summary>
    public void PrepareEditingControlForEdit(bool selectAll)
    {
    }

    /// <summary>Cursor del panel de edición: el habitual del control.</summary>
    public System.Windows.Forms.Cursor EditingPanelCursor => Cursor;

    /// <summary>El control no cambia de sitio al cambiar la fecha o la hora.</summary>
    public bool RepositionEditingControlOnValueChange => false;

    /// <summary>Tabla que hospeda el control.</summary>
    public System.Windows.Forms.DataGridView? EditingControlDataGridView
    {
        get => _dataGridView;
        set => _dataGridView = value;
    }

    /// <summary>Verdadero cuando el usuario ha cambiado la fecha o la hora.</summary>
    public bool EditingControlValueChanged
    {
        get => _valueChanged;
        set => _valueChanged = value;
    }

    // Marca el valor como sucio y avisa a la tabla al cambiar día u hora.
    private void NotifyChanged()
    {
        _valueChanged = true;
        _dataGridView?.NotifyCurrentCellDirty(true);
    }
}
