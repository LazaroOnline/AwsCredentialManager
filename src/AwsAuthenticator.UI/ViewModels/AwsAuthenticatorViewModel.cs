using System.Reactive;
using ReactiveUI;
using Splat;
using System.DirectoryServices.AccountManagement;


namespace AwsAuthenticator.ViewModels;

public class AwsAuthenticatorViewModel : BaseViewModel
{
	private readonly IAwsAuthenticator _awsAuthenticator;
	private readonly AppSettingsWriter _appSettingsWriter;

	#region Properties
	public string AwsAccountId { get; set; }

	public string AwsUserName { get; set; }

	/// <summary>
	/// Token generator secret key of 64 alpha-numeric characters code from aws console in the 'Manage MFA device' section.
	/// </summary>
	private string _awsMfaGeneratorSecretKey = "";
	public string AwsMfaGeneratorSecretKey
	{
		get => _awsMfaGeneratorSecretKey;
		set => this.RaiseAndSetIfChanged(ref _awsMfaGeneratorSecretKey, value);
	}

	private string _awsProfileSource = AwsCredentialsFile.DEFAULT_PROFILE;
	public string AwsProfileSource
	{
		get => _awsProfileSource;
		set => this.RaiseAndSetIfChanged(ref _awsProfileSource, value);
	}

	private string _awsProfileToEdit = "";
	public string AwsProfileToEdit
	{
		get => _awsProfileToEdit;
		set => this.RaiseAndSetIfChanged(ref _awsProfileToEdit, value);
	}

	public string AwsProfileToEdit_Default { get; set; } = AwsCredentialsFile.DEFAULT_PROFILE_MFA;

	private string _awsTokenCode = "";
	public string AwsTokenCode
	{
		get => _awsTokenCode;
		set => this.RaiseAndSetIfChanged(ref _awsTokenCode, value);
	}

	private List<string> _awsLocalProfileList = new List<string>();
	public List<string> AwsLocalProfileList
	{
		get => _awsLocalProfileList;
		set => this.RaiseAndSetIfChanged(ref _awsLocalProfileList, value);
	}

	public string AwsCurrentProfileName { get; set; } = "";

	private bool _isAboutVisible;
	public bool IsAboutVisible
	{
		get => _isAboutVisible;
		set => this.RaiseAndSetIfChanged(ref _isAboutVisible, value);
	}

	private bool _isValidAwsMfaGeneratorSecretKey;
	public bool IsValidAwsMfaGeneratorSecretKey
	{
		get => _isValidAwsMfaGeneratorSecretKey;
		set => this.RaiseAndSetIfChanged(ref _isValidAwsMfaGeneratorSecretKey, value);
	}

	private bool _isEnabledGenerateTokenButton;
	public bool IsEnabledGenerateTokenButton
	{
		get => _isEnabledGenerateTokenButton;
		set => this.RaiseAndSetIfChanged(ref _isEnabledGenerateTokenButton, value);
	}

	private bool _hasAwsTokenCode;
	public bool HasAwsTokenCode
	{
		get => _hasAwsTokenCode;
		set => this.RaiseAndSetIfChanged(ref _hasAwsTokenCode, value);
	}

	// https://avaloniaui.net/docs/controls/button
	public ReactiveCommand<Unit, Unit> OnOpenAboutWindow { get; }
	public ReactiveCommand<Unit, Unit> OnCloseAboutPopup { get; }

	#endregion


	// Constructor required by the designer tools.
	public AwsAuthenticatorViewModel()
		: this(null, null, null) // Call the other constructor
	{
		System.Diagnostics.Debug.WriteLine($"Default constructor used for {nameof(AwsAuthenticatorViewModel)}.");
	}

	public AwsAuthenticatorViewModel(
		 IAwsAuthenticator? awsAuthenticator
		,AppSettingsWriter? appSettingsWriter = null
		,AwsSettings? awsSettings = null
	)
	{
		_awsAuthenticator = awsAuthenticator ?? Splat.Locator.Current.GetService<IAwsAuthenticator>();
		_appSettingsWriter = appSettingsWriter ?? Splat.Locator.Current.GetService<AppSettingsWriter>();
		awsSettings = awsSettings ?? Splat.Locator.Current.GetService<AwsSettings>();

		//_awsAuthenticator = awsAuthenticator ?? new Core.AwsAuthenticator();
		//_appSettingsWriter = appSettingsWriter ?? new AppSettingsWriter();

		AwsAccountId = awsSettings?.AccountId ?? "";
		AwsUserName = awsSettings?.UserName ?? "";
		AwsProfileSource = awsSettings?.ProfileSource ?? "";
		AwsProfileToEdit = awsSettings?.Profile ?? "";
		AwsMfaGeneratorSecretKey = awsSettings?.MfaGeneratorSecretKey ?? "";
		AwsTokenCode = awsSettings?.TokenCode ?? "";

		if (string.IsNullOrEmpty(AwsUserName)) {
			AwsUserName = UserInfo.GetUserFullName();
		}

		AwsLocalProfileList = _awsAuthenticator?.GetAwsLocalProfileList();

		this.WhenAnyValue(x => x.AwsMfaGeneratorSecretKey).Subscribe(x => {
			this.IsValidAwsMfaGeneratorSecretKey = GetIsValidAwsMfaGeneratorSecretKey();
		});

		this.WhenAnyValue(x => x.AwsMfaGeneratorSecretKey, x => x.IsLoading).Subscribe(x => {
			this.IsEnabledGenerateTokenButton = GetIsEnabledGenerateTokenButton();
		});

		this.WhenAnyValue(x => x.AwsTokenCode).Subscribe(x => {
			this.HasAwsTokenCode = !GetIsAwsTokenCodeEmpty();
		});

		OnOpenAboutWindow = ReactiveCommand.Create(() => {
			this.IsAboutVisible = true;
		});
		OnCloseAboutPopup = ReactiveCommand.Create(() => {
			this.IsAboutVisible = false;
		});
	}

