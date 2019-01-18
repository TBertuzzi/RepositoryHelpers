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

        //Default Transaction
        private DbTransaction _transaction;
        private DbTransaction Transaction
        {
            set
            {
                _transaction = value;
            }
            get
            {
                if (_transaction == null)
                {
                    if (DBConnection.State == ConnectionState.Closed)
                        DBConnection.Open();

                    _transaction = DBConnection.BeginTransaction(_connection.Database != DataBaseType.Oracle ? _connection.IsolationLevel
                        : IsolationLevel.ReadCommitted);
                }
                return _transaction;
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

        private void DisposeDB()
        {
            if (DbCommand.Transaction == null)
            {
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                DbCommand.Dispose();
                DbCommand = null;
            }
        }

        #endregion

        #region Transaction

        /// <summary>
        /// Start transaction
        /// </summary>
        public void BeginTransaction()
        {
            DbCommand.Transaction = Transaction;
        }

        /// <summary>
        /// Commit transaction
        /// </summary>
        public void CommitTransaction()
        {
            if (DbCommand?.Transaction != null)
            {
                DbCommand.Transaction.Commit();
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                _transaction.Dispose();
                _transaction = null;
            }
        }

        /// <summary>
        /// Rollback transaction
        /// </summary>
        public void RollbackTransaction()
        {
            if (DbCommand.Transaction != null)
            {
                DbCommand.Transaction.Rollback();
                DBConnection.Close();
                _DBConnection.Dispose();
                _DBConnection = null;
                _transaction.Dispose();
                _transaction = null;
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
        public async Task UpdateAsync(T item)
        {
            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            if (DbCommand?.Transaction != null)
                throw new CustomRepositoryException("This method does not support transaction.Use ExecuteQuery");

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

                    await connection.ExecuteAsync(sql.ToString(), parameters);
                }
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
        public void Update(T item)
            => UpdateAsync(item).Wait();


        /// <summary>
        /// Insert an item asynchronously (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public async Task<int> InsertAsync(T item, bool identity)
        {

            if (_connection.Database == DataBaseType.Oracle)
                throw new NotImplementedDatabaseException();

            if (DbCommand?.Transaction != null)
                throw new CustomRepositoryException("This method does not support transaction.Use ExecuteQuery");

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


        /// <summary>
        /// Insert an item (does not support transaction)
        /// </summary>
        /// <param name="item"> item to insert</param>
        /// <param name="identity">  Return primary key</param>
        /// <returns>Table Primary key or number of rows affected</returns>
        public int Insert(T item, bool identity) =>
            InsertAsync(item, identity).Result;

        /// <summary>
        /// Get all rows in the table asynchronously (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public async Task<IEnumerable<T>> GetAsync()
        {
            try
            {
                if (DbCommand?.Transaction != null)
                    throw new CustomRepositoryException("This method does not support transaction.Use GetDataSet");

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

        /// <summary>
        /// Get all rows in the table (does not support transaction)
        /// </summary>
        /// <returns>All rows in the table</returns>
        public IEnumerable<T> Get()
            => GetAsync().Result;


        /// <summary>
        /// Get the result of a query with parameters asynchronously (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public async Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            {
                if (DbCommand?.Transaction != null)
                    throw new CustomRepositoryException("This method does not support transaction.Use GetDataSet");

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

        /// <summary>
        /// Get the result of a query with parameters (does not support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>List of results</returns>
        public IEnumerable<T> Get(string sql, Dictionary<string, object> parameters)
            => GetAsync(sql, parameters).Result;


        /// <summary>
        /// Get the item by id asynchronously (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public async Task<T> GetByIdAsync(object id)
        {
            try
            {
                if (DbCommand?.Transaction != null)
                    throw new CustomRepositoryException("This method does not support transaction.Use GetDataSet");

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


        /// <summary>
        /// Get the item by id (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        /// <returns>Item</returns>
        public T GetById(object id)
            => GetByIdAsync(id).Result;

        /// <summary>
        /// Delete an item by id asynchronously (does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public async Task DeleteAsync(object id)
        {
            try
            {
                if (DbCommand?.Transaction != null)
                    throw new CustomRepositoryException("This method does not support transaction.Use GetDataSet");

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

        /// <summary>
        /// Delete an item by id(does not support transaction)
        /// </summary>
        /// <param name="id">Primary Key</param>
        public void Delete(object id)
            => DeleteAsync(id).Wait();


        #endregion

        #region ADO


        /// <summary>
        /// Get DataSet result with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet of results</returns>
        public DataSet GetDataSet(string sql, Dictionary<string, object> parameters)
        {
            try
            {
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
                DisposeDB();
            }
        }

        /// <summary>
        /// Executes a query with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public async Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            {
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
                DisposeDB();
            }
        }

        /// <summary>
        /// Executes a query with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Number of rows affected</returns>
        public int ExecuteQuery(string sql, Dictionary<string, object> parameters)
            => ExecuteQueryAsync(sql, parameters).Result;

        /// <summary>
        /// Executes a insert with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="identity">Primary Key or Oracle sequence</param>
        /// <returns>Primary Key After Insert</returns>
        public async Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity)
        {
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
                DisposeDB();
            }
        }

        /// <summary>
        /// Executes a insert with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <param name="identity">Primary Key or Oracle sequence</param>
        /// <returns>Primary Key After Insert</returns>
        public string ExecuteQuery(string sql, Dictionary<string, object> parameters, string identity)
                  => ExecuteQueryAsync(sql, parameters, identity).Result;


        /// <summary>
        /// Executes a Procedure with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public async Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters)
        {
            try
            {
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
                DisposeDB();
            }
        }

        /// <summary>
        /// Executes a Procedure with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Procedure return</returns>
        public int ExecuteProcedure(string procedure, Dictionary<string, object> parameters)
                => ExecuteProcedureAsync(procedure, parameters).Result;

        /// <summary>
        /// Get Procedure DataSet with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>DataSet</returns>
        public DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters)
        {
            try
            {
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
                DisposeDB();
            }
        }

        /// <summary>
        /// Executes Scalar with parameters (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public object ExecuteScalar(string sql, Dictionary<string, object> parameters)
          => ExecuteScalarAsync(sql, parameters).Result;


        /// <summary>
        /// Executes Scalar with parameters asynchronously (Support transaction)
        /// </summary>
        /// <param name="sql">Query</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>Scalar result</returns>
        public async Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters)
        {
            try
            {
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
                DisposeDB();
            }
        }

        #endregion

    }
}
