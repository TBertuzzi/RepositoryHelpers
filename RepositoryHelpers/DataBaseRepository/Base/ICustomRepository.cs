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
        T GetById(object id);
        Task<T> GetByIdAsync(object id);
        int Insert(T item, bool identity);
        Task<int> InsertAsync(T item, bool identity);
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
