using Game.UI;
using Machine = System.Reflection.PortableExecutable.Machine;

namespace Game.Test;

[Parallelizable(ParallelScope.All)]
public class Tests
{
    private readonly GameData _gameData = new GameData();
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var player = new Player(_gameData.AllResources)
        {
            MachineUnits =
            [
                new Machine(_gameData.Miner, _gameData.MineIronOre),
            ]
        };
        player.Turn();
        Assert.That(player.Resources[_gameData.IronOre], Is.EqualTo(1));
    }
    
    [Test]
    public void Test2()
    {
        var player = new Player(_gameData.AllResources)
        {
            MachineUnits =
            [
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
            ]
        };
        for(var i =0; i < 6; i++)
            player.Turn();
        Assert.That(player.Resources[_gameData.IronOre], Is.EqualTo(3));
        Assert.That(player.Resources[_gameData.IronPlate], Is.EqualTo(1));
    }
    
    [Test]
    public void Test3()
    {
        var player = new Player(_gameData.AllResources)
        {
            MachineUnits =
            [
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
            ]
        };
        player.Resources[_gameData.IronOre] = 100;
        for(var i =0; i < 9; i++)
            player.Turn();
        Assert.That(player.Resources[_gameData.IronOre], Is.EqualTo(91));
        Assert.That(player.Resources[_gameData.IronPlate], Is.EqualTo(12));
    }
    [Test]
    public void Test4()
    {
        var player = new Player(_gameData.AllResources)
        {
            MachineUnits =
            [
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Miner, _gameData.MineIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
                new Machine(_gameData.Smelter, _gameData.MeltIronOre),
            ]
        };
        for(var i =0; i < 6; i++)
            player.Turn();
        Assert.That(player.Resources[_gameData.IronOre], Is.EqualTo(4));
        Assert.That(player.Resources[_gameData.IronPlate], Is.EqualTo(1));
    }
}