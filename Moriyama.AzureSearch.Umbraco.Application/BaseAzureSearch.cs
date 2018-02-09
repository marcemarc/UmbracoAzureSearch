using Microsoft.Azure.Search;
using Moriyama.AzureSearch.Umbraco.Application.Models;
using Newtonsoft.Json;
using System.IO;

namespace Moriyama.AzureSearch.Umbraco.Application
{
    public abstract class BaseAzureSearch
    {
        protected AzureSearchConfig _config;
        protected readonly string _path;

        // Number of docs to be processed at a time.
        const int BatchSize = 999;

        public BaseAzureSearch(string path)
        {
            _path = path;

           
            var configData = File.ReadAllText(Path.Combine(path, @"config\AzureSearch.config"));

            _config = JsonConvert.DeserializeObject<AzureSearchConfig>(configData, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore });
        }

        public void SaveConfiguration(AzureSearchConfig config)
        {
            _config = config;
            
            File.WriteAllText(Path.Combine(_path, @"config\AzureSearch.config"), JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore }));
        }

        public AzureSearchConfig GetConfiguration()
        {
            return _config;
        }

        public SearchServiceClient GetClient()
        {
            var serviceName = _config.SearchServiceName;
            var apiKey = _config.SearchServiceAdminApiKey;

            var serviceClient = new SearchServiceClient(serviceName, new SearchCredentials(apiKey));
            return serviceClient;
        }

        
    }
}
