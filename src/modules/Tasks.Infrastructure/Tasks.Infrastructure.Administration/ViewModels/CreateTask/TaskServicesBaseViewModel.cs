using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Input;
using Anapher.Wpf.Toolkit.Extensions;
using Anapher.Wpf.Toolkit.Windows;
using Microsoft.Extensions.DependencyInjection;
using Prism.Commands;
using Tasks.Infrastructure.Administration.Library;
using Tasks.Infrastructure.Administration.Utilities;
using Tasks.Infrastructure.Core;
using Unclassified.TxLib;

namespace Tasks.Infrastructure.Administration.ViewModels.CreateTask
{
    public abstract class TaskServicesBaseViewModel<TServiceDescription> : TaskServicesViewModel where TServiceDescription : ITaskServiceDescription
    {
        protected readonly ObservableCollection<TaskViewModelView> _childs;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly IWindowService WindowService;
        private DelegateCommand _addNewCommand;
        private bool _isSelected;
        private DelegateCommand<TaskViewModelView> _removeChildCommand;
        protected TaskViewModelView _selectedChild;
        protected ITaskServiceDescription _selectedService;

        protected TaskServicesBaseViewModel(IWindowService windowService, IServiceProvider serviceProvider)
        {
            WindowService = windowService;
            ServiceProvider = serviceProvider;
            _childs = new ObservableCollection<TaskViewModelView>();

            Childs = new ListCollectionView(_childs);

            AvailableServices = serviceProvider.GetRequiredService<IEnumerable<TServiceDescription>>().Cast<ITaskServiceDescription>().ToList();
        }

        public override ListCollectionView Childs { get; }
        public override IList<ITaskServiceDescription> AvailableServices { get; }

        public override TaskViewModelView SelectedChild
        {
            get => _selectedChild;
            set
            {
                var oldValue = _selectedChild;
                if (SetProperty(ref _selectedChild, value))
                {
                    if (oldValue != null)
                        oldValue.IsSelected = false;
                    if (value != null)
                        value.IsSelected = true;
                }
            }
        }

        public override ITaskServiceDescription SelectedService
        {
            get => _selectedService;
            set
            {
                if (SetProperty(ref _selectedService, value))
                {
                    if (value == null)
                    {
                        SelectedChild = null;
                    }
                    else
                    {
                        var viewModelView = CreateView(value);
                        if (_childs.Any())
                            RemoveChild(_childs.Single());
                        AddChild(viewModelView);

                        SelectedChild = viewModelView;
                    }
                }
            }
        }

        public override ICommand AddNewCommand
        {
            get
            {
                return _addNewCommand ?? (_addNewCommand = new DelegateCommand(() =>
                {
                    if (WindowService.ShowDialog<TaskCreateServiceViewModel>(
                            Tx.T("TasksInfrastructure:CreateTask.CreateNewEntry", "entry", EntryName), vm => vm.Initialize(this),
                            out var viewModel) == true)
                        AddChild(viewModel.View);
                }));
            }
        }

        public override ICommand RemoveChildCommand
        {
            get
            {
                return _removeChildCommand ?? (_removeChildCommand = new DelegateCommand<TaskViewModelView>(parameter =>
                {
                    RemoveChild(parameter);

                    if (parameter == SelectedChild)
                    {
                        parameter.IsSelected = false;
                        SelectedChild = null;
                    }

                    if (parameter.Description == SelectedService)
                        SelectedService = null;

                    if (_childs.Count == 1)
                    {
                        SelectedChild = _childs.Single();
                        SetProperty(ref _selectedService, _childs.Single().Description, nameof(SelectedService));
                    }
                }));
            }
        }

        public override bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                    if (value && _childs.Count > 1)
                        SelectedChild = null;
            }
        }

        public override IEnumerable<ValidationResult> ValidateInput()
        {
            foreach (var taskViewModelView in _childs)
            {
                var validationResult = TaskServiceViewModelUtils.ValidateInput(taskViewModelView.ViewModel);
                if (validationResult != ValidationResult.Success)
                    yield return validationResult;
            }
        }

        public override IEnumerable<ValidationResult> ValidateContext(MazeTask mazeTask)
        {
            foreach (var taskViewModelView in _childs)
            {
                var validationResult = TaskServiceViewModelUtils.ValidateContext(taskViewModelView.ViewModel, mazeTask);
                if (validationResult != ValidationResult.Success)
                    yield return validationResult;
            }

            foreach (var child in _childs)
            {
                var audienceAttribute = child.ViewModel.GetType().GetCustomAttribute<TaskAudienceAttribute>();
                if (audienceAttribute != null)
                    switch (audienceAttribute.Mode)
                    {
                        case TaskAudienceMode.Clients:
                            if (!mazeTask.Audience.Any() && !mazeTask.Audience.IsAll)
                                yield return new ValidationResult("No clients included");
                            break;
                        case TaskAudienceMode.Server:
                            if (!mazeTask.Audience.IncludesServer)
                                yield return new ValidationResult("No server included");
                            break;
                        case TaskAudienceMode.ClientsAndServer:
                            break;
                    }
            }
        }

        protected void AddChild(TaskViewModelView taskViewModelView)
        {
            _childs.Add(taskViewModelView);
            taskViewModelView.PropertyChanged += ChildOnPropertyChanged;
        }

        protected void RemoveChild(TaskViewModelView taskViewModelView)
        {
            _childs.Remove(taskViewModelView);
            taskViewModelView.PropertyChanged -= ChildOnPropertyChanged;
        }

        private void ChildOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(TaskViewModelView.IsSelected):
                    var taskViewModel = (TaskViewModelView) sender;
                    if (taskViewModel.IsSelected)
                        SelectedChild = taskViewModel;
                    break;
            }
        }
    }
}