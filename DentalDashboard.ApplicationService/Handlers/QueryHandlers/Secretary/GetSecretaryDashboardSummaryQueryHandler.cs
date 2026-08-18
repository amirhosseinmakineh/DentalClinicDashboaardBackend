using DentalDashboard.ApplicationService.Contract.Requests.Secretary.Queries;
using DentalDashboard.ApplicationService.Contract.Responses.SecretaryResponse;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using Microsoft.EntityFrameworkCore;
using DentalDashboard.ApplicationService.Contract.IServices;

namespace DentalDashboard.ApplicationService.Handlers.QueryHandlers.Secretary;

public class GetSecretaryDashboardSummaryQueryHandler
    : IQueryHandler<GetSecretaryDashboardSummaryQuery, SecretaryDashboardSummaryResponse>
{
    private readonly IReservationRepository reservationRepository;
    private readonly ISecretaryAccessService accessService;

    public GetSecretaryDashboardSummaryQueryHandler(IReservationRepository reservationRepository,
        ISecretaryAccessService accessService)
    {
        this.reservationRepository = reservationRepository;
        this.accessService = accessService;
    }

    public async Task<SecretaryDashboardSummaryResponse> HandleAsync(
        GetSecretaryDashboardSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var reservations = reservationRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsCanceled);
        var access = await accessService.GetAccessAsync(query.SecretaryUserId, cancellationToken);
        if (!access.IsSecretary)
            reservations = reservations.Where(_ => false);
        else if (!access.HasFullAccess)
            reservations = reservations.Where(x => access.AllowedDays.Contains(x.ReservationAt.DayOfWeek));

        return new SecretaryDashboardSummaryResponse
        {
            NeedCall = await reservations.CountAsync(
                x => x.SecretaryAnnouncementStatus == null ||
                     x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.NotCalled,
                cancellationToken),
            Confirmed = await reservations.CountAsync(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.Confirmed,
                cancellationToken),
            NoAnswer = await reservations.CountAsync(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.NoAnswer,
                cancellationToken),
            Cancelled = await reservations.CountAsync(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.CancelledByPatient,
                cancellationToken)
        };
    }
}
