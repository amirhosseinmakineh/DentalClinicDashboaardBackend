using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.Models;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Infrastracture.Repository;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DentalDashboard.Tests.Secretary;

public sealed class ReviewReservationAttendanceCommandHandlerTests
{
    [Fact]
    public async Task Approve_requires_doctor_name()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Handler.HandleAsync(new ReviewReservationAttendanceCommand
        {
            ReservationId = fixture.Reservation.Id,
            SecretaryUserId = Guid.NewGuid(),
            PatientReceivedService = true,
            DoctorName = "   "
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("نام دکتر", result.Message);
    }

    [Fact]
    public async Task Approve_trims_doctor_and_persists_final_state()
    {
        await using var fixture = await Fixture.CreateAsync();
        var secretaryId = Guid.NewGuid();

        var result = await fixture.Handler.HandleAsync(new ReviewReservationAttendanceCommand
        {
            ReservationId = fixture.Reservation.Id,
            SecretaryUserId = secretaryId,
            PatientReceivedService = true,
            DoctorName = "  دکتر محمدی  ",
            Note = "تایید شد"
        });

        Assert.True(result.IsSuccess);
        var saved = await fixture.Context.Reservations.SingleAsync();
        Assert.Equal("دکتر محمدی", saved.DoctorName);
        Assert.True(saved.PatientReceivedService);
        Assert.True(saved.SecretaryApprovedConsultantConfirmation);
        Assert.Equal(Status.SecretaryApproved, saved.AttendanceConfirmationStatus);
        Assert.Equal(secretaryId, saved.SecretaryUserId);
        Assert.True(saved.IsAttendanceScoreApplied);
    }

    [Fact]
    public async Task Reject_does_not_require_or_store_doctor_name()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Handler.HandleAsync(new ReviewReservationAttendanceCommand
        {
            ReservationId = fixture.Reservation.Id,
            SecretaryUserId = Guid.NewGuid(),
            PatientReceivedService = false,
            DoctorName = null
        });

        Assert.True(result.IsSuccess);
        var saved = await fixture.Context.Reservations.SingleAsync();
        Assert.Null(saved.DoctorName);
        Assert.False(saved.PatientReceivedService);
        Assert.False(saved.SecretaryApprovedConsultantConfirmation);
        Assert.Equal(Status.SecretaryRejected, saved.AttendanceConfirmationStatus);
    }

    [Fact]
    public async Task Future_reservation_cannot_be_reviewed()
    {
        await using var fixture = await Fixture.CreateAsync(DateTime.Now.AddMinutes(10));

        var result = await fixture.Handler.HandleAsync(new ReviewReservationAttendanceCommand
        {
            ReservationId = fixture.Reservation.Id,
            PatientReceivedService = true,
            DoctorName = "دکتر تست"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("بعد از زمان مراجعه", result.Message);
    }

    [Fact]
    public async Task Final_review_cannot_be_submitted_twice()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Reservation.IsAttendanceScoreApplied = true;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Handler.HandleAsync(new ReviewReservationAttendanceCommand
        {
            ReservationId = fixture.Reservation.Id,
            PatientReceivedService = false
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("قبلا ثبت شده", result.Message);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(DentalContext context, Reservation reservation)
        {
            Context = context;
            Reservation = reservation;
            Handler = new ReviewReservationAttendanceCommandHandler(
                new ReservationRepository(context),
                new ConsultantProfileRepository(context));
        }

        public DentalContext Context { get; }
        public Reservation Reservation { get; }
        public ReviewReservationAttendanceCommandHandler Handler { get; }

        public static async Task<Fixture> CreateAsync(DateTime? reservationAt = null)
        {
            var options = new DbContextOptionsBuilder<DentalContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            var context = new DentalContext(options);
            var consultant = new ConsultantProfile
            {
                Id = 1,
                UserId = Guid.NewGuid(),
                NationalCode = "",
                Address = "",
                IsCompleteProfile = true
            };
            var lead = new LeadAssignment
            {
                Id = 1,
                UserName = "بیمار تست",
                PhoneNumber = "09120000000"
            };
            var reservation = new Reservation
            {
                Id = 1,
                ConsultantProfileId = consultant.Id,
                LeadAssignmentId = lead.Id,
                ReservationAt = reservationAt ?? DateTime.Now.AddMinutes(-10),
                InitialReservationAt = reservationAt ?? DateTime.Now.AddMinutes(-10),
                AttendanceConfirmationStatus = Status.ConsultantConfirmedPresent
            };
            context.AddRange(consultant, lead, reservation);
            await context.SaveChangesAsync();
            return new Fixture(context, reservation);
        }

        public ValueTask DisposeAsync() => Context.DisposeAsync();
    }

    private static class Status
    {
        public const ReservationAttendanceConfirmationStatus ConsultantConfirmedPresent =
            ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent;
        public const ReservationAttendanceConfirmationStatus SecretaryApproved =
            ReservationAttendanceConfirmationStatus.SecretaryApproved;
        public const ReservationAttendanceConfirmationStatus SecretaryRejected =
            ReservationAttendanceConfirmationStatus.SecretaryRejected;
    }
}
