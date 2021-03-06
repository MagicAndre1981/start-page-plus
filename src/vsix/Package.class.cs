﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

using Luminous.Code.Extensions.ExceptionExtensions;
using Luminous.Code.VisualStudio.Commands;
using Luminous.Code.VisualStudio.Packages;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Tasks = System.Threading.Tasks;

namespace StartPagePlus
{
    using Commands;

    using Options.Models;
    using Options.Pages;

    using UI.ToolWindows;
    using UI.ViewModels;

    using static PackageGuids;
    using static Vsix;

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]

    [InstalledProductRegistration(Name, Description, Version)]
    [Guid(PackageString)]
    [DisplayName(Name)]

    [ProvideOptionPage(typeof(DialogPageProvider.General), Name, nameof(DialogPageProvider.General), 0, 0, supportsAutomation: true)]
    [ProvideOptionPage(typeof(DialogPageProvider.StartTab), Name, StartTabOptions.Category, 0, 0, supportsAutomation: true)]
    [ProvideOptionPage(typeof(DialogPageProvider.RecentItems), Name, RecentItemsOptions.Category, 0, 0, supportsAutomation: true)]
    [ProvideOptionPage(typeof(DialogPageProvider.NewsItems), Name, NewsItemsOptions.Category, 0, 0, supportsAutomation: true)]

    [ProvideToolWindow(typeof(StartPagePlusToolWindow), Style = VsDockStyle.Tabbed, Window = "DocumentWell", MultiInstances = false)]

    public sealed class PackageClass : AsyncPackageBase
    {
        public PackageClass() : base(PackageCommandSet, Name, Description)
            => _ = new ViewModelLocator();

        protected override async Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await ViewStartPagePlus.InstantiateAsync(this);
            await StartPagePlusOptions.InstantiateAsync(this);

            //await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        protected override async Tasks.Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            await base.InitializeToolWindowAsync(toolWindowType, id, cancellationToken);

            // any potentially expensive work, preferably done on a background thread where possible

            // ApplicationRegistryRoot;
            return this; // this is passed to the tool window constructor
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
            => (toolWindowType == typeof(StartPagePlusToolWindow).GUID)
                ? this
                : null; //base.GetAsyncToolWindowFactory(toolWindowType);

        // TODO: is GetToolWindowTitle REALLY necessary?
        protected override string GetToolWindowTitle(Type toolWindowType, int id)
            => (toolWindowType == typeof(StartPagePlusToolWindow))
                ? $"{Vsix.Name}"
                : base.GetToolWindowTitle(toolWindowType, id);

        public static CommandResult ShowToolWindow<T>(CancellationToken cancellationToken, string problem = null)
            where T : ToolWindowPane
        {
            try
            {
                return ShowToolWindow(typeof(T), problem);
            }
            catch (Exception ex)
            {
                return new ProblemResult(problem ?? ex.ExtendedMessage());
            }
        }

        public static CommandResult ShowToolWindow(Type type, string problem = null, int id = 0)
        {
            CommandResult commandResult = null;

            try
            {
                Instance.JoinableTaskFactory.RunAsync(async delegate
                {
                    try
                    {
                        using (var window = await Instance.ShowToolWindowAsync(type, id, create: true, Instance.DisposalToken))
                        {
                            if ((null == window) || (null == window.Frame))
                            {
                                throw new NotSupportedException("Cannot create tool window");
                            }

                            await Instance.JoinableTaskFactory.SwitchToMainThreadAsync();

                            var windowFrame = (IVsWindowFrame)window.Frame;
                            var result = windowFrame.Show();

                            ErrorHandler.ThrowOnFailure(result);
                        }

                        commandResult = new SuccessResult();
                    }
                    catch (Exception ex)
                    {
                        commandResult = new ProblemResult(problem ?? ex.ExtendedMessage());
                    }
                });

                return commandResult;
            }
            catch (Exception ex)
            {
                return new ProblemResult(problem ?? ex.ExtendedMessage());
            }
        }
    }
}
