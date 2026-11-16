using Tempest.Application.Settings;

namespace Tempest.Tests.Application;

public sealed class WhatsAppLinkBuilderTests
{
    [Fact]
    public void Builds_a_wa_me_link_with_the_rendered_and_encoded_message()
    {
        var settings = new WhatsAppSettingsDto("5215512345678", "¡Hola! Quiero {producto} a {precio}", UpdatedAtUtc: null);

        var link = WhatsAppLinkBuilder.ForProduct(settings, "Katana 1/7", "$149.00");

        Assert.Equal(
            "https://wa.me/5215512345678?text=%C2%A1Hola%21%20Quiero%20Katana%201%2F7%20a%20%24149.00",
            link);
    }

    [Fact]
    public void Returns_null_while_the_number_is_not_configured()
    {
        var settings = new WhatsAppSettingsDto(PhoneNumber: null, "Hola {producto}", UpdatedAtUtc: null);

        Assert.Null(WhatsAppLinkBuilder.ForProduct(settings, "Katana", "$1.00"));
    }

    [Fact]
    public void Falls_back_to_the_default_template_when_empty()
    {
        var message = WhatsAppLinkBuilder.RenderMessage("  ", "Katana", "$1.00");

        Assert.Equal("¡Hola! Me interesa comprar el producto Katana que vi a $1.00.", message);
    }

    [Theory]
    [InlineData("+52 1 55 1234 5678", "5215512345678")]
    [InlineData("hola", null)]
    public void Normalizes_phone_numbers(string input, string? expected) =>
        Assert.Equal(expected, WhatsAppLinkBuilder.NormalizePhoneNumber(input));
}
