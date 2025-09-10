// In DatabaseWriter.Worker/Services/IDatasetRepository.cs
using System.Collection.Generic;
using System.Threading.Tasks;
using DatabaseWriter.Worker.Models;

public interface IDatasetRepository
{
    Task SaveDatasetsAsync(IEnemrable<DatabaseWriter> datasets);
}