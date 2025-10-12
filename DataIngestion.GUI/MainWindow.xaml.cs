using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DataIngestion.Services;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;

namespace DataIngestion.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KaggleApiClient? _apiClient;
        private IMessagePublisher? _messagePublisher;
        public MainWindow()
        {
            InitializeComponent();
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                // Load configuration from user secrets
                IConfiguration config = new ConfigurationBuilder().AddUserSecrets<MainWindow>().Build();

                string? kaggleUsername = config["Kaggle:Username"];
                string? kaggleApiKey = config["Kaggle:ApiKey"];
                string? rabbitMqConnection = config["RabbitMQ:ConnectionString"];

                // Validate Configuration
                if (string.IsNullOrEmpty(kaggleUsername) || string.IsNullOrEmpty(kaggleApiKey))
                {
                    MessageBox.Show("Kaggle credentials not found in user secrets.\n\n" +
                        "Please run:\n" +
                        "dotnet user-secrets set \"Kaggle:Username\" \"your-username\"\n" +
                        "dotnet user-secrets set \"Kaggle:ApiKey\" \"your-api-key", "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    searchButton.IsEnabled = false;
                    return;
                }

                // Initilize services
                _apiClient = new KaggleApiClient(kaggleUsername, kaggleApiKey);

                if (!string.IsNullOrEmpty(rabbitMqConnection))
                {
                    _messagePublisher = new RabbitMqPublisher(rabbitMqConnection);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Configuation Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                searchButton.IsEnabled = false;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = searchInput.Text.Trim();

            // Validate input
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                MessageBox.Show("Please enter a search term.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_apiClient == null)
            {
                MessageBox.Show("Configuration error. Please restart the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            await SearchAndDisplayDataAsync(searchTerm);
        }

        private async Task SearchAndDisplayDataAsync(string searchTerm)
        {
            try
            {
                // Disable button and show loading
                searchButton.IsEnabled = false;
                searchButton.Content = "Searching...";
                searchResultDataGrid.ItemsSource = null;

                // Call Kaggle API
                string? resultJson = await _apiClient!.ListDatasetsAsync(searchTerm);

                if (resultJson != null)
                {
                    // Deserialize JSON to list of datasets 
                    var datasets = JsonSerializer.Deserialize<List<KaggleDataset>>(resultJson);

                    if (datasets != null && datasets.Count > 0)
                    {
                        // Bind data to grid
                        searchResultDataGrid.ItemsSource = datasets;

                        // Publish to RabbitMQ (if configured)
                        if (_messagePublisher != null)
                        {
                            await Task.Run(() => _messagePublisher.Publish(resultJson));
                        }

                        MessageBox.Show($"Found {datasets.Count} datasets!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Failed to retrieve data from Kaggle API. \n\n" + "Please check your credentials and try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error parsing JSON response:\n{ex.Message}", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occurred:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable button
                searchButton.IsEnabled = true;
                searchButton.Content = "Search";
            }
        }
    }

    // Model class to match Kaggle API response
    public class KaggleDataset
    {
        // Using JsonPropertyName to map from API's property names
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int ID { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ref")]
        public string? Ref { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string? Title { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("subtitle")]
        public string? Subtitle { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("creatorName")]
        public string? CreatorName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ownerName")]
        public string? OwnerName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("totalBytes")]
        public long TotalBytes { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("url")]
        public string? Url { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("downloadCount")]
        public int DownloadCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("voteCount")]
        public int VoteCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("viewCount")]
        public int ViewCount { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("usabilityRating")]
        public double UsabilityRating { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("licenseName")]
        public string? LicenseName { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("kernelCount")]
        public int KernelCount { get; set; }

        //Calculated property to show file size in readable format
        public string FileSizeFormatted => FormatBytes(TotalBytes);
        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

    }
}
