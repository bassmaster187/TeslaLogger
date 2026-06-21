using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.Extensions.Logging.Abstractions;
using MySql.Data.MySqlClient;
using TeslaLogger.Data.Abstractions;
using TeslaLogger.Data.Implementations;

namespace TeslaLogger
{
    /// <summary>
    /// Periodically logs process statistics (thread count, memory usage, database table sizes).
    /// Uses <see cref="IDbConnectionFactory"/> for database connections.
    /// </summary>
    public class TLStats
    {
        private static readonly Lazy<TLStats> _instance = new Lazy<TLStats>(() => new TLStats());
        private static readonly Lazy<IDbConnectionFactory> _connectionFactory =
            new Lazy<IDbConnectionFactory>(() => new DbConnectionFactory(NullLogger<DbConnectionFactory>.Instance, DBHelper.DBConnectionstring));

        private TLStats()
        {
            Logfile.Log("TLStats initialized");
        }

        public static TLStats GetInstance() => _instance.Value;

        /// <summary>
        /// Main loop: periodically dumps process statistics to the log.
        /// Runs until the provided CancellationToken is cancelled.
        /// </summary>
        /// <param name="cancellationToken">Token to stop the loop.</param>
        /// <returns>A Task that completes when the loop ends.</returns>
        public static async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                Logfile.Log(await DumpAsync(cancellationToken));
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (DateTime.Now.Minute % 30 == 0)
                    {
                        Logfile.Log(await DumpAsync(cancellationToken));
                        // sleep 55 minutes
                        await Task.Delay(3300000, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await Task.Delay(30000, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Logfile.Log("TLStats: RunAsync cancelled");
            }
            catch (Exception ex)
            {
                Logfile.Log($"TLStats: RunAsync failed: {ex.GetType().Name}: {ex.Message}");
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
        }

        /// <summary>
        /// Collects process statistics and database table sizes asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the database query.</param>
        /// <returns>Formatted statistics string.</returns>
        internal static async Task<string> DumpAsync(CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            sb.Append($"TeslaLogger process statistics{Environment.NewLine}");
            try
            {
                var process = Process.GetCurrentProcess();
                var threadCount = process.Threads.Count;

                sb.Append($"Thread count:        {threadCount,12}{Environment.NewLine}");
                sb.Append($"WorkingSet64:        {process.WorkingSet64,12}{Environment.NewLine}");
                sb.Append($"PeakWorkingSet64:    {process.PeakWorkingSet64,12}{Environment.NewLine}");
                sb.Append($"PrivateMemorySize64: {process.PrivateMemorySize64,12}{Environment.NewLine}");
                sb.Append($"VirtualMemorySize64: {process.VirtualMemorySize64,12}{Environment.NewLine}");
                sb.Append($"StartTime: {process.StartTime}{Environment.NewLine}");
                sb.Append($"Database sizes: DB {DBHelper.Database}{Environment.NewLine}");

                using var con = await _connectionFactory.Value.CreateConnectionAsync(cancellationToken);
                using var cmd = new MySqlCommand(@"
SELECT
  TABLE_NAME AS `Table`,
  ROUND((DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024) AS `Size (MB)`,
  ROUND((DATA_LENGTH) / 1024 / 1024) AS `Data (MB)`,
  ROUND((INDEX_LENGTH) / 1024 / 1024) AS `Index (MB)`
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = @dbschema
  and ROUND((DATA_LENGTH + INDEX_LENGTH) / 1024 / 1024) > 0.9
ORDER BY
  (DATA_LENGTH + INDEX_LENGTH)
DESC", con);
                cmd.Parameters.Add("@dbschema", MySqlDbType.VarChar, 64).Value = DBHelper.Database;

                using var dr = await cmd.ExecuteReaderAsync(cancellationToken);
                var firstLine = false;
                while (await dr.ReadAsync(cancellationToken))
                {
                    if (!firstLine)
                    {
                        ExceptionlessClient.Default
                            .CreateLog("TLStats", $"largest table {dr[0]} has {dr[1]}mb (data:{dr[2]} index:{dr[3]})", Exceptionless.Logging.LogLevel.Info)
                            .FirstCarUserID()
                            .Submit();
                    }
                    firstLine = true;
                    sb.Append($"  table {dr[0]} has {dr[1]}mb (data:{dr[2]} index:{dr[3]}){Environment.NewLine}");
                }
            }
            catch (MySqlException ex)
            {
                Logfile.Log($"TLStats Dump DB error: {ex.ErrorCode} - {ex.Message}");
                ex.ToExceptionless().FirstCarUserID().Submit();
            }
            catch (InvalidOperationException ex)
            {
                Logfile.Log($"TLStats Dump error: {ex.Message}");
            }
            return sb.ToString();
        }
    }
}

