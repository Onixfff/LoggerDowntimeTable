using LoggerDowntimeTable.Bd;
using LoggerDowntimeTable.Exceptions;
using MySql.Data.MySqlClient;
using NLog;
using System;
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
                try
                {
                    _logger.Trace("Начало цикла");

                    var result = await db.SynchronizeRecept();

                    if(result.error != null)
                    {
                        _logger.Error(result.error + "== Program ==");
                        throw new Exception(result.error);
                    }

                    _logger.Trace("Сверка Recepts прошла успешно");



                    _logger.Trace("Конец цикла");
                    Thread.Sleep(5000);
                }
                catch (ExceptionRecept ex)
                {

                }
                catch (MySqlException ex)
                {

                }
                catch(Exception ex)
                {

                }
            }
        }
    }
}
