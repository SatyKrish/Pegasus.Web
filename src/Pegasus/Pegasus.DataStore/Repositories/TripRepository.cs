using System;
using System.Collections.Generic;
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
    public class TripRepository : ITripRepository
    {
        private const string DocumentDatabaseName = "PegasusDb";
        private const string DocumentCollectionName = "TripCollection";

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private DocumentClient _documentClient;
        private Database _documentDatabase;
        private DocumentCollection _documentCollection;

        public TripRepository(ILogger<TripRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeAsync().Wait();
        }

        public async Task<Trip> GetByTripReferenceAsync(string tripReference)
        {
            var tripQuery = this._documentClient
                .CreateDocumentQuery<Trip>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName))
                .Where(t => t.TripReference == tripReference)
                .AsDocumentQuery();

            return await tripQuery.ExecuteFirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Trip>> GetByTripDetailsAsync(string fromCity, string toCity, string journeyDate)
        {
            var tripQuery = this._documentClient
                .CreateDocumentQuery<Trip>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName))
                .Where(t => t.Details.FromCity == fromCity && t.Details.ToCity == toCity && t.JourneyDate == journeyDate)
                .AsDocumentQuery();

            return await tripQuery.ExecuteToListAsync();
        }

        public async Task<string> AddAsync(Trip trip)
        {
            trip.Id = Guid.NewGuid().ToString();
            trip.Status = TripStatus.Scheduled;
            trip.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName), trip);

            return trip.TripReference;
        }

        public async Task UpdateAsync(Trip trip)
        {
            trip.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, DocumentCollectionName, trip.Id), trip);
        }

        public async Task ResetAsync(Trip trip)
        {
            // Reset trip status and seat status
            trip.Status = TripStatus.Scheduled;
            trip.LastUpdatedDate = DateTime.UtcNow;
            foreach (var seat in trip.Seats)
            {
                seat.Status = SeatStatus.Available;
            }

            await this._documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, DocumentCollectionName, trip.Id), trip);
        }

        private async Task InitializeAsync()
        {
            _documentClient = DocumentClientFactory.Create(_configuration);
            _documentDatabase = await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DocumentDatabaseName });
            _documentCollection = await _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = DocumentCollectionName });
        }
    }
}
