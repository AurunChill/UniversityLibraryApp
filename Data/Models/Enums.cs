using System.ComponentModel;
using System.Reflection;

public static class EnumExt
{
    public static string Text<T>(this T v) where T : struct, Enum
    {
        var fi   = typeof(T).GetField(v.ToString());
        var attr = fi?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? v.ToString();
    }
}

public enum OperationType
{
    [Description("приход")] Приход,
    [Description("списание")] Списание,
    [Description("долг")] Долг
}

public enum DebtorStatus
{
    [Description("в срок")] В_Срок,
    [Description("просрочено")] Просрочено,
    [Description("возвращено без оплаты")] Возвращено_Без_Оплаты,
    [Description("просрочено возвращено с оплатой")] Просрочено_Возвращено_С_Оплатой,
    [Description("утеряно")] Утеряно
}