using System;
using System.Collections.Generic;

namespace Pegasus.Web.Models
{
    public class BookingResponseModel
    {
        public string BookingReference { get; set; }
        public string BookingStatus { get; set; }
        public string FromCity { get; set; }
        public string ToCity { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public Vehicle VehicleDetails { get; set; }
        public IEnumerable<Seat> BookedSeats { get; set; }

        public class Vehicle
        {
            public string TrafficServiceProvider { get; set; }
            public string VehicleName { get; set; }
            public string VehicleNumber { get; set; }
        }

        public class Seat
        {
            public string SeatNumber { get; set; }
            public string SeatPosition { get; set; }
        }
    }
}