using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Pegasus.DataStore.Documents;
using Pegasus.DataStore.Interfaces;
using Pegasus.Web;
using Pegasus.Web.Controllers;
using Pegasus.Web.Models;

namespace Pegasus.Test.Controllers
{
    [TestClass]
    public class BookingControllerTests
    {
        private readonly Mock<ITripRepository> _mockTripRepository = new Mock<ITripRepository>();
        private readonly Mock<IBookingRepository> _mockBookingRepository = new Mock<IBookingRepository>();
        private readonly Mock<ILogger<BookingController>> _mockLogger = new Mock<ILogger<BookingController>>();

        [TestMethod]
        public async Task BookingController_Retrieve_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            var bookingStatus = BookingStatus.Initiated;
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, bookingStatus).FirstOrDefault())
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Retrieve(bookingReference);
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var booking = objResult.Value as BookingResponseModel;
            Assert.IsNotNull(booking);
            Assert.AreEqual(bookingReference, booking.BookingReference);
            Assert.AreEqual(bookingStatus.ToString(), booking.BookingStatus);
            Assert.AreEqual(2, booking.BookedSeats.Count());
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Retrieve_WhenBookingDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Booking))
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Retrieve(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Retrieve_WhenTripDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            var bookingStatus = BookingStatus.Initiated;
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, bookingStatus).FirstOrDefault())
                .Verifiable();
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Trip))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Retrieve(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Initiate_WhenSeatsAvailable_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.AddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(bookingReference)
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Initiate(new BookingRequestModel
            {
                TripReference = tripReference,
                Seats = new string[] { "1", "2" }
            });
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var actualBookingReference = objResult.Value as string;
            Assert.IsNotNull(actualBookingReference);
            Assert.AreEqual(bookingReference, actualBookingReference);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.AddAsync(It.IsAny<Booking>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Initiate_WhenTripDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Trip))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.AddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(bookingReference)
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Initiate(new BookingRequestModel
            {
                TripReference = tripReference,
                Seats = new string[] { "1", "2" }
            });

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Initiate_WhenSeatNotAvailable_ShouldReturnSeatNotAvailableStatusCode()
        {
            // Arrange
            const int SEAT_NOT_AVAILABLE = 1001;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollectionWithBookedSeats(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.AddAsync(It.IsAny<Booking>()))
                .ReturnsAsync(bookingReference)
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Initiate(new BookingRequestModel
            {
                TripReference = tripReference,
                Seats = new string[] { "1", "2" }
            });
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(SEAT_NOT_AVAILABLE, statusCodeResult.StatusCode);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.AddAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenBookingInitiated_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var objResult = result as OkObjectResult;

            // Assert
            Assert.IsNotNull(objResult);
            var bookingResponse = objResult.Value as BookingResponseModel;
            Assert.IsNotNull(bookingResponse);
            Assert.AreEqual(bookingReference, bookingResponse.BookingReference);
            Assert.AreEqual(BookingStatus.Completed, bookingResponse.BookingStatus.ToEnum<BookingStatus>());
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenBookingDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Booking))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenBookingCompleted_ShouldReturnBookingAlreadyCompletedStatusCode()
        {
            // Arrange
            const int BOOKING_ALREADY_COMPLETED = 2002;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Completed).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(BOOKING_ALREADY_COMPLETED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenBookingCancelled_ShouldReturnBookingAlreadyCancelledStatusCode()
        {
            // Arrange
            const int BOOKING_ALREADY_CANCELLED = 2001;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Cancelled).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(BOOKING_ALREADY_CANCELLED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }        

        [TestMethod]
        public async Task BookingController_Confirm_WhenTripDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Trip))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenTripCancelled_ShouldReturnTripAlreadyCancelledStatusCode()
        {
            // Arrange
            const int TRIP_ALREADY_CANCELLED = 2005;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Cancelled).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var statusCodeResult = result as StatusCodeResult;
            
            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(TRIP_ALREADY_CANCELLED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenTripStarted_ShouldReturnTripAlreadyStartedStatusCode()
        {
            // Arrange
            const int TRIP_ALREADY_STARTED = 2003;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Started).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(TRIP_ALREADY_STARTED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Confirm_WhenTripCompleted_ShouldReturnTripAlreadyCompletedStatusCode()
        {
            // Arrange
            const int TRIP_ALREADY_COMPLETED = 2004;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Completed).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.ConfirmAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Confirm(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(TRIP_ALREADY_COMPLETED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.ConfirmAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenBookingInitiated_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);
            var objResult = result as OkResult;

            // Assert
            Assert.IsNotNull(objResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenBookingDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Booking))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenBookingCompleted_ShouldReturnBookingCannotBeCancelledStatusCode()
        {
            // Arrange
            const int BOOKING_CANNOT_BE_CANCELLED = 2006;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Completed).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(BOOKING_CANNOT_BE_CANCELLED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenBookingCancelled_ShouldReturnBookingAlreadyCancelledStatusCode()
        {
            // Arrange
            const int BOOKING_ALREADY_CANCELLED = 2001;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Cancelled).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(BOOKING_ALREADY_CANCELLED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Never);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenTripDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(default(Trip))
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);

            // Assert
            Assert.IsNotNull(result as NotFoundObjectResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenTripCancelled_ShouldReturnOk()
        {
            // Arrange
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Cancelled).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);

            // Assert
            Assert.IsNotNull(result as OkResult);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Once);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenTripStarted_ShouldReturnTripAlreadyStartedStatusCode()
        {
            // Arrange
            const int TRIP_ALREADY_STARTED = 2003;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Started).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(TRIP_ALREADY_STARTED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }

        [TestMethod]
        public async Task BookingController_Cancel_WhenTripCompleted_ShouldReturnTripAlreadyCompletedStatusCode()
        {
            // Arrange
            const int TRIP_ALREADY_COMPLETED = 2004;
            var tripReference = StringHelper.RandomString(8);
            var bookingReference = StringHelper.RandomString(6);
            _mockTripRepository
                .Setup(m => m.GetByTripReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetTripCollection(tripReference, TripStatus.Completed).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.GetByBookingReferenceAsync(It.IsAny<string>()))
                .ReturnsAsync(TestDataHelper.GetBookingCollection(bookingReference, tripReference, BookingStatus.Initiated).FirstOrDefault())
                .Verifiable();
            _mockBookingRepository
                .Setup(m => m.CancelAsync(It.IsAny<Booking>()))
                .Returns(Task.FromResult<object>(null))
                .Verifiable();

            // Act
            var bookingController = new BookingController(_mockTripRepository.Object, _mockBookingRepository.Object, _mockLogger.Object);
            var result = await bookingController.Cancel(bookingReference);
            var statusCodeResult = result as StatusCodeResult;

            // Assert
            Assert.IsNotNull(statusCodeResult);
            Assert.AreEqual(TRIP_ALREADY_COMPLETED, statusCodeResult.StatusCode);
            _mockBookingRepository.Verify(m => m.GetByBookingReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockTripRepository.Verify(m => m.GetByTripReferenceAsync(It.IsAny<string>()), Times.Once);
            _mockBookingRepository.Verify(m => m.CancelAsync(It.IsAny<Booking>()), Times.Never);
        }
    }
}
