using DentalDashboard.Domain.Models;
namespace DentalDashboard.Domain.PatientReferrals;
public enum PatientReferralStatus { Submitted=1, Contacted=2, ReservedPendingAdminApproval=3, ApprovedRewarded=4, Rejected=5 }
public enum PatientWalletTransactionType { ReferralReward=1, Withdrawal=2, ManualCredit=3, ManualDebit=4 }
public sealed class PatientReferral : BaseAuditableEntity<long> {
 public Guid ReferrerPatientUserId {get;set;} public User ReferrerPatientUser {get;set;}=default!;
 public string ReferredFirstName {get;set;}=default!; public string ReferredLastName {get;set;}=default!; public string ReferredPhoneNumber {get;set;}=default!; public string? Description {get;set;}
 public PatientReferralStatus Status {get;set;}=PatientReferralStatus.Submitted; public Guid? SecretaryUserId {get;set;} public User? SecretaryUser {get;set;} public DateTime? ContactedAt {get;set;}
 public long? LeadAssignmentId {get;set;} public LeadAssignment? LeadAssignment {get;set;} public long? ReservationId {get;set;} public Reservation? Reservation {get;set;}
 public decimal RewardAmount {get;set;} public Guid? ReviewedByAdminId {get;set;} public User? ReviewedByAdmin {get;set;} public DateTime? ReviewedAt {get;set;} public string? RejectionReason {get;set;}
}
public sealed class PatientWallet : BaseAuditableEntity<long> { public Guid PatientUserId {get;set;} public User PatientUser {get;set;}=default!; public decimal Balance {get;set;} public ICollection<PatientWalletTransaction> Transactions {get;set;}=[]; }
public sealed class PatientWalletTransaction : BaseAuditableEntity<long> { public long WalletId {get;set;} public PatientWallet Wallet {get;set;}=default!; public Guid PatientUserId {get;set;} public User PatientUser {get;set;}=default!; public long PatientReferralId {get;set;} public PatientReferral PatientReferral {get;set;}=default!; public decimal Amount {get;set;} public PatientWalletTransactionType TransactionType {get;set;} public string Description {get;set;}=default!; }
