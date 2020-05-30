using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RepositoryHelpers.DataBaseRepository.Base
{
    internal interface ICustomRepository<T>
    {
        string GetConnectionString();

        IEnumerable<T> Get(CustomTransaction customTransaction, int? commandTimeout);
        IEnumerable<T> Get(CustomTransaction customTransaction);
        IEnumerable<T> Get();

        IEnumerable<T> Get(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        IEnumerable<T> Get(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<T> Get(string sql, Dictionary<string, object> parameters);
        IEnumerable<T> Get(string sql);
        
        Task<IEnumerable<T>> GetAsync(CustomTransaction customTransaction, int? commandTimeout);
        Task<IEnumerable<T>> GetAsync(CustomTransaction customTransaction);
        Task<IEnumerable<T>> GetAsync();

        Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters);
        Task<IEnumerable<T>> GetAsync(string sql);

        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map);
        
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map);

        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map);

        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map);

        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map);

        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map);

        Task<T> GetByIdAsync(object id, CustomTransaction customTransaction, int? commandTimeout);
        Task<T> GetByIdAsync(object id, CustomTransaction customTransaction);
        Task<T> GetByIdAsync(object id);
        
        T GetById(object id, CustomTransaction customTransaction, int? commandTimeout);
        T GetById(object id, CustomTransaction customTransaction);
        T GetById(object id);

        Task<object> InsertAsync(T item, bool identity, CustomTransaction customTransaction, int? commandTimeout);
        Task<object> InsertAsync(T item, bool identity, CustomTransaction customTransaction);
        Task<object> InsertAsync(T item, bool identity);

        object Insert(T item, bool identity, CustomTransaction customTransaction, int? commandTimeout);
        object Insert(T item, bool identity, CustomTransaction customTransaction);
        object Insert(T item, bool identity);

        Task UpdateAsync(T item, CustomTransaction customTransaction, int? commandTimeout);
        Task UpdateAsync(T item, CustomTransaction customTransaction);
        Task UpdateAsync(T item);

        void Update(T item, CustomTransaction customTransaction, int? commandTimeout);
        void Update(T item, CustomTransaction customTransaction);
        void Update(T item);
        
        Task DeleteAsync(object id, CustomTransaction customTransaction, int? commandTimeout);
        Task DeleteAsync(object id, CustomTransaction customTransaction);
        Task DeleteAsync(object id);
        
        void Delete(object id, CustomTransaction customTransaction, int? commandTimeout);
        void Delete(object id, CustomTransaction customTransaction);
        void Delete(object id);
        
        DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        DataSet GetDataSet(string sql, Dictionary<string, object> parameters);

        Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters);
        Task<int> ExecuteQueryAsync(string sql);

        int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        int ExecuteQuery(string sql, Dictionary<string, object> parameters);
        int ExecuteQuery(string sql);

        Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity, CustomTransaction customTransaction, int? commandTimeout);
        Task<string> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, string identity, CustomTransaction customTransaction);

        Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<int> ExecuteProcedureAsync(string procedure, Dictionary<string, object> parameters);
        Task<int> ExecuteProcedureAsync(string procedure);

        int ExecuteProcedure(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        int ExecuteProcedure(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        int ExecuteProcedure(string procedure, Dictionary<string, object> parameters);
        int ExecuteProcedure(string procedure);
        
        DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        DataSet GetProcedureDataSet(string procedure, Dictionary<string, object> parameters);

        Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters);

        object ExecuteScalar(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction, int? commandTimeout);
        object ExecuteScalar(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        object ExecuteScalar(string sql, Dictionary<string, object> parameters);
    }
}
