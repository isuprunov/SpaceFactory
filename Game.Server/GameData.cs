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
    Heat,
    Energy,
    Radiance,
    ElectromagneticInterference
}

public enum MachineKind
{
    Miner,
    Production
}

public record class ResourceType(string Id, ResourceFormat Format) : IId
{
    public ReceptPart ToReceptPart(int count) => new ReceptPart(this, count);
}

public record ReceptPart(ResourceType ResourceType, double Count)
{
    public double Count { get; set; } = Count;
}

public record Recept(string Id, List<ReceptPart> InResources, List<ReceptPart> OutResources) : IId
{
    public static Recept Create(string id, List<ReceptPart> inResources, List<ReceptPart> outResources, double energy = 0, double heat = 0)
    {
        if (energy > 0)
            inResources =
            [
                new ReceptPart(GameData.Energy, energy),
                ..inResources
            ];
        else if (energy < 0)
            outResources =
            [
                new ReceptPart(GameData.Energy, energy),
                ..inResources
            ];
        if (heat > 0)
            inResources =
            [
                new ReceptPart(GameData.Energy, heat),
                ..inResources
            ];
        else if (heat < 0)
            outResources =
            [
                new ReceptPart(GameData.Energy, heat),
                ..inResources
            ];
        return new Recept(id, inResources, outResources);
    }
    
    public static Recept Create(string id, double energy = 0, double heat = 0, params List<ReceptPart> resources)
    {
        if(energy != 0)
            resources.Add(new ReceptPart(GameData.Energy, energy));
        if(heat != 0)
            resources.Add(new ReceptPart(GameData.Heat, heat));
        var inResources = resources.Where(m=> m.Count > 0).ToList();
        var outResources = resources.Where(m=> m.Count < 0).Select(m =>
        {
            m.Count *=-1;
            return m;
        }).ToList();
        return new Recept(id, inResources, outResources);
    }
}

public record MachineType(string Id, List<Recept> AvailableProcesses, Dictionary<ResourceType, double> Cost, double Size, double Weight, MachineKind MachineKind) : IId;

public interface IResourceCount
{
    public double Count { get; set; }
    public ResourceType ResourceType { get; init; }
}

public record class ResourceContainer(ResourceType ResourceType, double Count, double MaxCount) : IResourceCount
{
    public double Count { get; set; } = Count;
    public double MaxCount { get; set; } = MaxCount;
}

public class Deposit : IResourceCount
{
    public double Count { get; set; }
    public required double FirstCount { get; set; }
    public double Performance => double.Max(Count / FirstCount * BeginPerformance, BeginPerformance / 10);
    public required double BeginPerformance { get; set; }
    public required int UsedSlots { get; set; }
    public required int Slots { get; set; }
    public int FreeSlots => Slots - UsedSlots;
    public required ResourceType ResourceType { get; init; }
}

public record Machine(MachineType MachineType, Recept? CurrentRecept = null)
{
    public required int Count { get; set; }
    public Recept? CurrentRecept { get; set; } = CurrentRecept;
    public ReceptPart? MinerRecept => CurrentRecept?.OutResources.Single();
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
    public readonly MachineType Heater;
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
        Heat = new ResourceType("Heat", ResourceFormat.Heat);
        Energy = new ResourceType("Energy", ResourceFormat.Energy);
        // Radiance = new ResourceType("Radiance", ResourceFormat.Particles);
        // ElectromagneticInterference = new ResourceType("ElectromagneticInterference", ResourceFormat.Particles);


        IronPlate = new ResourceType("IronPlate", ResourceFormat.Plates);
        CouplePlate = new ResourceType("CouplePlate", ResourceFormat.Plates);
        Conductor = new ResourceType("Conductor", ResourceFormat.Plates);
        Brick = new ResourceType("Brick", ResourceFormat.Plates);

        AllResources = [Oil, IronOre, CoupleOre, Coal, Stone, IronPlate, CouplePlate, Conductor, Brick, Heat, Energy];

        BurnCoal = Recept.Create("BurnCoal", 0, -30, Coal.ToReceptPart(1));
        MineIronOre = Recept.Create("MineIronOre", [], [new ReceptPart(IronOre, 1)]);
        MineCoupleOre = Recept.Create("MineCoupleOre", [], [new ReceptPart(CoupleOre, 1)]);
        MineCoupleOre = Recept.Create("MineCoalOre", [], [new ReceptPart(Coal, 1)]);
        MineCoupleOre = Recept.Create("MineStone", [], [new ReceptPart(Stone, 1)]);

        MeltIronOre = Recept.Create("MeltIronOre", 5, 1, IronOre.ToReceptPart(3), IronPlate.ToReceptPart(-1));
        MeltCoupleOre = Recept.Create("MeltCoupleOre", [new ReceptPart(CoupleOre, 3)], [new ReceptPart(CouplePlate, 1)]);
        MeltCoupleOre = Recept.Create("MeltCoupleOre", [new ReceptPart(CoupleOre, 3)], [new ReceptPart(CouplePlate, 1)]);
        NoneRecept = Recept.Create("NoneProcess", [], []);
        ProductionConductor = Recept.Create("ProductionConductor", [new ReceptPart(CouplePlate, 3)], [new ReceptPart(Conductor, 1)]);
        RotateTurbine = Recept.Create("RotateTurbine", -10, 10);

        AllRecepts = [MineCoupleOre, MineIronOre, MeltIronOre, MeltCoupleOre, ProductionConductor, BurnCoal,RotateTurbine];

        Turbine = new MachineType("Turbine", [RotateTurbine], new Dictionary<ResourceType, double>()
        {
            { Brick, 1 },
            { IronPlate, 1 }
        },1,1, MachineKind.Production);
        
        Heater = new MachineType("Heater", [BurnCoal], new Dictionary<ResourceType, double>()
        {
            { Brick, 1 },
            { IronPlate, 1 }
        },1,1, MachineKind.Production);
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

        AllMachinesTypes = [Miner, Heater, Turbine, Smelter, BasicConstructor];
        // foreach (var machineType in AllMachinesTypes) 
        //     machineType.AvailableProcesses.Add(NoneRecept);
    }

    public MachineType Turbine { get; set; }

    public Recept RotateTurbine { get; set; }

    public Recept BurnCoal { get; set; }

    public static ResourceType Heat { get; set; }
    public static ResourceType Energy { get; set; }
}