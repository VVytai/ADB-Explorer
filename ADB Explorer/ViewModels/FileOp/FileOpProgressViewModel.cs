﻿using ADB_Explorer.Models;

namespace ADB_Explorer.ViewModels;

public abstract class FileOpProgressViewModel : ViewModelBase
{
    public Services.FileOperation.OperationStatus Status { get; }

    private FileOpFilter.FilterType FilterType => Status switch
    {
        Services.FileOperation.OperationStatus.Waiting => FileOpFilter.FilterType.Pending,
        Services.FileOperation.OperationStatus.InProgress => FileOpFilter.FilterType.Running,
        Services.FileOperation.OperationStatus.Completed => FileOpFilter.FilterType.Completed,
        Services.FileOperation.OperationStatus.Canceled => FileOpFilter.FilterType.Canceled,
        Services.FileOperation.OperationStatus.Failed => FileOpFilter.FilterType.Failed,
        _ => throw new NotSupportedException(),
    };

    public string Name => FileOpFilter.GetFilterName(FilterType);

    private bool isValidationInProgress = false;
    public bool IsValidationInProgress
    {
        get => isValidationInProgress;
        set => Set(ref isValidationInProgress, value);
    }

    public FileOpProgressViewModel(Services.FileOperation.OperationStatus status)
    {
        Status = status;
    }
}
