namespace FWOffice.Services
{
    // App-wide "working year" — the equivalent of the legacy gstrYear. Year-specific
    // screens (Orders, Invoices, Products, Payments, ...) read this and reload when it
    // changes. Defaults to the current year; scoped per circuit (per user session).
    public class WorkingYearState
    {
        public int Year { get; private set; } = DateTime.Today.Year;

        public event Action? OnChange;

        public void SetYear(int year)
        {
            if (year < 2000 || year > 2100 || year == Year) return;
            Year = year;
            OnChange?.Invoke();
        }
    }
}
