using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using RepositoryHelpers.DataBase;
using RepositoryHelpers.DataBaseRepository.Base;
using RepositoryHelpers.Utils;

namespace RepositoryHelpers.DataBaseRepository
{
    public sealed class CustomRepository<T> : ICustomRepository<T>
    {
        private const string DapperIgnore = "RepositoryHelpers.DAPPERIGNORE";
        
        private readonly Connection _connection;
        public CustomRepository(Connection connection)
        {
            _connection = connection;
        }

        #region DAPPER

        public async Task UpdateAsync(T item)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();
            
            using (var connection = _connection.DataBaseConnection)
            {
                var sql = new StringBuilder();

                var parameters = new Dictionary<string, object>();

                sql.AppendLine($"update {typeof(T).Name} set ");

                foreach (var p in item.GetType().GetProperties())
                {
                    if (item.GetType().GetProperty(p.Name) == null) continue;
                    
                    var dapperAttribute = p.CustomAttributes.Any() ? p.CustomAttributes.ToList()[0].AttributeType.ToString() : "";
                    if (p.Name.ToUpper() != "ID" && dapperAttribute.ToUpper() != DapperIgnore)
                    {
                        sql.Append($" {p.Name} = @{p.Name},");
                    }
                    parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                }

                sql.Remove(sql.Length - 1, 1);
                sql.AppendLine(" where id = @ID");

                await connection.ExecuteAsync(sql.ToString(), parameters);
            }
        }

        public void Update(T item)
            => UpdateAsync(item).Wait();


        public async Task<int> InsertAsync(T item, bool identity)
        {

            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();
            
            using (var connection = _connection.DataBaseConnection)
            {
                var sql = new StringBuilder();

                var parameters = new Dictionary<string, object>();

                sql.AppendLine($"insert into {typeof(T).Name} values (");

                foreach (var p in item.GetType().GetProperties())
                {
                    if (item.GetType().GetProperty(p.Name) == null) continue;
                    
                    var dapperAttribute = p.CustomAttributes.Any() ? p.CustomAttributes.ToList()[0].AttributeType.ToString(): "";
                    
                    if (p.Name.ToUpper() == "ID" ||
                        dapperAttribute.ToUpper() == DapperIgnore) continue;
                    
                    sql.Append($"@{p.Name},");
                    parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                }

                sql.Remove(sql.Length - 1, 1);
                sql.AppendLine(");");

                if (identity)
                {
                    sql.AppendLine("SELECT CAST(SCOPE_IDENTITY() as int);");
                    return connection.QuerySingleOrDefault<int>(sql.ToString(), parameters);
                }
                else
                {
                    return await connection.ExecuteAsync(sql.ToString(), parameters);
                }
            }
        }

        public int Insert(T item, bool identity) =>
            InsertAsync(item, identity).Result;


        public async Task<IEnumerable<T>> GetAsync()
        {
            using (DbConnection connection = _connection.DataBaseConnection)
            {
                return await connection.QueryAsync<T>($"Select * from {typeof(T).Name} ");
            }
        }

        public IEnumerable<T> Get()
            => GetAsync().Result;


        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters)
        {

            using (DbConnection connection = _connection.DataBaseConnection)
            {
                return await connection.QueryAsync<T>(sql, parameters);
            }
        }

        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters)
            => GetAsync(sql, parameters).Result;


        public async Task<T> GetByIdAsync(object id)
        {
            using (var connection = _connection.DataBaseConnection)
            {
                return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {typeof(T).Name} where id = @ID ", new { ID = id });
            }
        }

        public T GetById(object id)
            => GetByIdAsync(id).Result;


        public async Task DeleteAsync(object id)
        {
            using (var connection = _connection.DataBaseConnection)
            {
                var sql = new StringBuilder();

                var parameters = new Dictionary<string, object>
                {
                    { "@ID", id }
                };

                sql.AppendLine($"delete from {typeof(T).Name} where id = @ID");

                await connection.ExecuteAsync(sql.ToString(), parameters);
            }
        }

        public void Delete(object id)
            => DeleteAsync(id).Wait();

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
            => ExecuteScalarAsync(sql, parameters).Result;


        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters)
        {
            using (var connection = _connection.DataBaseConnection)
            {
                return await connection.ExecuteScalarAsync(sql, parameters);
            }
        }

        #endregion

        #region ADO


        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters)
        {
            using (var connection = _connection.DataBaseConnection)
            {
                using (var cmd = _connection.GetCommand(sql, connection))
                {
                    cmd.Parameters.Clear();
                    cmd.CommandTimeout = 120; 

                    foreach (var parameter in parameters)
                    {
                        cmd.Parameters.Add(_connection.GetParameter(parameter));
                    }

                    using (var da = _connection.GetDataAdapter())
                    {
                        da.SelectCommand = cmd;

                        using (var ds = new DataSet())
                        {
                            da.Fill(ds);
                            return ds;
                        }
                    }
                }
            }
        }

        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters)
        {
            using (var connection = _connection.DataBaseConnection)
            {
                return await connection.ExecuteAsync(sql, parameters);
            }
        }

        public int ExecuteQuery(string sql, Dictionary<string, object> parameters)
            => ExecuteQueryAsync(sql, parameters).Result;

        #endregion

    }
}
