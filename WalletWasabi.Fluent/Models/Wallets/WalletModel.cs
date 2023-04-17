using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using NBitcoin;
using ReactiveUI;
using WalletWasabi.Blockchain.TransactionProcessing;
using WalletWasabi.Blockchain.Transactions;
using WalletWasabi.Fluent.Extensions;
using WalletWasabi.Fluent.Helpers;
using WalletWasabi.Fluent.ViewModels.Wallets.Labels;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.Models.Wallets;

internal class WalletModel : ReactiveObject, IWalletModel
{
	private readonly Wallet _wallet;
	private readonly TransactionHistoryBuilder _historyBuilder;

	public WalletModel(Wallet wallet)
	{
		_wallet = wallet;
		_historyBuilder = new TransactionHistoryBuilder(_wallet);

		RelevantTransactionProcessed =
			Observable.FromEventPattern<ProcessedResult?>(_wallet, nameof(_wallet.WalletRelevantTransactionProcessed))
					  .ObserveOn(RxApp.MainThreadScheduler);

		Transactions =
			Observable.Defer(() => BuildSummary().ToObservable())
					  .Concat(RelevantTransactionProcessed.SelectMany(_ => BuildSummary()))
					  .ToObservableChangeSet(x => x.TransactionId);

		Addresses = Observable
			.Defer(() => GetAddresses().ToObservable())
			.Concat(RelevantTransactionProcessed.ToSignal().SelectMany(_ => GetAddresses()))
			.ToObservableChangeSet(x => x.Text);

		WalletType = WalletHelpers.GetType(_wallet.KeyManager);
	}

	public IObservable<IChangeSet<IAddress, string>> Addresses { get; }

	private IObservable<EventPattern<ProcessedResult?>> RelevantTransactionProcessed { get; }

	public string Name => _wallet.WalletName;

	public bool IsLoggedIn => throw new NotImplementedException();

	public IObservable<IChangeSet<TransactionSummary, uint256>> Transactions { get; }

	public WalletType WalletType { get; }

	public IAddress GetNextReceiveAddress(IEnumerable<string> destinationLabels)
	{
		var pubKey = _wallet.GetNextReceiveAddress(destinationLabels);
		return new Address(_wallet.KeyManager, pubKey);
	}

	public bool IsHardwareWallet => _wallet.KeyManager.IsHardwareWallet;

	public bool IsWatchOnlyWallet => _wallet.KeyManager.IsWatchOnly;

	public IEnumerable<(string Label, int Score)> GetMostUsedLabels(Intent intent) =>
		_wallet.GetLabelsWithRanking(intent);

	private IEnumerable<TransactionSummary> BuildSummary()
	{
		return _historyBuilder.BuildHistorySummary();
	}

	private IEnumerable<IAddress> GetAddresses()
	{
		return _wallet.KeyManager
					  .GetKeys()
					  .Reverse()
					  .Select(x => new Address(_wallet.KeyManager, x));
	}

	public async Task<(bool Success, bool CompatibilityPasswordUsed)> TryLoginAsync(string password)
	{
		var compatibilityPassword = "";
		var isPasswordCorrect = await Task.Run(() => _wallet.TryLogin(password, out var compatibilityPassword));

		var compatibilityPasswordUsed = compatibilityPassword is { };

		return (isPasswordCorrect, compatibilityPasswordUsed);
	}
}
