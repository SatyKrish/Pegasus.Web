using System;
using System.Collections.Generic;

namespace Pegasus.Web.Models
{
    public class TripResponseModel
    {
        public IEnumerable<Trip> Trips { get; set; }

        public class Trip
        {
            public string TripReference { get; set; }
            public string TripStatus { get; set; }
            public string FromCity { get; set; }
            public string ToCity { get; set; }
            public DateTime DepartureTime { get; set; }
            public DateTime ArrivalTime { get; set; }
            public Vehicle VehicleDetails { get; set; }
            public IEnumerable<Seat> Seats { get; set; }
        }

        public class Vehicle
        {
            public string TrafficServiceProvider { get; set; }
            public string VehicleName { get; set; }
            public string VehicleNumber { get; set; }
        }

        public class Seat
        {
            public string SeatNumber { get; set; }
            public string Position { get; set; }
            public string AvailabilityStatus { get; set; }
        }
    }
}