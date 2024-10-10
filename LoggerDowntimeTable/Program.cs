using LoggerDowntimeTable.Bd;
using NLog;
using System.Threading;
using System.Threading.Tasks;

namespace LoggerDowntimeTable
{
    internal class Program
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            _logger.Trace("Start program");
            Task.Run(async () => await StartProgram()).GetAwaiter().GetResult();
        }

        private static async Task StartProgram()
        {
            Database db = new Database(_logger);

            while (true)
            {
                _logger.Trace("Начало цикла");

                db.

                _logger.Trace("Конец цикла");
                Thread.Sleep(5000);
            }
        }
    }
}
