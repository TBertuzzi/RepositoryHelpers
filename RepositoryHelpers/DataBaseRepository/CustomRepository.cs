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

        #region IDb
        //DefaultConnection
        private DbConnection _DBConnection;
        private DbConnection DBConnection
        {
            get
            {
                if (_DBConnection == null)
                {
                    try
                    {
                        _DBConnection = _connection.DataBaseConnection;
                    }
                    catch (Exception exception)
                    {
                        throw new Exception(exception.Message);
                    }
                }

                return _DBConnection;
            }
            set
            {
                _DBConnection = value;
            }
        }


        //Default Command
        private DbCommand _DBCommand;
        public DbCommand DbCommand
        {
            set
            {
                _DBCommand = value;
            }
            get
            {
                if (_DBCommand == null)
                {
                    _DBCommand = _connection.GetCommand();
                    _DBCommand.Connection = DBConnection;
                }
                return _DBCommand;
            }
        }

        private void DisposeDB(bool dispose)
        {
            if (dispose)
            {
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                DbCommand.Dispose();
                DbCommand = null;
            }
        }

        #endregion


        #region DAPPER

        private bool IgnoreAttribute(IEnumerable<CustomAttributeData> customAttributes)
        {
            var customAttributeData = customAttributes.ToList();
            if (customAttributeData.Any())
            {
                var containsAttributes = customAttributeData.Where(x => x.AttributeType.ToString().ToUpper() == DapperIgnore
               || x.AttributeType.ToString().ToUpper() == IdentityIgnore);

                return containsAttributes.Any();
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
                var primaryKeyAttribute = p.CustomAttributes.ToList().Any(x => x.AttributeType.ToString().ToUpper() == PrimaryKey);

                if (primaryKeyAttribute)
                {
                    return p.Name;
                }
            }

            return "";
        }

        /// <summary>
        /// Update an item asynchronously (does not support transaction)
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public async Task UpdateAsync(T item, CustomTransaction customTransaction)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            var isCustomTransaction = customTransaction != null;

            try
            {
                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;

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
                            parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                        }

                        var primaryKeyAttribute = p.CustomAttributes.ToList().Any(x => x.AttributeType.ToString().ToUpper() == PrimaryKey);

                        if (primaryKeyAttribute)
                        {
                            primaryKey = p.Name;
                            parameters.Add($"@{primaryKey}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                        }
                    }

                    sql.Remove(sql.Length - 1, 1);

                    if (string.IsNullOrEmpty(primaryKey))
                        throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                    sql.AppendLine($" where {primaryKey} = @{primaryKey}");

                if(isCustomTransaction)
                    await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction);
                else
                    await connection.ExecuteAsync(sql.ToString(), parameters);
              //  }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Update an item(does not support transaction)
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public void Update(T item, CustomTransaction customTransaction)
            => UpdateAsync(item, customTransaction).Wait();

        /// <summary>
        /// Update an item(does not support transaction)
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public void Update(T item)
            => UpdateAsync(item, null).Wait();

        /// <summary>
        /// Update an item(does not support transaction)
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public async Task UpdateAsync(T item)
            => await UpdateAsync(item, null);


        /// <summary>
        /// Insert an item asynchronously (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<int> InsertAsync(T item, bool identity, CustomTransaction customTransaction)
        {

            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            var isCustomTransaction = customTransaction != null;

            try
            {
                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;

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
                    if (isCustomTransaction)
                        return await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction);
                    else
                        return await connection.ExecuteAsync(sql.ToString(), parameters);
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }


        /// <summary>
        /// Insert an item (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public int Insert(T item, bool identity, CustomTransaction customTransaction) =>
            InsertAsync(item, identity, customTransaction).Result;

        /// <summary>
        /// Insert an item (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public int Insert(T item, bool identity) =>
            InsertAsync(item, identity, null).Result;

        /// <summary>
        /// Insert an item (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<int> InsertAsync(T item, bool identity) =>
           await InsertAsync(item, identity, null);

        /// <summary>
        /// Get all rows in the table asynchronously (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync(CustomTransaction customTransaction)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;

                if(isCustomTransaction)
                    return await connection.QueryAsync<T>($"Select * from {typeof(T).Name} ", customTransaction.DbCommand.Transaction);
                else
                    return await connection.QueryAsync<T>($"Select * from {typeof(T).Name} ");
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Get all rows in the table (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get(CustomTransaction customTransaction)
            => GetAsync(customTransaction).Result;

        /// <summary>
        /// Get all rows in the table (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get()
            => GetAsync(null).Result;

        /// <summary>
        /// Get all rows in the table (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync()
            => await GetAsync(null);


        /// <summary>
        /// Get the result of a query with parameters asynchronously (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;

                if(isCustomTransaction)
                    return await connection.QueryAsync<T>(sql, parameters, customTransaction.DbCommand.Transaction);
                else
                    return await connection.QueryAsync<T>(sql, parameters);

            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        /// <summary>
        /// Get the result of a query with parameters (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Get the result of a query with parameters (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters)
            => GetAsync(sql, parameters, null).Result;

        /// <summary>
        /// Get the result of a query with parameters (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters)
            => await GetAsync(sql, parameters, null);


        /// <summary>
        /// Get the item by id asynchronously (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id, CustomTransaction customTransaction)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var primaryKey = "";
                primaryKey = GetPrimaryKey(typeof(T));

                if (string.IsNullOrEmpty(primaryKey))
                    throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;

                if (isCustomTransaction)
                    return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {typeof(T).Name} where {primaryKey} = @ID ", new { ID = id }, customTransaction.DbCommand.Transaction);
                else
                    return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {typeof(T).Name} where {primaryKey} = @ID ", new { ID = id });

            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }


        /// <summary>
        /// Get the item by id (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public T GetById(object id, CustomTransaction customTransaction)
            => GetByIdAsync(id, customTransaction).Result;

        /// <summary>
        /// Get the item by id (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public T GetById(object id)
            => GetByIdAsync(id,null).Result;

        /// <summary>
        /// Get the item by id (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id)
            => await GetByIdAsync(id, null);

        /// <summary>
        /// Delete an item by id asynchronously (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public async Task DeleteAsync(object id, CustomTransaction customTransaction)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var primaryKey = "";
                primaryKey = GetPrimaryKey(typeof(T));

                if (string.IsNullOrEmpty(primaryKey))
                    throw new CustomRepositoryException("PrimaryKeyAttribute not defined");

                var connection = isCustomTransaction ? customTransaction.DbCommand.Connection : _connection.DataBaseConnection;
                var sql = new StringBuilder();

                    var parameters = new Dictionary<string, object>
                {
                    { "@ID", id }
                };

                    sql.AppendLine($"delete from {typeof(T).Name} where {primaryKey} = @ID");

                if(isCustomTransaction)
                    await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction);
                else

                    await connection.ExecuteAsync(sql.ToString(), parameters);

            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        /// <summary>
        /// Delete an item by id(does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public void Delete(object id, CustomTransaction customTransaction)
            => DeleteAsync(id, customTransaction).Wait();

        /// <summary>
        /// Delete an item by id(does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public void Delete(object id)
            => DeleteAsync(id, null).Wait();

        /// <summary>
        /// Delete an item by id(does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public async Task DeleteAsync(object id)
            => await DeleteAsync(id, null);



        #endregion

        #region ADO

        /// <summary>
        /// Get DataSet result with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters)
           => GetDataSet(sql, parameters, null);


        /// <summary>
        /// Get DataSet result with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;


                DbCommand.CommandType = CommandType.Text;
                DbCommand.Parameters.Clear();
                DbCommand.CommandTimeout = 120;
                DbCommand.CommandText = sql;

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();

                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                using (var da = _connection.GetDataAdapter())
                {
                    da.SelectCommand = DbCommand;

                    using (var ds = new DataSet())
                    {
                        da.Fill(ds);
                        return ds;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }

        /// <summary>
        /// Executes a query with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;


                DbCommand.CommandType = CommandType.Text;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();
                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                DbCommand.CommandText = sql;
                return await DbCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }

        /// <summary>
        /// Executes a query with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => ExecuteQueryAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Executes a query with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters)
            => ExecuteQueryAsync(sql, parameters, null).Result;

        /// <summary>
        /// Executes a query with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters)
            => await ExecuteQueryAsync(sql, parameters, null);


        /// <summary>
        /// Executes a insert with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="identity">Primary Key or Oracle sequence</param>
        /// <returns>Primary Key After Insert</returns>
        public async Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity, CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                StringBuilder sbSql = new StringBuilder();

                if (_connection.Database == DataBaseType.SqlServer)
                {
                    sbSql.AppendLine(sql);
                    sbSql.AppendLine("SELECT CAST(SCOPE_IDENTITY() as int);");
                }
                else
                {
                    sbSql.AppendLine($"BEGIN {sql} SELECT {identity}.currval FROM DUAL END; ");
                }

                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                DbCommand.CommandType = CommandType.Text;
                DbCommand.CommandTimeout = 120;
                DbCommand.Parameters.Clear();
                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                DbCommand.CommandText = sbSql.ToString();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();

                var identityReturn = await DbCommand.ExecuteScalarAsync();

                return identityReturn.ToString();
            }
            catch (Exception exp)
            {
                throw new Exception(exp.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }


        /// <summary>
        /// Executes a Procedure with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                DbCommand.CommandType = CommandType.StoredProcedure;
                DbCommand.CommandText = procedure;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();

                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                return await DbCommand.ExecuteNonQueryAsync();
            }
            catch (Exception exp)
            {
                throw new Exception(exp.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }

        /// <summary>
        /// Executes a Procedure with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
                => ExecuteProcedureAsync(procedure, parameters,customTransaction).Result;

        /// <summary>
        /// Executes a Procedure with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters)
                => ExecuteProcedureAsync(procedure, parameters, null).Result;

        /// <summary>
        /// Executes a Procedure with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters)
                => await ExecuteProcedureAsync(procedure, parameters, null);

        /// <summary>
        /// Get Procedure DataSet with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet</returns>
        public DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                DbCommand.CommandType = CommandType.StoredProcedure;
                DbCommand.CommandText = procedure;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();

                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                using (var da = _connection.GetDataAdapter())
                {
                    da.SelectCommand = DbCommand;

                    using (var ds = new DataSet())
                    {
                        da.Fill(ds);
                        return ds;
                    }
                }
            }
            catch (Exception exp)
            {
                throw new Exception(exp.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }

        /// <summary>
        /// Executes Scalar with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
          => ExecuteScalarAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Executes Scalar with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
          => ExecuteScalarAsync(sql, parameters, null).Result;

        /// <summary>
        /// Executes Scalar with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters) 
          => await ExecuteScalarAsync(sql, parameters, null);


        /// <summary>
        /// Executes Scalar with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters,CustomTransaction customTransaction)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                DbCommand.CommandType = CommandType.Text;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();
                foreach (var parameter in parameters)
                {
                    DbCommand.Parameters.Add(_connection.GetParameter(parameter));
                }

                DbCommand.CommandText = sql;
                return await DbCommand.ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
            finally
            {
                DisposeDB(isCustomTransaction);
            }
        }

        public string GetConnectionString()
        {
            return this._connection.ConnectionString;
        }

        #endregion

    }
}
