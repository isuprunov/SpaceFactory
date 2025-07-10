using System.Reflection;
using Avalonia.Media.Imaging;
using Game.UI.Events;

namespace Game.UI;

public interface IId
{
    public string Id { get; init; }
}



public enum ResourceFormat
{
    Particles,
    Plates,
    Gas,
    Liquid
}

public record class ResourceType : IId
{
    public ResourceType(string Id, ResourceFormat Format, Bitmap? Image = null!)
    {
        this.Id = Id;
        this.Format = Format;
        this.Image = Image;
    }

    public string Id { get; init; }
    public ResourceFormat Format { get; init; }
    public Bitmap? Image { get; set; }

    public void Deconstruct(out string Id, out ResourceFormat Format, out Bitmap? Image)
    {
        Id = this.Id;
        Format = this.Format;
        Image = this.Image;
    }
}

public record Resource(ResourceType ResourceType, double Count)
{
    public double Count { get; set; } = Count;
}

public record Recept(string Id, List<Resource> SourceRecepts, List<Resource> DestinationRecepts, double Speed) : IId;

public record MachineType(string Id, List<Recept> AvailableProcesses) : IId;

public record Machine(MachineType MachineType, Recept? CurrentResourceProcess = null)
{
    public void Work(Dictionary<string, Resource> resources)
    {
        if(CurrentResourceProcess == null)
            return;
        var allNeedResourcesHas = CurrentResourceProcess.SourceRecepts.All(partRecept => resources[partRecept.ResourceType.Id].Count >= partRecept.Count * Count* CurrentResourceProcess.Speed);
        if (allNeedResourcesHas)
        {
            foreach (var partRecept in CurrentResourceProcess.SourceRecepts)
                resources[partRecept.ResourceType.Id].Count -= partRecept.Count * Count * CurrentResourceProcess.Speed;
            foreach (var partRecept in CurrentResourceProcess.DestinationRecepts)
                resources[partRecept.ResourceType.Id].Count += partRecept.Count * Count * CurrentResourceProcess.Speed;
        }
    }

    public required int Count { get; set; }
    public Recept? CurrentResourceProcess { get; set; } = CurrentResourceProcess;
    public required string Id { get; set; }
}



public class Player
{
    public string Id { get; init; }
    private readonly Action<UpdateModel> _update;

    public Player(Action<UpdateModel> update, GameData gameData)
    {
        _update = update;
        Resources = gameData.AllResources.ToDictionary(m => m.Id, m => new Resource(m, 0));
        MachineTypes = gameData.AllMachinesTypes.ToDictionary(m => m.Id, m=> m);
        Recepts = gameData.AllRecepts.ToDictionary(m=> m.Id, m=> m);
        Machines = new Dictionary<string, Machine>();
        CreateMachine(gameData.Miner, gameData.MineIronOre);
        update(new UpdateModel
        {
            InitVm = new InitVm()
            {
                ResourceItems = Resources.Select(pair => ResourceItem.CreateViewModel(pair.Value))
                    .ToArray(),
                ReceptItems = gameData.AllRecepts.Select(ReceptItem.CreateViewModel).ToArray(),
                MachineTypeItems = gameData.AllMachinesTypes.Select(MachineTypeItem.CreateViewModel).ToArray(),
                MachineItems = Machines.Select(m=> m.Value).Select(MachineItem.CreateViewModel).ToArray()
            },
        });
    }

    public Dictionary<string, Resource> Resources { get; set; }
    public Dictionary<string, Recept> Recepts { get; set; }
    public Dictionary<string, Machine> Machines { get; set; }
    public Dictionary<string, MachineType> MachineTypes { get; set; }

    private int _globalId = 0;


    private Machine CreateMachine(MachineType machineType, Recept? recept)
    {
        var machine = new Machine(machineType, recept)
        {
            Id = $"Id{Interlocked.Increment(ref _globalId)}",
            Count = 1,
        };
        Machines.Add(machine.Id, machine);
        return machine;
    }
    
    public void CreateMachine(string machineTypeId, string? receptId)
    {
        Recept? recept = null;
        if(receptId != null)
            recept = Recepts[receptId];
        _update(new UpdateModel()
        {
            CreateMachine = MachineItem.CreateViewModel(CreateMachine(MachineTypes[machineTypeId], recept))
        });
    }
    
