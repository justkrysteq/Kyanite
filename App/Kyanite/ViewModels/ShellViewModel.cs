using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Kyanite.Core.Dialogs;
using Kyanite.ViewModels.Main;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kyanite.ViewModels;

/// <summary>
/// controls what content is now displayed
/// </summary>
internal partial class ShellViewModel : ViewModelBase
{
	[ObservableProperty] ViewModelBase? _currentViewModel;

	public IDialogService DialogService { get; }
	readonly IServiceProvider _serviceProvider;

	public ShellViewModel(IServiceProvider serviceProvider, IDialogService dialogService)
	{
		_serviceProvider = serviceProvider;
		DialogService = dialogService;

		UpdateView();
	}

	[RelayCommand]
	void CloseDialog()
	{
		DialogService.Close();
	}

	public void UpdateView()
	{
		CurrentViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
	}
}
