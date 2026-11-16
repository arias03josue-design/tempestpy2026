using Tempest.Domain.Entities;
using Tempest.Domain.Enums;

namespace Tempest.Infrastructure.Persistence.Seed;

/// <summary>Demo catalog (anime figures, collectibles and gaming gear) with bundled SVG artwork.</summary>
internal static class SampleCatalog
{
    private const string ImageBase = "/images/products/";

    private static readonly (string Name, string Description, decimal Price, string Image, Rarity Rarity, bool Active)[] Items =
    [
        ("Figura Kitsune Ronin 1/7 — Edición Limitada",
            "PVC pintado a mano con base iluminada y máscara intercambiable. Tiraje numerado.",
            189.99m, "kitsune-ronin.svg", Rarity.Legendary, true),
        ("Teclado Mecánico Neon Strike 75%",
            "Switches lineales hot-swap, keycaps PBT y retroiluminación RGB por tecla.",
            129.90m, "neon-strike-keyboard.svg", Rarity.Epic, true),
        ("Headset Void Walker 7.1",
            "Sonido envolvente virtual, micrófono con cancelación de ruido y almohadillas viscoelásticas.",
            89.50m, "void-walker-headset.svg", Rarity.Rare, true),
        ("Réplica Katana Tempest Edge",
            "Réplica decorativa de 100 cm con hoja sin filo, tsuka envuelta y soporte de exhibición.",
            149.00m, "tempest-edge-katana.svg", Rarity.Epic, true),
        ("Mouse Spectral X Inalámbrico",
            "Sensor óptico de 26K DPI, 58 g de peso y hasta 90 horas de batería.",
            69.99m, "spectral-x-mouse.svg", Rarity.Rare, true),
        ("Control Pulse Pro Hall Effect",
            "Joysticks magnéticos sin drift, gatillos ajustables y conexión Bluetooth / 2.4 GHz.",
            74.90m, "pulse-pro-controller.svg", Rarity.Rare, true),
        ("Figura Chibi Mecha Guardian",
            "Figura articulada de 12 cm con armadura desmontable y efectos de energía translúcidos.",
            39.99m, "mecha-guardian.svg", Rarity.Common, true),
        ("Mousepad XXL Neo Tokyo Nights",
            "Tela microtexturizada de 90 × 40 cm con bordes cosidos y base antideslizante.",
            24.99m, "neo-tokyo-mousepad.svg", Rarity.Common, true),
        ("Peluche Slime Kawaii XL",
            "Peluche ultrasuave de 40 cm para tu setup. Vuelve pronto a stock.",
            29.90m, "slime-plush.svg", Rarity.Common, false),
    ];

    public static IEnumerable<Product> Create(DateTime utcNow) =>
        Items.Select((item, index) => Product.Create(
            item.Name,
            item.Description,
            item.Price,
            ImageBase + item.Image,
            item.Rarity,
            item.Active,
            utcNow.AddMinutes(-index))); // newest first = listed order
}
