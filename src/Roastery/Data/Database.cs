using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Roastery.Util;
using Serilog;

namespace Roastery.Data
{
    class Database
    {
        readonly string _schemaName;
        readonly ILogger _logger;
        readonly object _sync = new();
        readonly IDictionary<string, IIdentifiable> _data = new Dictionary<string, IIdentifiable>();
        
        public Database(ILogger logger, string schemaName)
        {
            _schemaName = schemaName;
            _logger = logger.ForContext<Database>();
        }

        public void BulkLoad<T>(params T[] rows) where T: IIdentifiable, new()
        {
            lock (_sync)
            {
                foreach (var row in rows)
                {
                    _data.Add(row.Id, Clone(row));
                }
            }
        }

        public async Task<List<T>> SelectAsync<T>() where T: IIdentifiable, new()
        {
            return await SelectAsync<T>(null, null);
        }
        
        public async Task<List<T>> SelectAsync<T>(
            Func<T, bool> predicate,
            string where) where T: IIdentifiable, new()
        {
            // Not how you should build SQL at home, folks ;-)
            var sql = $"select * from {TableName<T>()}";
            if (where != null)
                sql += " where " + where;

            List<T> rows;
            lock (_sync)
                rows = _data.Values.OfType<T>().Where(predicate ?? (_ => true)).Select(Clone).ToList();

            await LogExecAsync(sql, rows.Count);
            return rows;
        }

        public async Task InsertAsync<T>(T row) where T: IIdentifiable, new()
        {
            row.Id = typeof(T).Name.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("n").Substring(10);
            
            lock (_sync)
                _data.Add(row.Id, Clone(row));

            var columns = typeof(T).GetTypeInfo().DeclaredProperties
                .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic &&
                            (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) || p.PropertyType == typeof(DateTime)))
                .Where(p => p.Name != nameof(IIdentifiable.Id))
                .ToDictionary(
                    p => p.Name.ToLowerInvariant(),
                    p => AsSqlLiteral(p.GetValue(row)));
            
            var sql = $"insert into {TableName<T>()} ({string.Join(", ", columns.Keys)}) values ({string.Join(", ", columns.Values)}) returning id;";
            await LogExecAsync(sql, 1);
        }
        
        public async Task UpdateAsync<T>(T row, string updatedColumns) where T: IIdentifiable, new()
        {
            var rows = 0;
            lock (_sync)
            {
                if (_data.TryGetValue(row.Id, out var existing) &&
                    existing is T)
                {
                    rows = 1;
                    _data[row.Id] = Clone(row);
                }
            }
            
            var sql = $"update {TableName<T>()} set {updatedColumns} where id = '{row.Id}';";
            await LogExecAsync(sql, rows);
        }
        
        public async Task DeleteAsync<T>(string rowId) where T: IIdentifiable, new()
        {
            var rows = 0;
            lock (_sync)
            {
                if (_data.TryGetValue(rowId, out var existing) &&
                    existing is T)
                {
                    rows = 1;
                    _data.Remove(rowId);
                }
            }
            
            var sql = $"delete from {TableName<T>()} where id = '{rowId}';";
            await LogExecAsync(sql, rows);
        }

        static string AsSqlLiteral(object o)
        {
            if (o == null) return "null";
            if (o is string s) return $"'{s.Replace("'", "''")}'";
            if (o.GetType().GetTypeInfo().IsEnum) return $"'{o}'";
            return ((IFormattable) o).ToString(null, CultureInfo.InvariantCulture);
        }

        string TableName<T>() where T : IIdentifiable, new()
        {
            return _schemaName + "." + typeof(T).Name.ToLowerInvariant();
        }

        async Task LogExecAsync(string sql, int rowCount)
        {
            if (Distribution.OnceIn(200))
            {
                throw new OperationCanceledException(
                    "A deadlock was detected and the transaction chosen as the deadlock victim.");
            }
            
            var sw = Stopwatch.StartNew();
            var delay = 10 + (int)(Distribution.Uniform() * Math.Pow(rowCount, 1.6));
            await Task.Delay(delay);
            _logger.Debug("Execution of {Sql} affected {RowCount} rows in {Elapsed:0.000} ms",
                sql, rowCount, sw.Elapsed.TotalMilliseconds);
        }

        static T Clone<T>(T value) where T: IIdentifiable, new()
        {
            var dest = new T();
            var cloneable = typeof(T).GetTypeInfo().DeclaredProperties
                .Where(p => p.CanRead && p.GetMethod!.IsPublic && !p.GetMethod.IsStatic &&
                            p.CanWrite && p.SetMethod!.IsPublic && !p.SetMethod.IsStatic &&
                            (p.PropertyType.IsPrimitive || p.PropertyType == typeof(string) ||
                             p.PropertyType == typeof(DateTime)));

            foreach (var prop in cloneable)
            {
                var src = prop.GetValue(value);
                prop.SetValue(dest, src);
            }

            return dest;
        }
    }
}