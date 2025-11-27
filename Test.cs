{
  "ConnectionStrings": {
    "OracleDb": "User Id=USER;Password=PASS;Data Source=YOUR_ORACLE_HOST:1521/ORCL;"
  }
}

using System.Data;
using System.Threading.Tasks;

public interface IOracleService
{
    Task<DataTable> QueryAsync(string sql, params OracleParameter[] parameters);
    Task<int> ExecuteAsync(string sql, params OracleParameter[] parameters);
}

using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Threading.Tasks;

public class OracleService : IOracleService
{
    private readonly string _connectionString;

    public OracleService(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("OracleDb");
    }

    /// <summary>
    /// Выполняет SELECT запрос и возвращает DataTable.
    /// </summary>
    public async Task<DataTable> QueryAsync(string sql, params OracleParameter[] parameters)
    {
        var table = new DataTable();

        using var conn = new OracleConnection(_connectionString);
        using var cmd = new OracleCommand(sql, conn);

        if (parameters != null)
            cmd.Parameters.AddRange(parameters);

        await conn.OpenAsync();

        using var reader = await cmd.ExecuteReaderAsync();
        table.Load(reader);

        return table;
    }

    /// <summary>
    /// Выполняет INSERT, UPDATE, DELETE. Возвращает количество изменённых строк.
    /// </summary>
    public async Task<int> ExecuteAsync(string sql, params OracleParameter[] parameters)
    {
        using var conn = new OracleConnection(_connectionString);
        using var cmd = new OracleCommand(sql, conn);

        if (parameters != null)
            cmd.Parameters.AddRange(parameters);

        await conn.OpenAsync();

        return await cmd.ExecuteNonQueryAsync();
    }
}