using System.Linq.Expressions;
using System.Reflection;
using DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands;
using DentalDashboard.ApplicationService.Handlers.CommandHandlers.Reservation;
using DentalDashboard.Controllers;
using DentalDashboard.Domain.Enums;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using Microsoft.EntityFrameworkCore.Query;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class PatientFileServiceResultTests
{
    [Fact]
    public void ReferencedFilesMatchTheOriginalPatientEvenAfterPhoneChanges()
    {
        var file = new PatientFile { PatientReferenceId = 15, PhoneNumber = "09120000000" };
        Assert.True(PatientFileServiceResultRules.Matches(file, 15, "09121111111"));
        Assert.False(PatientFileServiceResultRules.Matches(file, 16, file.PhoneNumber));
        file.PatientReferenceId = null;
        Assert.True(PatientFileServiceResultRules.Matches(file, 16, file.PhoneNumber));
        Assert.False(PatientFileServiceResultRules.Matches(file, 16, "091200000001"));
        file.PhoneNumber = "";
        Assert.False(PatientFileServiceResultRules.Matches(file, 16, ""));
    }

    [Fact]
    public void ServiceResultRequiresCurrentApprovalAndFinancialSnapshot()
    {
        var finance = new PatientFinancialCase { TotalAmount = 100, PrePaymentAmount = 100, PaymentMethod = "نقدی" };
        var workflow = new PatientFileWorkflow {
            FinancialCase = finance, ConsultationCompleted = true,
            TreatmentConfirmedAtUtc = DateTime.UtcNow, FinanceConfirmedAtUtc = DateTime.UtcNow,
            ProfileConfirmedAtUtc = DateTime.UtcNow, ApprovedAtUtc = DateTime.UtcNow,
            ApprovedFinanceHash = PatientFileWorkflowRules.FinanceHash(finance)
        };
        Assert.True(PatientFileWorkflowRules.IsApproved(workflow));
        finance.TotalAmount = 200;
        Assert.False(PatientFileWorkflowRules.IsApproved(workflow));
        finance.TotalAmount = 100;
        workflow.ClearApproval();
        Assert.False(PatientFileWorkflowRules.IsApproved(workflow));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    public async Task ExistingHandlerPreservesConsultantRewardEligibilityAndRejectsDuplicateResults(
        bool received, bool secretaryOwned, bool eligible)
    {
        var reservation = new Reservation { Id = 1, ConsultantProfileId = 2,
            ReservationAt = DateTime.Now.AddDays(-1),
            OwnerType = secretaryOwned ? ReservationOwnerType.Secretary : null };
        var saves = 0;
        var reservations = RepositoryDouble.Create<IReservationRepository>(method => method.Name switch {
            "GetAll" => new AsyncQuery<Reservation>(new[] { reservation }.AsQueryable()),
            "Update" => null,
            "SaveChange" => Save(),
            _ => throw new NotSupportedException(method.Name)
        });
        Task Save() { saves++; return Task.CompletedTask; }
        var consultants = RepositoryDouble.Create<IConsultantProfileRepository>(method => method.Name == "GetAll"
            ? new AsyncQuery<ConsultantProfile>(new[] { new ConsultantProfile { Id = 2 } }.AsQueryable())
            : throw new NotSupportedException(method.Name));
        var handler = new ReviewReservationAttendanceCommandHandler(reservations, consultants);
        var command = new ReviewReservationAttendanceCommand { ReservationId = 1, PatientReceivedService = received,
            DoctorName = received ? "دکتر تست" : null, SecretaryUserId = Guid.NewGuid() };
        if (received) {
            command.DoctorName = " ";
            Assert.False((await handler.HandleAsync(command)).IsSuccess);
            Assert.Equal(0, saves);
            command.DoctorName = "دکتر تست";
        }
        Assert.True((await handler.HandleAsync(command)).IsSuccess);
        Assert.Equal(received, reservation.PatientReceivedService);
        Assert.Equal(eligible, reservation.ConsultantRewardEligibleAt.HasValue);
        Assert.Equal(received ? "دکتر تست" : null, reservation.DoctorName);
        Assert.True(reservation.IsAttendanceScoreApplied);
        Assert.Equal(1, saves);
        Assert.False((await handler.HandleAsync(command)).IsSuccess);
        Assert.Equal(1, saves);
    }

    public class RepositoryDouble : DispatchProxy
    {
        private Func<MethodInfo, object?> callback = null!;
        public static T Create<T>(Func<MethodInfo, object?> callback) where T : class {
            var proxy = Create<T, RepositoryDouble>();
            ((RepositoryDouble)(object)proxy).callback = callback;
            return proxy;
        }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => callback(targetMethod!);
    }

    private sealed class AsyncQuery<T>(IQueryable<T> source) : IQueryable<T>, IAsyncQueryProvider
    {
        public Type ElementType => typeof(T);
        public Expression Expression => source.Expression;
        public IQueryProvider Provider => this;
        public IEnumerator<T> GetEnumerator() => source.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public IQueryable CreateQuery(Expression expression) => source.Provider.CreateQuery(expression);
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQuery<TElement>(source.Provider.CreateQuery<TElement>(expression));
        public object? Execute(Expression expression) => source.Provider.Execute(expression);
        public TResult Execute<TResult>(Expression expression) => source.Provider.Execute<TResult>(expression);
        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default) {
            var result = source.Provider.Execute(expression);
            var resultType = typeof(TResult).GetGenericArguments()[0];
            return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType).Invoke(null, [result])!;
        }
    }
}
