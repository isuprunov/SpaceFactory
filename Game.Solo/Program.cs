// See https://aka.ms/new-console-template for more information

using System.Net.Sockets;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        _ = Task.Run(() => Task.FromResult(Game.Server.Program.Main()));
        Task.Delay(1000);

        Game.UI.Program.Main([]);
    }
}



//
// var gameName = "game1";
// var client = RestService.For<IGameServer>("http://localhost:5000");
// await client.CreateGame(gameName);
// var playerId = await client.CreatePlayer(gameName);
// await client.StartGame(gameName);
//
// while (true)
// {
//     var model = await client.UpdateModel(playerId);
//     await Task.Delay(50);
// }

// var tcpClient = new TcpClient();
// await tcpClient.ConnectAsync("localhost", 12345);
// var networkStream = tcpClient.GetStream();
// using StreamWriter sw = new StreamWriter(networkStream);
// using StreamReader sr = new StreamReader(networkStream);
// await sw.WriteLineAsync(playerId);
// await sw.FlushAsync();



//
// await Task.Delay(1000000);
// Console.WriteLine("Hello, World!");
//
// public interface IGameServer
// {
//     [Get("/create-game")]
//     public Task CreateGame(string gameName);
//     
//     [Get("/create-player")]
//     public Task<string> CreatePlayer(string gameName);
//     
//     [Get("/start-game")]
//     public Task StartGame(string gameName);
//
//     [Get("/update-state")]
//     public Task<UpdateModel> UpdateModel(string playerId);
// }