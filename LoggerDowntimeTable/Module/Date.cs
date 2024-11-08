using LoggerDowntimeTable.Exceptions;
using System;

namespace LoggerDowntimeTable.Module
{
    internal abstract class BaseDate
    {
        public int Id { get; } = default;

        public DateTime TimeStamp { get; private set; } = default;

        public TimeSpan Difference { get; private set; }

        public Recept Recept { get; private set; } = default;

        public bool IsPastData { get; private set; } = default;

        public int IdTypeDowntime { get; private set; } = default;

        public string TypeDownTime { get; private set; } = default;

        public string Comment { get; private set; } = default;

        protected BaseDate(int id, DateTime timestamp, TimeSpan difference, Recept recept, int idTypeDowntime, string typeDownTime = default, string comment = default)
        {
            Id = id;
            TimeStamp = timestamp;
            Difference = difference;
            Recept = recept;
            IsPastData = false;
            IdTypeDowntime = idTypeDowntime;
        }

        protected virtual (BaseDate date, Exception error) Create(int id, DateTime timestamp, TimeSpan difference, Recept recept, int idTypeDowntime, string typeDownTime = default, string comment = default)
        {

            if (id == 0)
            {
                return (null, new ExceptionCreateDate("Неправельно задан id"));
            }

            if (timestamp == null)
            {
                return (null, new ExceptionCreateDate("Неправельно задан timestamp"));
            }

            if (difference == null)
            {
                return (null, new ExceptionCreateDate("Неправельно задан difference"));
            }

            if (recept == null || recept.Time == null || string.IsNullOrWhiteSpace(recept.Name))
            {
                return (null, new ExceptionCreateDate("Неправельно задан recept"));
            }

            NewDate newDate = new NewDate(id, timestamp, difference, recept, idTypeDowntime);

            return (newDate, null);
        }
    }

    internal class NewDate : BaseDate
    {
        public NewDate(int id, DateTime timestamp, TimeSpan difference, Recept recept, int idTypeDowntime) : base(id, timestamp, difference, recept, idTypeDowntime) {}
        
    }

    internal class OldDate : BaseDate
    {

        private OldDate(int id, DateTime timestamp, TimeSpan difference, Recept recept, int idTypeDowntime, string typeDownTime, string comments) : base(id, timestamp, difference, recept, idTypeDowntime){}

        protected override (BaseDate date, Exception error) Create(int id, DateTime timestamp, TimeSpan difference, Recept recept, int idTypeDowntime, string typeDownTime, string comment)
        {
            var result = base.Create(id, timestamp, difference, recept, idTypeDowntime);

            if (result.error != null)
            {
                return (null, result.error);
            }

            if (string.IsNullOrWhiteSpace(typeDownTime))
            {
                return (null, new ExceptionCreateDate("Неправельно задан typeDownTime"));
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                return (null, new ExceptionCreateDate("Неправельно задан comments"));
            }

            OldDate oldDate = new OldDate(id, timestamp, difference, recept, idTypeDowntime, typeDownTime, comment);

            return (oldDate, null);
        }
    }
}
