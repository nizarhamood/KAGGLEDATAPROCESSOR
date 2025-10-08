using DatabaseWriter.Worker;
using DatabaseWriter.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// Register the repository
builder.Services.AddSingleton<IDatasetRepository, DatasetRepository>();

var host = builder.Build();
host.Run();