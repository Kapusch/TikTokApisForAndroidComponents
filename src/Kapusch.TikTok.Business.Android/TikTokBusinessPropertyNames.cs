namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// Standard event property names of the TikTok Business SDK for Android 1.7.1
/// (<c>TTContentsEventConstants.Params</c> and <c>TTContentParams</c>).
/// </summary>
/// <remarks>TikTok discards custom property keys, so prefer these names.</remarks>
public static class TikTokBusinessPropertyNames
{
	/// <summary>
	/// Monetary value of the event. The Android helpers send it as a number, like the SDK's
	/// <c>TTContentsEvent.Builder.setValue(double)</c>; the iOS package sends a decimal string.
	/// </summary>
	public const string Value = "value";

	/// <summary>ISO 4217 currency code, see <see cref="TikTokBusinessCurrencies"/>.</summary>
	public const string Currency = "currency";

	/// <summary>Identifier of the content or product.</summary>
	public const string ContentId = "content_id";

	/// <summary>Type of content, for example <c>product</c> or <c>subscription</c>.</summary>
	public const string ContentType = "content_type";

	/// <summary>Free-form description.</summary>
	public const string Description = "description";

	/// <summary>Array of content items. Use <see cref="TikTokBusinessContent"/> with the typed helpers.</summary>
	public const string Contents = "contents";

	/// <summary>Android only: order identifier (<c>TTContentsEventConstants.Params.EVENT_PROPERTY_ORDER_ID</c>).</summary>
	public const string OrderId = "order_id";

	/// <summary>Content item price (inside <see cref="Contents"/>).</summary>
	public const string Price = "price";

	/// <summary>Content item quantity (inside <see cref="Contents"/>).</summary>
	public const string Quantity = "quantity";

	/// <summary>Content item category (inside <see cref="Contents"/>).</summary>
	public const string ContentCategory = "content_category";

	/// <summary>Content item name (inside <see cref="Contents"/>).</summary>
	public const string ContentName = "content_name";

	/// <summary>Content item brand (inside <see cref="Contents"/>).</summary>
	public const string Brand = "brand";

	/// <summary>
	/// Ad revenue payload of <see cref="TikTokBusinessEventNames.ImpressionLevelAdRevenue"/>. Listed for
	/// parity with the iOS package; the Android SDK has no typed ad revenue API and forwards the key as is.
	/// </summary>
	public const string AdRevenue = "ad_revenue";
}

/// <summary>
/// Android only: the currency codes listed by the TikTok Business SDK for Android 1.7.1
/// (<c>TTContentsEventConstants.Currency</c>). The typed helpers accept any ISO 4217 code, like the
/// iOS package; use this list to check a code against the SDK's own enumeration.
/// </summary>
public static class TikTokBusinessCurrencies
{
	/// <summary>All supported ISO 4217 codes.</summary>
	public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
	{
		"AED", "ARS", "AUD", "BDT", "BHD", "BIF", "BOB", "BRL", "CAD", "CHF", "CLP", "CNY", "COP", "CRC",
		"CZK", "DKK", "DZD", "EGP", "EUR", "GBP", "GTQ", "HKD", "HNL", "HUF", "IDR", "ILS", "INR", "ISK",
		"JPY", "KES", "KHR", "KRW", "KWD", "KZT", "MAD", "MOP", "MXN", "MYR", "NGN", "NIO", "NOK", "NZD",
		"OMR", "PEN", "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RUB", "SAR", "SEK", "SGD", "THB", "TRY",
		"TWD", "UAH", "USD", "VES", "VND", "ZAR", "BGN", "IQD", "JOD", "LBP", "TZS",
	};

	/// <summary>Returns whether <paramref name="currency"/> is supported by the SDK.</summary>
	public static bool IsSupported(string? currency) => currency is not null && Supported.Contains(currency);
}
