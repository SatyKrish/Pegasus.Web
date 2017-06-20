using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pegasus.DataStore.Documents
{
    public class Seat
    {
        public string SeatNumber { get; set; }
        public SeatPosition Position { get; set; }
        public SeatStatus Status { get; set; }
    }

    public enum SeatPosition
    {
        Window = 0,
        Middle = 1,
        Aisle = 2
    }

    public enum SeatStatus
    {
        Available = 0,
        Blocked = 1,
        Booked = 2
    }
}
