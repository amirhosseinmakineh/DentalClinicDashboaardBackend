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
        var reservationsQuery = reservationRepository.GetAll()
            .AsNoTracking()
            .Where(x => !x.IsDeleted && !x.IsCanceled);

        var access = await accessService.GetAccessAsync(
            query.SecretaryUserId,
            cancellationToken);

        var reservations = access.IsSecretary
            ? await reservationsQuery.ToListAsync(cancellationToken)
            : [];


        return new SecretaryDashboardSummaryResponse
        {
            NeedCall = reservations.Count(
                x => x.SecretaryAnnouncementStatus == null ||
                     x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.NotCalled),

            Confirmed = reservations.Count(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.Confirmed),

            NoAnswer = reservations.Count(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.NoAnswer),

            Cancelled = reservations.Count(
                x => x.SecretaryAnnouncementStatus == SecretaryAnnouncementStatus.CancelledByPatient),

            AfterSalesServices = reservations.Count(
                x => x.ReservationType == ReservationType.AfterSalesService)
        };
    }
}
