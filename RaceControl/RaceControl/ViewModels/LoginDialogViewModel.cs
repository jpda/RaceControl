﻿using CredentialManagement;
using Prism.Commands;
using Prism.Services.Dialogs;
using RaceControl.Core.Mvvm;
using RaceControl.Services.Interfaces.F1TV;
using RaceControl.Services.Interfaces.F1TV.Authorization;
using System;
using System.Windows.Input;
using DialogResult = Prism.Services.Dialogs.DialogResult;

namespace RaceControl.ViewModels
{
    public class LoginDialogViewModel : DialogViewModelBase
    {
        private const string RaceControlF1TV = "RaceControlF1TV";

        private readonly IAuthorizationService _authorizationService;

        private ICommand _loginCommand;

        private string _email;
        private string _password;
        private string _error;
        private bool _canClose;

        public LoginDialogViewModel(IAuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public override string Title => "Login to F1TV";

        public ICommand LoginCommand => _loginCommand ??= new DelegateCommand(LoginExecute, CanLoginExecute).ObservesProperty(() => Email).ObservesProperty(() => Password);

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string Error
        {
            get => _error;
            set => SetProperty(ref _error, value);
        }

        public bool CanClose
        {
            get => _canClose;
            set => SetProperty(ref _canClose, value);
        }

        public override void OnDialogOpened(IDialogParameters parameters)
        {
            base.OnDialogOpened(parameters);

            if (LoadCredential() && LoginCommand.CanExecute(null))
            {
                LoginCommand.Execute(null);
            }
        }

        public override bool CanCloseDialog()
        {
            return CanClose;
        }

        private bool CanLoginExecute()
        {
            return !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
        }

        private async void LoginExecute()
        {
            Error = null;
            TokenResponse token;

            try
            {
                token = await _authorizationService.LoginAsync(Email, Password);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return;
            }

            SaveCredential();
            CanClose = true;
            RaiseRequestClose(new DialogResult(ButtonResult.OK, new DialogParameters
            {
                { "token", token.Token }
            }));
        }

        private bool LoadCredential()
        {
            using (var credential = new Credential())
            {
                credential.Target = RaceControlF1TV;

                var loaded = credential.Load();

                if (loaded)
                {
                    Email = credential.Username;
                    Password = credential.Password;
                }

                return loaded;
            }
        }

        private bool SaveCredential()
        {
            using (var credential = new Credential())
            {
                credential.Target = RaceControlF1TV;
                credential.Type = CredentialType.Generic;
                credential.PersistanceType = PersistanceType.LocalComputer;
                credential.Username = Email;
                credential.Password = Password;

                return credential.Save();
            }
        }
    }
}