namespace DentalDashboard.ApplicationService.Handlers.Helpers;

public static class ReservationAppointmentTime
{
    public static bool TryResolve(
        DateTime reservationAt,
        DateTime? appointmentDateTime,
        out DateTime value,
        out string? error)
    {
        if (appointmentDateTime.HasValue &&
            reservationAt != default &&
            reservationAt != appointmentDateTime.Value)
        {
            value = default;
            error = "زمان مراجعه در appointmentDateTime و reservationAt یکسان نیست";
            return false;
        }

        value = appointmentDateTime ?? reservationAt;
        if (value == default || value.Year < 2000 || value.Year > 2100)
        {
            error = "زمان مراجعه معتبر نیست";
            return false;
        }

        error = null;
        return true;
    }
}
