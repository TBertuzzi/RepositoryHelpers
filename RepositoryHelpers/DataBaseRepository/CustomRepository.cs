using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
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
        private const string DapperIgnore = "REPOSITORYHELPERS.DAPPERIGNORE";
        private const string PrimaryKey = "REPOSITORYHELPERS.PRIMARYKEY";
        private const string IdentityIgnore = "REPOSITORYHELPERS.IDENTITYIGNORE";

        private readonly Connection _connection;
        public CustomRepository(Connection connection)
        {
            _connection = connection;
        }

        #region DAPPER

        private bool IgnoreAttribute(IEnumerable<CustomAttributeData> customAttributes)
        {
            if (customAttributes.Any())
            {
                var constainsAttributes = customAttributes.ToList().Where(x => x.AttributeType.ToString().ToUpper() == DapperIgnore
               || x.AttributeType.ToString().ToUpper() == IdentityIgnore);

                return constainsAttributes.Any();
            }
            else
            {
                return false;
            }
        }

        private string GetPrimaryKey(Type type)
        {
            foreach (var p in type.GetProperties())
            {
                var primaryKeyAttribute = p.CustomAttributes.ToList().Where(x => x.AttributeType.ToString().ToUpper() == PrimaryKey).Any();

                if (primaryKeyAttribute)
                {
                    return p.Name;
                }
            }

            return "";
        }


        public async Task UpdateAsync(T item)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            try
            {
                using (var connection = _connection.DataBaseConnection)
                {
                    var sql = new StringBuilder();
                    var primaryKey = "";
                    var parameters = new Dictionary<string, object>();

                    sql.AppendLine($"update {typeof(T).Name} set ");

                    foreach (var p in item.GetType().GetProperties())
                    {
                        if (item.GetType().GetProperty(p.Name) == null) continue;

                        if (!IgnoreAttribute(p.CustomAttributes))
                        {
                            sql.Append($" {p.Name} = @{p.Name},");
                        }
                        parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));

                        var primaryKeyAttribute = p.CustomAttributes.ToList().Where(x => x.AttributeType.ToString().ToUpper() == PrimaryKey).Any();

                        if (primaryKeyAttribute)
                        {
                            primaryKey = p.Name;
                        }
                    }

                    sql.Remove(sql.Length - 1, 1);

                    if (string.IsNullOrEmpty(primaryKey))
                        throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                    sql.AppendLine($" where {primaryKey} = @ID");

                    await connection.ExecuteAsync(sql.ToString(), parameters);
                }
            }
            catch(Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        public void Update(T item)
            => UpdateAsync(item).Wait();


        public async Task<int> InsertAsync(T item, bool identity)
        {

            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            try
            {
                using (var connection = _connection.DataBaseConnection)
                {
                    var sql = new StringBuilder();
                    var sqlParameters = new StringBuilder();

                    var parameters = new Dictionary<string, object>();

                    foreach (var p in item.GetType().GetProperties())
                    {
                        if (item.GetType().GetProperty(p.Name) == null) continue;

                        if (IgnoreAttribute(p.CustomAttributes)) continue;

                        sqlParameters.Append($"@{p.Name},");
                        parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                    }

                    sqlParameters.Remove(sqlParameters.Length - 1, 1);

                    sql.AppendLine($"insert into {typeof(T).Name} ({sqlParameters.ToString().Replace("@", "")}) values ({sqlParameters.ToString()}) ");
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
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

      
        public int Insert(T item, bool identity) =>
            InsertAsync(item, identity).Result;


        public async Task<IEnumerable<T>> GetAsync()
        {
            try
            {
                using (DbConnection connection = _connection.DataBaseConnection)
                {
                    return await connection.QueryAsync<T>($"Select * from {typeof(T).Name} ");
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        public IEnumerable<T> Get()
            => GetAsync().Result;


        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                using (DbConnection connection = _connection.DataBaseConnection)
                {
                    return await connection.QueryAsync<T>(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters)
            => GetAsync(sql, parameters).Result;


        public async Task<T> GetByIdAsync(object id)
        {
            try
            {
                var primaryKey = "";
                primaryKey = GetPrimaryKey(typeof(T));

                if (string.IsNullOrEmpty(primaryKey))
                    throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                using (var connection = _connection.DataBaseConnection)
                {
                    return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {typeof(T).Name} where {primaryKey} = @ID ", new { ID = id });
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

      

        public T GetById(object id)
            => GetByIdAsync(id).Result;


        public async Task DeleteAsync(object id)
        {
            try
            {
                var primaryKey = "";
                primaryKey = GetPrimaryKey(typeof(T));

                if (string.IsNullOrEmpty(primaryKey))
                    throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                using (var connection = _connection.DataBaseConnection)
                {
                    var sql = new StringBuilder();

                    var parameters = new Dictionary<string, object>
                {
                    { "@ID", id }
                };

                    sql.AppendLine($"delete from {typeof(T).Name} where {primaryKey} = @ID");

                    await connection.ExecuteAsync(sql.ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        public void Delete(object id)
            => DeleteAsync(id).Wait();

        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
            => ExecuteScalarAsync(sql, parameters).Result;


        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            { 
            using (var connection = _connection.DataBaseConnection)
            {
                return await connection.ExecuteScalarAsync(sql, parameters);
            }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        #endregion

        #region ADO


        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters)
        {
            try
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
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                using (var connection = _connection.DataBaseConnection)
                {
                    return await connection.ExecuteAsync(sql, parameters);
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        public int ExecuteQuery(string sql, Dictionary<string, object> parameters)
            => ExecuteQueryAsync(sql, parameters).Result;

        #endregion

    }
}
