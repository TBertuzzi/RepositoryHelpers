using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using RepositoryHelpers.DataBase;
using RepositoryHelpers.DataBaseRepository.Base;
using RepositoryHelpers.Mapping;
using RepositoryHelpers.Utils;

namespace RepositoryHelpers.DataBaseRepository
{
    public sealed class CustomRepository<T> : ICustomRepository<T>
    {
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
                    _DBConnection = _connection.DataBaseConnection;

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

        public void DisposeDB(bool dispose)
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

        private DbConnection GetConnection(CustomTransaction customTransaction)
            => customTransaction?.DbCommand?.Connection ?? _connection.DataBaseConnection;

        /// <summary>
        /// Update an item asynchronously 
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <param name="customTransaction">Has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns></returns>
        public async Task UpdateAsync(T item, CustomTransaction customTransaction, int? commandTimeout)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            var isCustomTransaction = customTransaction != null;

            try
            {
                var connection = GetConnection(customTransaction);

                var sql = new StringBuilder();
                var parameters = new Dictionary<string, object>();

                var primaryKey = MappingHelper.GetPrimaryKey(typeof(T));

                if (!primaryKey.Any())
                    throw new CustomRepositoryException("Primary key is not defined");

                sql.AppendLine($"update {MappingHelper.GetTableName(typeof(T))} set ");

                foreach (var p in item.GetType().GetProperties())
                {
                    if (item.GetType().GetProperty(p.Name) == null) continue;

                    if (!MappingHelper.IsIgnored(typeof(T), p))
                    {
                        sql.Append($" {p.Name} = @{p.Name},");
                        parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                    }
                    else if (primaryKey.Contains(p.Name))
                        parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                }
                sql.Remove(sql.Length - 1, 1);

                sql.Append($" where");

                foreach(var pkcolumn in primaryKey)
                {
                    sql.Append($" {pkcolumn} = @{pkcolumn} AND");
                }
                sql.Remove(sql.Length - 4, 4);

                if (isCustomTransaction)
                    await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction, commandTimeout);
                else
                    await connection.ExecuteAsync(sql.ToString(), parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Update an item asynchronously 
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <param name="customTransaction">Has a transaction object</param>
        /// <returns></returns>
        public async Task UpdateAsync(T item, CustomTransaction customTransaction)
            => await UpdateAsync(item, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public async Task UpdateAsync(T item)
            => await UpdateAsync(item, null, null).ConfigureAwait(false);

        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns></returns>
        public void Update(T item, CustomTransaction customTransaction, int? commandTimeout)
            => UpdateAsync(item, customTransaction, commandTimeout).Wait();
        
        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns></returns>
        public void Update(T item, CustomTransaction customTransaction)
            => UpdateAsync(item, customTransaction, null).Wait();

        /// <summary>
        /// Update an item
        /// </summary>
        /// <param name="item"> item to update</param>
        /// <returns></returns>
        public void Update(T item)
            => UpdateAsync(item, null, null).Wait();


        /// <summary>
        /// Insert an item asynchronously 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<object> InsertAsync(T item, bool identity, CustomTransaction customTransaction, int? commandTimeout)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            var isCustomTransaction = customTransaction != null;

            try
            {
                var connection = GetConnection(customTransaction);

                var sql = new StringBuilder();
                var sqlParameters = new StringBuilder();

                var parameters = new Dictionary<string, object>();

                foreach (var p in item.GetType().GetProperties())
                {
                    if (item.GetType().GetProperty(p.Name) == null) continue;

                    if (MappingHelper.IsIgnored(typeof(T), p)) continue;

                    sqlParameters.Append($"@{p.Name},");
                    parameters.Add($"@{p.Name}", item.GetType().GetProperty(p.Name)?.GetValue(item));
                }

                sqlParameters.Remove(sqlParameters.Length - 1, 1);

                sql.AppendLine($"insert into {MappingHelper.GetTableName(typeof(T))} ({sqlParameters.ToString().Replace("@", "")}) ");

                if (identity)
                {
                    var identityColumn = MappingHelper.GetIdentityColumn(typeof(T));
                    if (string.IsNullOrEmpty(identityColumn))
                        throw new CustomRepositoryException("Identity column is not defined");

                    sql.AppendLine($" OUTPUT inserted.{identityColumn} values ({sqlParameters.ToString()}) ");

                    if (isCustomTransaction)
                        return connection.QuerySingleOrDefault<dynamic>(sql.ToString(), parameters, customTransaction.DbCommand.Transaction, commandTimeout).Id;
                    else
                        return connection.QuerySingleOrDefault<dynamic>(sql.ToString(), parameters, commandTimeout: commandTimeout).Id;
                }
                else
                {
                    sql.AppendLine($" values ({sqlParameters.ToString()}) ");

                    if (isCustomTransaction)
                        return await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction, commandTimeout);
                    else
                        return await connection.ExecuteAsync(sql.ToString(), parameters, commandTimeout: commandTimeout);
                }
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Insert an item 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<object> InsertAsync(T item, bool identity, CustomTransaction customTransaction)
            => await InsertAsync(item, identity, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Insert an item 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<object> InsertAsync(T item, bool identity)
            => await InsertAsync(item, identity, null, null).ConfigureAwait(false);

        /// <summary>
        /// Insert an item 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public object Insert(T item, bool identity, CustomTransaction customTransaction, int? commandTimeout)
            => InsertAsync(item, identity, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Insert an item 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public object Insert(T item, bool identity, CustomTransaction customTransaction)
            => InsertAsync(item, identity, customTransaction).Result;

        /// <summary>
        /// Insert an item 
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public object Insert(T item, bool identity)
            => InsertAsync(item, identity).Result;

        /// <summary>
        /// Get all rows in the table asynchronously 
        /// </summary>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync(CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryAsync<T>($"Select * from {MappingHelper.GetTableName(typeof(T))} ", transaction: customTransaction.DbCommand.Transaction, commandTimeout: commandTimeout);
                else
                    return await connection.QueryAsync<T>($"Select * from {MappingHelper.GetTableName(typeof(T))} ", commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Get all rows in the table 
        /// </summary>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync(CustomTransaction customTransaction)
            => await GetAsync(customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get all rows in the table 
        /// </summary>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync()
            => await GetAsync(customTransaction: null, commandTimeout: null).ConfigureAwait(false);

        /// <summary>
        /// Get all rows in the table 
        /// </summary>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get(CustomTransaction customTransaction, int? commandTimeout)
            => GetAsync(customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Get all rows in the table 
        /// </summary>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get(CustomTransaction customTransaction)
            => GetAsync(customTransaction).Result;

        /// <summary>
        /// Get all rows in the table 
        /// </summary>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get()
            => GetAsync().Result;


        /// <summary>
        /// Get the result of a query with parameters asynchronously 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryAsync<T>(sql, parameters, customTransaction.DbCommand.Transaction, commandTimeout);
                else
                    return await connection.QueryAsync<T>(sql, parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        /// <summary>
        /// Get the result of a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await GetAsync(sql, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters)
            => await GetAsync(sql, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a query without parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql)
            => await GetAsync(sql, null, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => GetAsync(sql, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Get the result of a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Get the result of a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters)
            => GetAsync(sql, parameters).Result;

        /// <summary>
        /// Get the result of a query without parameters
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql)
            => GetAsync(sql).Result;

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryAsync<TFirst, TSecond, TReturn>(sql, map, parameters, customTransaction.DbCommand.Transaction, commandTimeout: commandTimeout);
                else
                    return await connection.QueryAsync<TFirst, TSecond, TReturn>(sql, map, parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await GetAsync<TFirst, TSecond, TReturn>(sql, map, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters)
            => await GetAsync<TFirst, TSecond, TReturn>(sql, map, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map)
            => await GetAsync<TFirst, TSecond, TReturn>(sql, map, null, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => GetAsync<TFirst, TSecond, TReturn>(sql, map, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetAsync<TFirst, TSecond, TReturn>(sql, map, parameters, customTransaction).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters)
            => GetAsync<TFirst, TSecond, TReturn>(sql, map, parameters).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with 2 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map)
            => GetAsync<TFirst, TSecond, TReturn>(sql, map).Result;

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, customTransaction.DbCommand.Transaction, commandTimeout: commandTimeout);
                else
                    return await connection.QueryAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters)
            => await GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map)
            => await GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, null, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters, customTransaction).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters)
            => GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map, parameters).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with 3 input types 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map)
            => GetAsync<TFirst, TSecond, TThird, TReturn>(sql, map).Result;

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryAsync<TReturn>(sql, types, map, parameters, customTransaction.DbCommand.Transaction, commandTimeout: commandTimeout);
                else
                    return await connection.QueryAsync<TReturn>(sql, types, map, parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await GetAsync<TReturn>(sql, types, map, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters)
            => await GetAsync<TReturn>(sql, types, map, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the asynchronously result of a multi-mapping query with an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map)
            => await GetAsync<TReturn>(sql, types, map, null, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => GetAsync<TReturn>(sql, types, map, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Get the result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetAsync<TReturn>(sql, types, map, parameters, customTransaction).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with parameters and an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters)
            => GetAsync<TReturn>(sql, types, map, parameters).Result;

        /// <summary>
        /// Get the result of a multi-mapping query with an arbitrary number of input types
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="types">Array of types in the recordset.</param>
        /// <param name="map">The function to map row types to the return type</param>
        /// <returns>List of results</returns>
        public IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map)
            => GetAsync<TReturn>(sql, types, map).Result;


        /// <summary>
        /// Get the item by id asynchronously 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var primaryKey = MappingHelper.GetPrimaryKey(typeof(T));

                if (!primaryKey.Any())
                    throw new CustomRepositoryException("Primary key is not defined");

                if (primaryKey.Count > 1)
                    throw new CustomRepositoryException("This method does not support a composite primary key");

                var primaryKeyColumnName = primaryKey.FirstOrDefault();
                var connection = GetConnection(customTransaction);

                if (isCustomTransaction)
                    return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {MappingHelper.GetTableName(typeof(T))} where {primaryKeyColumnName} = @ID ", new { ID = id }, customTransaction.DbCommand.Transaction, commandTimeout);
                else
                    return await connection.QueryFirstOrDefaultAsync<T>($"Select * from {MappingHelper.GetTableName(typeof(T))} where {primaryKeyColumnName} = @ID ", new { ID = id }, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }
        }
        
        /// <summary>
        /// Get the item by id 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id, CustomTransaction customTransaction)
            => await GetByIdAsync(id, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Get the item by id 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id)
            => await GetByIdAsync(id, null, null).ConfigureAwait(false);

        /// <summary>
        /// Get the item by id 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Item</returns>
        public T GetById(object id, CustomTransaction customTransaction, int? commandTimeout)
            => GetByIdAsync(id, customTransaction, commandTimeout).Result;

        /// <summary>
        /// Get the item by id 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Item</returns>
        public T GetById(object id, CustomTransaction customTransaction)
            => GetByIdAsync(id, customTransaction).Result;

        /// <summary>
        /// Get the item by id 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public T GetById(object id)
            => GetByIdAsync(id).Result;

        /// <summary>
        /// Delete an item by id asynchronously 
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        public async Task DeleteAsync(object id, CustomTransaction customTransaction, int? commandTimeout)
        {
            try
            {
                var isCustomTransaction = customTransaction != null;

                var primaryKey = MappingHelper.GetPrimaryKey(typeof(T));

                if (!primaryKey.Any())
                    throw new CustomRepositoryException("Primary key is not defined");

                if (primaryKey.Count > 1)
                    throw new CustomRepositoryException("This method does not support a composite primary key");

                var primaryKeyColumnName = primaryKey.FirstOrDefault();

                var connection = GetConnection(customTransaction);
                var sql = new StringBuilder();

                var parameters = new Dictionary<string, object>
                {
                    { "@ID", id }
                };

                sql.AppendLine($"delete from {MappingHelper.GetTableName(typeof(T))} where {primaryKeyColumnName} = @ID");

                if (isCustomTransaction)
                    await connection.ExecuteAsync(sql.ToString(), parameters, customTransaction.DbCommand.Transaction, commandTimeout);
                else
                    await connection.ExecuteAsync(sql.ToString(), parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                throw new CustomRepositoryException(ex.Message);
            }

        }

        /// <summary>
        /// Delete an item by id
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        public async Task DeleteAsync(object id, CustomTransaction customTransaction)
            => await DeleteAsync(id, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Delete an item by id
        /// </summary>
        /// <param name="id">Primary Key</param>
        public async Task DeleteAsync(object id)
            => await DeleteAsync(id, null, null).ConfigureAwait(false);

        /// <summary>
        /// Delete an item by id
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        public void Delete(object id, CustomTransaction customTransaction, int? commandTimeout)
            => DeleteAsync(id, customTransaction, commandTimeout).Wait();
        
        /// <summary>
        /// Delete an item by id
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <param name="customTransaction"> has a transaction object</param>
        public void Delete(object id, CustomTransaction customTransaction)
            => DeleteAsync(id, customTransaction).Wait();

        /// <summary>
        /// Delete an item by id
        /// </summary>
        /// <param name="id">Primary Key</param>
        public void Delete(object id)
            => DeleteAsync(id).Wait();



        #endregion

        #region ADO

        /// <summary>
        /// Get DataSet result with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

                DbCommand.CommandType = CommandType.Text;
                DbCommand.Parameters.Clear();
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
        /// Get DataSet result with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetDataSet(sql, parameters, customTransaction, null);

        /// <summary>
        /// Get DataSet result with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters)
           => GetDataSet(sql, parameters, null, null);

        /// <summary>
        /// Executes a query with parameters asynchronously 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

                DbCommand.CommandType = CommandType.Text;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
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
        /// Executes a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await ExecuteQueryAsync(sql, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Executes a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters)
            => await ExecuteQueryAsync(sql, parameters, customTransaction: null, commandTimeout: null).ConfigureAwait(false);

        /// <summary>
        /// Executes a query without parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql)
            => await ExecuteQueryAsync(sql, parameters: null, customTransaction: null, commandTimeout: null).ConfigureAwait(false);

        /// <summary>
        /// Executes a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => ExecuteQueryAsync(sql, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Executes a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => ExecuteQueryAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Executes a query with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters)
            => ExecuteQueryAsync(sql, parameters).Result;

        /// <summary>
        /// Executes a query without parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql)
            => ExecuteQueryAsync(sql).Result;


        /// <summary>
        /// Executes a insert with parameters asynchronously 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="identity">Primary Key or Oracle sequence</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Primary Key After Insert</returns>
        public async Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity, CustomTransaction customTransaction, int? commandTimeout)
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

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

                DbCommand.CommandType = CommandType.Text;
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
        /// Executes a insert with parameters asynchronously 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="identity">Primary Key or Oracle sequence</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Primary Key After Insert</returns>
        public async Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity, CustomTransaction customTransaction)
            => await ExecuteQueryAsync(sql, parameters, identity, customTransaction, null).ConfigureAwait(false);


        /// <summary>
        /// Executes a Procedure with parameters asynchronously 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

                DbCommand.CommandType = CommandType.StoredProcedure;
                DbCommand.CommandText = procedure;
                DbCommand.Parameters.Clear();

                if (DBConnection.State == ConnectionState.Closed)
                    DBConnection.Open();
                    
                if (parameters != null)
                {
                    foreach (var parameter in parameters)
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
        /// Executes a Procedure with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await ExecuteProcedureAsync(procedure, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Executes a Procedure with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters)
            => await ExecuteProcedureAsync(procedure, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Executes a Procedure without parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure)
            => await ExecuteProcedureAsync(procedure, null, null, null).ConfigureAwait(false);

        /// <summary>
        /// Executes a Procedure with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => ExecuteProcedureAsync(procedure, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Executes a Procedure with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => ExecuteProcedureAsync(procedure, parameters, customTransaction).Result;

        /// <summary>
        /// Executes a Procedure with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters)
            => ExecuteProcedureAsync(procedure, parameters).Result;

        /// <summary>
        /// Executes a Procedure without parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure)
            => ExecuteProcedureAsync(procedure).Result;

        /// <summary>
        /// Get Procedure DataSet with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>DataSet</returns>
        public DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

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
        /// Get Procedure DataSet with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>DataSet</returns>
        public DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => GetProcedureDataSet(procedure, parameters, customTransaction, null);

        /// <summary>
        /// Get Procedure DataSet with parameters 
        /// </summary>
        /// <param name="procedure">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet</returns>
        public DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters)
            => GetProcedureDataSet(procedure, parameters, null, null);

        /// <summary>
        /// Executes Scalar with parameters asynchronously 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
        {
            var isCustomTransaction = customTransaction != null;

            try
            {
                if (isCustomTransaction)
                    DbCommand = customTransaction.DbCommand;
                else
                    DbCommand.Connection = DBConnection;

                if (commandTimeout.HasValue)
                    DbCommand.CommandTimeout = commandTimeout.Value;

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

        /// <summary>
        /// Executes Scalar with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => await ExecuteScalarAsync(sql, parameters, customTransaction, null).ConfigureAwait(false);

        /// <summary>
        /// Executes Scalar with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters)
            => await ExecuteScalarAsync(sql, parameters, null, null).ConfigureAwait(false);

        /// <summary>
        /// Executes Scalar with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <param name="commandTimeout">Number of seconds before command execution timeout</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout)
            => ExecuteScalarAsync(sql, parameters, customTransaction, commandTimeout).Result;
        
        /// <summary>
        /// Executes Scalar with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="customTransaction"> has a transaction object</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction)
            => ExecuteScalarAsync(sql, parameters, customTransaction).Result;

        /// <summary>
        /// Executes Scalar with parameters 
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
            => ExecuteScalarAsync(sql, parameters).Result;

        public string GetConnectionString()
            => this._connection.ConnectionString;

        #endregion

    }
}
