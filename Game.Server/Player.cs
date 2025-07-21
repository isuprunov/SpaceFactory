using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Game.Server;

[AttributeUsage(AttributeTargets.Method)]
public class IgnoreRpcMethod : Attribute;

public static class ModelExtensions
{
    public static ResourceContainerModel CreateModel(this ResourceContainer resourceContainerCost) => new(resourceContainerCost.ResourceType.Id, resourceContainerCost.Count, resourceContainerCost.MaxCount);
    public static ResourceCostModel CreateModel(this ReceptPart receptPart) => new(receptPart.ResourceType.Id, receptPart.Count);
    public static ReceptModel CreateModel(this Recept recept) => new(recept.Id, recept.InResources.Select(CreateModel).ToArray(), recept.OutResources.Select(CreateModel).ToArray());
    public static MachineModel CreateModel(this Machine machine) => new(machine.Id, machine.MachineType.Id, machine.Count, machine.CurrentRecept?.Id);
    public static DepositModel CreateModel(this Deposit deposit) => new(deposit.ResourceType.Id, deposit.Count, deposit.FirstCount, deposit.Performance, deposit.BeginPerformance, deposit.Slots, deposit.UsedSlots);

    public static MachineTypeModel CreateModel(this MachineType machineType) => new(machineType.Id,
        machineType.AvailableProcesses.Select(recept => recept.Id).ToArray(),
        machineType.Cost.ToDictionary(m => m.Key.Id, m => m.Value));
}

public record ResourceContainerModel(string ResourceTypeId, double Count, double MaxCount);

public record ResourceCostModel(string ResourceTypeId, double Count);

public record ReceptModel(string Id, ResourceCostModel[] InResources, ResourceCostModel[] OutResources);

public record MachineModel(string Id, string MachineTypeId, int Count, string? CurrentReceptId);

public record MachineTypeModel(string Id, string[] AvailableReceptIds, Dictionary<string, double> Cost);

public record DepositModel(string ResourceTypeId, double Count, double BeginCount, double Performance, double BeginPerformance, int Slots, int UsedSlots);


[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(BuildMachineAnswer), nameof(BuildMachineAnswer))]
[JsonDerivedType(typeof(DestroyMachineAnswer), nameof(DestroyMachineAnswer))]
[JsonDerivedType(typeof(SwapMachineAnswer), nameof(SwapMachineAnswer))]
[JsonDerivedType(typeof(ErrorAnswer), nameof(ErrorAnswer))]
[JsonDerivedType(typeof(StateAnswer), nameof(StateAnswer))]
public class Answer { }

public class BuildMachineAnswer(string machineTypeId, string? receptId) : Answer
{
    public string MachineTypeId { get; set; } = machineTypeId;
    public string? ReceptId { get; set; } = receptId;
}

public class DestroyMachineAnswer(string machineTypeId, string? receptId) : Answer
{
    public string MachineTypeId { get; set; } = machineTypeId;
    public string? ReceptId { get; set; } = receptId;
}

public class SwapMachineAnswer(string machineTypeId, string? decrementReceptId, string? incrementReceptId) : Answer
{
    public string MachineTypeId { get; set; } = machineTypeId;
    public string? DecrementReceptId { get; set; } = decrementReceptId;
    public string? IncrementReceptId { get; set; } = incrementReceptId;
}

public class ErrorAnswer(string error) : Answer
{
    public string Error { get; set; } = error;
}

public class StateAnswer : Answer
{
    public ResourceContainerModel[] Resources { get; set; } = default!;
    public DepositModel[] UsedDeposits { get; set; } = default!;
    public double CurrentSize { get; set; }
    public double CurrentWeight { get; set; }
    public double MaxSize { get; set; }
    public double MaxWeight { get; set; }
}

public class InitAnswer 
{
    public required ResourceContainerModel[] Resources { get; set; }
    public required ReceptModel[] Recepts { get; set; }
    public required MachineModel[] Machines { get; set; }
    public required MachineTypeModel[] MachineTypes { get; set; }
    public required DepositModel[] Deposits { get; set; }
}



public class Player
{
    private readonly GameData _gameData;
    public string Id { get; init; }

    private double _maxSize = 10000;
    private double _maxWeight = 10000;

    private double _currentSize;
    private double _currentWeight;

    private ConcurrentQueue<Answer> _answers = new();

    private Zone? _zone;

    public Player(GameData gameData, string playerId)
    {
        _gameData = gameData;
        Id = playerId;
        Resources = gameData.AllResources.ToDictionary(m => m.Id, m => new ResourceContainer(m, 300, 5000));
        MachineTypes = gameData.AllMachinesTypes.ToDictionary(m => m.Id, m => m);
        Recepts = gameData.AllRecepts.ToDictionary(m => m.Id, m => m);
        Machines = new Dictionary<string, Machine>();
        foreach (var (_, machineType) in MachineTypes)
        {
            Machine machine;
            foreach (var recept in machineType.AvailableProcesses)
            {
                machine = new Machine(machineType, recept)
                {
                    Id = $"{machineType.Id}_{recept.Id}",
                    Count = 0,
                };
                Machines.Add(machine.Id, machine);
            }

            machine = new Machine(machineType)
            {
                Id = $"{machineType.Id}_",
                Count = 0,
            };
            Machines.Add(machine.Id, machine);
        }

        Machines[$"{nameof(gameData.Core)}_{nameof(_gameData.ProductionLogisticDroneInCore)}"].Count = 1;
    }