    public void ChangeRecept(string machineId, string? receptId)
    {
        Recept? recept = null;
        if(receptId != null)
            recept = Recepts[receptId];
        var machine = Machines[machineId];
        machine.CurrentResourceProcess = recept;
        _update(new UpdateModel()
        {
            ChangeRecept = new ChangeRecept(machine.Id, recept?.Id) 
        });
    }
    
    public void IncrementCountMachine(Machine machine)
    {
        machine.Count++;
    }

    public void Turn()
    {
        foreach (var (_, machine) in Machines)
            machine.Work(Resources);
    }

    // public bool RemoveMachine(Machine machine)
    // {
    //     machine.Count--;
    //     if (machine.Count != 0) 
    //         return false;
    //     Machines.Remove(machine);
    //     return true;
    // }
    
}

public class GameData
{
    // Resources
    public readonly ResourceType IronOre;
    public readonly ResourceType CoupleOre;
    public readonly ResourceType Coal;
    public readonly ResourceType Stone;
    public readonly ResourceType Oil;

    public readonly ResourceType IronPlate;
    public readonly ResourceType CouplePlate;
    public readonly ResourceType Brick;
    public readonly ResourceType Conductor;

    public readonly List<ResourceType> AllResources;

    // Processes
    public readonly Recept MineIronOre;
    public readonly Recept MineCoupleOre;
    public readonly Recept MeltIronOre;
    public readonly Recept MeltCoupleOre;
    public readonly Recept ProductionConductor;
    public Recept NoneRecept { get; set; }

    public readonly List<Recept> AllRecepts;

    // Machines
    public readonly MachineType Miner;
    public readonly MachineType Smelter;
    public readonly MachineType BasicConstructor;

    public readonly List<MachineType> AllMachinesTypes;

    

    public GameData()
    {
        Oil= new ResourceType("Oil", ResourceFormat.Liquid);
        IronOre = new ResourceType("IronOre", ResourceFormat.Particles);
        CoupleOre = new ResourceType("CoupleOre", ResourceFormat.Particles);
        Coal = new ResourceType("Coal", ResourceFormat.Particles);
        Stone = new ResourceType("Stone", ResourceFormat.Particles);
        
        IronPlate = new ResourceType("IronPlate", ResourceFormat.Plates);
        CouplePlate = new ResourceType("CouplePlate", ResourceFormat.Plates);
        Conductor = new ResourceType("Conductor", ResourceFormat.Plates);
        Brick= new ResourceType("Brick", ResourceFormat.Plates);

        AllResources = [Oil, IronOre, CoupleOre, Coal, Stone, IronPlate, CouplePlate, Conductor, Brick];
            
        MineIronOre = new Recept("MineIronOre", [], [new Resource(IronOre, 1)], 1);
        MineCoupleOre = new Recept("MineCoupleOre", [], [new Resource(CoupleOre, 1)], 1);

        MeltIronOre = new Recept("MeltIronOre", [new Resource(IronOre, 3)], [new Resource(IronPlate, 1)], .1);
        MeltCoupleOre = new Recept("MeltCoupleOre", [new Resource(CoupleOre, 3)], [new Resource(CouplePlate, 1)], 10);
        NoneRecept = new Recept("NoneProcess", [], [], 0);
        
        AllRecepts = [MineCoupleOre, MineIronOre, MeltIronOre, MeltCoupleOre, NoneRecept];

        ProductionConductor = new Recept("ProductionConductor", [new Resource(CouplePlate, 3)], [new Resource(Conductor, 1)], 10);

        Miner = new MachineType("Miner", [MineIronOre, MineCoupleOre]);
        Smelter = new MachineType("Smelter", [MeltIronOre, MeltCoupleOre]);
        BasicConstructor = new MachineType("BasicConstructor", [ProductionConductor]);

        AllMachinesTypes = [Miner, Smelter, BasicConstructor];
        foreach (var machineType in AllMachinesTypes)
        {
            machineType.AvailableProcesses.Add(NoneRecept);
        }
    }

    
}