using System;

namespace BlockCrush.UI
{
    public static class CountdownText
    {
        public static string Format(TimeSpan remaining) => remaining.TotalHours > 99 ? "99:59:59" : string.Format("{0:00}:{1:00}:{2:00}", (int)remaining.TotalHours, remaining.Minutes, remaining.Seconds);
    }
}
