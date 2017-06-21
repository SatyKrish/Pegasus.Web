using System;

namespace Pegasus.DataStore.Documents
{
    public class Booking : DocumentBase
    {
        public string BookingReference { get; set; }
        public BookingStatus Status { get; set; }
        public int InitiatedTimeEpoch { get; set; }  // User for determining whether a booking timedout
        public string[] BookedSeats { get; set; }
        public string TripReference { get; set; }
    }

    public enum BookingStatus
    {
        Initiated = 0,
        Completed = 1,
        Cancelled = 2
    }
}