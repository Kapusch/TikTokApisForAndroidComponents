namespace Kapusch.TikTok.Business.Android;

/// <summary>
/// Standard event names of the TikTok Business SDK for Android 1.7.1, copied verbatim from
/// <c>com.tiktok.appevents.base.EventName</c>, <c>TTContentsEventConstants.ContentsEventName</c>
/// and <c>TTConst.AutoEvents</c>.
/// </summary>
/// <remarks>
/// The app SDK has no <c>CompletePayment</c> event: that name belongs to the web pixel taxonomy.
/// Use <see cref="Purchase"/> for app purchases.
/// </remarks>
public static class TikTokBusinessEventNames
{
	/// <summary>Automatic: first initialization on a device. Can also be sent manually when automatic tracking is disabled.</summary>
	public const string InstallApp = "InstallApp";

	/// <summary>Automatic: every launch. Can also be sent manually when automatic tracking is disabled.</summary>
	public const string LaunchApp = "LaunchAPP";

	/// <summary>Automatic: the user opened the app again the day after install.</summary>
	public const string SecondDayRetention = "2Dretention";

	/// <summary>A free trial started.</summary>
	public const string StartTrial = "StartTrial";

	/// <summary>A paid subscription started.</summary>
	public const string Subscribe = "Subscribe";

	/// <summary>A purchase completed. Also emitted by automatic in-app purchase tracking when it is enabled.</summary>
	public const string Purchase = "Purchase";

	/// <summary>The user created an account.</summary>
	public const string Registration = "Registration";

	/// <summary>The user signed in.</summary>
	public const string Login = "Login";

	/// <summary>An item was added to the cart.</summary>
	public const string AddToCart = "AddToCart";

	/// <summary>An item was added to a wishlist.</summary>
	public const string AddToWishlist = "AddToWishlist";

	/// <summary>The checkout flow started.</summary>
	public const string Checkout = "Checkout";

	/// <summary>A content or product page was viewed.</summary>
	public const string ViewContent = "ViewContent";

	/// <summary>Payment details were added.</summary>
	public const string AddPaymentInfo = "AddPaymentInfo";

	/// <summary>A search was performed.</summary>
	public const string Search = "Search";

	/// <summary>A level was achieved.</summary>
	public const string AchieveLevel = "AchieveLevel";

	/// <summary>A tutorial was completed.</summary>
	public const string CompleteTutorial = "CompleteTutorial";

	/// <summary>A group was created.</summary>
	public const string CreateGroup = "CreateGroup";

	/// <summary>A role or character was created.</summary>
	public const string CreateRole = "CreateRole";

	/// <summary>A lead was generated.</summary>
	public const string GenerateLead = "GenerateLead";

	/// <summary>An in-app ad was clicked.</summary>
	public const string InAppAdClick = "InAppADClick";

	/// <summary>
	/// An in-app ad impression. The Android SDK spells it <c>InAppAdImpr</c>; the iOS SDK spells it
	/// <c>InAppADImpr</c>.
	/// </summary>
	public const string InAppAdImpression = "InAppAdImpr";

	/// <summary>Impression-level ad revenue.</summary>
	public const string ImpressionLevelAdRevenue = "ImpressionLevelAdRevenue";

	/// <summary>A group was joined.</summary>
	public const string JoinGroup = "JoinGroup";

	/// <summary>Content was rated.</summary>
	public const string Rate = "Rate";

	/// <summary>In-app credits were spent.</summary>
	public const string SpendCredits = "SpendCredits";

	/// <summary>An achievement was unlocked.</summary>
	public const string UnlockAchievement = "UnlockAchievement";

	/// <summary>Deprecated upstream.</summary>
	[Obsolete("Deprecated by the TikTok Business SDK.")]
	public const string LoanApplication = "LoanApplication";

	/// <summary>Deprecated upstream.</summary>
	[Obsolete("Deprecated by the TikTok Business SDK.")]
	public const string LoanApproval = "LoanApproval";

	/// <summary>Deprecated upstream.</summary>
	[Obsolete("Deprecated by the TikTok Business SDK.")]
	public const string LoanDisbursal = "LoanDisbursal";

	/// <summary>
	/// iOS only: restored purchase, sent automatically by the iOS SDK purchase observer. Listed for
	/// parity with the iOS package; the Android SDK never sends it.
	/// </summary>
	public const string PurchaseRestored = "PurchaseRestored";
}
