using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using ReactiveUI;

namespace AwsCredentialManager.ViewModels
{
    public class BaseViewModel : ReactiveObject
	{
		public const string DATE_FORMAT = "yyy-M-d HH:mm:ss";

		private bool _isLoading;
		public bool IsLoading
		{
			get => _isLoading;
			set => this.RaiseAndSetIfChanged(ref _isLoading, value);
		}

		private string _logs;
		public string Logs
		{
			get => _logs;
			set => this.RaiseAndSetIfChanged(ref _logs, value);
		}

		private bool _isLogEmpty;
		public bool IsLogEmpty
		{
			get => _isLogEmpty;
			set => this.RaiseAndSetIfChanged(ref _isLogEmpty, value);
		}

		public bool GetIsLogEmpty() =>
			string.IsNullOrWhiteSpace(Logs);

		public BaseViewModel()
		{
			this.WhenAnyValue(x => x.Logs).Subscribe(x => {
				this.IsLogEmpty = GetIsLogEmpty();
			});

		}

		
		public async Task ExecuteAsyncWithLoadingAndExceptionLogging(Action action
			,CancellationToken cancellationToken = default(CancellationToken)
			,TaskCreationOptions taskCreationOptions = default(TaskCreationOptions)
			,TaskScheduler scheduler = null)
		{
			await WithExceptionLogging(() =>
				 ExecuteAsyncWithLoading(action, cancellationToken, taskCreationOptions, scheduler)
			);
		}

		public async Task WithExceptionLogging(Func<Task> action)
		{
			try
			{
				await action();
			}
			catch (Exception ex)
			{
				Logs = $"ERROR: {DateTime.Now.ToString("yyy-M-d HH:mm:ss")} {ex}";
			}
		}

		public void WithExceptionLogging(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Logs = $"ERROR: {DateTime.Now.ToString("yyy-M-d HH:mm:ss")} {ex}";
			}
		}

		
		public Task ExecuteAsyncWithLoading(Action action
			,CancellationToken cancellationToken = default(CancellationToken)
			,TaskCreationOptions taskCreationOptions = default(TaskCreationOptions)
			,TaskScheduler scheduler = null)
		{
			IsLoading = true;
			return ExecuteAsync(action, cancellationToken, taskCreationOptions, scheduler);
		}

		protected TaskScheduler UIContext = TaskScheduler.FromCurrentSynchronizationContext();

		protected virtual Task ExecuteAsync(Action action
			,CancellationToken cancellationToken = default(CancellationToken)
			,TaskCreationOptions taskCreationOptions = default(TaskCreationOptions)
			,TaskScheduler scheduler = null)
		{
			Task task = null;
			if (scheduler == null) {
				task = Task.Factory.StartNew((arg) => { action(); }, cancellationToken, taskCreationOptions);
			} else {
				task = Task.Factory.StartNew(action, cancellationToken, taskCreationOptions, scheduler);
			}
			task.ConfigureAwait(true);
			task.ContinueWith((t) => {
				IsLoading = false;
			}, UIContext); // TaskScheduler.FromCurrentSynchronizationContext());
			return task;
		}

		public void ClearLogsCommand()
		{
			Logs = "";
		}

	}
}
