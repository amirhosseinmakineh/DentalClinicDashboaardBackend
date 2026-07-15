using DentalDashboard.ApplicationService.Handlers.Helpers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Infrastracture.Context;
using DentalDashboard.Utilities.Convertor;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Services;

public class ReservationsExportService
{
    private readonly DentalContext context;

    public ReservationsExportService(DentalContext context) => this.context = context;

    public async Task<byte[]> ExportReservationsCsvAsync(
        DateTime? from,
        DateTime? to,
        long? consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(from, to, consultantProfileId, onlyConsultantConfirmations: false);
        return await BuildCsvAsync(query, cancellationToken);
    }

    public async Task<byte[]> ExportConsultantAttendanceConfirmationsCsvAsync(
        DateTime? from,
        DateTime? to,
        long? consultantProfileId,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(from, to, consultantProfileId, onlyConsultantConfirmations: true);
        return await BuildCsvAsync(query, cancellationToken);
    }

    private IQueryable<Domain.Models.Reservation> BuildQuery(
        DateTime? from,
        DateTime? to,
        long? consultantProfileId,
        bool onlyConsultantConfirmations)
    {
        var query = context.Reservations.AsNoTracking()
            .Include(x => x.LeadAssignment)
            .Include(x => x.ConsultantProfile)!.ThenInclude(x => x.User)
            .Where(x => !x.IsDeleted);

        if (!onlyConsultantConfirmations)
            query = query.Where(x => !x.IsCanceled);
        else
            query = query.Where(x =>
                x.ConsultantAttendanceConfirmedAt != null ||
                x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedPresent ||
                x.AttendanceConfirmationStatus == ReservationAttendanceConfirmationStatus.ConsultantConfirmedAbsent);

        if (consultantProfileId.HasValue)
            query = query.Where(x => x.ConsultantProfileId == consultantProfileId.Value);

        query = query.ApplyReservationAtFilter(
            date: null,
            from: from,
            to: to);

        return query.OrderByDescending(x => x.ReservationAt).ThenByDescending(x => x.Id);
    }

    private static async Task<byte[]> BuildCsvAsync(
        IQueryable<Domain.Models.Reservation> query,
        CancellationToken cancellationToken)
    {
        var rows = await query
            .Select(x => new
            {
                x.Id,
                x.LeadAssignmentId,
                x.ConsultantProfileId,
                ConsultantName = x.ConsultantProfile != null && x.ConsultantProfile.User != null
                    ? x.ConsultantProfile.User.FirstName + " " + x.ConsultantProfile.User.LastName
                    : string.Empty,
                ConsultantPhone = x.ConsultantProfile != null && x.ConsultantProfile.User != null
                    ? x.ConsultantProfile.User.PhoneNumber
                    : string.Empty,
                PatientName = x.LeadAssignment != null ? x.LeadAssignment.UserName : string.Empty,
                PatientPhone = x.LeadAssignment != null ? x.LeadAssignment.PhoneNumber : string.Empty,
                PatientCity = x.LeadAssignment != null ? x.LeadAssignment.PatientCity : null,
                PatientRegion = x.LeadAssignment != null ? x.LeadAssignment.PatientRegion : null,
                SecondaryPhone = x.LeadAssignment != null ? x.LeadAssignment.SecondaryPhoneNumber : null,
                AttendanceProbability = x.LeadAssignment != null ? x.LeadAssignment.AttendanceProbabilityPercent : null,
                LeadAssignmentType = x.LeadAssignment != null ? x.LeadAssignment.AssignmentType : (LeadAssignmentType?)null,
                x.ReservationAt,
                x.AttendanceConfirmationStatus,
                x.ConsultantAttendanceConfirmedAt,
                x.ConsultantSaysPatientAttended,
                x.ConsultantAttendanceNote,
                x.SecretaryReviewedAt,
                x.SecretaryApprovedConsultantConfirmation,
                x.SecretaryReviewNote,
                x.IsAttendanceScoreApplied,
                x.AttendanceScoreValue,
                x.AttendanceScoreAppliedAt,
                x.Description,
                x.IsCanceled,
                x.CreatedAt,
                HasPatientProfile = x.PatientUserId.HasValue
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            CsvExportHelper.JoinRow(
                "شناسه رزرو",
                "شناسه لید",
                "شناسه مشاور",
                "نام مشاور",
                "موبایل مشاور",
                "نام بیمار",
                "موبایل بیمار",
                "شماره دوم بیمار",
                "شهر بیمار",
                "منطقه بیمار",
                "نوع لید",
                "احتمال حضور (درصد)",
                "تاریخ و ساعت رزرو",
                "وضعیت تایید حضور",
                "زمان اعلام مشاور",
                "مشاور: بیمار آمد؟",
                "یادداشت مشاور",
                "زمان بررسی منشی",
                "منشی تایید کرد؟",
                "یادداشت منشی",
                "امتیاز اعمال شد؟",
                "مقدار امتیاز",
                "زمان اعمال امتیاز",
                "پرونده بیمار تشکیل شده؟",
                "لغو شده؟",
                "توضیحات",
                "تاریخ ایجاد رزرو")
        };

        foreach (var row in rows)
        {
            lines.Add(CsvExportHelper.JoinRow(
                row.Id.ToString(),
                row.LeadAssignmentId.ToString(),
                row.ConsultantProfileId.ToString(),
                row.ConsultantName,
                row.ConsultantPhone,
                row.PatientName,
                row.PatientPhone,
                row.SecondaryPhone ?? string.Empty,
                row.PatientCity ?? string.Empty,
                row.PatientRegion ?? string.Empty,
                row.LeadAssignmentType.HasValue ? row.LeadAssignmentType.Value.ToPersian() : string.Empty,
                row.AttendanceProbability?.ToString() ?? string.Empty,
                DateConvertor.ToPersianDateTimeString(row.ReservationAt),
                row.AttendanceConfirmationStatus.ToPersian(),
                row.ConsultantAttendanceConfirmedAt.HasValue
                    ? DateConvertor.ToPersianDateTimeString(row.ConsultantAttendanceConfirmedAt.Value)
                    : string.Empty,
                AdminReportPersianLabels.ToYesNoNullable(row.ConsultantSaysPatientAttended),
                row.ConsultantAttendanceNote ?? string.Empty,
                row.SecretaryReviewedAt.HasValue
                    ? DateConvertor.ToPersianDateTimeString(row.SecretaryReviewedAt.Value)
                    : string.Empty,
                AdminReportPersianLabels.ToYesNoNullable(row.SecretaryApprovedConsultantConfirmation),
                row.SecretaryReviewNote ?? string.Empty,
                AdminReportPersianLabels.ToYesNo(row.IsAttendanceScoreApplied),
                row.AttendanceScoreValue?.ToString() ?? string.Empty,
                row.AttendanceScoreAppliedAt.HasValue
                    ? DateConvertor.ToPersianDateTimeString(row.AttendanceScoreAppliedAt.Value)
                    : string.Empty,
                AdminReportPersianLabels.ToYesNo(row.HasPatientProfile),
                AdminReportPersianLabels.ToYesNo(row.IsCanceled),
                row.Description ?? string.Empty,
                DateConvertor.ToPersianDateTimeString(row.CreatedAt)));
        }

        return CsvExportHelper.BuildFile(lines.ToArray());
    }
}
