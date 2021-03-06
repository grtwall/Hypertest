﻿#region License

// Copyright (c) 2014 Chandramouleswaran Ravichandran
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Hypertest.Core.Controls;
using Hypertest.Core.Handlers;
using Hypertest.Core.Interfaces;
using Hypertest.Core.Service;
using Hypertest.Core.Tests;
using Hypertest.Core.Toolbox;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Prism.Events;
using Microsoft.Practices.Prism.Modularity;
using Microsoft.Practices.Unity;
using Wide.Interfaces;
using Wide.Interfaces.Controls;
using Wide.Interfaces.Events;
using Wide.Interfaces.Services;
using Wide.Interfaces.Settings;
using Wide.Interfaces.Themes;
using Hypertest.Core.Results;

namespace Hypertest.Core
{
    [Module(ModuleName = "Hypertest.Core")]
    [ModuleDependency("Wide.Tools.Logger")]
    public class CoreModule : IModule
    {
        private readonly IUnityContainer _container;
        private readonly IEventAggregator _eventAggregator;
        private IRunnerRegistry _runnerRegistry;

        public CoreModule(IUnityContainer container, IEventAggregator eventAggregator)
        {
            _container = container;
            _eventAggregator = eventAggregator;
        }

        public void Initialize()
        {
            _eventAggregator.GetEvent<SplashMessageUpdateEvent>()
                .Publish(new SplashMessageUpdateEvent { Message = "Loading Core Module" });
            RegisterParts();
            LoadTheme();
            LoadCommands();
            LoadMenus();
            LoadToolbar();
            LoadSettings();
        }

        private void LoadToolbar()
        {
            _eventAggregator.GetEvent<SplashMessageUpdateEvent>()
                .Publish(new SplashMessageUpdateEvent { Message = "Toolbar.." });
            var toolbarService = _container.Resolve<IToolbarService>();
            var menuService = _container.Resolve<IMenuService>();

            toolbarService.Add(new ToolbarViewModel("Standard", 1) { Band = 1, BandIndex = 1 });
            toolbarService.Get("Standard").Add(menuService.Get("_File").Get("_New"));
            toolbarService.Get("Standard").Add(menuService.Get("_File").Get("_Open"));

            toolbarService.Add(new ToolbarViewModel("Edit", 2) { Band = 1, BandIndex = 2 });
            toolbarService.Get("Edit").Add(menuService.Get("_Edit").Get("_Undo"));
            toolbarService.Get("Edit").Add(menuService.Get("_Edit").Get("_Redo"));
            toolbarService.Get("Edit").Add(menuService.Get("_Edit").Get("Cut"));
            toolbarService.Get("Edit").Add(menuService.Get("_Edit").Get("Copy"));
            toolbarService.Get("Edit").Add(menuService.Get("_Edit").Get("_Paste"));
            
            toolbarService.Add(new ToolbarViewModel("Debug", 3) { Band = 1, BandIndex = 3 });
            toolbarService.Get("Debug").Add(menuService.Get("_Edit").Get("Play"));

            menuService.Get("_Tools").Add(toolbarService.RightClickMenu);

            //Initiate the position settings changes for toolbar
            _container.Resolve<IToolbarPositionSettings>();
        }

        private void LoadSettings()
        {
            var manager = _container.Resolve<ISettingsManager>();
        }

        private void RegisterParts()
        {
            _container.RegisterType<WebTestScenarioHandler>();
            _container.RegisterType<WebTestResultHandler>();
            _container.RegisterType<WebTestScenarioViewModel>();
            _container.RegisterType<WebTestResultViewModel>();
            _container.RegisterType<WebTestScenarioView>();
            _container.RegisterType<WebTestResultView>();
            _container.RegisterType<ITestRegistry, TestRegistry>(new ContainerControlledLifetimeManager());
            _container.RegisterType<IRunnerRegistry, RunnerRegistry>(new ContainerControlledLifetimeManager());
            _container.RegisterType<WebScenarioRunner>(new ContainerControlledLifetimeManager());
            _container.RegisterType<ToolboxView>();
            _container.RegisterType<ToolboxModel>();
            _container.RegisterType<ToolboxViewModel>();

            _container.RegisterType<WebTestCurrentResultViewModel>(new ContainerControlledLifetimeManager());
            _container.Resolve<WebTestCurrentResultViewModel>();

            IContentHandler handler = _container.Resolve<WebTestScenarioHandler>();
            _container.Resolve<IContentHandlerRegistry>().Register(handler);

            handler = _container.Resolve<WebTestResultHandler>();
            _container.Resolve<IContentHandlerRegistry>().Register(handler);

            _runnerRegistry = _container.Resolve<IRunnerRegistry>();
            _runnerRegistry.Add(typeof(WebScenarioRunner));

            var registry = _container.Resolve<ITestRegistry>();

            //Register the test cases that you want to be able to participate
            registry.Add(typeof(FolderTestCase));
            registry.Add(typeof(ExpressionTestCase));
            registry.Add(typeof(LooperTestCase));
            registry.Add(typeof(RunScenarioTestCase));
            registry.Add(typeof(SetVariableTestCase));

#if DEBUG
            registry.Add(typeof(DebugInfoTestCase));
#endif

            //Add the toolbox to the workspace
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            workspace.Tools.Add(_container.Resolve<ToolboxViewModel>());
        }

