using DentalDashboard.LeadManagement.Application.Consultation;
using Xunit;

namespace DentalDashboard.IntegrationTests;

public class ConsultationInputTests
{
    [Theory]
    [InlineData("+989123456789")]
    [InlineData("00989123456789")]
    [InlineData("۹۱۲۳۴۵۶۷۸۹")]
    [InlineData("۰۹۱۲ ۳۴۵ ۶۷۸۹")]
    [InlineData("(0912)-345-6789")]
    public void Common_mobile_formats_normalize_to_one_identity(string phone) =>
        Assert.Equal("09123456789", ConsultationInput.NormalizePhone(phone));

    [Theory]
    [InlineData("0912")]
    [InlineData("+49123456789")]
    [InlineData("091234567890")]
    [InlineData("0912345<script>")]
    public void Invalid_mobiles_are_rejected(string phone) =>
        Assert.False(ConsultationInput.IsValidPhone(ConsultationInput.NormalizePhone(phone)));

    [Theory]
    [InlineData("علی احمدی", true)]
    [InlineData("  امیرحسین   مکینه ", true)]
    [InlineData("آنا-ماریا احمدی", true)]
    [InlineData("۱۲۳", false)]
    [InlineData("<script>", false)]
    [InlineData("ع ا", false)]
    public void Names_require_letters_and_allow_real_names(string name, bool expected) =>
        Assert.Equal(expected, ConsultationInput.IsValidName(ConsultationInput.NormalizeName(name)));
}
