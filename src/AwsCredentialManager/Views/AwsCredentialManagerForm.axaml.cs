using Avalonia;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AwsCredentialManager.ViewModels;

namespace AwsCredentialManager.Views
{
	public partial class AwsCredentialManagerForm : UserControl
	{
		public AwsCredentialManagerForm()
		{
			InitializeComponent();

			var awsTokenTextBox = this.FindControl<TextBox>("AwsTokenTextBox");
			if (awsTokenTextBox != null)
			{
				awsTokenTextBox.AttachedToVisualTree += (s, e) => awsTokenTextBox.Focus();
			}
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);

			var autoCompleteAwsProfileToEdit = this.FindControl<AutoCompleteBox>("AutoCompleteAwsProfileToEdit");
			var autoCompleteAwsProfileSource = this.FindControl<AutoCompleteBox>("AutoCompleteAwsProfileSource");
			// TODO: show the options dropdown when the AutoCompleteBox gets the focus before typing anything.
			/*
			autoCompleteAwsProfileToEdit.GotFocus += (sender, e) => {
				if (!autoCompleteAwsProfileToEdit.IsDropDownOpen)
					autoCompleteAwsProfileToEdit.IsDropDownOpen = true;
			};
			autoCompleteAwsProfileSource.GotFocus += (sender, e) => {
				if (!autoCompleteAwsProfileSource.IsDropDownOpen)
					autoCompleteAwsProfileSource.IsDropDownOpen = true;
			};
			*/
		}

		// TODO: try to bind "OnCloseAboutPopup" directly from XAML instead from this code-behind:
		public void AboutViewExitHandler(object sender, RoutedEventArgs e)
		{
			var viewModel = (AwsCredentialManagerViewModel)this.DataContext;
			viewModel.IsAboutVisible = false;
			e.Handled = true;
		}

	}
}
