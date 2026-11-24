using System.Data;
using DentalDashboard.ApplicationService.Accounting.Services;
using DentalDashboard.ApplicationService.Contract.Accounting;
using DentalDashboard.Domain.Accounting;
using DentalDashboard.Domain.IRepositories;
using DentalDashboard.Domain.Models;
using DentalDashboard.Domain.PatientReferrals;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Enums;
using Xunit;

namespace DentalDashboard.Consultation.Tests;

public sealed class AccountingSettlementTests
{
    [Fact]
    public async Task ReferralRewardHasOnlyOneCreditAndUsesTheActualReviewer()
    {
        var repository = new Repository();
        var unit = new Unit();
        var service = new AccountingSettlementService(repository, unit);
        var actor = Guid.NewGuid();
        await service.ReviewReferralAsync(1, actor, true, null, default);
        Assert.Equal(1000m, repository.Wallet.Balance);
        Assert.Equal(actor, repository.Referral.ReviewedByAdminId);
        Assert.Single(repository.ReferralRewards);
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.ReviewReferralAsync(1, actor, true, null, default));
        Assert.Single(repository.ReferralRewards);
        Assert.Equal(IsolationLevel.Serializable, unit.Isolation);
    }
    [Fact]
    public async Task ReferralCannotBeApprovedBeforeServiceAndRejectionDoesNotCreditTheWallet()
    {
        var repository = new Repository { Served = false };
        var service = new AccountingSettlementService(repository, new Unit());
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.ReviewReferralAsync(1, Guid.NewGuid(), true, null, default));
        Assert.Empty(repository.ReferralRewards);
        await service.ReviewReferralAsync(1, Guid.NewGuid(), false, "رد توسط حسابدار", default);
        Assert.Equal(PatientReferralStatus.Rejected, repository.Referral.Status);
        Assert.Equal(0, repository.Wallet.Balance);
    }
    [Fact]
    public async Task InstallmentCannotBePaidTwiceOrExceedTheRemainingTreatmentAmount()
    {
        var repository = new Repository();
        var service = new AccountingSettlementService(repository, new Unit());
        await service.PayInstallmentAsync(10, Guid.NewGuid(), default);
        Assert.Single(repository.Case.Transactions);
        Assert.Equal(PatientFinancialCaseStatus.Completed, repository.Case.Status);
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.PayInstallmentAsync(10, Guid.NewGuid(), default));
        Assert.Single(repository.Case.Transactions);
        repository = new Repository();
        repository.Case.TotalAmount = 900;
        service = new AccountingSettlementService(repository, new Unit());
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.PayInstallmentAsync(10, Guid.NewGuid(), default));
        Assert.Empty(repository.Case.Transactions);
    }
    [Fact]
    public async Task ExcessGuaranteeOnlyCollectsTheOverdueDebtAndSettlesItsOriginalSource()
    {
        var repository = new Repository();
        repository.Case.Installments.Clear();
        repository.Case.Cheques.Add(new() { Id = 11, IsGuarantee = true, Amount = 2000, DueDate = DateTime.UtcNow.AddDays(-1), Status = PatientChequeStatus.Pending });
        repository.Case.Cheques.Add(new() { Id = 12, IsGuarantee = false, Amount = 1000, Status = PatientChequeStatus.Unpaid });
        repository.Case.Debts.Add(new() { Id = 13, Amount = 1000, DueDate = DateTime.UtcNow.AddDays(-1), SourceType = PatientDebtSourceType.Cheque, SourceId = 12 });
        var service = new AccountingSettlementService(repository, new Unit());
        await service.CollectGuaranteeAsync(11, Guid.NewGuid(), default);
        Assert.Equal(1000, Assert.Single(repository.Case.Transactions).Amount);
        Assert.All(repository.Case.Cheques, x => Assert.Equal(PatientChequeStatus.Paid, x.Status));
        Assert.Equal(PatientDebtStatus.Paid, Assert.Single(repository.Case.Debts).Status);
        await Assert.ThrowsAsync<AccountingValidationException>(() => service.CollectGuaranteeAsync(11, Guid.NewGuid(), default));
        Assert.Single(repository.Case.Transactions);
    }
    [Theory]
    [InlineData(1500, 500, 1000, true)]
    [InlineData(1499, 500, 1000, false)]
    [InlineData(1500, 1500, 0, true)]
    [InlineData(1500, -1, 0, false)]
    public void TreatmentEditsRespectBothCollectionsAndTheUnpaidPlan(decimal total, decimal collected, decimal commitments, bool expected) =>
        Assert.Equal(expected, AccountingTreatmentRules.CanSetTotal(total, collected, commitments));

    private sealed class Unit : IUnitOfWork
    {
        public IsolationLevel Isolation;
        public Task BeginTransactionAsync(CancellationToken ct = default, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted) { Isolation = isolationLevel; return Task.CompletedTask; }
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
        public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
    private sealed class Repository : IAccountingSettlementRepository
    {
        public bool Served = true;
        public PatientReferral Referral = new() { Id = 1, ReservationId = 2, Status = PatientReferralStatus.ReservedPendingAdminApproval,
            ReferrerPatientUserId = Guid.NewGuid(), RewardAmount = 1000, ReferredFirstName = "test", ReferredLastName = "patient" };
        public PatientWallet Wallet = new();
        public List<PatientWalletTransaction> ReferralRewards = [];
        public PatientFinancialCase Case = new() { Id = Guid.NewGuid(), TotalAmount = 1000 };
        public Repository() { Case.Installments.Add(new() { Id = 10, PatientFinancialCaseId = Case.Id, Amount = 1000 }); }
        public Task<PatientReferral?> ReferralAsync(long id, CancellationToken ct) => Task.FromResult<PatientReferral?>(Referral);
        public Task<bool> ReservationServedAsync(long id, CancellationToken ct) => Task.FromResult(Served);
        public Task<bool> ReferralRewardExistsAsync(long id, CancellationToken ct) => Task.FromResult(ReferralRewards.Count > 0);
        public Task<PatientWallet> PatientWalletAsync(Guid userId, CancellationToken ct) => Task.FromResult(Wallet);
        public void AddReferralReward(PatientWalletTransaction transaction) => ReferralRewards.Add(transaction);
        public Task<PatientFinancialCase?> CaseForInstallmentAsync(long id, CancellationToken ct) => Task.FromResult<PatientFinancialCase?>(Case);
        public Task<PatientFinancialCase?> CaseForGuaranteeAsync(long id, CancellationToken ct) => Task.FromResult<PatientFinancialCase?>(Case);
        public void AddPayment(PatientFinancialTransaction transaction) => Case.Transactions.Add(transaction);
    }
}
