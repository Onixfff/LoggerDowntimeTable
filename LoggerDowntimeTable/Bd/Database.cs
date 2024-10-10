using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        private async Task<(List<Recept> recepts, string error)> GetServerRecepts()
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
                                recepts.Add(new Recept(reader.GetString(0)));
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

        private async Task<(List<Recept> recepts, string error)> GetLocalPCRecepts()
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
                                recepts.Add(new Recept(reader.GetString(0)));
                            }
                            reader.Close();
                        }
                        return (recepts, null);
                    }
                }
                catch (TimeoutException ex)
                {
                    _logger.Error(ex, "GetLocalPCRecepts > Error (TimeoutException ex)");
                    return (null, "GetLocalPCRecepts > Error (TimeoutException ex)");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex.Message);
                    return (null, $"{ex.Message}");
                }
                finally 
                {
                    _logger.Trace("GetLocalPCRecepts > End");
                    await connection.CloseAsync(); 
                }
            }
        }
    }
}
