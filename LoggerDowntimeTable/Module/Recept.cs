using LoggerDowntimeTable.Exceptions;
using System;

namespace LoggerDowntimeTable
{
    public class Recept
    {
        public string Name { get; set; }
        public TimeSpan Time { get; private set; }

        public static (Recept recept, Exception error) Create(string name, TimeSpan timeSpan = default(TimeSpan))
        {

            if(name == null)
            {
                return (null, new ExceptionRecept("Неправельно задали name"));
            }

            if(timeSpan != default && timeSpan != DateTime.Now.TimeOfDay)
            {
                return (null, new ExceptionRecept("Неправельно задали timespan"));
            }

            Recept recept = new Recept(name, timeSpan);

            return (recept, null);
        }

        private Recept(string name, TimeSpan timeSpan = default(TimeSpan))
        {
            Name = name;
            Time = timeSpan == default(TimeSpan) ? new TimeSpan(0, 7, 30) : timeSpan;
        }
    }
}
