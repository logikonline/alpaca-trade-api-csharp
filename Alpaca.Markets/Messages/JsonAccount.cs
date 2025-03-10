using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Alpaca.Markets;

[SuppressMessage("ReSharper", "StringLiteralTypo")]
[DebuggerDisplay("{DebuggerDisplay,nq}", Type = nameof(IAccount))]
[SuppressMessage(
	"Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
	Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
internal sealed class JsonAccount : IAccount
{
	/// <summary>
	/// Initializes a new instance of the <see cref="JsonAccount"/> class.
	/// Required for JSON.NET deserialization.
	/// </summary>
	[JsonConstructor]
	public JsonAccount()
	{
	}
	[JsonProperty(PropertyName = "id", Required = Required.Default)]
	public Guid AccountId { get; set; }

	[JsonProperty(PropertyName = "account_number", Required = Required.Default)]
	public String? AccountNumber { get; set; }

	[JsonProperty(PropertyName = "status", Required = Required.Always)]
	public AccountStatus Status { get; set; }

	[JsonProperty(PropertyName = "crypto_status", Required = Required.Default)]
	public AccountStatus CryptoStatus { get; set; }

	[JsonProperty(PropertyName = "currency", Required = Required.Default)]
	public String? Currency { get; set; }

	[JsonProperty(PropertyName = "cash", Required = Required.Always)]
	public Decimal TradableCash { get; set; }

	[JsonProperty(PropertyName = "pattern_day_trader", Required = Required.Always)]
	public Boolean IsDayPatternTrader { get; set; }

	[JsonProperty(PropertyName = "trading_blocked", Required = Required.Always)]
	public Boolean IsTradingBlocked { get; set; }

	[JsonProperty(PropertyName = "transfers_blocked", Required = Required.Always)]
	public Boolean IsTransfersBlocked { get; set; }

	[JsonProperty(PropertyName = "account_blocked", Required = Required.Always)]
	public Boolean IsAccountBlocked { get; set; }

	[JsonProperty(PropertyName = "trade_suspended_by_user", Required = Required.Default)]
	public Boolean TradeSuspendedByUser { get; set; }

	[JsonProperty(PropertyName = "shorting_enabled", Required = Required.Default)]
	public Boolean ShortingEnabled { get; set; }

	[JsonProperty(PropertyName = "multiplier", Required = Required.Default)]
	public Multiplier Multiplier { get; set; }

	[JsonProperty(PropertyName = "buying_power", Required = Required.Default)]
	public Decimal? BuyingPower { get; set; }

	[JsonProperty(PropertyName = "daytrading_buying_power", Required = Required.Default)]
	public Decimal? DayTradingBuyingPower { get; set; }

	[JsonProperty(PropertyName = "non_marginable_buying_power", Required = Required.Default)]
	public Decimal? NonMarginableBuyingPower { get; set; }

	[JsonProperty(PropertyName = "regt_buying_power", Required = Required.Default)]
	public Decimal? RegulationBuyingPower { get; set; }

	[JsonProperty(PropertyName = "long_market_value", Required = Required.Default)]
	public Decimal? LongMarketValue { get; set; }

	[JsonProperty(PropertyName = "short_market_value", Required = Required.Default)]
	public Decimal? ShortMarketValue { get; set; }

	[JsonProperty(PropertyName = "equity", Required = Required.Default)]
	public Decimal? Equity { get; set; }

	[JsonProperty(PropertyName = "last_equity", Required = Required.Default)]
	public Decimal LastEquity { get; set; }

	[JsonProperty(PropertyName = "initial_margin", Required = Required.Default)]
	public Decimal? InitialMargin { get; set; }

	[JsonProperty(PropertyName = "maintenance_margin", Required = Required.Default)]
	public Decimal MaintenanceMargin { get; set; }

	[JsonProperty(PropertyName = "last_maintenance_margin", Required = Required.Default)]
	public Decimal LastMaintenanceMargin { get; set; }

	[JsonProperty(PropertyName = "daytrade_count", Required = Required.Default)]
	public UInt64 DayTradeCount { get; set; }

	[JsonProperty(PropertyName = "sma", Required = Required.Default)]
	public Decimal Sma { get; set; }

	[JsonConverter(typeof(AssumeUtcIsoDateTimeConverter))]
	[JsonProperty(PropertyName = "created_at", Required = Required.Always)]
	public DateTime CreatedAtUtc { get; set; }

	[JsonProperty(PropertyName = "accrued_fees", Required = Required.Default)]
	public Decimal? AccruedFees { get; set; }

	[JsonProperty(PropertyName = "pending_transfer_in", Required = Required.Default)]
	public Decimal? PendingTransferIn { get; set; }

	[JsonProperty(PropertyName = "pending_transfer_out", Required = Required.Default)]
	public Decimal? PendingTransferOut { get; set; }

	[JsonProperty(PropertyName = "options_trading_level", Required = Required.Default)]
	public OptionsTradingLevel? OptionsTradingLevel { get; set; }

	[JsonProperty(PropertyName = "options_approved_level", Required = Required.Default)]
	public OptionsTradingLevel? OptionsApprovedLevel { get; set; }

	[JsonProperty(PropertyName = "options_buying_power", Required = Required.Default)]
	public Decimal? OptionsBuyingPower { get; set; }

	// New properties to match JSON payload
	[JsonProperty(PropertyName = "admin_configurations", Required = Required.Default)]
	public AdminConfigurations? AdminConfigurations { get; set; }

	[JsonProperty(PropertyName = "user_configurations", Required = Required.Default)]
	public object? UserConfigurations { get; set; }

	[JsonProperty(PropertyName = "effective_buying_power", Required = Required.Default)]
	public Decimal? EffectiveBuyingPower { get; set; }

	[JsonProperty(PropertyName = "bod_dtbp", Required = Required.Default)]
	public Decimal? BodDtbp { get; set; }

	[JsonProperty(PropertyName = "portfolio_value", Required = Required.Default)]
	public Decimal? PortfolioValue { get; set; }

	[JsonProperty(PropertyName = "position_market_value", Required = Required.Default)]
	public Decimal? PositionMarketValue { get; set; }

	[JsonProperty(PropertyName = "balance_asof", Required = Required.Default)]
	public String? BalanceAsOf { get; set; }

	[JsonProperty(PropertyName = "intraday_adjustments", Required = Required.Default)]
	public Decimal? IntradayAdjustments { get; set; }

	[JsonProperty(PropertyName = "pending_reg_taf_fees", Required = Required.Default)]
	public Decimal? PendingRegTafFees { get; set; }

	[OnDeserialized]
	[UsedImplicitly]
	internal void OnDeserializedMethod(
		StreamingContext _)
	{
		if (String.IsNullOrEmpty(Currency))
		{
			Currency = "USD";
		}
	}

	[ExcludeFromCodeCoverage]
	public override String ToString() =>
		JsonConvert.SerializeObject(this);

	[ExcludeFromCodeCoverage]
	[DebuggerBrowsable(DebuggerBrowsableState.Never)]
	private String DebuggerDisplay =>
		$"{nameof(IAccount)} {{ ID = {AccountId:B}, Number = \"{AccountNumber}\", Status = {Status}, Currency = \"{Currency}\" }}";
}

/// <summary>
/// Represents the administrative configurations for an account.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage(
	"Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
	Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
internal sealed class AdminConfigurations
{
	/// <summary>
	/// Gets or sets a value indicating whether instant ACH transfers are allowed.
	/// </summary>
	[JsonProperty(PropertyName = "allow_instant_ach")]
	public bool AllowInstantAch { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether crypto trading is disabled.
	/// </summary>
	[JsonProperty(PropertyName = "disable_crypto")]
	public bool DisableCrypto { get; set; }
}