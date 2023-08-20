using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using Grpc.Net.Client;
using server.Protos;
using System.Net;

namespace client.Gui
{
    internal class Welcome : Window
    {
        GrpcChannel? channel = null;

        async void OnConnectButtonClicked()
        {
            string url = urlBox.Text.ToString() ?? "";
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
                MessageBox.ErrorQuery("Error", "Invalid URL", "OK");
            else
            {
                connectingFrame.Visible = true;
                urlBox.Enabled = false;
                connectButton.Enabled = false;
                try
                {
                    HttpClient.DefaultProxy = new WebProxy(); // Reset proxy to ensure http version is as configured
                    if (versionGroup.SelectedItem == 0)
                        channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = new HttpClientHandlerForceToUseHttp1_1() });
                    else
                        channel = GrpcChannel.ForAddress(url);
                    var client = new server.Protos.Account.AccountClient(channel);
                    var respondAsync = await client.PingAsync(new server.Protos.PingRequest()).ResponseAsync;
                    MessageBox.Query("Congratulation", "Connection success!", "OK");
                    // todo
                }
                catch (Exception e)
                {
                    MessageBox.ErrorQuery("Exception", e.Message, "OK");
                    OnCancelButtonClicked();
                }
            }
        }

        void OnCancelButtonClicked()
        {
            channel?.Dispose();
            channel = null;
            connectingFrame.Visible = false;
            urlBox.Enabled = true;
            connectButton.Enabled = true;
        }

        public Welcome()
        {
            X = Pos.Center() - 20;
            Y = Pos.Center() - 5;
            Width = 40;
            Height = 12;
            this.Add(welcomeLabel, serverUrlLable, urlBox, useLabel, versionGroup, connectButton, connectingFrame);
            connectingFrame.Add(connectingLable, cancelButton);
            connectButton.Clicked += OnConnectButtonClicked;
            cancelButton.Clicked += OnCancelButtonClicked;
        }



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



        // MainFrame for connecting
        readonly FrameView connectingFrame = new()
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = 30,
            Height = 3,
            Visible = false,
        };

        readonly Label connectingLable = new()
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
