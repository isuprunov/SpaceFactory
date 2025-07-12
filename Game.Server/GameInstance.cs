namespace Game.Server;

public class GameInstance
{
    private readonly GameData _gameData;
    public string Id { get; set; }
    private List<Player> Players { get; set; } = [];
    private List<Zone> Zones { get; set; }

    public Player AddPlayers(string playerId)
    {
        var newPlayer = new Player(_gameData, playerId);
        newPlayer.SetZone(Zones.First());
        Players.Add(newPlayer);
        return newPlayer;
    }

    public GameInstance(GameData gameData)
    {
        _gameData = gameData;
        Zones =
        [
            new Zone()
            {
                Deposits = new Dictionary<string, Deposit>()
                {
                    {
                        gameData.IronOre.Id,
                        new Deposit
                        {
                            Count = 10000,
                            FirstCount = 10000,
                            BeginPerformance = .7,
                            Slots = 3,
                            ResourceType = gameData.IronOre,
                            UsedSlots = 0
                        }
                    }
                }
            }
        ];
    }

    public Task Start(Mutex mutex) =>
        Task.Run(async () =>
        {
            while (true)
            {
                mutex.WaitOne();
                foreach (var zone in Zones)
                    foreach (var (_,deposit) in zone.Deposits) 
                        deposit.UsedSlots = 0;
                foreach (var player in Players)
                    player.Turn();
                mutex.ReleaseMutex();
                await Task.Delay(100);
            }
        });
}