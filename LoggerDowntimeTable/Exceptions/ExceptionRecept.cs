using System;

namespace LoggerDowntimeTable.Exceptions
{
    internal class ExceptionRecept : Exception
    {
        public ExceptionRecept(string message) : base(message) { }
    }
}
