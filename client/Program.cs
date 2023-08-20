// .\client.exe login --url https://localhost:2333 --usr user --pwd 123456
// .\client.exe register --url https://localhost:2333 --usr user --pwd 123456 --code 2a01cf54-65bb-4ba8-99a9-71b1f4cc74e2

using Grpc.Net.Client;
using server;
using System.Net.Http;
using System.Threading.Channels;
using client;
using Terminal.Gui;
using client.Gui;

Application.Init();
Application.Top.Add(new Welcome());
Application.Run();
Application.Shutdown();
