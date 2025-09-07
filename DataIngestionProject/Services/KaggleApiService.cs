using System;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace DataIngestion.Services
{
    public class KaggleApiClient
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = "https://www.kaggle.com/api/v1";

        public KaggleApiClient(string username, string apiKey)
        {
            _client = new HttpClient();

            // 1. Create the basic authentication header
            var authenticationString = $"{username}:{apiKey}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));

            // 2. Add the header to the HTTPClients default request headers
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

        }

        public async Task<string> ListDatasetsAsync(string searchTerm)
        {
            // 3. Construct the request URL with query parameters
            var requestUrl = $"{_baseUrl}/datasets/list?search={Uri.EscapeDataString(searchTerm)}";

            Console.WriteLine($"Sending request to: {requestUrl}");

            try
            {
                // 4. Send the GET request
                HttpResponseMessage response = await _client.GetAsync(requestUrl);

                // Ensure the request was successful
                response.EnsureSuccessStatusCode();

                // 5. Read and return the response body
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (HttpRequestException e)
            {
                {
                    Console.WriteLine($"\nException Caught!");
                    Console.WriteLine($"Message: {e.Message}");
                    return null;
                }
            }

        }
    }
}