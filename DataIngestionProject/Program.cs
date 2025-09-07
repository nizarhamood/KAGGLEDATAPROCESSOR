using System;
using System.Text.Json;
using System.Threading.Tasks;
using DataIngestion.Services;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Kaggle API clien...");

        IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        string kaggleUsername = config["Kaggle:Username"]; // Place these in a seperate file and call them
        string kaggleApiKey = config["Kaggle:ApiKey"]; // Place these in a seperate file and call them

        // 1. Create an instance of your service
        var apiClient = new KaggleApiClient(kaggleUsername, kaggleApiKey);

        // 2. Define what you want to search for.
        string searchTerm = "airbnb";

        // 3. Call the method on the instance and 'await' the result
        string resultJson = await apiClient.ListDatasetsAsync(searchTerm);

        // 4. Check the result and print it
        if (resultJson != null)
        {
            Console.WriteLine("\n-- API Response Received");

            // Pretty-print the JSON to make it readable
            try
            {
                using var jDoc = JsonDocument.Parse(resultJson);
                var formattedJson = JsonSerializer.Serialize(jDoc, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(formattedJson);
            }
            catch (JsonException)
            {
                // If the response isn't valid JSON for some reason
                Console.WriteLine(resultJson);
            }
        }
        else
        {
            Console.WriteLine("\n--- Failed to retrieve data from Kaggle API ---");
        }

        Console.WriteLine("API ingestion has been completed. Please press any key to exit");
        Console.ReadLine();
    }
}