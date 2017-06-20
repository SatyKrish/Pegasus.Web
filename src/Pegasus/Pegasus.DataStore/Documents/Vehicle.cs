namespace Pegasus.DataStore.Documents
{
    public class Vehicle : DocumentBase
    {
        public string Tsp { get; set; } // Transport service provider
        public string Vin { get; set; } // Vehicle identification number
        public VehicleDetails Details { get; set; }
        public Seat[] Seats { get; set; }
    }

    public class VehicleDetails
    {
        public string Make { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
    }
}