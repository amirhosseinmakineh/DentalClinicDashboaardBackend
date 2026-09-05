using DentalDashboard.Accounting.Domain.PatientFinance.Enums;
using DentalDashboard.Framwork.Cqrs.Abstraction.Read;
using DentalDashboard.Domain.Models;

namespace DentalDashboard.Accounting.Contracts.PatientFinance
    .Queries;

public sealed
    record GetPatientFinancialCaseDetailsQuery(Guid PatientFinancialCaseId)
    : IQuery<PatientFinancialCaseDetailsDto?>;
