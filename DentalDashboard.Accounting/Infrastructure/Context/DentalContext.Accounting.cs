using DentalDashboard.Domain.Secretary.Accountant.Entities;
using DentalDashboard.Domain.Secretary.Accountant.PatientFinance.Entities;
using DentalDashboard.Domain.Secretary.Accountant.SecretarySales.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalDashboard.Infrastracture.Context;

public partial class DentalContext
{
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<ExpenseCategory> ExpenseCategories => Set<ExpenseCategory>();
    public DbSet<PatientFinancialCase> PatientFinancialCases => Set<PatientFinancialCase>();
    public DbSet<PatientCheque> PatientCheques => Set<PatientCheque>();
    public DbSet<PatientPromissoryNote> PatientPromissoryNotes => Set<PatientPromissoryNote>();
    public DbSet<PatientDebt> PatientDebts => Set<PatientDebt>();
    public DbSet<PatientFinancialTransaction> PatientFinancialTransactions => Set<PatientFinancialTransaction>();
    public DbSet<SecretarySaleService> SecretarySaleServices => Set<SecretarySaleService>();
    public DbSet<SecretarySale> SecretarySales => Set<SecretarySale>();
    public DbSet<SecretaryWallet> SecretaryWallets => Set<SecretaryWallet>();
    public DbSet<SecretaryWalletTransaction> SecretaryWalletTransactions => Set<SecretaryWalletTransaction>();
}
