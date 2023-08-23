using Terminal.Gui;
using client.Gui;
using client;

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
        };
        Application.Run(loginWindow);
    };

    Application.Top.Add(welcomeWindow);
    try
    {
        Application.Run();
    }
    catch(Exception e)
    {
        ExceptionDialog.Show(e);
    }
    Application.Shutdown();
}
