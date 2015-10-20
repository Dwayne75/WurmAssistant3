﻿using System;
using System.Linq;
using AldursLab.Essentials.Eventing;
using AldursLab.PersistentObjects;
using AldursLab.WurmApi;
using AldursLab.WurmAssistant3.Core.Areas.Calendar;
using AldursLab.WurmAssistant3.Core.Areas.Config;
using AldursLab.WurmAssistant3.Core.Areas.Config.Contracts;
using AldursLab.WurmAssistant3.Core.Areas.Config.Modules;
using AldursLab.WurmAssistant3.Core.Areas.Features;
using AldursLab.WurmAssistant3.Core.Areas.Features.Contracts;
using AldursLab.WurmAssistant3.Core.Areas.Features.Views;
using AldursLab.WurmAssistant3.Core.Areas.Logging;
using AldursLab.WurmAssistant3.Core.Areas.Logging.Contracts;
using AldursLab.WurmAssistant3.Core.Areas.Logging.Views;
using AldursLab.WurmAssistant3.Core.Areas.LogSearcher;
using AldursLab.WurmAssistant3.Core.Areas.MainMenu;
using AldursLab.WurmAssistant3.Core.Areas.MainMenu.Views;
using AldursLab.WurmAssistant3.Core.Areas.Persistence;
using AldursLab.WurmAssistant3.Core.Areas.SoundEngine;
using AldursLab.WurmAssistant3.Core.Areas.Timers;
using AldursLab.WurmAssistant3.Core.Areas.TrayPopups;
using AldursLab.WurmAssistant3.Core.Areas.WurmApi;
using AldursLab.WurmAssistant3.Core.IoC;
using AldursLab.WurmAssistant3.Core.Root.Components;
using AldursLab.WurmAssistant3.Core.Root.Contracts;
using JetBrains.Annotations;
using Ninject;
using Ninject.Activation.Strategies;

namespace AldursLab.WurmAssistant3.Core.Root
{
    public class CoreBootstrapper : IDisposable
    {
        readonly IKernel kernel = new StandardKernel();

        readonly MainForm mainForm;
        readonly WurmAssistantDataDirectory dataDirectory;

        public CoreBootstrapper([NotNull] MainForm mainForm)
        {
            if (mainForm == null) throw new ArgumentNullException("mainForm");
            this.mainForm = mainForm;

            SetupActivationStrategyComponents();
            
            dataDirectory = new WurmAssistantDataDirectory();
            dataDirectory.Lock();

            kernel.Bind<ISuperFactory>().To<SuperFactory>().InSingletonScope();
            kernel.Bind<WurmAssistantConfig, IWurmAssistantConfig>().To<WurmAssistantConfig>().InSingletonScope();

            kernel.Bind<IWurmAssistantDataDirectory>().ToConstant(dataDirectory);
            kernel.ProhibitGet<WurmAssistantDataDirectory>();

            kernel.Bind<WinFormsThreadMarshaller, IThreadMarshaller, IWurmApiEventMarshaller>()
                  .ToConstant(new WinFormsThreadMarshaller(mainForm));

            kernel.Bind<MainForm, IHostEnvironment, IUpdateLoop, ISystemTrayContextMenu>().ToConstant(mainForm);

            kernel.Bind<ProcessStarter, IProcessStarter>().To<ProcessStarter>().InSingletonScope();
            kernel.Bind<UserNotifier, IUserNotifier>().To<UserNotifier>().InSingletonScope();
            kernel.Bind<BinDirectory, IBinDirectory>().To<BinDirectory>().InSingletonScope();
            kernel.Bind<WaVersion, IWaVersion>().To<WaVersion>().InSingletonScope();
            kernel.Bind<WaExecutionInfoProvider, IWaExecutionInfoProvider>().To<WaExecutionInfoProvider>().InSingletonScope();
            kernel.Bind<ChangelogManager, IChangelogManager>().To<ChangelogManager>().InSingletonScope();

            LoggingSetup.Setup(kernel);
            mainForm.SetLogView(kernel.Get<LogView>());

            PersistenceSetup.BindPersistenceSystems(kernel);
            var manager = kernel.Get<PersistenceManager>();
            manager.LoadAndStartTracking(mainForm);
            mainForm.AfterPersistentStateLoaded();
        }

        public IWurmAssistantConfig WurmAssistantConfig { get { return kernel.Get<IWurmAssistantConfig>(); } }

        void SetupActivationStrategyComponents()
        {
            var allComponents = kernel.Components.GetAll<IActivationStrategy>().ToList();
            if (allComponents.Count != 7)
            {
                throw new Exception("Unexpected count of Ninject IActivationStrategy components");
            }
            kernel.Components.RemoveAll<IActivationStrategy>();
            kernel.Components.Add<IActivationStrategy, ActivationCacheStrategy>();
            kernel.Components.Add<IActivationStrategy, PropertyInjectionStrategy>();
            kernel.Components.Add<IActivationStrategy, MethodInjectionStrategy>();
            kernel.Components.Add<IActivationStrategy, PreInitializeActionsStrategy>();
            kernel.Components.Add<IActivationStrategy, InitializableStrategy>();
            kernel.Components.Add<IActivationStrategy, StartableStrategy>();
            kernel.Components.Add<IActivationStrategy, DisposableStrategy>();
        }

        public void Bootstrap()
        {
            WurmApiSetup.TryAutodetectWurmInstallDir(kernel);
            
            // this is where 'first time' config dialog is shown, if required
            ConfigSetup.ConfigureApplication(kernel);

            ConfigSetup.BindComponents(kernel);

            WurmApiSetup.BindSingletonApi(kernel);
            WurmApiSetup.ValidateWurmGameClientConfig(kernel);

            TrayPopupsSetup.BindTrayPopups(kernel);
            SoundEngineSetup.BindSoundEngine(kernel);

            LogSearcherSetup.BindLogSearcher(kernel);
            CalendarSetup.BindCalendar(kernel);
            TimersSetup.BindTimers(kernel);

            FeaturesSetup.BindFeaturesManager(kernel);
            MainMenuSetup.BindMenu(kernel);

            mainForm.SetMenuView(kernel.Get<MenuView>());
            mainForm.SetModulesView(kernel.Get<FeaturesView>());

            var prov = kernel.Get<IWaExecutionInfoProvider>();
            mainForm.Text += string.Format(" ({0})", prov.Get());

            var featureManager = kernel.Get<IFeaturesManager>();
            featureManager.InitFeatures();

            ShowChangelog();
        }

        void ShowChangelog()
        {
            var changelogManager = kernel.Get<IChangelogManager>();
            var userNotify = kernel.Get<IUserNotifier>();
            var logger = kernel.Get<ILogger>();
            try
            {
                var changes = changelogManager.GetNewChanges();
                if (!string.IsNullOrWhiteSpace(changes))
                {
                    changelogManager.ShowChanges(changes);
                    changelogManager.UpdateLastChangeDate();
                }
            }
            catch (Exception exception)
            {
                logger.Warn(exception, "Error at parsing or opening changelog");
                userNotify.NotifyWithMessageBox("Error opening changelog, see logs for details.", NotifyKind.Warning);
            }
        }

        public void Dispose()
        {
            try
            {
                kernel.Dispose();
            }
            finally
            {
                dataDirectory.Unlock();
            }
        }
    }
}