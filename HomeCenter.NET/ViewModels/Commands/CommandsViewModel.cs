﻿using System;
using System.Linq;
using Caliburn.Micro;
using H.NET.Storages;
using H.NET.Storages.Extensions;
using HomeCenter.NET.Extensions;
using HomeCenter.NET.Services;

// ReSharper disable UnusedMember.Global

namespace HomeCenter.NET.ViewModels.Commands
{
    public class CommandsViewModel : SaveCancelViewModel
    {
        #region Properties

        public RunnerService RunnerService { get; }
        public HookService HookService { get; }

        public BindableCollection<UserCommandViewModel> UserCommands { get; }
        public BindableCollection<AllCommandViewModel> AllCommands { get; }
        public BindableCollection<VariableViewModel> Variables { get; }
        public BindableCollection<ProcessViewModel> Processes { get; }

        #endregion

        #region Constructors

        public CommandsViewModel(RunnerService runnerService, HookService hookService)
        {
            RunnerService = runnerService ?? throw new ArgumentNullException(nameof(runnerService));
            HookService = hookService ?? throw new ArgumentNullException(nameof(hookService));

            UserCommands = new BindableCollection<UserCommandViewModel>(
                RunnerService.Storage.UniqueValues(entry => entry.Value).Select(i => new UserCommandViewModel(i.Value)));
            AllCommands = new BindableCollection<AllCommandViewModel>(
                RunnerService.GetSupportedCommands().Select(i => new AllCommandViewModel(i)));
            Variables = new BindableCollection<VariableViewModel>(
                RunnerService.GetSupportedVariables().Select(i => new VariableViewModel(i)));
            Processes = new BindableCollection<ProcessViewModel>(
                RunnerService.Processes.Select(i => new ProcessViewModel(i)));

            // TODO: May be -event required?
            RunnerService.BeforeRun += (o, e) => NotifyOfPropertyChange(nameof(Processes));
            RunnerService.AfterRun += (o, e) => NotifyOfPropertyChange(nameof(Processes));

            SaveAction = () =>
            {
                RunnerService.Storage.Save();

                // TODO: simplify?
                hookService.UpdateCombinations();
            };
            CancelAction = () => RunnerService.Storage.Load(); // Cancel changes TODO: may me need to use TempStorage instead this?
        }

        #endregion

        #region Public methods

        public void Add()
        {
            var command = new Command();
            command.Keys.Add(new SingleKey(string.Empty));
            command.Lines.Add(new SingleCommand(string.Empty));

            var viewModel = new CommandSettingsViewModel(command, RunnerService, HookService);
            var result = this.ShowDialog(viewModel);
            if (result != true)
            {
                return;
            }

            UserCommands.Add(new UserCommandViewModel(command));
            foreach (var key in command.Keys)
            {
                RunnerService.Storage[key.Text] = command;
            }
        }

        public void EditCommand(CommandViewModel viewModel)
        {
            switch (viewModel)
            {
                case UserCommandViewModel userCommandViewModel:
                    var newCommand = (Command)userCommandViewModel.Command.Clone();
                    var dialogViewModel = new CommandSettingsViewModel(newCommand, RunnerService, HookService);
                    var result = this.ShowDialog(dialogViewModel);
                    if (result != true)
                    {
                        return;
                    }

                    foreach (var key in userCommandViewModel.Command.Keys)
                    {
                        RunnerService.Storage.Remove(key.Text);
                    }
                    foreach (var key in newCommand.Keys)
                    {
                        RunnerService.Storage[key.Text] = newCommand;
                    }

                    var index = UserCommands.IndexOf(userCommandViewModel);
                    UserCommands.Remove(userCommandViewModel);
                    UserCommands.Insert(index, new UserCommandViewModel(newCommand));

                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void DeleteCommand(CommandViewModel viewModel)
        {
            switch (viewModel)
            {
                case UserCommandViewModel userCommandViewModel:
                    foreach (var key in userCommandViewModel.Command.Keys)
                    {
                        RunnerService.Storage.Remove(key.Text);
                    }
                    UserCommands.Remove(userCommandViewModel);
                    break;

                case ProcessViewModel processViewModel:
                    processViewModel.Process.Cancel();
                    NotifyOfPropertyChange(nameof(Processes));
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public void RunCommand(CommandViewModel viewModel)
        {
            switch (viewModel)
            {
                case UserCommandViewModel userCommandViewModel:
                    foreach (var line in userCommandViewModel.Command.Lines)
                    {
                        RunnerService.Run(line.Text);
                    }
                    break;

                case AllCommandViewModel allCommandViewModel:
                    RunnerService.Run($"{allCommandViewModel.Prefix} {viewModel.Description}");
                    break;

                case VariableViewModel _:
                    viewModel.Description = RunnerService.GetVariableValue(viewModel.Name)?.ToString() ?? string.Empty;
                    break;

                case ProcessViewModel _:
                    RunnerService.Run(viewModel.Name);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
        
        #endregion

    }
}
