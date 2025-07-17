using Microsoft.OpenApi.Models;

namespace Game.Server;

public static class Program
{
    public static async Task Main()
    {
        var mutex = new Mutex();

        
        var games = new Dictionary<string, GameInstance>();
        var players = new Dictionary<string, Player>();
        var gamesByPlayer = new Dictionary<string, string>();
        var connectedPlayers = new Dictionary<string, Player>();
        var gameData = new GameData();


        var builder = WebApplication.CreateBuilder();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.UseAllOfForInheritance();
            c.UseAllOfToExtendReferenceSchemas();
            c.UseOneOfForPolymorphism(); // <-- ключевая строка
        });

        var app = builder.Build();

        app.MapGet("/create-game", (string gameName) =>
        {
            var newGame = new GameInstance(gameData)
            {
                Id = gameName,
            };
            games.Add(gameName, newGame);
        });

        app.MapGet("/create-player", (string gameName) =>
        {
            var newPlayerId = Guid.NewGuid().ToString();

            var newPlayer = games[gameName].AddPlayers(newPlayerId);
            players.Add(newPlayerId, newPlayer);
            gamesByPlayer[newPlayerId] = gameName;
            return newPlayerId;
        });

        app.MapGet("/start-game", (string gameName) =>
        {
            var game = games[gameName];
            game.Start(mutex);
        });

        app.MapGet("/api/GetModelState", (HttpContext ctx) =>
            {
                var player = players[ctx.Request.Headers["playerId"]!];
                mutex.WaitOne();
                var res = player.GetModelState(); // возвращает List<IAnswer>
                mutex.ReleaseMutex();
                return Results.Ok(res);
            })
            .Produces<List<Answer>>();
        

        PlayerEndpoint.RegisterEndpoint(app, players, mutex);

        app.UseSwagger();
        app.UseSwaggerUI();


        await app.RunAsync();
    }
    
}
