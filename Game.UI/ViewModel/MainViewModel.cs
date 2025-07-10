using System.Collections.ObjectModel;
using System.Reactive;
using System.Reflection;
using Avalonia.Media.Imaging;
using DynamicData.Binding;
using Game.UI.Events;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Game.UI.ViewModel;

public class MainViewModel : ReactiveObject
{
    private readonly Player _player;
    public GameData GameData { get; set; } = new GameData();


    [Reactive] public ObservableKeyedCollection<ResourceItem> Resources { get; set; }
    [Reactive] public ObservableKeyedCollection<MachineItem> Machines { get; set; }
    [Reactive] public ObservableCollection<MachineTypeItem> AvailableMachineForBuilds { get; set; }

    public Bitmap FullImage { get; set; }


    public static Bitmap? LoadResources(string id)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"Game.UI.Assets.{id}.png");
        if (stream is null)
            return null;
        return new Bitmap(stream);
    }
    
    public static Bitmap? LoadResources(IId id) => LoadResources(id.Id);

    private void LoadReceptImage(ReceptItem recept)
    {
        recept.Image = LoadResources(recept.Id);
        foreach (var sourceReceptItem in recept.SourceReceptItems)
            sourceReceptItem.Image = LoadResources(sourceReceptItem.ResourceTypeId);
        foreach (var destinationReceptItem in recept.DestinationReceptItems)
            destinationReceptItem.Image = LoadResources(destinationReceptItem.ResourceTypeId);
    }
    
    private void LoadMachineImage(MachineItem machine)
    {
        machine.Image = LoadResources(machine.MachineTypeId);
        machine.ChangeCurrentProcessOpenPopup = ReactiveCommand.Create(() =>
        {
            machine.IsPopupOpenSelectResourceProcess = true;
        });
        foreach (var availableRecept in machine.AvailableRecepts)
        {
            availableRecept.ChangeCurrentProcess = ReactiveCommand.Create(() =>
            {
                machine.IsPopupOpenSelectResourceProcess = false;
                _player.ChangeRecept(machine.Id, availableRecept.ReceptItem.Id);
            });
            LoadReceptImage(availableRecept.ReceptItem);
        }
        if (machine.CurrentRecept != null) 
            LoadReceptImage(machine.CurrentRecept);
    }
    
    public void ChangeUpdateModel(UpdateModel updateModel)
    {

        if (updateModel.InitVm != null)
        {
            Resources = new ObservableKeyedCollection<ResourceItem>(updateModel.InitVm.ResourceItems, item => item.ResourceTypeId);
            foreach (var resource in Resources)
                resource.Image = LoadResources(resource.ResourceTypeId);

            Recepts = new ObservableKeyedCollection<ReceptItem>(updateModel.InitVm.ReceptItems, item => item.Id);

            AvailableMachineForBuilds = new ObservableKeyedCollection<MachineTypeItem>(updateModel.InitVm.MachineTypeItems, item => item.Id);
            foreach (var machineTypeItem in AvailableMachineForBuilds)
            {
                machineTypeItem.Image = LoadResources(machineTypeItem.Id);
                machineTypeItem.CreateMachine = ReactiveCommand.Create(() => { _player.CreateMachine(machineTypeItem.Id, null); });
            }
            Machines = new ObservableKeyedCollection<MachineItem>(updateModel.InitVm.MachineItems, item => item.Id);
            foreach (var machine in Machines) 
                LoadMachineImage(machine);
        }

        if (updateModel.CreateMachine != null)
        {
            LoadMachineImage(updateModel.CreateMachine);
            Machines.Add(updateModel.CreateMachine);
        }

        if (updateModel.ChangeRecept != null)
        {
            var machine = Machines[updateModel.ChangeRecept.MachineId];
            machine.CurrentRecept = updateModel.ChangeRecept.ReceptId == null ? null : Recepts[updateModel.ChangeRecept.ReceptId];
        }


        // foreach (var resource in updateModel.Resources)
        // {
        //     )
        //     if (Resources.TryGetValue(resource.ResourceTypeId, out var resourceItem) == false)
        //     {
        //         resourceItem = ResourceItem.CreateViewModel(resource.ResourceTypeId, resource.Count);
        //         Resources.Add(resourceItem);
        //     }
        //     else
        //         resourceItem.Count = resource.Count;
        // }
    }

    public ObservableKeyedCollection<ReceptItem> Recepts { get; set; }

    public MainViewModel()
    {
        _player = new Player(ChangeUpdateModel, GameData)
        {
            //Machines =
            //[
                //new Machine(GameData.Miner, GameData.MineIronOre),
                // new(GameData.Miner, GameData.MineIronOre),
                // new(GameData.Miner, GameData.MineCoupleOre),
                //new(GameData.Smelter, GameData.MeltIronOre),
                // new(GameData.Smelter, GameData.MeltIronOre),
                // new(GameData.Smelter, GameData.MeltIronOre)
            //]
        };
        //Resources = new ObservableKeyedCollection<ResourceItem>(m=> m.ResourceTypeId);
        
        //Machines = new ObservableCollection<MachineItem>(_player.Machines.Select(machine => MachineItem.CreateViewModel(this, _player, machine)));

        //AvailableMachineForBuilds = new ObservableCollection<MachineTypeItem>(GameData.AllMachinesTypes.Select(machineType => MachineTypeItem.CreateViewModel(this, _player, machineType)));

        Task.Run(async () =>
        {
            while (true)
            {

                _player.Turn();
                await Task.Delay(100);
            }
        });
    }
}


