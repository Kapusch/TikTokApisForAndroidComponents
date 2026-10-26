namespace Kapusch.TikTok.Business.Android;

/// <summary>One item of the standard <c>contents</c> property.</summary>
/// <param name="ContentId">Identifier of the item.</param>
/// <param name="ContentName">Display name of the item.</param>
/// <param name="ContentCategory">Category of the item.</param>
/// <param name="Brand">Brand of the item.</param>
/// <param name="Price">Unit price.</param>
/// <param name="Quantity">Quantity.</param>
public sealed record TikTokBusinessContent(
	string? ContentId = null,
	string? ContentName = null,
	string? ContentCategory = null,
	string? Brand = null,
	decimal? Price = null,
	int? Quantity = null
);
