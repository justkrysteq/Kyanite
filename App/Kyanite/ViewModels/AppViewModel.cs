using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Controls.ErrorInformation;
using Kyanite.Core.Dialogs;
using Kyanite.Core.Services;
using Kyanite.Modules;
using Kyanite.Services;
using Kyanite.ViewModels.Main;
using Kyanite.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kyanite.ViewModels;

/// <summary>
/// viewmodel for entire application
/// </summary>
internal partial class AppViewModel(IServiceProvider serviceProvider) : ViewModelBase
{
	[RelayCommand]
	public void OpenApplication()
	{
		var desktop = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;

		if (desktop.MainWindow is not null)
			return;

		var shellProvider = serviceProvider.GetRequiredService<ShellViewModelProvider>();

		desktop.MainWindow = new MainWindow()
		{
			DataContext = shellProvider.CreateOrGetShell()
		};

		desktop.MainWindow.Closed += async (_, _) =>
		{
			var moduleManager = serviceProvider.GetRequiredService<ModuleManager>();
			var shellViewModelProvider = serviceProvider.GetRequiredService<ShellViewModelProvider>();
			var appSettingsService = serviceProvider.GetRequiredService<IAppSettingsService>();

			//async void: any exception thrown here (failed save) would abort the process and lose the session
			try
			{
				if (shellViewModelProvider.TryRetriveShellCurrentViewModel(out MainViewModel mainViewModel) &&
					mainViewModel.SelectedModule is not null)
					await moduleManager.SaveModule(mainViewModel.SelectedModule);

				moduleManager.ClearData();
				shellProvider.DisposeShell();
				desktop.MainWindow = null;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Closing failed, window reopened: {ex}");

				//window is already gone; reopen it so the user sees the error instead of a vanished app
				desktop.MainWindow = null;
				OpenApplication();

				serviceProvider.GetRequiredService<IDialogService>()
					.CreateBuilder()
					.WithViewModel(new ErrorInformationViewModel(ex.Message))
					.AddCloseButton()
					.BuildAndShow();

				return;
			}

			if (appSettingsService.CurrentAppSettings.KeepRunningInBackground)
				desktop.MainWindow = null;
			else
				desktop.Shutdown();
		};

		desktop.MainWindow.Show();
	}

	[RelayCommand]
	void CloseApplication()
	{
		if (Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			desktop.Shutdown();
	}
}
