﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using H.Services;
using HomeCenter.NET.Initializers;
using HomeCenter.NET.Input;
using HomeCenter.NET.Properties;
using HomeCenter.NET.Services;
using HomeCenter.NET.Utilities;
using HomeCenter.NET.ViewModels;
using HomeCenter.NET.ViewModels.Commands;
using HomeCenter.NET.ViewModels.Modules;
using HomeCenter.NET.ViewModels.Settings;
using HomeCenter.NET.ViewModels.Utilities;
using HomeCenter.NET.Views;

namespace HomeCenter.NET
{
    public class Bootstrapper : BootstrapperBase
    {
        private SimpleContainer? Container { get; set; }
        private MainView? MainView { get; set; }

        public Bootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            Container = new SimpleContainer();

            //Container.Instance(Container);

            Container
                .Singleton<HookService>();

            Container
                .Singleton<IWindowManager, HWindowManager>()
                .Singleton<IEventAggregator, EventAggregator>()
                //.Singleton<ModuleService>()
                .Singleton<StorageService>()
                .Instance(Settings.Default);

            Container
                .PerRequest<CommandSettingsViewModel>()
                .PerRequest<ModuleSettingsViewModel>()
                .PerRequest<CommandsViewModel>()
                .Singleton<SettingsViewModel>()
                .Singleton<PopupViewModel>()
                .Singleton<MainViewModel>();

            Container
                .PerRequest<StaticModulesInitializer>();

            base.Configure();

            #region Add Visibility Name Convection

            ConventionManager.AddElementConvention<UIElement>(UIElement.VisibilityProperty, "Visibility", "VisibilityChanged");

            static void BindVisibilityProperties(IEnumerable<FrameworkElement> frameWorkElements, Type viewModel)
            {
                foreach (var frameworkElement in frameWorkElements)
                {
                    var propertyName = frameworkElement.Name + "IsVisible";
                    var property = viewModel.GetPropertyCaseInsensitive(propertyName);
                    if (property == null)
                    {
                        continue;
                    }

                    var convention = ConventionManager
                        .GetElementConvention(typeof(FrameworkElement));
                    ConventionManager.SetBindingWithoutBindingOverwrite(
                        viewModel,
                        propertyName,
                        property,
                        frameworkElement,
                        convention,
                        convention.GetBindableProperty(frameworkElement));
                }
            }

            var baseBindProperties = ViewModelBinder.BindProperties;
            ViewModelBinder.BindProperties =
                (elements, viewModel) =>
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    BindVisibilityProperties(elements, viewModel);

                    // ReSharper disable once PossibleMultipleEnumeration
                    return baseBindProperties(elements, viewModel);
                };

            // Need to override BindActions as well, as it's called first and filters out anything it binds to before
            // BindProperties is called.
            var baseBindActions = ViewModelBinder.BindActions;
            ViewModelBinder.BindActions =
                (elements, viewModel) =>
                {
                    // ReSharper disable once PossibleMultipleEnumeration
                    BindVisibilityProperties(elements, viewModel);

                    // ReSharper disable once PossibleMultipleEnumeration
                    return baseBindActions(elements, viewModel);
                };

            #endregion

            #region Key Triggers
            
            var defaultCreateTrigger = Parser.CreateTrigger;

            Parser.CreateTrigger = (target, triggerText) =>
            {
                if (triggerText == null)
                {
                    return defaultCreateTrigger(target, null);
                }

                var triggerDetail = triggerText
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty);

                var splits = triggerDetail.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

                return splits[0] switch
                {
                    "KeyUp" => KeyUpTrigger.FromText(splits[1]),
                    _ => defaultCreateTrigger(target, triggerText)
                };
            };
            
            #endregion
        }

        private static void DisposeObject<T>() where T : class, IDisposable
        {
            var obj = IoC.GetInstance(typeof(T), null) as T ?? throw new ArgumentNullException();

            obj.Dispose();
        }

        private static async ValueTask DisposeAsyncObject<T>() where T : class, IAsyncDisposable
        {
            var obj = IoC.GetInstance(typeof(T), null) as T ?? throw new ArgumentNullException();

            await obj.DisposeAsync().ConfigureAwait(false);
        }

        private static T Get<T>() where T : class 
        {
            return IoC.GetInstance(typeof(T), null) as T ?? throw new ArgumentNullException();
        }

        protected override async void OnStartup(object sender, StartupEventArgs e)
        {
            InitializeHelper.CheckKillAll(e.Args);
            InitializeHelper.CheckNotFirstProcess(e.Args);

            // Catching unhandled exceptions
            WpfSafeActions.Initialize();

            var manager = Get<IWindowManager>();
            var instance = Get<PopupViewModel>();

            // Create permanent hidden PopupView
            await manager.ShowWindowAsync(instance);
            
            var model = Get<MainViewModel>();
            //var moduleService = Get<ModuleService>();

            var hWindowManager = manager as HWindowManager ?? throw new ArgumentNullException();

            // Create hidden window(without moment of show/hide)
            MainView = await hWindowManager.CreateWindowAsync(model) as MainView ?? throw new ArgumentNullException();

            // TODO: custom window manager is required
            model.IsVisible = e.Args.Contains("/restart") || !Settings.Default.IsStartMinimized;

            Get<StaticModulesInitializer>();

            var hookService = Get<HookService>();
            await hookService.InitializeAsync();

            hookService.UpCombinationCaught += (_, value) => model.Print($"Up: {value}");
            hookService.DownCombinationCaught += (_, value) => model.Print($"Down: {value}");

            InitializeHelper.CheckUpdate(e.Args);
            InitializeHelper.CheckRun(e.Args);
        }

        // ReSharper disable once UnusedMember.Global
        public static string StopMouseHook()
        {
            //((HookService)IoC.GetInstance(typeof(HookService), null)).MouseHook.Stop();

            return string.Empty;
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            MainView?.Dispose();

            //DisposeObject<HookService>();
            //DisposeObject<ModuleService>();

            Application.Shutdown();
        }

        protected override object GetInstance(Type service, string key)
        {
            Container = Container ?? throw new InvalidOperationException("Container is null");

            return Container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service)
        {
            Container = Container ?? throw new InvalidOperationException("Container is null");

            return Container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance)
        {
            Container = Container ?? throw new InvalidOperationException("Container is null");

            Container.BuildUp(instance);
        }

        protected override void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
#if DEBUG
            MessageBox.Show(e.Exception.ToString(), "An error as occurred", MessageBoxButton.OK, MessageBoxImage.Error);
#else           
            MessageBox.Show(e.Exception.Message, "An error as occurred", MessageBoxButton.OK, MessageBoxImage.Error); 
#endif
        }
    }
}
