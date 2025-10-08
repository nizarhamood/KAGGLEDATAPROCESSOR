using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DataIngestion.Services;
using Microsoft.Extensions.Configuration;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting Kaggle API client...");

        IConfiguration config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .Build();

        string? kaggleUsername = config["Kaggle:Username"]; // Set in user-secrets
        string? kaggleApiKey = config["Kaggle:ApiKey"]; // Set in user-secrets
        string? rabbitMqConnection = config["RabbitMQ:ConnectionString"]; // Set in user-secrets

        // Validate configuration
        if (string.IsNullOrEmpty(kaggleUsername) || string.IsNullOrEmpty(kaggleApiKey))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: Kaggle credentials not found in user secrets.");
            Console.ResetColor();
            return;
        }

        if (string.IsNullOrEmpty(rabbitMqConnection))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: RabbitMQ connection string not found in user secrets.");
            Console.ResetColor();
            return;
        }

        // 1. Create an instance of your service
        var apiClient = new KaggleApiClient(kaggleUsername, kaggleApiKey);

        // 2. Define what you want to search for.
        string searchTerm = "airbnb";

        // 3. Call the method on the instance and 'await' the result
        string? resultJson = await apiClient.ListDatasetsAsync(searchTerm);

        // 4. Check the result and print it
        if (resultJson != null)
        {
            Console.WriteLine("\n-- API Response Received. Saving to file...");

            // Display the JSON to the user in the console
            using var jDoc = JsonDocument.Parse(resultJson);
            var formattedJson = JsonSerializer.Serialize(jDoc, new JsonSerializerOptions { WriteIndented = true });

            // Block 1: Publish message to RabbitMQ
            try
            {
                Console.WriteLine("\n--- API Response Received. Publishing to RabbitMQ... ---");

                // Create an instance of the new service
                IMessagePublisher messagePublisher = new RabbitMqPublisher(rabbitMqConnection);

                // Call the service
                messagePublisher.Publish(resultJson);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Message published to RabbitMQ successfully.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error publishing to RabbitMQ: {ex.Message}");
                Console.ResetColor();
            }

            // Block 2: Pretty-print the JSON to make it readable
            try
            {
                Console.WriteLine(formattedJson);
            }
            catch (JsonException ex)
            {
                // If the response isn't valid JSON for some reason
                Console.WriteLine(resultJson);
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }

            // Block 3: Handle File saving logic
            try
            {
                // Define the output directory and create a unique filename with a timestamp
                string outputDirectory = "Output";
                string fileName = $"kaggle_response_{searchTerm}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string filePath = Path.Combine(outputDirectory, fileName);

                // Ensure the 'Output' directory exists before trying to save the file
                Directory.CreateDirectory(outputDirectory);

                // Asynchronously write the raw JSON string to the file
                await File.WriteAllTextAsync(filePath, formattedJson);

                // Confirm to the user that the file was saved successfully
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Successfully saved API response to: {filePath}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"An error occured while saving the file: {ex.Message}");
                Console.ResetColor();
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