	public bool GetIsValidAwsMfaGeneratorSecretKey() =>
		!string.IsNullOrWhiteSpace(AwsMfaGeneratorSecretKey)
		// Length validation removed to allow using the MFA generator for other providers.
		//&& AwsMfaGeneratorSecretKey.Length == Core.Services.AwsAuthenticator.MFA_DEVICE_GENERATOR_SECRET_KEY_LENGTH
		;

	public bool GetIsEnabledGenerateTokenButton() =>
		IsValidAwsMfaGeneratorSecretKey && !IsLoading;

	public bool GetIsAwsTokenCodeEmpty() =>
		string.IsNullOrWhiteSpace(AwsTokenCode);


	public async Task AutoUpdateCredentialsCommand()
	{
		await GenerateTokenCommand();
		await UpdateCredentialsCommand();
	}

	public void AwsProfileSourceClearCommand()
	{
		AwsProfileSource = "";
	}

	public async Task AwsProfileToEditClearCommand()
	{
		AwsProfileToEdit = "";
	}

	public async Task AwsTokenCodeClearCommand()
	{
		AwsTokenCode = "";
	}

	public async Task GenerateTokenCommand()
	{
		// This command is fast, it could run synchronously if required.
		// WithExceptionLogging(async () => {
		await ExecuteAsyncWithLoadingAndExceptionLogging(() => {
			AwsTokenCode = GenerateTokenFromAuthenticatorKey();
			Logs = $"CODE: {DateTime.Now.ToString(DATE_FORMAT)}:  {AwsTokenCode}";
		});
	}

	public async Task CopyTokenCodeCommand()
	{
		// This command is fast, it could run synchronously if required.
		// WithExceptionLogging(async () => {
		await ExecuteAsyncWithLoadingAndExceptionLogging(async () => {
			await Clipboard.SetTextAsync(AwsTokenCode);
		});
	}

	public async Task GenerateTokenAndCopyToClipboardCommand()
	{
		AwsTokenCode = GenerateTokenFromAuthenticatorKey();
		var isValidToken = !string.IsNullOrWhiteSpace(AwsTokenCode);
		if (isValidToken) {
			// https://docs.avaloniaui.net/docs/input/clipboard
			await Clipboard.SetTextAsync(AwsTokenCode);
		}
		else
		{
			// Show the errors from the logs:
			await Clipboard.SetTextAsync(Logs);
		}
	}

	public string GenerateTokenFromAuthenticatorKey()
	{
		return GenerateTokenFromAuthenticatorKey(AwsMfaGeneratorSecretKey);
	}

	public string GenerateTokenFromAuthenticatorKey(string mfaGeneratorSecretKey)
	{
		var authenticator = new TwoStepsAuthenticator.TimeAuthenticator();
		var tokenCode = authenticator.GetCode(mfaGeneratorSecretKey);
		return tokenCode;
	}

	public string GetAwsProfileToEdit()
	{
		return string.IsNullOrWhiteSpace(AwsProfileToEdit) ? AwsProfileToEdit_Default : AwsProfileToEdit;
	}

	public async Task UpdateCredentialsCommand()
	{
		await ExecuteAsyncWithLoadingAndExceptionLogging(() => {
			var hasEmptyToken = string.IsNullOrWhiteSpace(AwsTokenCode);
			if (hasEmptyToken && IsValidAwsMfaGeneratorSecretKey)
			{
				AwsTokenCode = GenerateTokenFromAuthenticatorKey();
			}
			Logs = "Updating...";
			// System.Threading.Thread.Sleep(6000); // Use this line for testing the loading spinner.
			var profileToEdit = GetAwsProfileToEdit();
			_awsAuthenticator.UpdateAwsAccount(AwsAccountId, AwsUserName, AwsTokenCode, AwsProfileSource, profileToEdit);
			Logs = $"Updated credentials successfully at {DateTime.Now.ToString(DATE_FORMAT)}.";
		});
	}

	public void OpenAwsCredentialsFileCommand()
	{
		_awsAuthenticator.OpenAwsCredentialsFile();
	}

	private const string AwsUserSecurityCredentialsWebPage = "https://console.aws.amazon.com/iam/home#/security_credentials?section=IAM_credentials";
	public void OpenAwsUserSecurityCredentialsWebPageCommand()
	{
		AwsUserSecurityCredentialsWebPage.TryOpenUrlInBrowser();
	}

	public void ReadAwsCurrentProfileName()
	{
		AwsCurrentProfileName = _awsAuthenticator.AwsGetCurrentUserProfile();
	}

	public void SaveConfig()
	{
		var newAppSettings = new AppSettings()
		{
			Aws = new AwsSettings
			{
				AccountId = AwsAccountId,
				UserName = AwsUserName,
				ProfileSource = AwsProfileSource,
				Profile = AwsProfileToEdit,
				MfaGeneratorSecretKey = AwsMfaGeneratorSecretKey,
			},
		};
		_appSettingsWriter.Save(newAppSettings);
	}

}