    private Dictionary<string, ResourceContainer> Resources { get; set; }
    private Dictionary<string, Recept> Recepts { get; set; }
    private Dictionary<string, Machine> Machines { get; set; }
    private Dictionary<string, MachineType> MachineTypes { get; set; }

    private int _globalId;


    public InitAnswer GetInitModel() => new()
    {
        Resources = Resources.Select(pair => pair.Value).Select(ModelExtensions.CreateModel).ToArray(),
        Recepts = _gameData.AllRecepts.Select(ModelExtensions.CreateModel).ToArray(),
        MachineTypes = _gameData.AllMachinesTypes.Select(ModelExtensions.CreateModel).ToArray(),
        Machines = Machines.Select(m => m.Value).Select(ModelExtensions.CreateModel).ToArray(),
        Deposits = _zone.Deposits.Select(m => m.Value.CreateModel()).ToArray()
    };

    [IgnoreRpcMethod]
    public List<Answer> GetModelState()
    {
        var answers = new List<Answer>();
        while (_answers.TryDequeue(out var answer)) 
            answers.Add(answer);
        return answers;
    }

    [IgnoreRpcMethod]
    public void SetZone(Zone zone)
    {
        _zone = zone;
    }

    private bool CanBuildMachine(MachineType machineType) => !machineType.Cost.Any(m => Resources[m.Key.Id].Count < m.Value) &&
                                                             _currentSize + machineType.Size < _maxSize &&
                                                             _currentWeight + machineType.Weight < _maxWeight;

    public void BuildMachine(string machineTypeId, string? receptId)
    {
        var machineId = $"{machineTypeId}_{receptId ?? string.Empty}";
        var machine = Machines[machineId];
        if (CanBuildMachine(machine.MachineType) == false)
        {
            _answers.Enqueue(new ErrorAnswer("1"));
        }

        foreach (var i in machine.MachineType.Cost)
            Resources[i.Key.Id].Count -= i.Value;
        _currentSize -= machine.MachineType.Size;
        _currentWeight -= machine.MachineType.Weight;
        machine.Count++;
        _answers.Enqueue(new BuildMachineAnswer(machineId, receptId));
    }

    public void DestroyMachine(string machineTypeId, string? receptId)
    {
        var machineId = $"{machineTypeId}_{receptId ?? string.Empty}";
        var machine = Machines[machineId];
        if (machine.Count == 0)
        {
            _answers.Enqueue(new ErrorAnswer("1"));
            return;
        }

        machine.Count--;
        _currentSize += machine.MachineType.Size;
        _currentWeight += machine.MachineType.Weight;
        _answers.Enqueue(new DestroyMachineAnswer(machineId, receptId));
    }

    private void SwapMachine(string machineTypeId, string? decrementReceptId, string? incrementReceptId)
    {
        var machineId = $"{machineTypeId}_{decrementReceptId ?? string.Empty}";
        var machine = Machines[machineId];
        if (machine.Count == 0)
        {
            _answers.Enqueue(new ErrorAnswer("1"));
            return;
        }

        machine.Count--;
        machineId = $"{machineTypeId}_{incrementReceptId ?? string.Empty}";
        machine = Machines[machineId];
        machine.Count++;
        _answers.Enqueue(new SwapMachineAnswer(machineId, decrementReceptId, incrementReceptId));
    }

    public void IdleMachine(string machineTypeId, string receptId) => SwapMachine(machineTypeId, receptId, null);

    public void ComeToWorkMachine(string machineTypeId, string receptId) => SwapMachine(machineTypeId, null, receptId);

    public void SetMaxCountResource(string resourceTypeId, double maxCount) => Resources[resourceTypeId].MaxCount = maxCount;

    // public void ChangeRecept(string machineId, string? receptId)
    // {
    //     Recept? recept = null;
    //     if (receptId != null)
    //         recept = Recepts[receptId];
    //     var machine = Machines[machineId];
    //     machine.CurrentRecept = recept;
    // }


    [IgnoreRpcMethod]
    public void Turn()
    {
        var resourceTmp = Resources.ToDictionary(m=> m.Key, m =>
        {
            var resourceContainer = m.Value;
            return new ResourceContainer(resourceContainer.ResourceType, 0, resourceContainer.MaxCount - resourceContainer.Count);
        });
        for (var i = 0; i < MachineLogic.Step; i++)
        {
            foreach (var (_, machine) in Machines)
            {
                var machineLogic = new MachineLogic(machine, resourceTmp);
                if (machine.MachineType.MachineKind == MachineKind.Miner)
                    machineLogic.WorkMinerMachine(_zone);
                if (machine.MachineType.MachineKind == MachineKind.Production)
                    machineLogic.WorkProductionMachine();
            }
        }
        var sum = resourceTmp.Values.Where(m => m.ResourceType.Format is ResourceFormat.Liquid or ResourceFormat.Particles or ResourceFormat.Unit).Select(m => m.Count).Sum();
        var drones = Resources[_gameData.LogisticDrone.Id].Count;
        var factor = drones > sum ? 1.0 : drones / sum;
        foreach (var (key, value) in resourceTmp)
            Resources[key].Count += value.Count * factor;

        _answers.Enqueue(new StateAnswer
        {
            Resources = Resources.Select(pair => pair.Value).Select(ModelExtensions.CreateModel).ToArray(),
            UsedDeposits = _zone.Deposits.Select(m => m.Value.CreateModel()).ToArray(),
            CurrentSize = _currentSize,
            CurrentWeight = _currentWeight,
            MaxSize = _maxSize,
            MaxWeight = _maxWeight,
        });
    }
}