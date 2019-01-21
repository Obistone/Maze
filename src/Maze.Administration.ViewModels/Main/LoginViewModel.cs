using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Threading;
using Anapher.Wpf.Toolkit;
using Anapher.Wpf.Toolkit.Windows;
using NuGet.Frameworks;
using Maze.Administration.Core;
using Maze.Administration.Core.Modules;
using Maze.Administration.Core.Rest;
using Maze.Administration.Library.Exceptions;
using Maze.Administration.Library.Extensions;
using Maze.Administration.Library.Rest.Modules.V1;
using Maze.ModuleManagement;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using Unclassified.TxLib;

namespace Maze.Administration.ViewModels.Main
{
    public class LoginViewModel : BindableBase
    {
        private readonly IModuleManager _moduleManager;
        private readonly IModuleCatalog _catalog;
        private readonly IRegionManager _regionManager;
        private readonly IWindowService _windowService;
        private readonly MazeRestClientWrapper _restClientWrapper;

        private static readonly NuGetFramework
            Framework = FrameworkConstants.CommonFrameworks.MazeAdministration10; //TODO move somewhere else

        private string _errorMessage;
        private bool _isLoggingIn;
        private AsyncDelegateCommand<SecureString> _loginCommand;
        private string _statusMessage;
        private string _username;

        public LoginViewModel(IModuleManager moduleManager, IModuleCatalog catalog, IRegionManager regionManager, IWindowService windowService,
            MazeRestClientWrapper restClientWrapper)
        {
            _moduleManager = moduleManager;
            _catalog = catalog;
            _regionManager = regionManager;
            _windowService = windowService;
            _restClientWrapper = restClientWrapper;
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set => SetProperty(ref _isLoggingIn, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public AsyncDelegateCommand<SecureString> LoginCommand
        {
            get
            {
                return _loginCommand ?? (_loginCommand = new AsyncDelegateCommand<SecureString>(async parameter =>
                {
                    IsLoggingIn = true;

                    try
                    {
                        StatusMessage = Tx.T("LoginView:Status.Authenticating");

                        var serverInfo = new ServerInfo {ServerUri = new Uri("http://localhost:54941/")};
                        var client = await MazeRestConnector.TryConnect(Username, parameter, serverInfo);

                        StatusMessage = Tx.T("LoginView:Status.RetrieveModules");

                        var modules = await ModulesResource.FetchModules(Framework, client);

                        var options = new ModulesOptions {LocalPath = "packages", TempPath = "temp"};
                        var directory = new ModulesDirectory(
                            new VersionFolderPathResolverFlat(Environment.ExpandEnvironmentVariables(options.LocalPath)));
                        var catalog = new ModulesCatalog(directory, Framework);

                        if (modules.Any())
                        {
                            StatusMessage = Tx.T("LoginView:Status.DownloadModules");

                            var downloader = new ModuleDownloader(directory, options);
                            await downloader.Load(modules, serverInfo, CancellationToken.None);

                            StatusMessage = Tx.T("LoginView:Status.LoadModules");

                            await catalog.Load(modules);

                            foreach (var package in catalog.Packages)
                            {
                                Type moduleType;

                                try
                                {
                                    moduleType = package.Assembly.GetExportedTypes().FirstOrDefault(x => typeof(IModule).IsAssignableFrom(x));
                                }
                                catch (FileLoadException e)
                                {
                                    _windowService.ShowErrorMessageBox($"{package.Context.Package}:\r\n{e.Message}");
                                    return;
                                }

                                if (moduleType != null)
                                {
                                    _catalog.AddModule(
                                        new ModuleInfo(package.Context.Package.Id, moduleType.AssemblyQualifiedName)
                                        {
                                            State = ModuleState.ReadyForInitialization
                                        });

                                    _moduleManager.LoadModule(package.Context.Package.Id);
                                }
                            }
                        }

                        _restClientWrapper.Initialize(client);
                        _regionManager.RequestNavigate(PrismModule.MainContent, PrismModule.MainContentOverviewView);
                    }
                    catch (RestAuthenticationException e)
                    {
                        ErrorMessage = e.GetRestExceptionMessage();
                    }
                    catch (Exception e)
                    {
                        ErrorMessage = e.Message;
                    }
                    finally
                    {
                        IsLoggingIn = false;
                    }
                }));
            }
        }
    }
}