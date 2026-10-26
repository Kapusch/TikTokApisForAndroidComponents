namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// Standard event property names of the TikTok Business SDK for Android 1.7.1
/// (<c>TTContentsEventConstants.Params</c> and <c>TTContentParams</c>).
/// </summary>
/// <remarks>TikTok discards custom property keys, so prefer these names.</remarks>
public static class TikTokBusinessPropertyNames
{
	/// <summary>Monetary value of the event, as a number.</summary>
	public const string Value = "value";

	/// <summary>ISO 4217 currency code, see <see cref="TikTokBusinessCurrencies"/>.</summary>
	public const string Currency = "currency";

	/// <summary>Identifier of the content or product.</summary>
	public const string ContentId = "content_id";

	/// <summary>Type of content, for example <c>product</c> or <c>subscription</c>.</summary>
	public const string ContentType = "content_type";

	/// <summary>Free-form description.</summary>
	public const string Description = "description";

	/// <summary>Array of content items, see the <c>Content*</c> item keys.</summary>
	public const string Contents = "contents";

	/// <summary>Order identifier.</summary>
	public const string OrderId = "order_id";

	/// <summary>Content item key: unit price.</summary>
	public const string ContentPrice = "price";

	/// <summary>Content item key: quantity.</summary>
	public const string ContentQuantity = "quantity";

	/// <summary>Content item key: category.</summary>
	public const string ContentCategory = "content_category";

	/// <summary>Content item key: name.</summary>
	public const string ContentName = "content_name";

	/// <summary>Content item key: brand.</summary>
	public const string ContentBrand = "brand";
}

/// <summary>Currency codes accepted by the TikTok Business SDK for Android 1.7.1 (<c>TTContentsEventConstants.Currency</c>).</summary>
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
