using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;

namespace Pegasus.DataStore.Repositories
{
    public class VehicleRepository : IVehicleRepository
    {
        private const string DocumentDatabaseName = "PegasusDb";
        private const string DocumentCollectionName = "VehicleCollection";

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private DocumentClient _documentClient;
        private Database _documentDatabase;
        private DocumentCollection _documentCollection;

        public VehicleRepository(ILogger<VehicleRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeAsync().Wait();
        }

        public async Task<Vehicle> GetByVinAsync(string vin)
        {
            var vehicleQuery = this._documentClient
                .CreateDocumentQuery<Vehicle>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName))
                .Where(v => v.Vin == vin)
                .AsDocumentQuery();

            return await vehicleQuery.ExecuteFirstOrDefaultAsync();
        }        

        public async Task AddAsync(Vehicle vehicle)
        {
            vehicle.Id = Guid.NewGuid().ToString();
            vehicle.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName), vehicle);
        }

        public async Task UpdateAsync(Vehicle vehicle)
        {
            vehicle.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName), vehicle);
        }

        private async Task InitializeAsync()
        {
            _documentClient = DocumentClientFactory.Create(_configuration);
            _documentDatabase = await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DocumentDatabaseName });
            _documentCollection = await _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = DocumentCollectionName });
        }
    }
}
