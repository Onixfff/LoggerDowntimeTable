using System;

namespace LoggerDowntimeTable
{
    internal class Recept
    {
        public string Name { get; set; }
        public TimeSpan Time { get; private set; }

        public Recept(string name, TimeSpan timeSpan = default(TimeSpan))
        {
            Name = name;
            Time = timeSpan == default(TimeSpan) ? new TimeSpan(0, 7, 30) : timeSpan;
        }
    }
}
