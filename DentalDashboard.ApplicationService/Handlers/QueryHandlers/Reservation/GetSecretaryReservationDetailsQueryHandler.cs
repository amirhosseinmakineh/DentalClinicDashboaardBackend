using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Reservation;

public sealed class GetSecretaryReservationDetailsQueryHandler(IReservationRepository reservations)
    : IQueryHandler<GetSecretaryReservationDetailsQuery, SecretaryReservationDetailsResponse?>
{
    public async Task<SecretaryReservationDetailsResponse?> HandleAsync(
        GetSecretaryReservationDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await reservations.GetAll()
            .AsNoTracking()
            .Where(reservation =>
                !reservation.IsDeleted &&
                reservation.Id == query.ReservationId)
            .Select(reservation => new SecretaryReservationDetailsResponse
            {
                Id = reservation.Id,
                PatientName = reservation.LeadAssignment.UserName,
                PatientPhoneNumber = reservation.LeadAssignment.PhoneNumber,
                ConsultantFullName =
                    reservation.ConsultantProfile.User.FirstName + " " +
                    reservation.ConsultantProfile.User.LastName,
                ReservationAt = reservation.ReservationAt,
                DoctorName = reservation.DoctorName
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
