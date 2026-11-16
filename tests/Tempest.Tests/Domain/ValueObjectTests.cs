using Tempest.Domain.Common;
using Tempest.Domain.ValueObjects;

namespace Tempest.Tests.Domain;

public sealed class WhatsAppNumberTests
{
    [Theory]
    [InlineData("5215512345678", "5215512345678")]
    [InlineData("+52 (55) 1234-5678", "525512345678")]
    [InlineData(" 506 8888.7777 ", "50688887777")]
    public void Normalizes_formatting_characters(string input, string expected)
    {
        Assert.True(WhatsAppNumber.TryCreate(input, out var number));
        Assert.Equal(expected, number.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc5215512345678")]
    [InlineData("1234567")]            // too short
    [InlineData("1234567890123456")]   // longer than E.164
    [InlineData("0123456789")]         // local trunk prefix instead of country code
    [InlineData("52+15512345678")]     // "+" only allowed first
    public void Rejects_invalid_numbers(string? input)
    {
        Assert.False(WhatsAppNumber.TryCreate(input, out _));
        Assert.Throws<DomainException>(() => WhatsAppNumber.Create(input));
    }
}

public sealed class MessageTemplateTests
{
    [Fact]
    public void Replaces_placeholders_case_insensitively()
    {
        var template = MessageTemplate.Create("Hola, quiero {PRODUCTO} por {Precio}.");

        Assert.Equal("Hola, quiero Katana por $149.00.", template.Render("Katana", "$149.00"));
    }

    [Fact]
    public void Renders_in_a_single_pass_so_values_are_never_reinterpreted()
    {
        var template = MessageTemplate.Create("{producto} a {precio}");

        Assert.Equal("Figura {precio} a $5.00", template.Render("Figura {precio}", "$5.00"));
    }

    [Fact]
    public void Default_template_uses_both_placeholders()
    {
        Assert.True(MessageTemplate.Default.Contains(MessageTemplate.ProductPlaceholder));
        Assert.True(MessageTemplate.Default.Contains(MessageTemplate.PricePlaceholder));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Rejects_empty_templates(string? input) => Assert.False(MessageTemplate.TryCreate(input, out _));

    [Fact]
    public void Rejects_templates_over_the_maximum_length() =>
        Assert.False(MessageTemplate.TryCreate(new string('x', MessageTemplate.MaxLength + 1), out _));
}

public sealed class UsernameTests
{
    [Fact]
    public void Is_trimmed_and_lowercased()
    {
        Assert.True(Username.TryCreate("  Maria.Ventas ", out var username));
        Assert.Equal("maria.ventas", username.Value);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("con espacio")]
    [InlineData("ñandú")]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456")] // 33 chars
    public void Rejects_invalid_usernames(string input) => Assert.False(Username.TryCreate(input, out _));
}
