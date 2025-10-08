// DatabaseWriter.Worker/Services/IDatasetRepository.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using DatabaseWriter.Worker.Models;

namespace DatabaseWriter.Worker.Services;

public interface IDatasetRepository
{
    Task SaveDatasetsAsync(IEnumerable<Dataset> datasets);
}