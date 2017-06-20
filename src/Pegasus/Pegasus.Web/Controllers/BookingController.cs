using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;
using Pegasus.Web.Models;

namespace Pegasus.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    public class BookingController : Controller
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger _logger;

        public BookingController(ITripRepository tripRepository, IBookingRepository bookingRepository, ILogger<BookingController> logger)
        {
            _tripRepository = tripRepository;
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        // GET api/booking/retrieve?bookingRef={123}
        [HttpGet]
        public async Task<IActionResult> Retrieve([FromQuery]string bookingRef)
        {
            var booking = await this._bookingRepository.GetByBookingReferenceAsync(bookingRef);
            if (booking == null)
            {
                return NotFound(bookingRef);
            }

            var trip = await this._tripRepository.GetByTripReferenceAsync(booking.TripReference);
            if (trip == null)
            {
                return NotFound(booking.TripReference);
            }

            var bookingResponse = new BookingResponseModel
            {
                BookingReference = booking.BookingReference,
                BookingStatus = booking.Status.ToString(),
                FromCity = trip.Details.FromCity,
                ToCity = trip.Details.ToCity,
                DepartureTime = trip.Details.DepartureTime,
                ArrivalTime = trip.Details.ArrivalTime,
                BookedSeats = trip.Seats.Select(s => new BookingResponseModel.Seat
                {
                    SeatNumber = s.SeatNumber,
                    SeatPosition = s.Position.ToString()
                })
            };

            return Ok(bookingResponse);
        }

        // POST api/booking/initiate
        [HttpPost]
        public async Task<IActionResult> Initiate([FromBody]BookingRequestModel bookingRequest)
        {
            var trip = await this._tripRepository.GetByTripReferenceAsync(bookingRequest.TripReference);
            if (trip == null)
            {
                return NotFound(bookingRequest.TripReference);
            }

            // Verify if opted seats are available for booking
            foreach (var seat in bookingRequest.Seats)
            {
                if (trip.Seats.Any(s => s.SeatNumber == seat && s.Status != SeatStatus.Available))
                {
                    const int SEAT_NOT_AVAILABLE = 1001;
                    return StatusCode(SEAT_NOT_AVAILABLE);
                }
            }

            var booking = new Booking
            {
                BookingReference = StringHelper.RandomString(6),
                TripReference = bookingRequest.TripReference,
                BookedSeats = bookingRequest.Seats.ToArray()
            };

            var bookingRef = await this._bookingRepository.AddAsync(booking);
            return Ok(bookingRef);
        }

        // POST api/booking/confirm
        [HttpPost]
        public async Task<IActionResult> Confirm([FromBody]string bookingRef)
        {
            var booking = await this._bookingRepository.GetByBookingReferenceAsync(bookingRef);
            if (booking == null)
            {
                return NotFound(bookingRef);
            }

            // Verify booking status
            switch (booking.Status)
            {
                case BookingStatus.Cancelled:
                    {
                        const int BOOKING_ALREADY_CANCELLED = 2001;
                        return StatusCode(BOOKING_ALREADY_CANCELLED);
                    }
                case BookingStatus.Completed:
                    {
                        const int BOOKING_ALREADY_COMPLETED = 2002;
                        return StatusCode(BOOKING_ALREADY_COMPLETED);
                    }
            }

            var trip = await this._tripRepository.GetByTripReferenceAsync(booking.TripReference);
            if (trip == null)
            {
                return NotFound(booking.TripReference);
            }

            // Verify trip status
            switch (trip.Status)
            {
                case TripStatus.Started:
                    {
                        const int TRIP_ALREADY_STARTED = 2003;
                        return StatusCode(TRIP_ALREADY_STARTED);
                    }
                case TripStatus.Completed:
                    {
                        const int TRIP_ALREADY_COMPLETED = 2004;
                        return StatusCode(TRIP_ALREADY_COMPLETED);
                    }
                case TripStatus.Cancelled:
                    {
                        const int TRIP_ALREADY_CANCELLED = 2005;
                        return StatusCode(TRIP_ALREADY_CANCELLED);
                    }
            }

            await this._bookingRepository.ConfirmAsync(booking);

            var bookingResponse = new BookingResponseModel
            {
                BookingReference = booking.BookingReference,
                FromCity = trip.Details.FromCity,
                ToCity = trip.Details.ToCity,
                DepartureTime = trip.Details.DepartureTime,
                ArrivalTime = trip.Details.ArrivalTime,
                BookingStatus = BookingStatus.Completed.ToString(),
                VehicleDetails = new BookingResponseModel.Vehicle
                {
                    TrafficServiceProvider = trip.Tsp,
                    VehicleNumber = trip.Vin,
                    VehicleName = trip.VehicleName
                }
            };
            return Ok(bookingResponse);
        }

        // POST api/booking/cancel
        [HttpDelete]
        public async Task<IActionResult> Cancel([FromBody]string bookingRef)
        {
            var booking = await this._bookingRepository.GetByBookingReferenceAsync(bookingRef);
            if (booking == null)
            {
                return NotFound(bookingRef);
            }

            // Verify booking status
            switch (booking.Status)
            {
                case BookingStatus.Cancelled:
                    {
                        const int BOOKING_ALREADY_CANCELLED = 2001;
                        return StatusCode(BOOKING_ALREADY_CANCELLED);
                    }
                case BookingStatus.Completed:
                    {
                        const int BOOKING_CANNOT_BE_CANCELLED = 2006;
                        return StatusCode(BOOKING_CANNOT_BE_CANCELLED);
                    }
            }
            
            var trip = await this._tripRepository.GetByTripReferenceAsync(booking.TripReference);
            if (trip == null)
            {
                return NotFound(booking.TripReference);
            }

            // Verify trip status
            switch (trip.Status)
            {
                case TripStatus.Started:
                    {
                        const int TRIP_ALREADY_STARTED = 2003;
                        return StatusCode(TRIP_ALREADY_STARTED);
                    }
                case TripStatus.Completed:
                    {
                        const int TRIP_ALREADY_COMPLETED = 2004;
                        return StatusCode(TRIP_ALREADY_COMPLETED);
                    }
                case TripStatus.Scheduled:
                    {
                        // Update status of cancelled seats to available
                        foreach (var bookedSeat in booking.BookedSeats)
                        {
                            var tripSeat = trip.Seats.FirstOrDefault(s => s.SeatNumber == bookedSeat);
                            if (tripSeat != null)
                            {
                                tripSeat.Status = SeatStatus.Available;
                            }
                        }

                        await this._tripRepository.UpdateAsync(trip);
                        break;
                    }
            }

            await this._bookingRepository.CancelAsync(booking);

            return Ok();
        }
    }
}
