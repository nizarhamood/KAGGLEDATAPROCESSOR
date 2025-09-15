// In DatabaseWriter.Worker/Services/IDatasetRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseWrite.Worker.Services;

public interface IDatasetRepository
{
    Task SaveDatasetsAsync(IEnumerable<DatabaseWriter> datasets);
}