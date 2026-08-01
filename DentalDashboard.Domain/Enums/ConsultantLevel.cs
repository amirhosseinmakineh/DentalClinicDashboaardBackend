namespace DentalDashboard.Domain.Enums;

/// <summary>
/// Business classification of a consultant. Authorization continues to use the
/// Consultant role; this value only describes the consultant's sales level.
/// </summary>
public enum ConsultantLevel : byte
{
    Test = 1,
    Seller = 2,
    TopSeller = 3
}
