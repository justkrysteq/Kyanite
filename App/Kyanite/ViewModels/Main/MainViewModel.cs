using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Controls.ErrorInformation;
using Kyanite.Controls.ModulePicker;
using Kyanite.Core.Dialogs;
using Kyanite.Core.Models;
using Kyanite.Core.Services;
using Kyanite.Dialogs.AppSettings;
using Kyanite.Modules;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Kyanite.ViewModels.Main;

internal partial class MainViewModel : ViewModelBase
{
	readonly IServiceProvider _serviceProvider;
	readonly ModuleManager _moduleManager;
	readonly IDialogService _dialogService;
	readonly IAppSettingsService _appSettingsService;

	bool _deletingModule;

	[ObservableProperty] bool _isDataLoaded;
	[ObservableProperty] bool _isPaneOpen = true;

	public ObservableCollection<Module> AllModules { get; } = [];
	[ObservableProperty] Module? _selectedModule;

	public MainViewModel(IServiceProvider serviceProvider, ModuleManager moduleManager, IDialogService dialogService, IAppSettingsService appSettingsService)
	{
		_serviceProvider = serviceProvider;
		_moduleManager = moduleManager;
		_dialogService = dialogService;
		_appSettingsService = appSettingsService;

		_ = PrepareModuleManager();
	}

	[RelayCommand]
	void OpenSettings()
	{
		_dialogService.CreateBuilder()
			.WithTitle("Settings")
			.WithSize(800, 600)
			.WithViewModel(new AppSettingsViewModel(_appSettingsService))
			.AddButton("Save", OnSettingsDialogClosed)
			.BuildAndShow();
	}

	void OnSettingsDialogClosed(Dialog dialog)
	{
		if (dialog.ViewModel is not AppSettingsViewModel vm)
			return;

		var appSettings = _appSettingsService.CurrentAppSettings;

		appSettings.DueSoonThresholdMinutes = vm.DueSoonThresholdMinutes;
		appSettings.EnableNotifications = vm.EnableNotifications;
		appSettings.NotifyOnOverdue = vm.NotifyOnOverdue;
		appSettings.KeepRunningInBackground = vm.KeepRunningInBackground;
		appSettings.ModulesDirectory = string.IsNullOrWhiteSpace(vm.ModulesDirectory) ? null : vm.ModulesDirectory.Trim();

		_appSettingsService.Save();
	}

	[RelayCommand]
	void CreateModule()
	{
		// module picker throws when the manager is still loading; a click during startup must not kill the app
		if (!_moduleManager.IsPrepared)
			return;

		_dialogService.CreateBuilder()
			.WithTitle("Create dialog")
			.WithSize(600, 400)
			.WithViewModel(new ModulePickerViewModel(_moduleManager))
			.AddButton("Close",
			(dialog) =>
			{
				var modulePickerVM = (ModulePickerViewModel)dialog.ViewModel;
				_ = FinalizeCreateModule(modulePickerVM.ModuleName, modulePickerVM.SelectedModule);
			})
			.BuildAndShow();
	}

	[RelayCommand]
	async Task DeleteModule(object moduleObj)
	{
		if (moduleObj is not Module module)
			throw new Exception("Provided object is not of a module type");

		try
		{
			if (SelectedModule == module)
			{
				_deletingModule = true;
				SelectedModule = null;
				_deletingModule = false;
			}

			await _moduleManager.DeleteModule(module);
			AllModules.Replace(_moduleManager.Modules);
		}
		catch (Exception ex)
		{
			_deletingModule = false;
			ShowError(ex.Message);
		}
	}

	[RelayCommand] void TogglePane() => IsPaneOpen = !IsPaneOpen;

	partial void OnSelectedModuleChanging(Module? oldValue, Module? newValue)
	{
		_ = SaveOldAndLoadNew(oldValue, newValue);
	}

	async Task FinalizeCreateModule(string moduleName, string selectedModule)
	{
		if (string.IsNullOrEmpty(selectedModule))
			return;

		try
		{
			var createdModule = await _moduleManager.CreateModule(moduleName, selectedModule);
			AllModules.Replace(_moduleManager.Modules);
			SelectedModule = createdModule;
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	async Task SaveOldAndLoadNew(Module? oldValue, Module? newValue)
	{
		try
		{
			if (oldValue is not null && !_deletingModule)
				await _moduleManager.SaveModule(oldValue);

			if (newValue is not null)
				await _moduleManager.LoadModule(newValue);
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	async Task PrepareModuleManager()
	{
		try
		{
			if (!_moduleManager.IsPrepared)
			{
				var modulesDirectory = Path.GetFullPath(
					_appSettingsService.CurrentAppSettings.ModulesDirectory ?? AppSettings.DefaultModulesDirectory);
				await _moduleManager.Prepare(_serviceProvider, modulesDirectory);
			}

			AllModules.Replace(_moduleManager.Modules);

			if (_moduleManager.LoadWarnings.Count > 0)
				ShowError(string.Join(Environment.NewLine, _moduleManager.LoadWarnings));
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
		finally
		{
			IsDataLoaded = true;
		}
	}

	void ShowError(string message)
	{
		_dialogService.CreateBuilder()
			.WithViewModel(new ErrorInformationViewModel(message))
			.AddCloseButton()
			.BuildAndShow();
	}
}
