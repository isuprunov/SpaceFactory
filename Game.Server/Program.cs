using Game.UI;
using Game.UI.Events;

var players = new List<Player>();
var gameData = new GameData();

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/create-player", () =>
{
    var newPlayer = new Player(Update, gameData)
    {
        Id = Guid.NewGuid().ToString(), 
    };
});

await app.RunAsync();


void Update(UpdateModel update)
{
    
}

