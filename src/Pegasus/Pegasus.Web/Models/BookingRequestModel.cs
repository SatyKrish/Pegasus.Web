using System.Collections.Generic;

namespace Pegasus.Web.Models
{
    public class BookingRequestModel
    {
        public string TripReference { get; set; }
        public IEnumerable<string> Seats { get; set; }
    }
}