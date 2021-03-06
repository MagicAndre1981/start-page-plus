﻿using System;

namespace StartPagePlus.UI.Interfaces
{
    using Observables;

    public interface IRecentItemCommandService
    {
        ObservableCommandList GetCommands(Action refresh, Action showSettings);

        ObservableContextCommandList GetContextCommands(
            bool canPin, Action pin,
            bool canUnpin, Action unpin,
            bool canRemove, Action remove,
            bool canCopyPath, Action copyPath,
            bool canOpenInVS, Action openInVS);
    }
}