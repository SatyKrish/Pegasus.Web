using System;
using System.Collections.Generic;

namespace Pegasus.Web.Models
{
    public class TripRequestModel
    {
        public string FromCity { get; set; }
        public string ToCity { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string VehicleNumber { get; set; }
    }
}