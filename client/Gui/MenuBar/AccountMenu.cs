using Grpc.Core;
using NStack;
using server.Protos;
using System.ComponentModel;
using Terminal.Gui;
using static server.Protos.Account;

namespace client.Gui
{
    public class AccountMenu : MenuBarItem
    {
        public AccountMenu()
        {
            Title = "Account";
            switch (ConnectionInfo.RoleCode)
            {
                case "User":
                    Children = new[] {
                        ChangePasswordItem,
                        ChangeUsernameItem,
                        DeleteAccountItem,
                        LogoutItem,
                    };
                    ChangePasswordItem.Action += ChangePassword;
                    ChangeUsernameItem.Action += ChangeUsername;
                    DeleteAccountItem.Action += DeleteAccount;
                    LogoutItem.Action += Logout;
                    break;
                case "Admin":
                    Children = new[] {
                        ChangePasswordItem,
                        ChangeUsernameItem,
                        DeleteAccountItem,
                        GenerateCodeItem,
                        LogoutItem,
                    };
                    ChangePasswordItem.Action += ChangePassword;
                    ChangeUsernameItem.Action += ChangeUsername;
                    DeleteAccountItem.Action += DeleteAccount;
                    GenerateCodeItem.Action += GenerateCode;
                    LogoutItem.Action += Logout;
                    break;
            }

        }

        async void ChangePassword()
        {
            try
            {
                ustring[] values = { "", "", "" };
                while (true)
                {
                    ustring[] labels = { "Old Password: ", "New Password: ", "Confirm Password: " };
                    bool confirmed = MultiInputDialog.Query("Change Password", "Enter your old password and your new password", labels, values, out ustring[]? results);
                    if (confirmed && results is not null)
                    {
                        values = results;
                        if (results[1].Equals(results[2]))
                        {
                            var request = new ChangePasswordRequest
                            {
                                TargetUsername = ConnectionInfo.Username,
                                Password = results[0].ToString(),
                                NewPassword = results[1].ToString(),
                            };
                            var client = new AccountClient(ConnectionInfo.GrpcChannel);
                            try
                            {
                                var respond = await VisualGrpc.InvokeAsync(client.ChangePasswordAsync, request);
                                if (respond.Success)
                                {
                                    MessageBox.Query("Success", "Change password success!", "OK");
                                    break;
                                }
                                else
                                {
                                    MessageBox.ErrorQuery("Fail", respond.Message, "Retry");
                                }
                            }
                            catch (RpcException e) when (e.StatusCode.Equals(StatusCode.Unauthenticated))
                            {
                                MessageBox.ErrorQuery("Fail", "Old password is incorrect", "Retry");
                                continue;
                            }
                        }
                        else
                        {
                            MessageBox.ErrorQuery("Fail", "New password and confirm password do not match", "Retry");
                            continue;
                        }
                    }
                    else { break; }
                }
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        async void ChangeUsername()
        {
            try
            {
                bool result = InputDialog.Query("Change Username"
                    , $"Your old username is {ConnectionInfo.Username}", "New Username:", "", out ustring NewUsername);
                if (!result || ustring.IsNullOrEmpty(NewUsername)) return;
                result = InputDialog.Query("Change Username"
                   , $"Your will change your username from {ConnectionInfo.Username} to {NewUsername}, retype your password to confirm it."
                   , "Password:", "", out ustring Password);
                if (!result) return;
                var request = new ChangeUsernameRequest
                {
                    TargetUsername = ConnectionInfo.Username,
                    Password = Password.ToString(),
                    NewUsername = NewUsername.ToString(),
                };
                var client = new AccountClient(ConnectionInfo.GrpcChannel);
                try
                {
                    var respond = await VisualGrpc.InvokeAsync(client.ChangeUsernameAsync, request);
                    if (respond.Success)
                    {
                        ConnectionInfo.LoadUsername(NewUsername.ToString()!);
                        var loginRequest = new LoginRequest
                        {
                            Username = ConnectionInfo.Username,
                            Password = Password.ToString(),
                        };
                        var loginRespond = await VisualGrpc.InvokeAsync(client.LoginAsync, loginRequest);
                        if (loginRespond.Success)
                        {
                            VisualGrpc.LoadToken(loginRespond.JwtToken);
                            MessageBox.Query("Success", "Change username success!", "OK");
                        }
                        else
                        {
                            MessageBox.ErrorQuery("Impossible Error", "Username is changed successfully, but login failed! "
                                + "Retry or contact the administrator for some help", "Exit");
                            Application.RequestStop();
                        }
                    }
                    else
                    {
                        MessageBox.ErrorQuery("Fail", respond.Message, "Exit");
                    }
                }
                catch (RpcException e) when (e.StatusCode.Equals(StatusCode.Unauthenticated))
                {
                    MessageBox.ErrorQuery("Fail", "Password is incorrect", "Exit");
                }
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        async void DeleteAccount()
        {
            try
            {
                int result;
                result = MessageBox.Query("Delete Account", "Are you shure to delete your account? "
                    + "This means you will lose all your data, and you can't redo this action!", "Yes", "No");
                if (result != 0) return;
                bool confirmed = InputDialog.Query("Delete Account", "Retype your password to confirm it", "Password:", "", out ustring Password);
                if (!confirmed) return;
                result = MessageBox.Query("Delete Account", "Are you really shure to delete your account? "
                    + "This means you will lose ALL YOUR DATA, and you CAN'T redo this action!", "Yes", "No");
                if (result != 0) return;
                var request = new DeleteAccountRequest
                {
                    TargetUsername = ConnectionInfo.Username,
                    Password = Password.ToString(),
                };
                var client = new AccountClient(ConnectionInfo.GrpcChannel);
                try
                {
                    var respond = await VisualGrpc.InvokeAsync(client.DeleteAccountAsync, request);
                    if (respond.Success)
                    {
                        MessageBox.Query("Success", "Delete account success!", "OK");
                        Application.RequestStop();
                    }
                    else
                    {
                        MessageBox.ErrorQuery("Fail", respond.Message, "Exit");
                    }
                }
                catch (RpcException e) when (e.StatusCode.Equals(StatusCode.Unauthenticated))
                {
                    MessageBox.ErrorQuery("Fail", "Password is incorrect", "Exit");
                }
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        void Logout()
        {
            try
            {
                VisualGrpc.ClearToken();
                ConnectionInfo.Clear();
                Application.RequestStop();
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        async void GenerateCode()
        {
            try
            {
                var request = new GenerateActivationCodeRequest
                {
                    Number = 1,
                };
                var client = new AccountClient(ConnectionInfo.GrpcChannel);
                var respond = await VisualGrpc.InvokeAsync(client.GenerateActivationCodeAsync, request);
                if (respond.Success)
                {
                    MessageBox.Query("Success", respond.ActivationCode, "OK");
                }
                else
                {
                    MessageBox.ErrorQuery("Fail", respond.Message, "Exit");
                }
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        readonly MenuItem ChangePasswordItem = new()
        {
            Title = "Change Password",
            //Help = "Change your password",
        };

        readonly MenuItem ChangeUsernameItem = new()
        {
            Title = "Change Username",
            //Help = "Change your username",
        };

        readonly MenuItem DeleteAccountItem = new()
        {
            Title = "Delete Account",
            //Help = "Delete your account",
        };

        readonly MenuItem LogoutItem = new()
        {
            Title = "Logout",
            //Help = "Logout of your account",
        };

        readonly MenuItem? GenerateCodeItem = new()
        {
            Title = "Generate Code",
            //Help = "Generate an activation code",
        };
    }
}
