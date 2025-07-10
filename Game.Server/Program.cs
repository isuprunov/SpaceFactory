using System.Net.Sockets;
using System.Threading.Channels;
using Game.UI;
using Game.UI.Events;
namespace Game.Server;

public static class Program
{
    public static async Task Main()
    {
        var games = new Dictionary<string, GameInstance>();
        var players = new Dictionary<string, Player>();
        var gamesByPlayer = new Dictionary<string, string>();
        var connectedPlayers = new Dictionary<string,Player>();
        var gameData = new GameData();

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();


        _ = Task.Run(async () =>
        {
            var tcpListener = TcpListener.Create(12345);
            tcpListener.Start();
            var client = await tcpListener.AcceptTcpClientAsync();
            _ = Task.Run(async () =>
            {
                var networkStream = client.GetStream();
                using StreamWriter sw = new StreamWriter(networkStream);
                using StreamReader sr = new StreamReader(networkStream);
                var playerId = await sr.ReadLineAsync();
                await foreach (var i in players[playerId].Channel.Reader.ReadAllAsync())
                {
                    
                }
            });
        });

        app.MapGet("/create-game", (string gameName) =>
        {
            var newGame = new GameInstance()
            {
                Id = gameName,
            }; 
            games.Add(gameName, newGame);
    
        });

        app.MapGet("/create-player", (string gameName) =>
        {
            var newPlayerId = Guid.NewGuid().ToString(); 
            var newPlayer = new Player(Update, gameData)
            {
                Id = newPlayerId
            };
            players.Add(newPlayerId, newPlayer);
            gamesByPlayer[newPlayerId] = gameName;
            games[gameName].Players.Add(newPlayer); 
            return newPlayerId;
        });



        app.MapGet("/start-game", (string gameName) =>
        {
            var game = games[gameName];
            game.Start();
            // if (game.Players.All(m => connectedPlayers.ContainsKey(m.Id)))
            // {
            //     game.Start();
            //     return "Ok";
            // }
            // else return "Not all connected players";
        });

        app.MapGet("/create-machine", (string gameName) =>
        {

        });

        await app.RunAsync();
    }
    
    static void Update(UpdateModel update)
    {
    
    }

}





public class GameInstance
{
    public string Id { get; set; }
    public List<Player> Players { get; set; } = new();
    
    public void Connect(string playerId)
    {
        
    }

    public void Start()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                foreach (var player in Players)
                    player.Turn();

                await Task.Delay(100);
            }
        });
    }
}

