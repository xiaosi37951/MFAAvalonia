using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.ValueType;
using MFAAvalonia.ViewModels.Pages;
using Avalonia.Controls;
using MFAAvalonia.Views.Pages;
using System.ComponentModel;

namespace MFAAvalonia.ViewModels.Other;

public partial class InstanceTabViewModel : ViewModelBase
{
    public readonly MaaProcessor Processor;

    public TaskQueueViewModel TaskQueueViewModel => MaaProcessorManager.Instance.GetViewModel(InstanceId);

    private Control? _view;

    public Control View
    {
        get
        {
            if (_view == null)
            {
                _view = new TaskQueueView
                {
                    DataContext = TaskQueueViewModel
                };
            }
            return _view;
        }
    }

    public InstanceTabViewModel(MaaProcessor processor)
    {
        Processor = processor;
        InstanceId = processor.InstanceId;
        UpdateName();

        IsRunning = processor.TaskQueue.Count > 0;
        processor.TaskQueue.CountChanged += OnTaskCountChanged;

        if (TaskQueueViewModel != null)
            TaskQueueViewModel.PropertyChanged += OnTaskQueueViewModelPropertyChanged;

        RefreshBadges();
    }

    private void OnTaskCountChanged(object? sender, ObservableQueue<MFATask>.CountChangedEventArgs e)
    {
        DispatcherHelper.RunOnMainThread(() =>
        {
            IsRunning = e.NewValue > 0;
            RefreshBadges();
        });
    }

    private void OnTaskQueueViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TaskQueueViewModel.CurrentController)
            or nameof(TaskQueueViewModel.CurrentResource)
            or nameof(TaskQueueViewModel.IsConnected))
        {
            DispatcherHelper.RunOnMainThread(RefreshBadges);
        }
    }

    public string InstanceId { get; }

    [ObservableProperty] private string _name = string.Empty;

    [ObservableProperty] private bool _isRunning;

    [ObservableProperty] private bool _isConnected;

    [ObservableProperty] private string _taskCountText = string.Empty;

    [ObservableProperty] private string _controllerBadgeText = string.Empty;

    [ObservableProperty] private string _resourceBadgeText = string.Empty;

    [ObservableProperty] private string _statusBadgeText = string.Empty;

    [ObservableProperty] private bool _isStatusConnectedVisible;

    [ObservableProperty] private bool _isStatusDisconnectedVisible;

    [ObservableProperty] private bool _isActive;

    private void RefreshBadges()
    {
        var vm = TaskQueueViewModel;

        IsConnected = vm?.IsConnected ?? false;
        TaskCountText = LangKeys.InstancePresetTaskCountFormat.ToLocalizationFormatted(false, Processor.TaskQueue.Count.ToString());

        ControllerBadgeText = vm?.CurrentController switch
        {
            MaaControllerTypes.Adb => LangKeys.Emulator.ToLocalization(),
            MaaControllerTypes.Win32 => LangKeys.Window.ToLocalization(),
            MaaControllerTypes.PlayCover => LangKeys.TabPlayCover.ToLocalization(),
            _ => vm?.CurrentController.ToString() ?? "-"
        };

        ResourceBadgeText = string.IsNullOrWhiteSpace(vm?.CurrentResource)
            ? "-"
            : vm.CurrentResource;

        StatusBadgeText = IsRunning
            ? LangKeys.Running.ToLocalization()
            : IsConnected
                ? LangKeys.Connected.ToLocalization()
                : LangKeys.Unconnected.ToLocalization();

        IsStatusConnectedVisible = !IsRunning && IsConnected;
        IsStatusDisconnectedVisible = !IsRunning && !IsConnected;
    }

    public void UpdateName()
    {
        Name = MaaProcessorManager.Instance.GetInstanceName(InstanceId);
    }

    /// <summary>
    /// 删除当前多开实例（用于设置界面的配置管理）
    /// </summary>
    [RelayCommand]
    private void DeleteConfiguration()
    {
        Instances.InstanceTabBarViewModel.CloseInstanceCommand.Execute(this);
    }
}
