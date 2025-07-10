using System.Collections.ObjectModel;
using System.Reactive;
using Avalonia.Media.Imaging;
using Game.UI.ViewModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Game.UI.Events;

public record ResourceChange(string ResourceTypeId, double Count);

public record ChangeRecept(string MachineId, string? ReceptId);

public class UpdateModelV2()
{
    
}

public class UpdateModel()
{
    public InitVm? InitVm { get; set; }
    public ResourceChange[]? SetResources { get; set; }
    public MachineItem? CreateMachine { get; set; }
    public ChangeRecept? ChangeRecept {get; set;}
}

// public class RequestModelAddMachine()
// {
//     
// }

public class InitVm
{
    public ResourceItem[] ResourceItems { get; set; }
    public ReceptItem[] ReceptItems { get; set; }
    public MachineItem[] MachineItems { get; set; }
    public MachineTypeItem[] MachineTypeItems { get; set; }
}

public class ResourceItem : ReactiveObject
{
    public required string ResourceTypeId { get; set; }
    [Reactive] public double Count { get; set; }
    public Bitmap? Image { get; set; }

    public static ResourceItem CreateViewModel(Resource resource) =>
        new()
        {
            ResourceTypeId = resource.ResourceType.Id,
            Count = resource.Count,
        };
}

public class ReceptItem : ReactiveObject
{
    public required string Id { get; set; }
    [Reactive] public Bitmap? Image { get; set; }
    [Reactive] public required List<ResourceItem> SourceReceptItems { get; set; }
    [Reactive] public required List<ResourceItem> DestinationReceptItems { get; set; }
    [Reactive] public bool DestinationReceptItemEmpty { get; set; }

    public static ReceptItem CreateViewModel(Recept recept) =>
        new()
        {
            Id = recept.Id,
            SourceReceptItems = recept.SourceRecepts.Select(ResourceItem.CreateViewModel).ToList(),
            DestinationReceptItems = recept.DestinationRecepts.Select(ResourceItem.CreateViewModel).ToList(),
            DestinationReceptItemEmpty = recept.DestinationRecepts.Any() == false
        };
}

public class MachineReceptItem : ReactiveObject
{
    public ReceptItem ReceptItem { get; set; }
    public ReactiveCommand<Unit, Unit> ChangeCurrentProcess { get; set; }
    
}

public class MachineItem : ReactiveObject
{
    public string Id { get; set; }
    public required string MachineTypeId { get; set; }
    public Bitmap? Image { get; set; }
    [Reactive] public int Count { get; set; }

    [Reactive] public bool IsPopupOpenSelectResourceProcess { get; set; }
    public required ObservableCollection<MachineReceptItem> AvailableRecepts { get; set; }
    [Reactive] public ReceptItem? CurrentRecept { get; set; }

    public ReactiveCommand<Unit, Unit> ChangeCurrentProcessOpenPopup { get; set; }
    //public ReactiveCommand<Unit, Unit> ChangeCurrentProcess { get; set; }

    public static MachineItem CreateViewModel(Machine machine)
    {
        var res = new MachineItem(machine)
        {
            Count = machine.Count,
            CurrentRecept = machine.CurrentResourceProcess == null ? null : ReceptItem.CreateViewModel(machine.CurrentResourceProcess),
            MachineTypeId = machine.MachineType.Id,
            AvailableRecepts = new ObservableCollection<MachineReceptItem>(machine.MachineType.AvailableProcesses
                .Select(receptItem=> new MachineReceptItem()
                {
                    ReceptItem = ReceptItem.CreateViewModel(receptItem)
                })),
        };
        // foreach (var resourceProcessItem in res.AvailableRecepts) 
        //     resourceProcessItem.MachineItem = res;
        return res;
    }

    private MachineItem(Machine machine)
    {
        Id = machine.Id;

        // ChangeCurrentProcess = ReactiveCommand.Create<ReceptItem>(resourceProcessItem =>
        // {
        //     IsPopupOpenSelectResourceProcess = false;
        //     Machine.CurrentResourceProcess = resourceProcessItem.Recept;
        //     CurrentProcess = ReceptItem.CreateViewModel(Machine.CurrentResourceProcess);
        // });
        // AddMachine = ReactiveCommand.Create(() =>
        // {
        //     player.IncrementCountMachine(machine);
        //     Count = machine.Count;
        // });
        //
        // RemoveMachine = ReactiveCommand.Create(() =>
        // {
        //     player.RemoveMachine(machine);
        //     Count = machine.Count;
        //     if (Count == 0)
        //         mainViewModel.Machines.Remove(this);
        // });
    }

    public ReactiveCommand<Unit, Unit> RemoveMachine { get; set; }

    public ReactiveCommand<Unit, Unit> AddMachine { get; set; }
}

public class MachineTypeItem
{
    public required string Id { get; set; }
    public Bitmap? Image { get; set; }
    public ReactiveCommand<Unit, Unit> CreateMachine { get; set; } = null!;

    public static MachineTypeItem CreateViewModel(MachineType machineType)
    {
        return new MachineTypeItem()
        {
            Id = machineType.Id,
            //Image = MainViewModel.LoadResources(machineType),
            // CreateMachine = ReactiveCommand.Create(() =>
            // {
            //     var machine = player.CreateMachine(machineType, null);
            //     mainViewModel.Machines.Add(MachineItem.CreateViewModel(mainViewModel, player, machine));
            // })
        };
    }
}