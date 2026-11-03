namespace Crm1.Tests;

/// <summary>
/// Ejecuta código de Windows Forms en un hilo STA dedicado, como exige el
/// control: los formularios se crean, se usan y se destruyen dentro del mismo
/// hilo. Si el código probado falla, se relanza la excepción real.
/// </summary>
internal static class WinFormsSta
{
    public static void Run(Action action) => Run<object?>(() =>
    {
        action();
        return null;
    });

    public static T Run<T>(Func<T> func)
    {
        T result = default!;
        Exception? error = null;

        var thread = new System.Threading.Thread(() =>
        {
            try
            {
                result = func();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(System.Threading.ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }

        return result;
    }
}
