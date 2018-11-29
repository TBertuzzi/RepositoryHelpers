using System;

namespace RepositoryHelpers.Repository.Base
{
    public interface ILiteDbRepository
    {
        int Id { get; set; }
        DateTime Update { get; set; }
    }
}
