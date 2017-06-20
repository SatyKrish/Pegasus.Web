using System;
using System.Collections.Generic;
using System.Linq;
using Pegasus.DataStore.Documents;

namespace Pegasus.Test
{
    public static class TestDataHelper
    {
        public static IEnumerable<Booking> GetBookingCollection(string bookingReference, string tripReference, BookingStatus? bookingStatus = BookingStatus.Completed)
        {
            return new List<Booking>
            {
                new Booking
                {
                    Id = Guid.NewGuid().ToString(),
                    BookingReference = bookingReference,
                    Status = bookingStatus.Value,
                    TripReference = tripReference,
                    BookedSeats = GetSeats().Select(s => s.SeatNumber).ToArray(),
                    LastUpdatedDate = DateTime.Now
                }
            };
        }

        public static List<Trip> GetTripCollection(string tripReference, TripStatus? tripStatus = TripStatus.Scheduled)
        {
            return new List<Trip>
            {
                new Trip
                {
                    Id = Guid.NewGuid().ToString(),
                    TripReference = tripReference,
                    JourneyDate = DateTime.Now.Date,
                    Status = tripStatus.Value,
                    Tsp = "Pegasus Travels",
                    Vin = "TestVin1",
                    VehicleName = "Volvo Transporter",
                    LastUpdatedDate = DateTime.Now,
                    Details = new TripDetails
                    {
                        FromCity = "CityA",
                        ToCity = "CityB",
                        DepartureTime = DateTime.Now.AddHours(2),
                        ArrivalTime = DateTime.Now.AddHours(10)
                    },
                    Seats = GetSeats()
                }
            };
        }

        public static List<Trip> GetTripCollectionWithBookedSeats(string tripReference)
        {
            return new List<Trip>
            {
                new Trip
                {
                    Id = Guid.NewGuid().ToString(),
                    TripReference = tripReference,
                    JourneyDate = DateTime.Now.Date,
                    Status = TripStatus.Scheduled,
                    Tsp = "Pegasus Travels",
                    Vin = "TestVin1",
                    VehicleName = "Volvo Transporter",
                    LastUpdatedDate = DateTime.Now,
                    Details = new TripDetails
                    {
                        FromCity = "CityA",
                        ToCity = "CityB",
                        DepartureTime = DateTime.Now.AddHours(2),
                        ArrivalTime = DateTime.Now.AddHours(10)
                    },
                    Seats = GetSeats(SeatStatus.Booked)
                }
            };
        }

        public static Vehicle GetVehicle(string vinReference)
        {
            return new Vehicle
            {
                Id = Guid.NewGuid().ToString(),
                Tsp = "Pegasus Travels",
                Vin = vinReference,
                LastUpdatedDate = DateTime.Now,
                Details = new VehicleDetails
                {
                    Make = "Volvo",
                    Model = "Transporter",
                    Year = "2016"
                },
                Seats = GetSeats()
            };
        }

        public static Seat[] GetSeats(SeatStatus? seatStatus = SeatStatus.Available)
        {
            return new Seat[]
            {
                new Seat
                {
                    SeatNumber = "1",
                    Position = SeatPosition.Aisle,
                    Status = seatStatus.Value
                },
                new Seat
                {
                    SeatNumber = "2",
                    Position = SeatPosition.Window,
                    Status = seatStatus.Value
                }
            };
        }
    }
}
