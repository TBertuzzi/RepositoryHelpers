using System.Collections.Generic;
using System.Linq;
using LiteDB;
using RepositoryHelpers.DataBaseRepository.Base;

namespace RepositoryHelpers.DataBaseRepository
{
    public sealed class LiteDbRepository<T> where T : ILiteDbRepository
    {
        private readonly LiteRepository _liteRepository;
        public LiteDbRepository(string dataBaseName)
        {

            _liteRepository = new LiteRepository(dataBaseName);
        }

        public IEnumerable<T> Get()
        {
            return _liteRepository.Query<T>().ToEnumerable();
        }

        public void Insert(T item)
        {
            _liteRepository.Insert<T>(item);
        }

        public void Update(T item)
        {
            _liteRepository.Update<T>(item);
        }

        public T GetById(int id)
        {
            return _liteRepository.Query<T>().Where(x => x.Id == id).ToEnumerable().FirstOrDefault();
        }
    }
}
