using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace RepositoryHelpers.DataBaseRepository.Base
{
    internal interface ICustomRepository<T>
    {
        //Objects
        string GetConnectionString();

        //Generic Objects for Using Dapper
        IEnumerable<T> Get();
        Task<IEnumerable<T>> GetAsync();
        IEnumerable<T> Get(string sql, Dictionary<string, object> parameters);
        Task<IEnumerable<T>> GetAsync(string sql, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TFirst, TSecond, TThird, TReturn>(string sql, Func<TFirst, TSecond, TThird, TReturn> map);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters);
        Task<IEnumerable<TReturn>> GetAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map, Dictionary<string, object> parameters);
        IEnumerable<TReturn> Get<TReturn>(string sql, Type[] types, Func<object[], TReturn> map);
        T GetById(object id);
        Task<T> GetByIdAsync(object id);
        object Insert(T item, bool identity, CustomTransaction customTransaction);
        Task<object> InsertAsync(T item, bool identity, CustomTransaction customTransaction);
        void Update(T item);
        Task UpdateAsync(T item);
        void Delete(object id);
        Task DeleteAsync(object id);
        object ExecuteScalar(string sql, Dictionary<string, object> parameters);
        Task<object> ExecuteScalarAsync(string sql, Dictionary<string, object> parameters);

        //ADO.net default Dataset
        DataSet GetDataSet(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        DataSet GetDataSet(string sql, Dictionary<string, object> parameters);
        int ExecuteQuery(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        int ExecuteQuery(string sql, Dictionary<string, object> parameters);
        Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters, CustomTransaction customTransaction);
        Task<int> ExecuteQueryAsync(string sql, Dictionary<string, object> parameters);

    }
}
