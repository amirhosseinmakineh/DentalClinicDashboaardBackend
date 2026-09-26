using DentalDashboard.Framwork.Cqrs.Abstraction.Wrire;
using System.Text.Json.Serialization;

namespace DentalDashboard.ApplicationService.Contract.Requests.Reservation.Commands
{
    public class ReviewReservationAttendanceCommand : ICommand
    {
        public long ReservationId { get; set; }
        [JsonIgnore]
        public Guid SecretaryUserId { get; set; }
        /// <summary>
        /// Indicates whether the patient actually received a service during the visit.
        /// A provided service confirms the visit; otherwise the visit is rejected.
        /// </summary>
        public bool? PatientReceivedService { get; set; }
        /// <summary>Backward-compatible alias for older secretary clients.</summary>
        public bool? Approved { get; set; }
        public string? Note { get; set; }
        public string? DoctorName { get; set; }
    }
}