        private void LoadTheme()
        {
            _eventAggregator.GetEvent<SplashMessageUpdateEvent>()
                .Publish(new SplashMessageUpdateEvent { Message = "Themes.." });
            var manager = _container.Resolve<IThemeManager>();
            var themeSettings = _container.Resolve<IThemeSettings>();
            var win = _container.Resolve<IShell>() as Window;
            manager.AddTheme(new LightTheme());
            manager.AddTheme(new DarkTheme());
            win.Dispatcher.Invoke(() => manager.SetCurrent(themeSettings.SelectedTheme));
        }

        private void LoadCommands()
        {
            _eventAggregator.GetEvent<SplashMessageUpdateEvent>()
                .Publish(new SplashMessageUpdateEvent { Message = "Commands.." });
            var manager = _container.Resolve<ICommandManager>();

            var openCommand = new DelegateCommand(OpenModule);
            var exitCommand = new DelegateCommand(CloseCommandExecute);
            var saveCommand = new DelegateCommand(SaveDocument, CanExecuteSaveDocument);
            var saveAsCommand = new DelegateCommand(SaveAsDocument, CanExecuteSaveAsDocument);
            var themeCommand = new DelegateCommand<string>(ThemeChangeCommand);
            var loggerCommand = new DelegateCommand(ToggleLogger);
            var toolboxCommand = new DelegateCommand(ToggleToolbox);
            var runCommand = new DelegateCommand(RunTest, CanRunTest);


            manager.RegisterCommand("OPEN", openCommand);
            manager.RegisterCommand("SAVE", saveCommand);
            manager.RegisterCommand("SAVEAS", saveAsCommand);
            manager.RegisterCommand("EXIT", exitCommand);
            manager.RegisterCommand("LOGSHOW", loggerCommand);
            manager.RegisterCommand("TOOLBOXSHOW", toolboxCommand);
            manager.RegisterCommand("THEMECHANGE", themeCommand);
            manager.RegisterCommand("RUNTEST", runCommand);
        }

