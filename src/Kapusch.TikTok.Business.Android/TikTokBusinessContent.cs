namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// One entry of the standard <c>contents</c> property, matching <c>TTContentParams</c>. Same
/// parameter order and defaults as the iOS package.
/// </summary>
/// <param name="ContentId">Content identifier.</param>
/// <param name="Price">
/// Unit price, sent as a string like the SDK does. <see langword="null"/> omits it, as the Android
/// SDK's <c>TTContentParams</c> does (the iOS package sends <c>0</c> instead).
/// </param>
/// <param name="Quantity">Quantity.</param>
/// <param name="ContentCategory">Optional category.</param>
/// <param name="ContentName">Optional name.</param>
/// <param name="Brand">Optional brand.</param>
public sealed record TikTokBusinessContent(
	string? ContentId = null,
	decimal? Price = null,
	int Quantity = 1,
	string? ContentCategory = null,
	string? ContentName = null,
	string? Brand = null
);
