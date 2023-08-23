/*  
 *  Namespace   :   client.Gui
 *  Filename    :   ConnectingWindow.cs
 *  Class       :   ConnectingWindow
 *  
 *  Creator     :   Nictheboy
 *  Create at   :   2023/08/22
 *  Last Modify :   2023/08/22
 *  
 */

using Grpc.Net.Client;
using System.Net;
using Terminal.Gui;

namespace client.Gui
{
    /// <summary>
    /// This window is used to test whether the server is available, and get a GrpcChannel, with a beautiful UI.
    /// </summary>
    /// <remarks>
    /// <para>
    /// To use it, simply create an instance of this class, and call Application.Run() on it.
    /// </para>
    /// <para>
    /// When the window is closed, the GrpcChannel is not disposed. You can get it from the <c>GrpcChannel</c> field.
    /// If the <c>GrpcChannel</c> field is null, the connection is failed.
    /// </para>
    /// </remarks>
    [Obsolete("This class is obsolete. Please use VisualGrpc.Invoke() to test a channel instead.")]
    public class ConnectingWindow : Window
    {
        /// <summary>
        /// The created channel will be stored here.
        /// </summary>
        public GrpcChannel? GrpcChannel = null;


        /// <summary>
        /// Create a new instance of <c>ConnectingWindow</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This window is used to test whether the server is available, and get a GrpcChannel, with a beautiful UI.
        /// </para>
        /// <para>
        /// To use it, simply create an instance of this class, and call Application.Run() on it.
        /// </para>
        /// <para>
        /// When the window is closed, the GrpcChannel is not disposed. You can get it from the <c>GrpcChannel</c> field.
        /// If the <c>GrpcChannel</c> field is null, the connection is failed.
        /// </para>
        /// </remarks>
        /// 
        /// 
        /// <param name="Url">Url of the server</param>
        /// <param name="UseHttp1Force">
        /// If it's true, connection uses HTTP/1.1.
        /// Otherwise, it uses HTTP/2, which is the default protocol of gRPC.
        /// </param>
        public ConnectingWindow(string Url, bool UseHttp1Force)
        {
            this.url = Url;
            this.useHttp1Force = UseHttp1Force;

            X = Pos.Center();
            Y = Pos.Center();
            Width = 30;
            Height = 3;
            this.Border.BorderStyle = Terminal.Gui.BorderStyle.Single;
            this.Border.BorderBrush = Terminal.Gui.Color.Black;
            this.Border.Effect3D = true;
            this.Border.Effect3DBrush = null;
            this.Border.DrawMarginFrame = true;
            this.Modal = true;
            this.Add(connectingLabel, cancelButton);
            this.Initialized += OnInitialized;
            cancelButton.Clicked += OnCancelButtonClicked;
        }

        async void OnInitialized(Object? sender, EventArgs args)
        {
            try
            {
                HttpClient.DefaultProxy = new WebProxy(); // Reset proxy to ensure http version is as configured
                if (useHttp1Force)
                    GrpcChannel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new HttpClientHandlerForceToUseHttp1_1() });
                else
                    GrpcChannel = GrpcChannel.ForAddress(url);
                var client = new server.Protos.Account.AccountClient(GrpcChannel);
                var respond = await client.PingAsync(new server.Protos.PingRequest());
                RequestStop();
            }
            catch (Exception e)
            {
                if (!canceled) // if connection is not cancelled
                {
                    ExceptionDialog.Show(e);
                    GrpcChannel = null;
                    RequestStop();
                }
            }
        }

        bool canceled = false;
        readonly string url;
        readonly bool useHttp1Force;

        void OnCancelButtonClicked()
        {
            canceled = true;
            GrpcChannel?.Dispose();
            GrpcChannel = null;
            RequestStop();
        }

        readonly Label connectingLabel = new()
        {
            Text = "Connecting...", // 13 characters
            X = 0,
            Y = 0,
            Width = 13,
            Height = 1,
        };

        readonly Button cancelButton = new()
        {
            Text = "Cancel", // 6 characters
            X = 17,
            Y = 0,
            Width = 6,
            Height = 1,
        };
    }
}
