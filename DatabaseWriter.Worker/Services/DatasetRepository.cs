// DatabaseWriter.Worker/Services/DatasetRepository.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseWriter.Worker.Services;

public class DatasetRepository : IDatasetRepository
{
    private readonly string _connectionString;

    public DatasetRepository(IConfiguration configuration)
    {
        // Get the connection string from the configuration
        _connectionString = configuration.GetConnectionString("Database");
    }

    public async Task SaveDatasetsAsync(IEnumerable<Dataset> datasets)
    {
        // INSERT DATABASE LOGIC HERE
        // E.g. Entity Framework core to connect to PostgreSQL....and insert the records

        Console.WriteLine($"--- Pretending to save {datasets.Count()} datasets to the database ---");
        foreach (var dataset in datasets)
        {
            Console.WriteLine($" - Save '{dataset.Title}'");
        }

        // This is just a placeholder so the method is async
        await Task.CompletedTask;
    }
}