using System.Globalization;

namespace Crm1.WindowsForms.Forms;

/// <summary>
/// Columna de fecha con calendario (specs/005, sección 3): al editar la celda
/// se despliega un DateTimePicker donde se elige el día, con las flechas de
/// mes y año. La celda guarda la fecha como texto dd/MM/aaaa, igual que las
/// fechas de los demás módulos.
/// </summary>
public class CalendarColumn : System.Windows.Forms.DataGridViewColumn
{
    /// <summary>
    /// Crea la columna con una celda de calendario como plantilla.
    /// </summary>
    public CalendarColumn() : base(new CalendarCell())
    {
    }

    /// <summary>
    /// Plantilla de celda: solo admite CalendarCell.
    /// </summary>
    public override System.Windows.Forms.DataGridViewCell? CellTemplate
    {
        get => base.CellTemplate;
        set => base.CellTemplate = value is CalendarCell
            ? value
            : throw new InvalidCastException("La plantilla de celda debe ser CalendarCell.");
    }
}

/// <summary>
/// Celda de texto que, al editarse, muestra un DateTimePicker con calendario
/// desplegable en lugar del cuadro de texto.
/// </summary>
public class CalendarCell : System.Windows.Forms.DataGridViewTextBoxCell
{
    /// <summary>
    /// Crea la celda con el formato de fecha español de los demás módulos.
    /// </summary>
    public CalendarCell()
    {
        Style.Format = "dd/MM/yyyy";
    }

    /// <summary>
    /// Al entrar a editar, el calendario parte de la fecha de la celda (o de
    /// hoy si está vacía o no se puede leer).
    /// </summary>
    public override void InitializeEditingControl(int rowIndex, object? initialFormattedValue,
        System.Windows.Forms.DataGridViewCellStyle cellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, cellStyle);
        if (DataGridView?.EditingControl is CalendarEditingControl control)
        {
            control.Value = DateTime.TryParseExact(
                Value?.ToString(),
                "dd/MM/yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime date)
                ? date
                : DateTime.Today;
        }
    }

    /// <summary>Control de edición: el calendario.</summary>
    public override Type EditType => typeof(CalendarEditingControl);

    /// <summary>Las celdas guardan la fecha como texto dd/MM/aaaa.</summary>
    public override Type ValueType => typeof(string);

    /// <summary>Valor inicial de una celda nueva: hoy.</summary>
    public override object DefaultNewRowValue =>
        DateTime.Today.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    /// <summary>
    /// El control de edición ya entrega el texto en dd/MM/aaaa; se guarda tal cual.
    /// </summary>
    public override object? ParseFormattedValue(object? formattedValue,
        System.Windows.Forms.DataGridViewCellStyle cellStyle,
        System.ComponentModel.TypeConverter? formattedValueTypeConverter,
        System.ComponentModel.TypeConverter? valueTypeConverter)
    {
        return formattedValue?.ToString();
    }
}

/// <summary>
/// DateTimePicker incrustado en la celda mientras se edita: el calendario
/// desplegable permite elegir año, mes y día con el ratón.
/// </summary>
internal class CalendarEditingControl : System.Windows.Forms.DateTimePicker,
    System.Windows.Forms.IDataGridViewEditingControl
{
    private System.Windows.Forms.DataGridView? _dataGridView;
    private bool _valueChanged;
    private int _rowIndex;

    /// <summary>
    /// Crea el control con el formato de fecha español.
    /// </summary>
    public CalendarEditingControl()
    {
        Format = System.Windows.Forms.DateTimePickerFormat.Custom;
        CustomFormat = "dd/MM/yyyy";
    }

    /// <summary>Fecha de la celda en dd/MM/aaaa.</summary>
    public object EditingControlFormattedValue
    {
        get => Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        set => Value = DateTime.TryParseExact(
            value?.ToString(),
            "dd/MM/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime date)
            ? date
            : DateTime.Today;
    }

    /// <summary>Fecha de la celda en dd/MM/aaaa.</summary>
    public object GetEditingControlFormattedValue(
        System.Windows.Forms.DataGridViewDataErrorContexts context) => EditingControlFormattedValue;

    /// <summary>
    /// Toma el tipo de letra y los colores de la celda para el calendario.
    /// </summary>
    public void ApplyCellStyleToEditingControl(System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle)
    {
        Font = dataGridViewCellStyle.Font;
        CalendarForeColor = dataGridViewCellStyle.ForeColor;
        CalendarMonthBackground = dataGridViewCellStyle.BackColor;
    }

    /// <summary>Fila que se está editando.</summary>
    public int EditingControlRowIndex
    {
        get => _rowIndex;
        set => _rowIndex = value;
    }

    /// <summary>
    /// El calendario necesita las flechas, Inicio/Fin y AvPág/RePág para
    /// moverse por meses y años.
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

    /// <summary>Nada que preparar: el calendario se despliega al pulsar.</summary>
    public void PrepareEditingControlForEdit(bool selectAll)
    {
    }

    /// <summary>Cursor del panel de edición: el habitual del control.</summary>
    public System.Windows.Forms.Cursor EditingPanelCursor => Cursor;

    /// <summary>El control no cambia de sitio al cambiar la fecha.</summary>
    public bool RepositionEditingControlOnValueChange => false;

    /// <summary>Tabla que hospeda el control.</summary>
    public System.Windows.Forms.DataGridView? EditingControlDataGridView
    {
        get => _dataGridView;
        set => _dataGridView = value;
    }

    /// <summary>Verdadero cuando el usuario ha cambiado la fecha.</summary>
    public bool EditingControlValueChanged
    {
        get => _valueChanged;
        set => _valueChanged = value;
    }

    /// <summary>
    /// Avisa a la tabla de que la celda está sucia al cambiar la fecha.
    /// </summary>
    protected override void OnValueChanged(EventArgs eventargs)
    {
        _valueChanged = true;
        _dataGridView?.NotifyCurrentCellDirty(true);
        base.OnValueChanged(eventargs);
    }
}
