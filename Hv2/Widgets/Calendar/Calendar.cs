
using Cosmo;

namespace Hv2UI;

public enum CalendarView
{
    WeekByDay,
    MonthByDay,
    MonthByWeek,
    YearByMonth,
}

public class Calendar : Widget
{
    public int X { get; set; }
    public int Y { get; set; }

    public DateOnly CurrentDate { get; set; }

    public CalendarView View { get; set; }

    public Calendar(int X, int Y, DateOnly Date, CalendarView View = CalendarView.MonthByDay)
    {
        this.X = X;
        this.Y = Y;
        CurrentDate = Date;

        this.View = View;
    }

    public override void OnInput(ConsoleKeyInfo cki)
    {
        
    }

    public override void Draw(Renderer r)
    {
        switch (View)
        {
            case CalendarView.WeekByDay:
                DrawWeekByDay(r);
                break;

            case CalendarView.MonthByDay:
                DrawMonthByDay(r);
                break;

            case CalendarView.MonthByWeek:
                DrawMonthByWeek(r);
                break;

            case CalendarView.YearByMonth:
                DrawYearByMonth(r);
                break;
        }
    }

    private static char[] DayNumerals = ['①', '②', '③', '④', '⑤', '⑥',	'⑦', '⑧', '⑨', '⑩', '⑪', '⑫', '⑬', '⑭', '⑮', '⑯', '⑰', '⑱', '⑲', '⑳', '㉑', '㉒', '㉓', '㉔', '㉕', '㉖', '㉗', '㉘', '㉙', '㉚', '㉛'];

    private void DrawWeekByDay(Renderer r)
    {
        for (int i = 0; i < DayNumerals.Length; i++)
        {
            r.WriteAt(X + i, Y, DayNumerals[i]);
        }
    }

    private void DrawMonthByDay(Renderer r)
    {

    }

    private void DrawMonthByWeek(Renderer r)
    {

    }

    private void DrawYearByMonth(Renderer r)
    {

    }
}
