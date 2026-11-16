using Tempest.Application.Common.Results;
using Tempest.Application.Products.Commands;
using Tempest.Application.Products.Queries;
using Tempest.Application.Settings;
using Tempest.Application.Settings.Commands;
using Tempest.Application.Settings.Queries;
using Tempest.Application.Users.Commands;
using Tempest.Application.Users.Queries;
using Tempest.Domain.Enums;

namespace Tempest.Tests.Integration;

public sealed class UseCaseTests
{
    [Fact]
    public async Task First_run_seeds_the_admin_the_settings_and_the_sample_catalog()
    {
        await using var app = await TestApp.CreateAsync();

        var catalog = await app.SendAsync(new GetPublicCatalogQuery());
        var settings = await app.SendAsync(new GetWhatsAppSettingsQuery());
        var admin = await app.SignInAsync();

        Assert.Equal(8, catalog.Value.Count); // 9 seeded, 1 inactive
        Assert.False(settings.Value.IsConfigured);
        Assert.True(admin.MustChangePassword);
    }

    [Fact]
    public async Task Login_failures_do_not_reveal_whether_the_user_exists()
    {
        await using var app = await TestApp.CreateAsync();

        var wrongPassword = await app.SendAsync(new LoginCommand("admin", "nope"));
        var unknownUser = await app.SendAsync(new LoginCommand("ghost", "admin"));

        Assert.Equal(ErrorType.Unauthorized, wrongPassword.Error?.Type);
        Assert.Equal(wrongPassword.Error?.Message, unknownUser.Error?.Message);
    }

    [Fact]
    public async Task Admin_use_cases_require_an_authenticated_caller()
    {
        await using var app = await TestApp.CreateAsync();

        var create = await app.SendAsync(new CreateProductCommand("Katana", null, 10m, "/x.png", Rarity.Common, true));
        var users = await app.SendAsync(new GetUsersQuery());

        Assert.Equal(ErrorType.Unauthorized, create.Error?.Type);
        Assert.Equal(ErrorType.Unauthorized, users.Error?.Type);
    }

    [Fact]
    public async Task Validation_errors_are_reported_per_field()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var result = await app.SendAsync(new CreateProductCommand("", null, 0m, "javascript:alert(1)", Rarity.Common, true));

        Assert.Equal(ErrorType.Validation, result.Error?.Type);
        Assert.Contains(nameof(CreateProductCommand.Name), result.Error!.FieldErrors.Keys);
        Assert.Contains(nameof(CreateProductCommand.Price), result.Error.FieldErrors.Keys);
        Assert.Contains(nameof(CreateProductCommand.ImageUrl), result.Error.FieldErrors.Keys);
    }

    [Fact]
    public async Task Inactive_products_are_hidden_from_the_public_catalog_only()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var created = await app.SendAsync(new CreateProductCommand("Figura Test", "Desc", 59.5m, "/x.png", Rarity.Epic, true));
        Assert.Contains((await app.SendAsync(new GetPublicCatalogQuery())).Value, p => p.Id == created.Value);

        var deactivated = await app.SendAsync(new SetProductStatusCommand(created.Value, IsActive: false));

        Assert.True(deactivated.IsSuccess);
        Assert.DoesNotContain((await app.SendAsync(new GetPublicCatalogQuery())).Value, p => p.Id == created.Value);
        Assert.Contains((await app.SendAsync(new GetAdminProductsQuery())).Value, p => p.Id == created.Value && !p.IsActive);
    }

    [Fact]
    public async Task Prices_round_trip_exactly_through_sqlite()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var created = await app.SendAsync(new CreateProductCommand("Precio", null, 1234567.89m, "/x.png", Rarity.Rare, true));
        var stored = (await app.SendAsync(new GetAdminProductsQuery())).Value.Single(p => p.Id == created.Value);

        Assert.Equal(1234567.89m, stored.Price);
    }

    [Fact]
    public async Task Changing_the_password_invalidates_existing_sessions()
    {
        await using var app = await TestApp.CreateAsync();
        var session = await app.SignInAsync();

        var wrongCurrent = await app.SendAsync(new ChangePasswordCommand("bad", "NuevaClave2026!"));
        var changed = await app.SendAsync(new ChangePasswordCommand("admin", "NuevaClave2026!"));

        Assert.Contains(nameof(ChangePasswordCommand.CurrentPassword), wrongCurrent.Error!.FieldErrors.Keys);
        Assert.True(changed.IsSuccess);
        Assert.False(await app.IsSessionValidAsync(session.Id, session.SecurityStamp));

        app.SignOut();
        var newSession = await app.SignInAsync("admin", "NuevaClave2026!");
        Assert.False(newSession.MustChangePassword);
        Assert.True(await app.IsSessionValidAsync(newSession.Id, newSession.SecurityStamp));
    }

    [Fact]
    public async Task Usernames_are_unique_regardless_of_case()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var duplicate = await app.SendAsync(new CreateUserCommand("ADMIN", "Password123"));
        var created = await app.SendAsync(new CreateUserCommand("Maria.Ventas", "Password123"));
        var users = await app.SendAsync(new GetUsersQuery());

        Assert.Contains(nameof(CreateUserCommand.Username), duplicate.Error!.FieldErrors.Keys);
        Assert.True(created.IsSuccess);
        Assert.Contains(users.Value, u => u.Username == "maria.ventas" && u.MustChangePassword);
    }

    [Fact]
    public async Task WhatsApp_settings_are_normalized_and_drive_the_catalog_links()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();

        var updated = await app.SendAsync(new UpdateWhatsAppSettingsCommand("+52 (55) 1234-5678", "Quiero {producto} a {precio}"));
        var settings = (await app.SendAsync(new GetWhatsAppSettingsQuery())).Value;

        Assert.True(updated.IsSuccess);
        Assert.Equal("525512345678", settings.PhoneNumber);
        Assert.Equal(
            "https://wa.me/525512345678?text=Quiero%20Katana%20a%20%24149.00",
            WhatsAppLinkBuilder.ForProduct(settings, "Katana", "$149.00"));
    }

    [Fact]
    public async Task Uploads_are_checked_by_content_and_removed_with_their_product()
    {
        await using var app = await TestApp.CreateAsync();
        await app.SignInAsync();
        byte[] png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

        var svg = await app.SendAsync(new UploadProductImageCommand(new MemoryStream("<svg onload=alert(1)>"u8.ToArray()), "x.png"));
        var upload = await app.SendAsync(new UploadProductImageCommand(new MemoryStream(png), "foto.png"));

        Assert.Equal(ErrorType.Validation, svg.Error?.Type);
        Assert.Matches("^/uploads/[a-f0-9]{32}\\.png$", upload.Value);
        var file = Path.Combine(app.UploadsPath, Path.GetFileName(upload.Value));
        Assert.True(File.Exists(file));

        var product = await app.SendAsync(new CreateProductCommand("Con imagen", null, 10m, upload.Value, Rarity.Common, true));
        await app.SendAsync(new DeleteProductCommand(product.Value));

        Assert.False(File.Exists(file));
    }
}
