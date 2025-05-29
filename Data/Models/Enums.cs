using System.ComponentModel;
using System.Reflection;

public static class EnumExt
{
    public static string Text(this OperationType v)
    {
        var fi   = v.GetType().GetField(v.ToString());
        var attr = fi?.GetCustomAttribute<DescriptionAttribute>();
        return attr?.Description ?? v.ToString();
    }
}

public enum OperationType { 
    [Description("приход")] Приход, 
    [Description("списание")]  Списание, 
    [Description("долг")]  Долг 
}

public enum DebtorStatus
{
    В_Срок,
    Просрочено,
    Возвращено_Без_Оплаты,
    Просрочено_Возвращено_С_Оплатой,
    Утеряно
}