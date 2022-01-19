using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using WalletWasabi.Fluent.ViewModels.Navigation;

namespace WalletWasabi.Fluent.ViewModels.Wallets;

public partial class WalletSettingsViewModel : RoutableViewModel
{
	[AutoNotify] private bool _preferPsbtWorkflow;
	[AutoNotify] private bool _autoCoinJoin;

	public WalletSettingsViewModel(WalletViewModelBase walletViewModelBase)
	{
		var wallet = walletViewModelBase.Wallet;
		Title = $"{wallet.WalletName} - Wallet Settings";
		_preferPsbtWorkflow = wallet.KeyManager.PreferPsbtWorkflow;
		_autoCoinJoin = wallet.KeyManager.AutoCoinJoin;
		IsHardwareWallet = wallet.KeyManager.IsHardwareWallet;
		IsWatchOnly = wallet.KeyManager.IsWatchOnly;

		SetupCancel(enableCancel: false, enableCancelOnEscape: true, enableCancelOnPressed: true);

		NextCommand = CancelCommand;

		VerifyRecoveryPhraseCommand = ReactiveCommand.Create(() => Navigate().To(new VerifyRecoveryPhraseViewModel(wallet)));

		this.WhenAnyValue(x => x.PreferPsbtWorkflow)
			.Skip(1)
			.Subscribe(value =>
			{
				wallet.KeyManager.PreferPsbtWorkflow = value;
				wallet.KeyManager.ToFile();
				walletViewModelBase.RaisePropertyChanged(nameof(walletViewModelBase.PreferPsbtWorkflow));
			});

		this.WhenAnyValue(x => x.AutoCoinJoin)
			.ObserveOn(RxApp.TaskpoolScheduler)
			.Skip(1)
			.Subscribe(x =>
			{
				wallet.KeyManager.AutoCoinJoin = x;
				wallet.KeyManager.ToFile();
			});
	}

	public bool IsHardwareWallet { get; }

	public bool IsWatchOnly { get; }

	public override sealed string Title { get; protected set; }

	public ICommand VerifyRecoveryPhraseCommand { get; }
}
