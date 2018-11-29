using System;

namespace RepositoryHelpers.DataBaseRepository.Base
{
    public interface ILiteDbRepository
    {
        int Id { get; set; }
        DateTime Update { get; set; }
    }
}
