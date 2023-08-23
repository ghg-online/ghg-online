/*  
 *  Namespace   :   client.Gui
 *  Filename    :   RegisterWindow.cs
 *  Class       :   RegisterWindow
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
    /// The register window of GHG Online Client.
    /// </summary>
    public class RegisterWindow : Window
    {
        /// <summary>
        /// The gRPC channel of the client.
        /// </summary>
        public GrpcChannel GrpcChannel;

        /// <summary>
        /// Modify this to change the info of the activation code.
        /// </summary>
        const string info = "You must have an activation code to register."
                + "If you don't have one, please contact the server administrator.";

        /// <summary>
        /// Event that will be triggered when the register is successful.
        /// </summary>
        public event Action<GrpcChannel> OnRegisterSuccess;

        /// <summary>
        /// Create a new register window.
        /// </summary>
        /// <param name="grpcChannel">
        /// You need to pass the gRPC channel of the client.
        /// </param>
        public RegisterWindow(GrpcChannel grpcChannel)
        {
            Width = 48;
            Height = 11;
            X = Pos.Center();
            Y = Pos.Center();
            Modal = true;
            this.Border.Effect3D = true;
            Title = "GHG Online Register";
            this.Add(createYourOwnAccountLabel, usernameLabel, usernameField, passwordLabel
                , passwordField, registerButton);
            Initialized += OnInitialized;
            registerButton.Clicked += OnRegisterButtonClicked;
            KeyPress += OnKeyPress;
            OnRegisterSuccess += (GrpcChannel grpcChannel) => { };
            GrpcChannel = grpcChannel;
            accountClient = new Account.AccountClient(GrpcChannel);
        }

        void OnInitialized(object? sender, EventArgs e)
        {
            GetActivationCode();
        }

        async void OnRegisterButtonClicked()
        {
            try
            {
                if (activationCode is null)
                {
                    //Application.MainLoop.Invoke(() => MessageBox.ErrorQuery("Error", info, "OK"));
                    MessageBox.ErrorQuery("Error", info, "OK");
                    return;
                }
                var request = new RegisterRequest
                {
                    Username = usernameField.Text.ToString(),
                    Password = passwordField.Text.ToString(),
                    ActivationCode = activationCode
                };
                var respond = await accountClient.RegisterAsync(request);
                //Application.MainLoop.Invoke(() =>
                //{
                if (respond.Success)
                {
                    MessageBox.Query("Success", "Your account has been created.", "OK");
                    OnRegisterSuccess(GrpcChannel);
                }
                else
                {
                    MessageBox.ErrorQuery("Error", respond.Message, "OK");
                }
                //});
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

        void GetActivationCode()
        {
            bool result = InputDialog.Query("Activation Code", info, "Code: "
                , "Xxxxxxxx-Xxxx-Xxxx-Xxxx-Xxxxxxxxxxxx", out ustring input);
            if (result)
                activationCode = input.ToString();
            else
                Application.RequestStop();
        }

        readonly Account.AccountClient accountClient;

        string? activationCode = null;

        readonly private Terminal.Gui.Label createYourOwnAccountLabel = new()
        {
            Width = 4,
            Height = 1,
            X = Pos.Center(),
            Y = 1,
            Data = "createYourOwnAccountLabel",
            Text = "Create your own account!",
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
            Width = 9,
            Height = 1,
            X = 30,
            Y = 7,
            Data = "registerButton",
            Text = "Register",
            TextAlignment = Terminal.Gui.TextAlignment.Centered,
            IsDefault = false,
        };
    }
}
