using System.Threading.Channels;

namespace Game.Server;

[AttributeUsage(AttributeTargets.Method)]
public class IgnoreRpcMethod : Attribute;

public static class ModelExtensions
{
    public static ResourceContainerModel CreateModel(this ResourceContainer resourceContainerCost) => new(resourceContainerCost.ResourceType.Id, resourceContainerCost.Count, resourceContainerCost.MaxCount);
    public static Player.ResourceCostModel CreateModel(this ResourceCost resourceCost) => new(resourceCost.ResourceType.Id, resourceCost.Count);
    public static Player.ReceptModel CreateModel(this Recept recept) => new(recept.Id, recept.InResources.Select(CreateModel).ToArray(), recept.OutResources.Select(CreateModel).ToArray(), recept.Speed);
    public static Player.MachineModel CreateModel(this Machine machine) => new(machine.Id, machine.MachineType.Id, machine.Count, machine.CurrentRecept?.Id);
    public static Player.DepositModel CreateModel(this Deposit deposit) => new(deposit.ResourceType.Id, deposit.Count, deposit.FirstCount, deposit.Performance, deposit.BeginPerformance, deposit.Slots, deposit.UsedSlots);

    public static Player.MachineTypeModel CreateModel(this MachineType machineType) => new(machineType.Id,
        machineType.AvailableProcesses.Select(recept => recept.Id).ToArray(),
        machineType.Cost.ToDictionary(m => m.Key.Id, m => m.Value));
}

public record ResourceContainerModel(string ResourceTypeId, double Count, double MaxCount);
public class Player
{
    private readonly GameData _gameData;
    public string Id { get; init; }

    private double _maxSize = 1000;
    private double _maxWeight = 1000;

    private double _currentSize = 0;
    private double _currentWeight = 0;
    private Zone? _zone;

    public Player(GameData gameData, string playerId)
    {
        _gameData = gameData;
        Id = playerId;
        Resources = gameData.AllResources.ToDictionary(m => m.Id, m => new ResourceContainer(m, 300, 500));
        MachineTypes = gameData.AllMachinesTypes.ToDictionary(m => m.Id, m => m);
        Recepts = gameData.AllRecepts.ToDictionary(m => m.Id, m => m);
        Machines = new Dictionary<string, Machine>();
    }

    private Dictionary<string, ResourceContainer> Resources { get; set; }
    private Dictionary<string, Recept> Recepts { get; set; }
    private Dictionary<string, Machine> Machines { get; set; }
    private Dictionary<string, MachineType> MachineTypes { get; set; }
    

    private int _globalId;

    private bool CanBuild(MachineType machineType) => !machineType.Cost.Any(m => Resources[m.Key.Id].Count < m.Value) &&
                                                      _currentSize + machineType.Size < _maxSize &&
                                                      _currentWeight + machineType.Weight < _maxWeight;

    public record ResourceCostModel(string ResourceTypeId, double Count);
    public record ReceptModel(string Id, ResourceCostModel[] InResources,ResourceCostModel[] OutResources, double Speed);
    public record MachineModel(string Id, string MachineTypeId, int Count, string? CurrentReceptId);
    public record MachineTypeModel(string Id, string[] AvailableReceptIds, Dictionary<string, double> Cost);
    public record DepositModel(string ResourceTypeId, double Count, double BeginCount, double Performance, double BeginPerformance, int Slots, int UsedSlots);
    public class InitVm
    {
        public ResourceContainerModel[] Resources { get; set; }
        public ReceptModel[] Recepts { get; set; }
        public MachineModel[] Machines { get; set; }
        public MachineTypeModel[] MachineTypes { get; set; }
        public DepositModel[] Deposits { get; set; }
        public double MaxWeight { get; set; }
        public double MaxSize { get; set; }
    }

    public InitVm GetInitModel() => new()
    {
        Resources = Resources.Select(pair => pair.Value).Select(ModelExtensions.CreateModel).ToArray(),
        Recepts = _gameData.AllRecepts.Select(ModelExtensions.CreateModel).ToArray(),
        MachineTypes = _gameData.AllMachinesTypes.Select(ModelExtensions.CreateModel).ToArray(),
        Machines = Machines.Select(m => m.Value).Select(ModelExtensions.CreateModel).ToArray(),
        Deposits = _zone.Deposits.Select(m=> m.Value.CreateModel()).ToArray()
    };
    
    public record State(ResourceContainerModel[] Resources, DepositModel[] UsedDeposits,  double CurrentSize, double CurrentWeight);
    
    public State GetModelState() => new(
        Resources.Select(pair => pair.Value).Select(ModelExtensions.CreateModel).ToArray(),
        _zone.Deposits.Select(m=> m.Value.CreateModel()).ToArray(),
        _currentSize, _currentWeight);

    [IgnoreRpcMethod]
    public void SetZone(Zone zone)
    {
        _zone= zone;
    }
    
    private void BuildMachine(Machine machine)
    {
        foreach (var i in machine.MachineType.Cost)
            Resources[i.Key.Id].Count -= i.Value;
        _currentSize -= machine.MachineType.Size;
        _currentWeight -= machine.MachineType.Weight;
        machine.Count++;
    }

    public void SetMaxCountResource(string resourceTypeId, double maxCount)
    {
        Resources[resourceTypeId].MaxCount = maxCount;
    }

    public MachineModel? CreateMachine(string machineTypeId, string? receptId)
    {
        var machineType = MachineTypes[machineTypeId];

        if (CanBuild(machineType) == false)
            return null;

        Recept? recept = null;
        if (receptId != null)
            recept = Recepts[receptId];
        var machine = new Machine(machineType, recept)
        {
            Id = $"Id{Interlocked.Increment(ref _globalId)}",
            Count = 0,
        };
        Machines.Add(machine.Id, machine);
        BuildMachine(machine);
        return machine.CreateModel();
    }

    public bool IncrementCountMachine(string machineId)
    {
        var machine = Machines[machineId];
        if (CanBuild(machine.MachineType) == false)
            return false;
        BuildMachine(machine);
        return true;
    }

    public void ChangeRecept(string machineId, string? receptId)
    {
        Recept? recept = null;
        if (receptId != null)
            recept = Recepts[receptId];
        var machine = Machines[machineId];
        machine.CurrentRecept = recept;
    }

    public void DecrementCountMachine(string machineId)
    {
        var machine = Machines[machineId];
        machine.Count--;
        _currentSize += machine.MachineType.Size;
        _currentWeight += machine.MachineType.Weight;
    }

    [IgnoreRpcMethod]
    public void Turn()
    {
        foreach (var (_, machine) in Machines)
        {
            switch (machine.MachineType.MachineKind)
            {
                case MachineKind.Production:
                    machine.Work(Resources);
                    break;
                case MachineKind.Miner:
                    machine.WorkMiner(Resources, _zone.Deposits);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}