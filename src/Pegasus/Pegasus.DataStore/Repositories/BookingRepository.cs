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
    public class BookingRepository : IBookingRepository
    {
        private const string DocumentDatabaseName = "PegasusDb";
        private const string DocumentCollectionName = "BookingCollection";

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        private DocumentClient _documentClient;
        private Database _documentDatabase;
        private DocumentCollection _documentCollection;

        public BookingRepository(ILogger<BookingRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeAsync().Wait();
        }

        public async Task<Booking> GetByBookingReferenceAsync(string bookingReference)
        {
            var bookingQuery = this._documentClient
                .CreateDocumentQuery<Booking>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName))
                .Where(b => b.BookingReference == bookingReference)
                .AsDocumentQuery();

            return await bookingQuery.ExecuteFirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Booking>> GetByTripReferenceAsync(string tripReference)
        {
            var bookingQuery = this._documentClient
                .CreateDocumentQuery<Booking>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName))
                .Where(b => b.TripReference == tripReference)
                .AsDocumentQuery();

            return await bookingQuery.ExecuteToListAsync();
        }

        public async Task<string> AddAsync(Booking booking)
        {
            booking.Id = Guid.NewGuid().ToString();
            booking.Status = BookingStatus.Initiated;
            booking.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, DocumentCollectionName), booking);

            return booking.BookingReference;
        }

        public async Task ConfirmAsync(Booking booking)
        {
            // Update booking status
            booking.Status = BookingStatus.Completed;
            booking.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, DocumentCollectionName, booking.Id), booking);
        }

        public async Task CancelAsync(Booking booking)
        {
            // Reset booking status
            booking.Status = BookingStatus.Cancelled;
            booking.LastUpdatedDate = DateTime.UtcNow;

            await this._documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, DocumentCollectionName, booking.Id), booking);
        }

        private async Task InitializeAsync()
        {
            _documentClient = DocumentClientFactory.Create(_configuration);
            _documentDatabase = await _documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DocumentDatabaseName });
            _documentCollection = await _documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = DocumentCollectionName });
        }
    }
}