        private void LoadMenus()
        {
            _eventAggregator.GetEvent<SplashMessageUpdateEvent>()
                .Publish(new SplashMessageUpdateEvent { Message = "Menus.." });
            var manager = _container.Resolve<ICommandManager>();
            var menuService = _container.Resolve<IMenuService>();
            var settingsManager = _container.Resolve<ISettingsManager>();
            var themeSettings = _container.Resolve<IThemeSettings>();
            var recentFiles = _container.Resolve<IRecentViewSettings>();
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            ToolViewModel logger = workspace.Tools.FirstOrDefault(f => f.ContentId == "Logger");
            ToolViewModel toolbox = workspace.Tools.FirstOrDefault(f => f.ContentId == "Toolbox");

            menuService.Add(new MenuItemViewModel("_File", 1));

            menuService.Get("_File").Add(
                (new MenuItemViewModel("_New", 3,
                    new BitmapImage(
                        new Uri(
                            @"pack://application:,,,/Hypertest;component/Images/new.png")),
                    manager.GetCommand("NEW"),
                    new KeyGesture(Key.N, ModifierKeys.Control, "Ctrl + N"))));

            menuService.Get("_File").Add(
                (new MenuItemViewModel("_Open", 4,
                    new BitmapImage(
                        new Uri(
                            @"pack://application:,,,/Hypertest;component/Images/open.png")),
                    manager.GetCommand("OPEN"),
                    new KeyGesture(Key.O, ModifierKeys.Control, "Ctrl + O"))));
            menuService.Get("_File").Add(new MenuItemViewModel("_Save", 5,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/save.png")),
                manager.GetCommand("SAVE"),
                new KeyGesture(Key.S, ModifierKeys.Control, "Ctrl + S")));
            menuService.Get("_File").Add(new SaveAsMenuItemViewModel("Save As..", 6,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/save.png")),
                manager.GetCommand("SAVEAS"), null, false, false, _container));

            menuService.Get("_File").Add(new MenuItemViewModel("Close", 8, null, manager.GetCommand("CLOSE"),
                new KeyGesture(Key.F4, ModifierKeys.Control, "Ctrl + F4")));

            menuService.Get("_File").Add(recentFiles.RecentMenu);

            menuService.Get("_File").Add(new MenuItemViewModel("E_xit", 101, null, manager.GetCommand("EXIT"),
                new KeyGesture(Key.F4, ModifierKeys.Alt, "Alt + F4")));


            menuService.Add(new MenuItemViewModel("_Edit", 2));
            menuService.Get("_Edit").Add(new MenuItemViewModel("_Undo", 1,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/undo.png")),
                ApplicationCommands.Undo));
            menuService.Get("_Edit").Add(new MenuItemViewModel("_Redo", 2,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/redo.png")),
                ApplicationCommands.Redo));
            menuService.Get("_Edit").Add(MenuItemViewModel.Separator(15));
            menuService.Get("_Edit").Add(new MenuItemViewModel("Cut", 20,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/cut.png")),
                ApplicationCommands.Cut));
            menuService.Get("_Edit").Add(new MenuItemViewModel("Copy", 21,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/copy.png")),
                ApplicationCommands.Copy));
            menuService.Get("_Edit").Add(new MenuItemViewModel("_Paste", 22,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest;component/Images/paste.png")),
                ApplicationCommands.Paste));
            menuService.Get("_Edit").Add(new RunTestMenuItemViewModel("Play", 23,
                new BitmapImage(
                    new Uri(
                        @"pack://application:,,,/Hypertest.Core;component/Images/RunScenario.png")),
                null, new KeyGesture(Key.F5, ModifierKeys.None, "F5")));
            menuService.Get("_Edit").Get("Play").Add(new MenuItemViewModel("Chrome", 231,
                null, null, null, true) {IsChecked = true});
            menuService.Get("_Edit").Get("Play").Add(new MenuItemViewModel("Firefox", 232,
                null, null, null, true));
            menuService.Get("_Edit").Get("Play").Add(new MenuItemViewModel("Internet Explorer", 233,
                null, null, null, true));

            menuService.Add(new MenuItemViewModel("_View", 3));
            if (logger != null)
                menuService.Get("_View").Add(new MenuItemViewModel("_Logger", 1,
                    new BitmapImage(
                        new Uri(
                            @"pack://application:,,,/Hypertest;component/Images/undo.png")),
                    manager.GetCommand("LOGSHOW")) { IsCheckable = true, IsChecked = logger.IsVisible });

            if (toolbox != null)
                menuService.Get("_View").Add(new MenuItemViewModel("_Toolbox", 2,
                    new BitmapImage(
                        new Uri(
                            @"pack://application:,,,/Hypertest;component/Images/undo.png")),
                    manager.GetCommand("TOOLBOXSHOW")) { IsCheckable = true, IsChecked = toolbox.IsVisible });

            menuService.Get("_View").Add(new MenuItemViewModel("Themes", 1));

            //Set the checkmark of the theme menu's based on which is currently selected
            menuService.Get("_View").Get("Themes").Add(new MenuItemViewModel("Dark", 1, null,
                manager.GetCommand("THEMECHANGE"))
                                                       {
                                                           IsCheckable = true,
                                                           IsChecked = (themeSettings.SelectedTheme == "Dark"),
                                                           CommandParameter = "Dark"
                                                       });
            menuService.Get("_View").Get("Themes").Add(new MenuItemViewModel("Light", 2, null,
                manager.GetCommand("THEMECHANGE"))
                                                       {
                                                           IsCheckable = true,
                                                           IsChecked = (themeSettings.SelectedTheme == "Light"),
                                                           CommandParameter = "Light"
                                                       });

            menuService.Add(new MenuItemViewModel("_Tools", 4));
            menuService.Get("_Tools").Add(new MenuItemViewModel("Settings", 1, null, settingsManager.SettingsCommand));

            menuService.Add(new MenuItemViewModel("_Help", 4));
        }

