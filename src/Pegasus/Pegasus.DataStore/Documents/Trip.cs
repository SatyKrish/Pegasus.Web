using System;

namespace Pegasus.DataStore.Documents
{
    public class Trip : DocumentBase
    {        
        public string TripReference { get; set; }
        public string JourneyDate { get; set; }
        public TripStatus Status { get; set; }
        public TripDetails Details { get; set; }
        public string Tsp { get; set; } // Transport service provider
        public string Vin { get; set; } // Vehicle identification number
        public string VehicleName { get; set; }
        public Seat[] Seats { get; set; }        
    }

    public enum TripStatus
    {
        Scheduled = 0,
        Started = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class TripDetails
    {
        public string FromCity { get; set; }
        public string ToCity { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
    }
}
