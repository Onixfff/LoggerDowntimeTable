using LoggerDowntimeTable.Exceptions;
using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoggerDowntimeTable.Bd
{
    internal class Database
    {
        private readonly ILogger _logger;
        private readonly string _errorOldBdMessage = "Unknown system variable 'lower_case_table_names'";

        public Database(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<(bool isComplite, string error)> SynchronizeRecept()
        {
            ExceptionRecept exceptionReceptError;

            var server = await GetServerRecepts();
            var local = await GetLocalPCRecepts();

            if(server.Error != null)
            {
                exceptionReceptError = new ExceptionRecept(server.Error);
                _logger.Error(exceptionReceptError.Message + "\n PlcServer is not take data");
                return (false, exceptionReceptError.Message + "\n == PlcServer is not take data ==");
            }

            if(local.Error != null)
            {
                exceptionReceptError = new ExceptionRecept(local.Error);
                _logger.Error(new ExceptionRecept("PlcLocal is not take data"));
                return (false, exceptionReceptError.Message + "\n == PlcLocal is not take data ==");
            }

            var result = await ChecksDataDifferenceRecepts(local.Recepts, server.Recepts);

            if(result.error != null)
            {
                exceptionReceptError = result.error;
                _logger.Error(new ExceptionRecept("Error DifferenceRecept"));
                return (false, exceptionReceptError.Message + "\n == Error DifferenceRecept ==");
            }

            return(true, null);
        }

        private async Task<(List<Recept> Recepts, string Error)> GetServerRecepts()
        {
            _logger.Trace("GetServerRecepts > Start");

            string query = "SELECT Name FROM spslogger.receptTime group by Name;";

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Server"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    List<Recept> recepts = new List<Recept>();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var result = Recept.Create(reader.GetString(0));

                                if (result.error != null)
                                {
                                    throw result.error;
                                }

                                recepts.Add(result.recept);
                            }
                            reader.Close();
                        }
                        return (recepts, null);
                    }
                }
                catch (MySqlException ex)
                {
                    _logger.Error(ex, "GetServerRecepts > Error (MySqlException ex)");
                    return (null, "GetServerRecepts > Error (MySqlException ex)");
                }
                catch (TimeoutException ex)
                {
                    _logger.Error(ex, "GetServerRecepts > Error (TimeoutException ex)");
                    return (null, "GetServerRecepts > Error (TimeoutException ex)");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "GetServerRecepts > Error (Exception ex)");
                    return (null, "GetServerRecepts > Error (Exception ex)");
                }
                finally
                {
                    await connection.CloseAsync();
                    _logger.Trace("GetServerRecepts > END");
                }
            }
        }

        private async Task<(List<Recept> Recepts, string Error)> GetLocalPCRecepts()
        {
            _logger.Trace("GetLocalPCRecepts > Start");

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Pc"].ConnectionString))
            {
                try
                {
                    try
                    {
                        await connection.OpenAsync();
                    }
                    catch (MySqlException ex)
                    {
                        if (ex.Message != _errorOldBdMessage)
                        {
                            _logger.Error(ex.Message);
                            return (null, "Error open connection");
                        }
                        else
                        {
                            goto Select;
                        }

                    }
                    catch (TimeoutException ex)
                    {
                        _logger.Error(ex, "GetLocalPCRecepts > Error (TimeoutException ex)");
                    }

                Select:
                    _logger.Trace("goto Select >> start");

                    string query = "SELECT recepte FROM spslogger.error_mas group by recepte;";

                    List<Recept> recepts = new List<Recept>();

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var result = Recept.Create(reader.GetString(0));

                                if(result.error != null)
                                {
                                    throw result.error;
                                }

                                recepts.Add(result.recept);
                            }
                            reader.Close();
                        }
                        return (recepts, null);
                    }
                }
                catch (ExceptionRecept ex)
                {
                    _logger.Error(ex, "GetLocalPCRecepts > Error (ExceptionRecept ex)");
                    return (null, "GetLocalPCRecepts > Error (ExceptionRecept ex)");
                }
                catch (TimeoutException ex)
                {
                    _logger.Error(ex, "GetLocalPCRecepts > Error (TimeoutException ex)");
                    return (null, "GetLocalPCRecepts > Error (TimeoutException ex)");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"{ex.Message}\n == {ex.TargetSite} ==");
                    return (null, $"{ex.Message} GetLocalPCRecepts > Error (Exception ex)");
                }
                finally 
                {
                    _logger.Trace("GetLocalPCRecepts > End");
                    await connection.CloseAsync(); 
                }
            }
        }

        private async Task<(bool isComplite, ExceptionRecept error)> ChecksDataDifferenceRecepts(List<Recept> localPcCRecepts, List<Recept> serverRecept)
        {
            if (localPcCRecepts != null && serverRecept != null)
            {
                List<Recept> recepts = localPcCRecepts.Where(r1 => !serverRecept.Any(r2 => r2.Name == r1.Name)).ToList();

                if (recepts.Count >= 1)
                {
                    _logger.Trace("Change data");
                    var result = await ChangeDBReceptTime(recepts);

                    if(result.error == null)
                    {
                        return (false, new ExceptionRecept(result.error));
                    }

                    return (true, null);
                }
                else
                {
                    _logger.Trace("The data match");
                    return(true, null);
                }
            }
            else
            {
                _logger.Error(new ExceptionRecept("Data reconciliation error"));
                return (false, new ExceptionRecept("Data reconciliation error"));
            }
        }

        private async Task<(bool isComplite, string error)> ChangeDBReceptTime(List<Recept> recepts)
        {
            string sqlInsert = "INSERT INTO spslogger.recepttime (Name, Time) VALUES (@Name, @Time)";

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Server"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (MySqlCommand command = new MySqlCommand(sqlInsert, connection))
                    {
                        command.Parameters.Add("@Name", MySqlDbType.VarChar);
                        command.Parameters.Add("@Time", MySqlDbType.Time);

                        foreach (Recept recept in recepts)
                        {
                            command.Parameters["@Name"].Value = recept.Name;
                            command.Parameters["@Time"].Value = recept.Time;
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
                catch(ExceptionRecept ex)
                {
                    _logger.Error(ex.Message, "Ошибка внутри");
                    return (false, ex.Message);
                }

                catch (MySqlException ex)
                {
                    _logger.Error(ex.Message, "Ошибка базы");
                    return (false, ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message, "Ошибка глобальная");
                    return (false, ex.Message);
                }
                finally { await connection.CloseAsync(); }

                return (true, null);
            }
        }

        //Create triger (testServer)
        public async Task<(bool isComplite, Exception error)> CreateTrigerInServer()
        {
            var result = await DropTrigerInServer();

            if (result.error != null)
            {
                _logger.Error(result.error.Message);
                return (false, result.error);
            }

            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Server"].ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string createTriggerQuery = @"
CREATE TRIGGER after_insert_mixreport_second
AFTER INSERT ON mixreport
FOR EACH ROW
BEGIN
    DECLARE previous_timestamp DATETIME;
    DECLARE previous_data_52 VARCHAR(255);
    DECLARE previous_id INT;
    DECLARE time_difference TIME;

    -- Получаем предыдущее событие на основе максимального Timestamp, который меньше текущего
    SELECT Timestamp, Data_52, DBID INTO previous_timestamp, previous_data_52, previous_id
    FROM mixreport
    WHERE Timestamp < NEW.Timestamp
    ORDER BY Timestamp DESC
    LIMIT 1;

    -- Если предыдущая запись найдена
    IF previous_timestamp IS NOT NULL THEN
        -- Сравнение временного интервала и значения Data_52
        SET time_difference = TIMEDIFF(NEW.Timestamp, previous_timestamp);
        IF time_difference >= '00:07:30' THEN
            IF previous_data_52 = NEW.Data_52 THEN
                -- Проверка, что previous_id и NEW.DBID еще не были использованы
                IF NOT EXISTS (
                    SELECT 1 FROM comparison_results
                    WHERE table1_id = previous_id OR table2_id = previous_id
                    OR table1_id = NEW.DBID OR table2_id = NEW.DBID
                ) THEN
                    -- Вставка данных в comparison_results
                    INSERT INTO comparison_results (table1_id, table2_id, difference, data_52)
                    VALUES (previous_id, NEW.DBID, time_difference, NEW.Data_52);
                END IF;
            END IF;
        END IF;
    END IF;
END;

";

                    using (var command = new MySqlCommand(createTriggerQuery, connection))
                    {
                        await command.ExecuteNonQueryAsync();
                        _logger.Trace("Triger create complite");
                        Console.WriteLine("Триггер create complite");
                    }
                }
                catch(TimeoutException ex)
                {
                    return (false, ex);
                }
                catch (AggregateException ex)
                {
                    // Пробегаем все вложенные исключения
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        return (false, ex);
                    }
                }
                catch (Exception ex)
                {
                    return (false, ex);
                }
                finally { await connection.CloseAsync(); }

                return (true, null);
            
            }
        }

        private async Task<(bool isComplite, Exception error)> DropTrigerInServer()
        {
            using (MySqlConnection connection = new MySqlConnection(ConfigurationManager.ConnectionStrings["Server"].ConnectionString))
            {
                try
                {

                    
                    await connection.OpenAsync();

                    string dropTriggerSql = "DROP TRIGGER IF EXISTS after_insert_mixreport_second;";

                    using (var dropCommand = new MySqlCommand(dropTriggerSql, connection))
                    {
                        await dropCommand.ExecuteNonQueryAsync();
                        _logger.Trace("Triger delite");
                        Console.WriteLine("Triger delite");
                    }
                }
                catch(InvalidOperationException ex)
                {
                    return (false, ex);
                }
                catch (TimeoutException ex)
                {
                    return (false, ex);
                }
                catch (AggregateException ex)
                {
                    // Пробегаем все вложенные исключения
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        return (false, ex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, ex.Message + $"\n == {ex.TargetSite} ==");
                    return (false, ex);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
            return (true, null);
        }
    }
}
