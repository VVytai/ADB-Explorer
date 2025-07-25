﻿using ADB_Explorer.Converters;
using ADB_Explorer.Services;

namespace ADB_Explorer.ViewModels;

internal class InProgSyncProgressViewModel : FileOpProgressViewModel
{
    private readonly AdbSyncProgressInfo adbInfo = null;

    public InProgSyncProgressViewModel() : base(FileOperation.OperationStatus.InProgress)
    {
        
    }

    public InProgSyncProgressViewModel(AdbSyncProgressInfo adbInfo) : this()
    {
        this.adbInfo = adbInfo;
    }

    public int? TotalPercentage => adbInfo?.TotalPercentage;

    public ulong? TotalBytesTransferred => adbInfo?.TotalBytesTransferred;

    public string TotalBytes => TotalBytesTransferred?.BytesToSize();

    public int? CurrentFilePercentage => adbInfo?.CurrentFilePercentage;

    public UInt64? CurrentFileBytesTransferred => adbInfo?.CurrentFileBytesTransferred;

    public string CurrentFilePath => adbInfo?.AndroidPath;

    public string CurrentFileName => Path.GetFileName(CurrentFilePath);

    public string CurrentFileNameWithoutExtension => Path.GetFileNameWithoutExtension(CurrentFilePath);
}
