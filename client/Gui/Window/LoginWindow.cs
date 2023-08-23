/*  
 *  Namespace   :   client.Gui
 *  Filename    :   LoginWindow.cs
 *  Class       :   LoginWindow
 *  
 *  Creater     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

using Grpc.Net.Client;
using NStack;
using server.Protos;
using Terminal.Gui;

namespace client.Gui
{
    /// <summary>
    /// The login window of GHG Online Client
    /// </summary>
    /// <remarks>
    /// When login finished, you can get the JWT token of the user from the Token property.
    /// You need the token to access the server.
    /// </remarks>
    public class LoginWindow : Window
    {
        /// <summary>
        /// The GrpcChannel of the server, used to create a AccountClient
        /// </summary>
        public GrpcChannel GrpcChannel;

        /// <summary>
        /// The username that user input, null if login failed
        /// </summary>
        public string? Username = null;

        /// <summary>
        /// The JWT token of the user, null if login failed
        /// </summary>
        public string? Token = null;

        /// <summary>
        /// Event raised when login success
        /// </summary>
        /// <remarks>
        /// The string in the event is the JWT token of the user,
        /// which is the same as the Token property.
        /// </remarks>
        public event Action<GrpcChannel, string> OnLoginSuccess;

        /// <summary>
        /// Create a new LoginWindow
        /// </summary>
        /// <param name="grpcChannel">You need to pass a GrpcChannel as argument to create a login window.</param>
        public LoginWindow(GrpcChannel grpcChannel)
        {
            Width = 48;
            Height = 11;
            X = Pos.Center();
            Y = Pos.Center();
            Modal = true;
            Border.Effect3D = true;
            Title = "GHG Online Login";
            this.Add(loginToServerLabel, usernameLabel, usernameField, passwordLabel
                , passwordField, registerButton, loginButton);
            registerButton.Clicked += OnRegisterButtonClicked;
            loginButton.Clicked += OnLoginButtonClicked;
            KeyPress += OnKeyPress;
            OnLoginSuccess += (_, _) => { };
            GrpcChannel = grpcChannel;
            accountClient = new Account.AccountClient(GrpcChannel);
        }

        void OnRegisterButtonClicked()
        {
            var registerWindow = new RegisterWindow(GrpcChannel);
            Application.Run(registerWindow);
        }

        async void OnLoginButtonClicked()
        {
            try
            {
                var loginRequest = new LoginRequest()
                {
                    Username = usernameField.Text.ToString(),
                    Password = passwordField.Text.ToString(),
                };
                var loginResponse = await VisualGrpc.InvokeAsync(accountClient.LoginAsync, loginRequest);
                if (loginResponse.Success)
                {
                    Token = loginResponse.JwtToken;
                    Username = loginRequest.Username;
                    OnLoginSuccess(GrpcChannel, Token);
                }
                else
                {
                    //Application.MainLoop.Invoke(() =>
                    //{
                    int index = MessageBox.ErrorQuery("Login failed", loginResponse.Message, "Exit", "Retry");
                    if (index == 0 || index == -1) Application.RequestStop();
                    //});
                }
            }
            catch (Exception e)
            {
                ExceptionDialog.Show(e);
            }
        }

        private void OnKeyPress(KeyEventEventArgs obj)
        {
            if (obj.KeyEvent.Key == Key.Esc)
            {
                Application.RequestStop();
            }
        }

        readonly Account.AccountClient accountClient;

        readonly private Terminal.Gui.Label loginToServerLabel = new()
        {
            Width = 4,
            Height = 1,
            X = Pos.Center(),
            Y = 1,
            Data = "createYourOwnAccountLabel",
            Text = "Login to a GHG Online Server",
            TextAlignment = Terminal.Gui.TextAlignment.Left,
        };

        readonly private Terminal.Gui.Label usernameLabel = new()
        {
            Width = 4,
            Height = 1,
            X = 4,
            Y = 3,
            Data = "usernameLabel",
            Text = "Username: ",
            TextAlignment = Terminal.Gui.TextAlignment.Left,
        };

        readonly private Terminal.Gui.TextField usernameField = new()
        {
            Width = 27,
            Height = 1,
            X = 14,
            Y = 3,
            Secret = false,
            Data = "usernameField",
            Text = "",
            TextAlignment = Terminal.Gui.TextAlignment.Left,
        };

        readonly private Terminal.Gui.Label passwordLabel = new()
        {
            Width = 4,
            Height = 1,
            X = 4,
            Y = 5,
            Data = "passwordLabel",
            Text = "Password:",
            TextAlignment = Terminal.Gui.TextAlignment.Left,
        };

        readonly private Terminal.Gui.TextField passwordField = new()
        {
            Width = 27,
            Height = 1,
            X = 14,
            Y = 5,
            Secret = false,
            Data = "passwordField",
            Text = "",
            TextAlignment = Terminal.Gui.TextAlignment.Left,
        };

        readonly private Terminal.Gui.Button registerButton = new()
        {
            Width = 12,
            Height = 1,
            X = 18,
            Y = 7,
            Data = "registerButton",
            Text = "Register",
            TextAlignment = Terminal.Gui.TextAlignment.Centered,
            IsDefault = false,
        };

        readonly private Terminal.Gui.Button loginButton = new()
        {
            Width = 9,
            Height = 1,
            X = 33,
            Y = 7,
            Data = "registerButton",
            Text = "Login",
            TextAlignment = Terminal.Gui.TextAlignment.Centered,
            IsDefault = false,
        };
    }
}
