using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;
using Pegasus.Web.Models;
using System;
using System.Collections.Generic;

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
            try
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

                // Retrieve seat information for this booking from trip
                var bookedSeats = new List<Seat>();
                foreach (var seat in booking.BookedSeats)
                {
                    var tripSeat = trip.Seats.FirstOrDefault(s => s.SeatNumber == seat);
                    if (tripSeat != null)
                    {
                        bookedSeats.Add(tripSeat);
                    }
                }

                var bookingResponse = new BookingResponseModel
                {
                    BookingReference = booking.BookingReference,
                    BookingStatus = booking.Status.ToString(),
                    FromCity = trip.Details.FromCity,
                    ToCity = trip.Details.ToCity,
                    DepartureTime = trip.Details.DepartureTime,
                    ArrivalTime = trip.Details.ArrivalTime,
                    BookedSeats = bookedSeats.Select(s => new BookingResponseModel.Seat
                    {
                        SeatNumber = s.SeatNumber,
                        SeatPosition = s.Position.ToString()
                    }),
                    VehicleDetails = new BookingResponseModel.Vehicle
                    {
                        TrafficServiceProvider = trip.Tsp,
                        VehicleNumber = trip.Vin,
                        VehicleName = trip.VehicleName
                    }
                };

                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }

        // POST api/booking/initiate
        [HttpPost]
        public async Task<IActionResult> Initiate([FromBody]BookingRequestModel bookingRequest)
        {
            try
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
                        return StatusCode(ErrorCodes.SEAT_NOT_AVAILABLE, nameof(ErrorCodes.SEAT_NOT_AVAILABLE));
                    }
                }

                // Initiate booking for this trip
                var booking = new Booking
                {
                    BookingReference = StringHelper.RandomString(6),
                    TripReference = bookingRequest.TripReference,
                    BookedSeats = bookingRequest.Seats.ToArray()
                };

                var bookingRef = await this._bookingRepository.AddAsync(booking);

                // Update trip seat status for this booking to blocked
                foreach (var bookedSeat in booking.BookedSeats)
                {
                    var tripSeat = trip.Seats.FirstOrDefault(s => s.SeatNumber == bookedSeat);
                    if (tripSeat != null)
                    {
                        tripSeat.Status = SeatStatus.Blocked;
                    }
                }

                await this._tripRepository.UpdateAsync(trip);

                return Ok(bookingRef);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }

        // PUT api/booking/confirm
        [HttpPut]
        public async Task<IActionResult> Confirm([FromBody]string bookingRef)
        {
            try
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
                            return StatusCode(ErrorCodes.BOOKING_ALREADY_CANCELLED, nameof(ErrorCodes.BOOKING_ALREADY_CANCELLED));
                        }
                    case BookingStatus.Completed:
                        {
                            return StatusCode(ErrorCodes.BOOKING_ALREADY_COMPLETED, nameof(ErrorCodes.BOOKING_ALREADY_COMPLETED));
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
                            return StatusCode(ErrorCodes.TRIP_ALREADY_STARTED, nameof(ErrorCodes.TRIP_ALREADY_STARTED));
                        }
                    case TripStatus.Completed:
                        {
                            return StatusCode(ErrorCodes.TRIP_ALREADY_COMPLETED, nameof(ErrorCodes.TRIP_ALREADY_COMPLETED));
                        }
                    case TripStatus.Cancelled:
                        {
                            return StatusCode(ErrorCodes.TRIP_ALREADY_CANCELLED, nameof(ErrorCodes.TRIP_ALREADY_CANCELLED));
                        }
                }

                // Confirm booking status
                await this._bookingRepository.ConfirmAsync(booking);

                // Update trip seat status of confirmed booking to booked
                var bookedSeats = new List<Seat>();
                foreach (var seat in booking.BookedSeats)
                {
                    var tripSeat = trip.Seats.FirstOrDefault(s => s.SeatNumber == seat);
                    if (tripSeat != null)
                    {
                        tripSeat.Status = SeatStatus.Booked;
                        bookedSeats.Add(tripSeat);
                    }
                }

                trip.LastUpdatedDate = DateTime.UtcNow;

                await _tripRepository.UpdateAsync(trip);

                // Return confirmed booking response
                var bookingResponse = new BookingResponseModel
                {
                    BookingReference = booking.BookingReference,
                    FromCity = trip.Details.FromCity,
                    ToCity = trip.Details.ToCity,
                    DepartureTime = trip.Details.DepartureTime,
                    ArrivalTime = trip.Details.ArrivalTime,
                    BookingStatus = BookingStatus.Completed.ToString(),
                    BookedSeats = bookedSeats.Select(s => new BookingResponseModel.Seat
                    {
                        SeatNumber = s.SeatNumber,
                        SeatPosition = s.Position.ToString()
                    }),
                    VehicleDetails = new BookingResponseModel.Vehicle
                    {
                        TrafficServiceProvider = trip.Tsp,
                        VehicleNumber = trip.Vin,
                        VehicleName = trip.VehicleName
                    }
                };
                return Ok(bookingResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }

        // PUT api/booking/cancel
        [HttpPut]
        public async Task<IActionResult> Cancel([FromBody]string bookingRef)
        {
            try
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
                            return StatusCode(ErrorCodes.BOOKING_ALREADY_CANCELLED, nameof(ErrorCodes.BOOKING_ALREADY_CANCELLED));
                        }
                    case BookingStatus.Completed:
                        {
                            return StatusCode(ErrorCodes.BOOKING_CANNOT_BE_CANCELLED, nameof(ErrorCodes.BOOKING_CANNOT_BE_CANCELLED));
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
                            return StatusCode(ErrorCodes.TRIP_ALREADY_STARTED, nameof(ErrorCodes.TRIP_ALREADY_STARTED));
                        }
                    case TripStatus.Completed:
                        {
                            return StatusCode(ErrorCodes.TRIP_ALREADY_COMPLETED, nameof(ErrorCodes.TRIP_ALREADY_COMPLETED));
                        }
                    case TripStatus.Scheduled:
                        {
                            // Update trip seat status of cancelled booking to available
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
            catch (Exception ex)
            {
                _logger.LogError("{0}", ex);
                throw;
            }
        }
    }
}
