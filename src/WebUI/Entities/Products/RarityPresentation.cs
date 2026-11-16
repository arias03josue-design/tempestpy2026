using Tempest.Domain.Enums;

namespace Tempest.WebUI.Entities.Products;

/// <summary>Display concerns of the product rarity (label and CSS modifier).</summary>
public static class RarityPresentation
{
    public static string ToLabel(this Rarity rarity) => rarity switch
    {
        Rarity.Common => "Común",
        Rarity.Rare => "Raro",
        Rarity.Epic => "Épico",
        Rarity.Legendary => "Legendario",
        _ => rarity.ToString(),
    };

    public static string ToCssModifier(this Rarity rarity) => rarity switch
    {
        Rarity.Rare => "rare",
        Rarity.Epic => "epic",
        Rarity.Legendary => "legendary",
        _ => "common",
    };
}