        #region Commands

        #region Open

        private void OpenModule()
        {
            var service = _container.Resolve<IOpenDocumentService>();
            service.Open();
        }

        #endregion

        #region Save

        private bool CanExecuteSaveDocument()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            if (workspace.ActiveDocument != null)
            {
                return workspace.ActiveDocument.Model.IsDirty;
            }
            return false;
        }

        private bool CanExecuteSaveAsDocument()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            return (workspace.ActiveDocument != null);
        }

        private void SaveDocument()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            var manager = _container.Resolve<ICommandManager>();
            if (workspace.ActiveDocument.Handler != null)
            {
                workspace.ActiveDocument.Handler.SaveContent(workspace.ActiveDocument);
            }
            manager.Refresh();
        }

        private void SaveAsDocument()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            var manager = _container.Resolve<ICommandManager>();
            if (workspace.ActiveDocument != null)
            {
                workspace.ActiveDocument.Handler.SaveContent(workspace.ActiveDocument, true);
                manager.Refresh();
            }
        }

        #endregion

        #region Theme

        private void ThemeChangeCommand(string s)
        {
            var manager = _container.Resolve<IThemeManager>();
            var menuService = _container.Resolve<IMenuService>();
            var win = _container.Resolve<IShell>() as Window;
            var mvm =
                menuService.Get("_View").Get("Themes").Get(manager.CurrentTheme.Name) as MenuItemViewModel;

            if (manager.CurrentTheme.Name != s)
            {
                if (mvm != null)
                    mvm.IsChecked = false;
                win.Dispatcher.InvokeAsync(() => manager.SetCurrent(s));
            }
            else
            {
                if (mvm != null)
                    mvm.IsChecked = true;
            }
        }

        #endregion

        #region Logger click

        private void ToggleLogger()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            var menuService = _container.Resolve<IMenuService>();
            ToolViewModel logger = workspace.Tools.First(f => f.ContentId == "Logger");
            if (logger != null)
            {
                logger.IsVisible = !logger.IsVisible;
                var mi = menuService.Get("_View").Get("_Logger") as MenuItemViewModel;
                mi.IsChecked = logger.IsVisible;
            }
        }

        private void ToggleToolbox()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            var menuService = _container.Resolve<IMenuService>();
            ToolViewModel toolbox = workspace.Tools.First(f => f.ContentId == "Toolbox");
            if (toolbox != null)
            {
                toolbox.IsVisible = !toolbox.IsVisible;
                var mi = menuService.Get("_View").Get("_Toolbox") as MenuItemViewModel;
                mi.IsChecked = toolbox.IsVisible;
            }
        }

        #endregion

        #region Run
        private void RunTest()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            if (workspace.ActiveDocument != null)
            {
                if (workspace.ActiveDocument.Model is TestScenario)
                {
                    var cvm = workspace.Documents.FirstOrDefault(x => x is WebTestCurrentResultViewModel) as WebTestCurrentResultViewModel;
                    if (cvm != null)
                    {
                        workspace.ActiveDocument = cvm;
                    }
                    else
                    {
                        cvm = _container.Resolve<WebTestCurrentResultViewModel>();
                        var runner = _runnerRegistry[workspace.ActiveDocument.Model as TestScenario];
                        if (runner.IsRunning == false)
                        {
                            TestScenario scenario = (workspace.ActiveDocument.Model as TestScenario).Clone() as TestScenario;
                            runner.Initialize(scenario);
                        }
                    }
                }
            }
        }

        private bool CanRunTest()
        {
            IWorkspace workspace = _container.Resolve<AbstractWorkspace>();
            if (workspace.ActiveDocument != null && workspace.ActiveDocument.Model is TestScenario)
            {
                var runner = _runnerRegistry[workspace.ActiveDocument.Model as TestScenario];
                return runner != null && (workspace.ActiveDocument.Model is TestScenario && runner.IsRunning == false);
            }
            return false;
        }

        #endregion

        #region Close

        private void CloseCommandExecute()
        {
            var shell = _container.Resolve<IShell>();
            shell.Close();
        }

        #endregion

        #endregion
    }
}