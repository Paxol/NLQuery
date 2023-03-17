using System.Data;
using System.Data.SqlClient;

namespace NLQuery;

public class DatabaseSchemaParser
{
    public required string ConnectionString { get; init; }

    private string _lastDbResult = string.Empty;

    public async Task<string> GetDatabaseSchemaAsync(bool forcePull = false)
    {
        if (!forcePull && !string.IsNullOrEmpty(_lastDbResult)) return _lastDbResult;

        await using (var conn = new SqlConnection(ConnectionString))
        {
            await conn.OpenAsync();

            IEnumerable<Table> tables = (await conn.GetSchemaAsync("Columns")).AsEnumerable()
                .Select(x => (x["TABLE_NAME"].ToString(), x["COLUMN_NAME"].ToString(), x["DATA_TYPE"].ToString()))
                .GroupBy(x => x.Item1)
                .Select(x => new Table
                {
                    Name = x.Key!,
                    Fields = x.Select(y=>(y.Item2!, y.Item3!))
                });

            _lastDbResult = string.Join("\n", tables);

            await conn.CloseAsync();
            
            return _lastDbResult;
        }
    }
    
    private record Table
    {
        public string Name { get; init; }
        public IEnumerable<(string name, string type)> Fields { get; init; }

        public override string ToString() => $"# {Name} ({string.Join(", ", Fields.Select(x => x.name))})";
    }
}

