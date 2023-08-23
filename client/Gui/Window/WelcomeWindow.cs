/*  
 *  Namespace   :   client.Gui
 *  Filename    :   VisualGrpc.cs
 *  Class       :   VisualGrpc
 *  
 *  Creater     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

using Terminal.Gui;
using Grpc.Net.Client;
using System.Net;

namespace client.Gui
{
    /// <summary>
    /// The welcome window. Used to connect to a server.
    /// </summary>
    public class WelcomeWindow : Window
    {
        /// <summary>
        /// If the connection is successful, this field will be set to the GrpcChannel.
        /// Otherwise, it will be null.
        /// </summary>
        public GrpcChannel? GrpcChannel = null;


        /// <summary>
        /// The event that will be triggered when the connection is successful.
        /// </summary>
        public event Action<GrpcChannel> OnConnectSuccess;

        /// <summary>
        /// Create a new WelcomeWindow.
        /// </summary>
        public WelcomeWindow()
        {
            Title = "GHG Online";
            X = Pos.Center() - 20;
            Y = Pos.Center() - 5;
            Width = 40;
            Height = 12;
            this.Modal = true;
            this.Border.BorderStyle = Terminal.Gui.BorderStyle.Single;
            this.Border.BorderBrush = Terminal.Gui.Color.Black;
            this.Border.Effect3D = true;
            this.Border.Effect3DBrush = null;
            this.Border.DrawMarginFrame = true;
            this.Add(welcomeLabel, serverUrlLable, urlBox, useLabel, versionGroup, connectButton);
            connectButton.Clicked += OnConnectButtonClicked;
            OnConnectSuccess += (channel) => { };
        }

        async void OnConnectButtonClicked()
        {
            string url = urlBox.Text.ToString() ?? "";
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                MessageBox.ErrorQuery("Error", "Invalid URL", "OK");
            else
            {
                try
                {
                    HttpClient.DefaultProxy = new WebProxy(); // Reset proxy to ensure http version is as configured
                    if (versionGroup.SelectedItem == 0)
                        GrpcChannel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new HttpClientHandlerForceToUseHttp1_1() });
                    else
                        GrpcChannel = GrpcChannel.ForAddress(url);
                    var client = new server.Protos.Account.AccountClient(GrpcChannel);
                    _ = await VisualGrpc.InvokeAsync(client.PingAsync, new server.Protos.PingRequest(), 0, -1);
                    OnConnectSuccess(GrpcChannel);
                }
                catch (Exception e)
                {
                    ExceptionDialog.Show(e);
                    GrpcChannel = null;
                }
            }
        }


        /*
        
        // This version of OnConnectButtonClicked is once used to use ConnectingWindow to connect to server.
        // But it is not used now because we have VisualGrpc.InvokeAsync.

        void OnConnectButtonClicked()
        {
            string url = urlBox.Text.ToString() ?? "";
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                MessageBox.ErrorQuery("Error", "Invalid URL", "OK");
            else
            {
                ConnectingWindow connectingWindow;
                if (versionGroup.SelectedItem == 0)
                    connectingWindow = new ConnectingWindow(url, true);
                else
                    connectingWindow = new ConnectingWindow(url, false);
                Application.Run(connectingWindow);
                if (connectingWindow.GrpcChannel is not null)
                {
                    GrpcChannel = connectingWindow.GrpcChannel;
                    var loginWindow = new LoginWindow(GrpcChannel);
                    Application.Run(loginWindow);
                    if (loginWindow.Token is not null)
                    {
                        MessageBox.Query("Login Success", $"Token:\n{loginWindow.Token}", "OK");
                    }
                }
            }
        }
        */

        readonly Label welcomeLabel = new()
        {
            Text = "Welcome to GHG Online", // 21 characters
            X = Pos.Center(),
            Y = 1,
            Width = 21,
            Height = 1
        };

        readonly Label serverUrlLable = new()
        {
            Text = "Server URL:", // 11 characters
            X = 4,
            Y = 3,
            Width = 29,
            Height = 1,
        };

        readonly TextField urlBox = new()
        {
            X = 4,
            Y = 4,
            Width = 29,
            Height = 1,
            Text = "https://grpc.ghg.org.cn",
        };

        readonly Label useLabel = new()
        {
            Text = "Use:", // 4 characters
            X = 4,
            Y = 5,
            Width = 4,
            Height = 1,
        };

        readonly RadioGroup versionGroup = new()
        {
            X = 8,
            Y = 5,
            Width = 20,
            Height = 1,
            RadioLabels = new NStack.ustring[] { "HTTP/1.1", "HTTP/2" }, // 8 characters
        };

        readonly Button connectButton = new()
        {
            Text = "Connect", // 7 characters
            X = Pos.Center(),
            Y = 8,
            Width = 7,
            Height = 1,
        };
    }
}
