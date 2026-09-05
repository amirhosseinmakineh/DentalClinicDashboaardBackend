using DentalDashboard.Accounting.Domain.Entities;
using DentalDashboard.Accounting.Domain.PatientFinance.Entities;
using DentalDashboard.Accounting.Domain.SecretarySales.Entities;
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
