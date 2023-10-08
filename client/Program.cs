using Terminal.Gui;
using client.Gui;
using client;
using Console = client.Gui.Console.Console;
using client.App;
using client.Api;
using client.Gui.StatusBar;

Exception? exception = null;
while (true)
{
    Application.Init();

    WelcomeWindow welcomeWindow = new();
    welcomeWindow.OnConnectSuccess += (channel) =>
    {
        var loginWindow = new LoginWindow(channel);
        loginWindow.OnLoginSuccess += (channel, token) =>
        {
            loginWindow.RequestStop();
            Application.Top.RemoveAll();
            VisualGrpc.LoadToken(token);
            ConnectionInfo.LoadGrpcChannel(channel);
            ConnectionInfo.LoadUsername(loginWindow.Username!);
            Application.Top.Add(new MenuBar(new[] { new AccountMenu() }));
            Application.Top.Add(new StatusBar(new[] { VisualGrpcStatusItem.Instance }));

            // Calculate height and width for console
            // Console needs a fixed display size, which is requirement of Terminal.ScreenLibrary library
            var view = new View()
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
            };
            Application.Top.Add(view);
            Application.Refresh();
            int width = view.Frame.Width;
            int height = view.Frame.Height;
            Application.Top.Remove(view);

            var console = new Console(width, height, Color.Green, Color.Black)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            Application.Top.Add(console);
            console.Run((pipe) =>
            {
                var RootApi = GhgApi.CreateInstanceWithGlobalConfiguration(pipe);
                new ApplicationWrapper(RootApi, new GhgMain(RootApi)).Run();
            });
        };
        Application.Run(loginWindow);
    };

    Application.Top.Add(welcomeWindow);
    try
    {
        if (exception is not null)
            ExceptionDialog.Show(exception);
        Application.Run();
    }
    catch (Exception e)
    {
        exception = e;
    }
    Application.Shutdown();
}
