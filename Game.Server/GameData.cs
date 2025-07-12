namespace Game.Server;

public interface IId
{
    public string Id { get; init; }
}

public enum ResourceFormat
{
    Particles,
    Plates,
    Gas,
    Liquid,
}

public enum MachineKind
{
    Miner, Production
}

public record class ResourceType(string Id, ResourceFormat Format) : IId;

public record ResourceCost(ResourceType ResourceType, double Count)
{
    public double Count { get; set; } = Count;
}

public record Recept(string Id, List<ResourceCost> InResources, List<ResourceCost> OutResources, double Speed) : IId;

public record MachineType(string Id, List<Recept> AvailableProcesses, Dictionary<ResourceType, double> Cost, double Size, double Weight, MachineKind MachineKind) : IId;

public record class ResourceContainer(ResourceType ResourceType, double Count, double MaxCount)
{
    public double Count { get; set; } = Count;
    public double MaxCount { get; set; } = MaxCount;
}

public class Deposit
{
    public required double Count { get; set; }
    public required double FirstCount { get; set; }
    public double Performance => double.Max(Count / FirstCount * BeginPerformance, BeginPerformance / 10);
    public required double BeginPerformance { get; set; }
    public required int UsedSlots { get; set; }
    public required int Slots { get; set; }
    public int FreeSlots => Slots - UsedSlots;
    public required ResourceType ResourceType { get; set; }
}

public record Machine(MachineType MachineType, Recept? CurrentRecept = null)
{
    private bool CanWork(Dictionary<string, ResourceContainer> resources, int factor) 
    {
        if (CurrentRecept == null)
            return false;
        var allNeedResourcesHas = CurrentRecept.InResources.All(partRecept => resources[partRecept.ResourceType.Id].Count >= partRecept.Count * factor * CurrentRecept.Speed);
        var storageNotFull = CurrentRecept.OutResources.Any(resourceCost => resources[resourceCost.ResourceType.Id].MaxCount < 
                                                                            resources[resourceCost.ResourceType.Id].Count + resourceCost.Count * factor * CurrentRecept.Speed) == false;
        return allNeedResourcesHas && storageNotFull;
    }
    public void Work(Dictionary<string, ResourceContainer> resources)
    {
        if (CurrentRecept == null)
            return;
        var factor = Count;
        if (CanWork(resources, factor) == false) 
            return;
        foreach (var partRecept in CurrentRecept.InResources)
            resources[partRecept.ResourceType.Id].Count -= partRecept.Count * factor * CurrentRecept.Speed;
        foreach (var partRecept in CurrentRecept.OutResources)
            resources[partRecept.ResourceType.Id].Count += partRecept.Count * factor * CurrentRecept.Speed;
    }
    


    public void WorkMiner(Dictionary<string, ResourceContainer> resources, Dictionary<string, Deposit>  deposits)
    {
        if (CurrentRecept == null)
            return;
        foreach (var resourceCost in CurrentRecept.OutResources)
        {
            if(deposits.TryGetValue(resourceCost.ResourceType.Id, out var deposit) == false)
                continue;
            if(deposit.FreeSlots == 0)
                continue;
            var useSlots = Count < deposit.FreeSlots ? Count : deposit.FreeSlots;
            if (CanWork(resources, useSlots) == false) 
                continue;
            deposit.UsedSlots += useSlots;
            var countResource = resourceCost.Count * useSlots * CurrentRecept.Speed;
            deposit.Count -= countResource;
            resources[resourceCost.ResourceType.Id].Count += countResource;
        }
    }

    public required int Count { get; set; }
    public Recept? CurrentRecept { get; set; } = CurrentRecept;
    public required string Id { get; set; }
}

public class Zone()
{
    public Dictionary<string, Deposit> Deposits { get; set; }
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
        Oil = new ResourceType("Oil", ResourceFormat.Liquid);
        IronOre = new ResourceType("IronOre", ResourceFormat.Particles);
        CoupleOre = new ResourceType("CoupleOre", ResourceFormat.Particles);
        Coal = new ResourceType("Coal", ResourceFormat.Particles);
        Stone = new ResourceType("Stone", ResourceFormat.Particles);

        IronPlate = new ResourceType("IronPlate", ResourceFormat.Plates);
        CouplePlate = new ResourceType("CouplePlate", ResourceFormat.Plates);
        Conductor = new ResourceType("Conductor", ResourceFormat.Plates);
        Brick = new ResourceType("Brick", ResourceFormat.Plates);

        AllResources = [Oil, IronOre, CoupleOre, Coal, Stone, IronPlate, CouplePlate, Conductor, Brick];

        MineIronOre = new Recept("MineIronOre", [], [new ResourceCost(IronOre, 1)], 1);
        MineCoupleOre = new Recept("MineCoupleOre", [], [new ResourceCost(CoupleOre, 1)], 1);

        MeltIronOre = new Recept("MeltIronOre", [new ResourceCost(IronOre, 3)], [new ResourceCost(IronPlate, 1)], .1);
        MeltCoupleOre = new Recept("MeltCoupleOre", [new ResourceCost(CoupleOre, 3)], [new ResourceCost(CouplePlate, 1)], 10);
        NoneRecept = new Recept("NoneProcess", [], [], 0);

        AllRecepts = [MineCoupleOre, MineIronOre, MeltIronOre, MeltCoupleOre, NoneRecept];

        ProductionConductor = new Recept("ProductionConductor", [new ResourceCost(CouplePlate, 3)], [new ResourceCost(Conductor, 1)], 10);

        Miner = new MachineType("Miner", [MineIronOre, MineCoupleOre], new Dictionary<ResourceType, double>()
        {
            { Brick, 1 },
            { IronPlate, 1 }
        }, 1, 1, MachineKind.Miner);
        Smelter = new MachineType("Smelter", [MeltIronOre, MeltCoupleOre], new Dictionary<ResourceType, double>()
        {
            { Brick, 1 },
            { IronPlate, 1 }
        }, 1, 1, MachineKind.Production);
        BasicConstructor = new MachineType("BasicConstructor", [ProductionConductor], new Dictionary<ResourceType, double>()
        {
            { Brick, 1 },
            { IronPlate, 1 }
        }, 1, 1, MachineKind.Production);

        AllMachinesTypes = [Miner, Smelter, BasicConstructor];
        foreach (var machineType in AllMachinesTypes)
        {
            machineType.AvailableProcesses.Add(NoneRecept);
        }
    }
}