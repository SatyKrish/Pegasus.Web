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
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Pegasus.WebJob
{
    public class Functions
    {
        private const string DocumentDatabaseName = "PegasusDB";
        private const string TripCollectionName = "TripCollection";
        private const string BookingCollectionName = "BookingCollection";
        private const int BookingTimeoutInMinutes = 1;

        // This function will get triggered/executed after every interval and it will work on documents in CosmosDB.
        public static async Task HandleBookingTimeoutAsync([TimerTrigger("00:01:00", RunOnStartup = true)] TimerInfo timer, TextWriter log)
        {
            log.WriteLine($"Scheduled job fired at {DateTime.UtcNow}");

            try
            {
                var endpointUri = ConfigurationManager.AppSettings["PegasusDB:EndpointUri"];
                var primaryKey = ConfigurationManager.AppSettings["PegasusDB:PrimaryKey"];
                var documentClient = new DocumentClient(new Uri(endpointUri), primaryKey);
                var documentDatabase = await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = DocumentDatabaseName });
                var bookingCollection = await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = BookingCollectionName });
                var tripCollection = await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(DocumentDatabaseName), new DocumentCollection { Id = TripCollectionName });

                // Action block for cancelling bookings of a given trip sequentially.
                // Bookings for different trips can be processed in parallel. 
                // This is to ensure data consistency for a given trip.
                var cancelBookingBlock = new ActionBlock<KeyValuePair<string, IEnumerable<Booking>>>(async (tripGroup) =>
                {
                    log.WriteLine("Cancelling {0} bookings for trip - {1}", tripGroup.Value.Count(), tripGroup.Key);
                    foreach (var booking in tripGroup.Value)
                    {
                        log.WriteLine("Cancelling bookings - {0} for trip - {1}", booking.BookingReference, booking.TripReference);

                        // Update booking status as cancelled
                        booking.Status = BookingStatus.Cancelled;
                        booking.LastUpdatedDate = DateTime.UtcNow;

                        await documentClient.ReplaceDocumentAsync(UriFactory.CreateDocumentUri(DocumentDatabaseName, BookingCollectionName, booking.Id), booking);

                        // Retrieve trip details by trip reference
                        var tripQuery = documentClient
                                .CreateDocumentQuery<Trip>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, TripCollectionName))
                                .Where(t => t.TripReference == tripGroup.Key)
                                .AsDocumentQuery();
                        var trip = tripQuery.ExecuteFirstOrDefaultAsync().GetAwaiter().GetResult();

                        // Update status of cancelled seats to available
                        if (trip != null)
                        {
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
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount * 2
                });

                // Identify bookings that have timedout
                int expiryTimeEpoch = DateTime.UtcNow.AddMinutes(-BookingTimeoutInMinutes).ToEpoch();
                var bookingQuery = documentClient
                    .CreateDocumentQuery<Booking>(UriFactory.CreateDocumentCollectionUri(DocumentDatabaseName, BookingCollectionName))
                    .Where(b => b.Status == BookingStatus.Initiated && b.InitiatedTimeEpoch < expiryTimeEpoch)
                    .AsDocumentQuery();
                var expiredBookings = await bookingQuery.ExecuteToListAsync();

                // Skip further processing if there are no expired bookings.
                if (expiredBookings.Count == 0) return;

                // Group expired bookings by trip, and post them to cancel bookings block.
                var tripGroups = expiredBookings
                    .GroupBy(b => b.TripReference)
                    .Select(g => new KeyValuePair<string, IEnumerable<Booking>>(g.Key, g.AsEnumerable()));
                
                foreach (var tripGroup in tripGroups)
                {
                    cancelBookingBlock.Post(tripGroup);
                }

                cancelBookingBlock.Complete();
                await cancelBookingBlock.Completion.ContinueWith(t => log.WriteLine("Cancel booking block completed processing.")); 
            }
            catch (Exception ex)
            {
                log.WriteLine($"{0}", ex);
            }
        }
    }
}
