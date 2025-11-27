/*
===============================================================
      ПОЛНЫЙ OracleService ДЛЯ BLAZOR SERVER
   ─ ЛОГИРОВАНИЕ  ─ ПРОЦЕДУРЫ ─ REF CURSOR ─ MULTI-CONNECTION ─
===============================================================

===============================================================
  appsettings.json  — кастомная секция подключений
===============================================================
"OracleConnections": {
  "Main":      "User Id=MAIN;Password=MAIN_PASS;Data Source=host:1521/main",
  "Analytics": "User Id=AN;Password=AN_PASS;Data Source=host:1521/analytics",
  "Archive":   "User Id=ARC;Password=ARC_PASS;Data Source=host:1521/archive"
}

===============================================================
  OracleConnectionsOptions.cs
===============================================================
public class OracleConnectionsOptions
{
    public string Main { get; set; }
    public string Analytics { get; set; }
    public string Archive { get; set; }
}

===============================================================
  Program.cs — регистрация DI
===============================================================
builder.Services.Configure<OracleConnectionsOptions>(
    builder.Configuration.GetSection("OracleConnections"));

builder.Services.AddScoped<IOracleService, OracleService>();

===============================================================
  IOracleService.cs — интерфейс
===============================================================
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

public interface IOracleService
{
    // SELECT
    Task<DataTable> QueryMainAsync(string sql, params OracleParameter[] p);
    Task<DataTable> QueryAnalyticsAsync(string sql, params OracleParameter[] p);
    Task<DataTable> QueryArchiveAsync(string sql, params OracleParameter[] p);

    // DML (INSERT / UPDATE / DELETE)
    Task<int> ExecuteMainAsync(string sql, params OracleParameter[] p);
    Task<int> ExecuteAnalyticsAsync(string sql, params OracleParameter[] p);
    Task<int> ExecuteArchiveAsync(string sql, params OracleParameter[] p);

    // Stored Procedures (без курсора)
    Task ExecuteProcedureAsync(string connStr, string procName, params OracleParameter[] p);

    // Stored Procedures (REF CURSOR)
    Task<DataTable> ExecuteProcedureCursorAsync(string connStr, string procName, params OracleParameter[] p);
}

===============================================================
  OracleService.cs — ПОЛНАЯ РЕАЛИЗАЦИЯ
===============================================================
using System.Data;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;

public class OracleService : IOracleService
{
    private readonly OracleConnectionsOptions _conn;

    public OracleService(IOptions<OracleConnectionsOptions> opt)
    {
        _conn = opt.Value;
    }

    // ============================================================
    //                PUBLIC HIGH-LEVEL METHODS
    // ============================================================

    public Task<DataTable> QueryMainAsync(string sql, params OracleParameter[] p)
        => QueryInternalAsync(_conn.Main, sql, p);

    public Task<DataTable> QueryAnalyticsAsync(string sql, params OracleParameter[] p)
        => QueryInternalAsync(_conn.Analytics, sql, p);

    public Task<DataTable> QueryArchiveAsync(string sql, params OracleParameter[] p)
        => QueryInternalAsync(_conn.Archive, sql, p);

    public Task<int> ExecuteMainAsync(string sql, params OracleParameter[] p)
        => ExecuteInternalAsync(_conn.Main, sql, p);

    public Task<int> ExecuteAnalyticsAsync(string sql, params OracleParameter[] p)
        => ExecuteInternalAsync(_conn.Analytics, sql, p);

    public Task<int> ExecuteArchiveAsync(string sql, params OracleParameter[] p)
        => ExecuteInternalAsync(_conn.Archive, sql, p);

    // ============================================================
    //                     PRIVATE INTERNALS
    // ============================================================

    private async Task<DataTable> QueryInternalAsync(string connStr, string sql, params OracleParameter[] parameters)
    {
        Logger.Info(LogType.Database, $"SQL Query: {sql}");

        var table = new DataTable();

        try
        {
            using var conn = new OracleConnection(connStr);
            using var cmd = new OracleCommand(sql, conn);

            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            table.Load(reader);

            Logger.Success(LogType.Database, $"Rows: {table.Rows.Count}");
        }
        catch (Exception ex)
        {
            Logger.Exception(LogType.Database, ex, $"Ошибка выполнения SELECT: {sql}");
            throw;
        }

        return table;
    }

    private async Task<int> ExecuteInternalAsync(string connStr, string sql, params OracleParameter[] parameters)
    {
        Logger.Info(LogType.Database, $"SQL Execute: {sql}");

        try
        {
            using var conn = new OracleConnection(connStr);
            using var cmd = new OracleCommand(sql, conn);

            if (parameters?.Length > 0)
                cmd.Parameters.AddRange(parameters);

            await conn.OpenAsync();
            int affected = await cmd.ExecuteNonQueryAsync();

            Logger.Success(LogType.Database, $"Rows affected: {affected}");
            return affected;
        }
        catch (Exception ex)
        {
            Logger.Exception(LogType.Database, ex, $"Ошибка выполнения DML: {sql}");
            throw;
        }
    }

    // ============================================================
    //               STORED PROCEDURE — NO CURSOR
    // ============================================================

    public async Task ExecuteProcedureAsync(string connStr, string procName, params OracleParameter[] p)
    {
        Logger.Info(LogType.Database, $"CALL PROCEDURE: {procName}");

        try
        {
            using var conn = new OracleConnection(connStr);
            using var cmd = new OracleCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (p?.Length > 0)
                cmd.Parameters.AddRange(p);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            Logger.Success(LogType.Database, "Procedure executed");
        }
        catch (Exception ex)
        {
            Logger.Exception(LogType.Database, ex, $"Ошибка выполнения процедуры: {procName}");
            throw;
        }
    }

    // ============================================================
    //               STORED PROCEDURE — REF CURSOR
    // ============================================================

    public async Task<DataTable> ExecuteProcedureCursorAsync(string connStr, string procName, params OracleParameter[] p)
    {
        Logger.Info(LogType.Database, $"CALL PROC CURSOR: {procName}");

        var table = new DataTable();

        try
        {
            using var conn = new OracleConnection(connStr);
            using var cmd = new OracleCommand(procName, conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            if (p?.Length > 0)
                cmd.Parameters.AddRange(p);

            // REF CURSOR OUTPUT
            var cursor = new OracleParameter("cursor_out", OracleDbType.RefCursor, ParameterDirection.Output);
            cmd.Parameters.Add(cursor);

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            table.Load(reader);

            Logger.Success(LogType.Database, $"Procedure cursor rows: {table.Rows.Count}");
        }
        catch (Exception ex)
        {
            Logger.Exception(LogType.Database, ex, $"Ошибка выполнения процедуры с курсором: {procName}");
            throw;
        }

        return table;
    }
}

===============================================================
 Пример процедуры в Oracle
===============================================================
CREATE OR REPLACE PROCEDURE GET_USERS(p_cursor OUT SYS_REFCURSOR) AS
BEGIN
    OPEN p_cursor FOR
        SELECT * FROM USERS;
END;

===============================================================
 Пример вызова процедуры с курсором
===============================================================
var table = await Oracle.ExecuteProcedureCursorAsync(
    _conn.Main,
    "GET_USERS"
);

===============================================================
 Пример вызова процедуры с параметрами
===============================================================
var p1 = new OracleParameter("id", OracleDbType.Int32, 10, ParameterDirection.Input);

await Oracle.ExecuteProcedureAsync(
    _conn.Main,
    "UPDATE_USER",
    p1
);

===============================================================
  ГОТОВО ✔
  Теперь сервис Oracle:
  - поддерживает SELECT/DML
  - поддерживает процедуры
  - поддерживает REF CURSOR
  - логирует все запросы
  - работает с несколькими DB
===============================================================
*/