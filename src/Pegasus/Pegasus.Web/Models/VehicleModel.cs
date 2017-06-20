using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pegasus.Web.Models
{
    public class VehicleModel
    {
        public string VehicleNumber { get; set; }
        public string TrafficServiceProvider { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public IEnumerable<Seat> Seats { get; set; }

        public class Seat
        {
            public string SeatNumber { get; set; }
            public string Position { get; set; }
        }
    }
}
