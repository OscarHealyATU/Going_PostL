using System;

public static class TimeUtil
{
    public static string NowSql() =>
        DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
}
