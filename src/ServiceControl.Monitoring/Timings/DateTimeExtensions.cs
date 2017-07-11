namespace ServiceControl.Monitoring.Timings
{
    using System;

    static class DateTimeExtensions
    {
        public static DateTime RoundDownToNearest(this DateTime dateTime, TimeSpan timespan)
            => new DateTime(dateTime.Ticks - dateTime.Ticks % timespan.Ticks, dateTime.Kind);
    }
}