using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Pegasus.DataStore;
using Pegasus.DataStore.Documents;
using System.Threading.Tasks;

namespace Pegasus.WebJob
{
    public class Functions
    {
        private const string DocumentDatabaseName = "PegasusDB";
        private const string BookingCollectionName = "BookingCollection";
        private const string TripCollectionName = "TripCollection";

        // This function will get triggered/executed after every interval and it will work on documents in CosmosDB.
        //public static void HandleBookingTimeoutJob([TimerTrigger("00:00:30", RunOnStartup = true)] TimerInfo timer, TextWriter log)
        public static async Task HandleBookingTimeoutAsync([TimerTrigger("00:01:00", RunOnStartup = true)] TimerInfo timer)
        {
            //log.WriteLine($"Scheduled job fired at {DateTime.UtcNow}");

            var endpointUri = ConfigurationManager.AppSettings["PegasusDB:EndpointUri"];
            var primaryKey = ConfigurationManager.AppSettings["PegasusDB:PrimaryKey"];
            var documentClient = new DocumentClient(new Uri(endpointUri), primaryKey);
            var documentDatabase = await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DocumentDatabaseName });
            var bookingCollection = await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = BookingCollectionName });
            var tripCollection = await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = TripCollectionName });

            // Reset bookings that have timedout
            int expiryTimeEpoch = DateTime.UtcNow.AddMinutes(-2).ToEpoch();
            var bookingQuery = documentClient
                .CreateDocumentQuery<Booking>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, BookingCollectionName))
                .Where(b => b.Status == BookingStatus.Initiated && b.InitiatedTimeEpoch < expiryTimeEpoch)
                .AsDocumentQuery();
            var bookings = await bookingQuery.ExecuteToListAsync();
            foreach (var booking in bookings)
            {
                booking.Status = BookingStatus.Cancelled;
                booking.LastUpdatedDate = DateTime.UtcNow;

                await documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, BookingCollectionName, booking.Id), booking);

                // Update seat status for associated trip
                var tripQuery = documentClient
                .CreateDocumentQuery<Trip>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, TripCollectionName))
                .Where(t => t.TripReference == booking.TripReference)
                .AsDocumentQuery();
                var trip = tripQuery.ExecuteFirstOrDefaultAsync().GetAwaiter().GetResult();

                if (trip != null)
                {
                    // Update status of cancelled seats to available
                    foreach (var bookedSeat in booking.BookedSeats)
                    {
                        var tripSeat = trip.Seats.FirstOrDefault(s => s.SeatNumber == bookedSeat);
                        if (tripSeat != null)
                        {
                            tripSeat.Status = SeatStatus.Available;
                        }
                    }

                    trip.LastUpdatedDate = DateTime.UtcNow;

                    await documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, TripCollectionName, trip.Id), trip);
                }

            }
        }
    }
}
