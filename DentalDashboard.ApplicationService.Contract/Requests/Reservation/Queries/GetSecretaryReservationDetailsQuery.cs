using DentalDashboard.ApplicationService.Contract.Responses.ReservationResponse;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Queries;

public sealed record GetSecretaryReservationDetailsQuery(long ReservationId)
    : IQuery<SecretaryReservationDetailsResponse?>;
