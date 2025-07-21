using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.Media.Imaging;
using Game.Client;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;


namespace Game.UI.ViewModel;

public class ResourceContainerViewModel : ReactiveObject
{
    public ResourceContainerViewModel(ResourceContainerModel model, GameClient client)
    {
        ResourceTypeId = model.ResourceTypeId;
        Count = model.Count;
        MaxCount = model.MaxCount;
        Image = MainViewModel.LoadResources(model.ResourceTypeId);
        this.WhenAnyValue(x => x.MaxCount)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .SelectMany(x => Observable.FromAsync(() => client.SetMaxCountResourceAsync(ResourceTypeId,x)))
            .Subscribe();
    }

    public string ResourceTypeId { get; set; }
    [Reactive] public double Count { get; set; }
    [Reactive] public double MaxCount { get; set; }
    public Bitmap? Image { get; set; }
}

public class ResourceCostViewModel(ResourceCostModel model) : ReactiveObject
{
    public string ResourceTypeId { get; set; } = model.ResourceTypeId;
    [Reactive] public double Count { get; set; } = model.Count;
    public Bitmap? Image { get; set; } = MainViewModel.LoadResources(model.ResourceTypeId);
}

public class ReceptViewModel(ReceptModel model) : ReactiveObject
{
    public string Id { get; set; } = model.Id;
    [Reactive] public Bitmap? Image { get; set; } = MainViewModel.LoadResources(model.Id);
    [Reactive] public ResourceCostViewModel[] InResources { get; set; } = model.InResources.Select(resourceModel => new ResourceCostViewModel(resourceModel)).ToArray();
    [Reactive] public ResourceCostViewModel[] OutResources { get; set; } = model.OutResources.Select(resourceModel => new ResourceCostViewModel(resourceModel)).ToArray();
    [Reactive] public bool DestinationReceptItemNotEmpty { get; set; } = model.OutResources.Any();
}

public class MachineReceptViewModel : ReactiveObject
{
    public ReceptViewModel ReceptItem { get; set; }
    //public ReactiveCommand<Unit, Unit> ChangeCurrentProcess { get; set; }
}
//
// public class MachineTypeViewModel : ReactiveObject
// {
//     public MachineTypeViewModel(MachineTypeModel model)
//     {
//         Id = model.Id;
//         Image = MainViewModel.LoadResources(model.Id);
//         AvailableReceptIds = model.AvailableReceptIds.ToArray();
//         Cost = model.Cost.Select(m => new ResourceCostViewModel(new ResourceCostModel
//         {
//             ResourceTypeId = m.Key,
//             Count = m.Value,
//         })).ToArray();
//     }
//
//     public string Id { get; set; }
//     public Bitmap? Image { get; set; }
//     public string[] AvailableReceptIds { get; set; }
//     public ResourceCostViewModel[] Cost { get; set; }
// }

public class DepositViewModel : ReactiveObject
{
    public DepositViewModel(DepositModel model)
    {
        ResourceTypeId = model.ResourceTypeId;
        Image = MainViewModel.LoadResources(model.ResourceTypeId);
        Count = model.Count;
        BeginCount = model.BeginCount;
        Performance = model.Performance;
        BeginPerformance = model.BeginPerformance;
        Slots = model.Slots;
        UsedSlots = model.UsedSlots;
        this.WhenAnyValue(x => x.BeginPerformance).Subscribe(m => BeginPerformanceInPercent = (int)Math.Round(m * 100));
        this.WhenAnyValue(x => x.Performance).Subscribe(m => PerformanceInPercent = (int)Math.Round(m * 100));
    }

    public string ResourceTypeId { get; set; }
    public Bitmap? Image { get; set; }
    [Reactive] public double Count { get; set; }
    [Reactive] public double BeginCount { get; set; }
    [Reactive] public double Performance { get; set; }
    [Reactive] public double BeginPerformance { get; set; }
    [Reactive] public double Slots { get; set; }
    [Reactive] public double UsedSlots { get; set; }
    [Reactive] public int PerformanceInPercent { get; set; }
    [Reactive] public int BeginPerformanceInPercent { get; set; }
}

public class MachineViewModel : ReactiveObject
{
    public MachineViewModel(MachineModel model, ObservableKeyedCollection<ReceptViewModel> allRecepts, GameClient gameClient)
    {
        Id = model.Id;
        MachineTypeId = model.MachineTypeId;
        Image = MainViewModel.LoadResources(model.MachineTypeId);
        Count = model.Count;
        CurrentRecept = model.CurrentReceptId == null ? null : allRecepts[model.CurrentReceptId];
        this.WhenAnyValue(m => m.CurrentRecept).Subscribe(m => CurrentReceptNull = m == null);
        BuildMachine = ReactiveCommand.CreateFromTask(() => gameClient.BuildMachineAsync(model.MachineTypeId, model.CurrentReceptId));
        BuildMachine = ReactiveCommand.CreateFromTask(() => gameClient.BuildMachineAsync(model.MachineTypeId, model.CurrentReceptId));


        // AvailableRecepts = allMachineTypes[model.MachineTypeId].AvailableReceptIds.Select(m => new MachineReceptViewModel()
        // {
        //     ReceptItem = allRecepts[m],
        // }).ToArray();

        //ChangeCurrentProcessOpenPopup = ReactiveCommand.Create(() => { IsPopupOpenSelectResourceProcess = true; });
        // IncrementCountMachine = ReactiveCommand.CreateFromTask(async () =>
        // {
        //     if (await gameClient.IncrementCountMachineAsync(model.Id))
        //         Count++;
        // });
        // DecrementCountMachine = ReactiveCommand.CreateFromTask(async () => await gameClient.DecrementCountMachineAsync(model.Id));
    }

    public string Id { get; set; }
    public string MachineTypeId { get; set; }
    public Bitmap? Image { get; set; }
    [Reactive] public int Count { get; set; }
    [Reactive] public ReceptViewModel? CurrentRecept { get; set; }

    [Reactive] public bool CurrentReceptNull { get; set; }

    //public MachineReceptViewModel[] AvailableRecepts { get; set; }
    public ReactiveCommand<Unit, Unit> BuildMachine { get; set; }
    public ReactiveCommand<Unit, Unit> DestroyMachine { get; set; }
    public ReactiveCommand<Unit, Unit> IdleMachine { get; set; } = null!;
    public ReactiveCommand<Unit, Unit> ComeToWorkMachine { get; set; } = null!;

    //[Reactive] public bool IsPopupOpenSelectResourceProcess { get; set; }

    //public ReactiveCommand<Unit, Unit> ChangeCurrentProcessOpenPopup { get; set; }
    // public ReactiveCommand<Unit, Unit> IncrementCountMachine { get; set; }
    // public ReactiveCommand<Unit, Unit> DecrementCountMachine { get; set; }
}

public class MainViewModel : ReactiveObject
{
    private GameClient _client;
    private string _playerId;
    [Reactive] public ObservableKeyedCollection<ResourceContainerViewModel> Resources { get; set; } = null!;

    [Reactive] public ObservableKeyedCollection<MachineViewModel> Machines { get; set; } = null!;

    //[Reactive] public ObservableKeyedCollection<MachineTypeViewModel> MachineTypes { get; set; }
    [Reactive] public ObservableKeyedCollection<DepositViewModel> Deposits { get; set; }
    public ObservableKeyedCollection<ReceptViewModel> Recepts { get; set; } = null!;

    public static Bitmap? LoadResources(string id)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream($"Game.UI.Assets.{id}.png");
        if (stream is not null)
            return new Bitmap(stream);
        var bitmap = SvgHelper.LoadSvg($"avares://Game.UI/StaticAssets/{id}.svg", 32, 32);
        if (bitmap is not null)
            return bitmap;

        return new Bitmap(new TextToPngRenderer(32, 32, "Monospace").Render(id));
    }


    private MachineViewModel Create(MachineModel machineModel, GameClient gameClient)
    {
        var machineViewModel = new MachineViewModel(machineModel, Recepts, gameClient)
        {
            ComeToWorkMachine = ReactiveCommand.CreateFromTask(() => gameClient.ComeToWorkMachineAsync(machineModel.MachineTypeId, machineModel.CurrentReceptId)),
            IdleMachine = ReactiveCommand.CreateFromTask(() => gameClient.IdleMachineAsync(machineModel.MachineTypeId, machineModel.CurrentReceptId))
        };
        return machineViewModel;
    }

    public MainViewModel()
    {
        Task.Run(async () =>
        {
            var gameName = "game1";
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new AnswerJsonConverter());
            var httpClient = new HttpClient();
            _client = new GameClient($"http://localhost:5000/", httpClient);
            await _client.CreateGameAsync(gameName);
            _playerId = await _client.CreatePlayerAsync(gameName);
            await _client.StartGameAsync(gameName);
            httpClient.DefaultRequestHeaders.Add("playerId", _playerId);

            var initVm = await _client.GetInitModelAsync();
            Resources = new ObservableKeyedCollection<ResourceContainerViewModel>(initVm.Resources.Select(resourceModel => new ResourceContainerViewModel(resourceModel, _client)), item => item.ResourceTypeId);
            foreach (var resource in Resources)
                resource.Image = LoadResources(resource.ResourceTypeId);
            Recepts = new ObservableKeyedCollection<ReceptViewModel>(initVm.Recepts.Select(receptModel => new ReceptViewModel(receptModel)), item => item.Id);
            // MachineTypes = new ObservableKeyedCollection<MachineTypeViewModel>(initVm.MachineTypes.Select(machineTypeModel => new MachineTypeViewModel(machineTypeModel)
            // {
            //     BuildMachine = ReactiveCommand.CreateFromTask(async () =>
            //     {
            //         var machineModel = await _client.Bu(machineTypeModel.Id, null);
            //         if (machineModel != null)
            //             Machines.Add(Create(machineModel, _client));
            //     })
            // }), item => item.Id);
            Machines = new ObservableKeyedCollection<MachineViewModel>(initVm.Machines.Select(machineModel => Create(machineModel, _client)), item => item.Id);
            Deposits = new ObservableKeyedCollection<DepositViewModel>(initVm.Deposits.Select(depositModel => new DepositViewModel(depositModel)), model => model.ResourceTypeId);
            //await MachineTypes["Miner"].CreateMachine.Execute();
            //await Machines.First().AvailableRecepts.First().ChangeCurrentProcess.Execute();


            while (true)
            {
                var state = await _client.GetModelStateAsync();
                foreach (var answer in state)
                {
                    
                    switch (answer)
                    {
                        case BuildMachineAnswer buildMachineAnswer:
                            Machines[$"{buildMachineAnswer.MachineTypeId}"].Count++;
                            break;
                        case DestroyMachineAnswer destroyMachineAnswer:
                            Machines[$"{destroyMachineAnswer.MachineTypeId}_{destroyMachineAnswer.ReceptId}"].Count++;
                            break;
                        case SwapMachineAnswer swapMachineAnswer:
                            Machines[$"{swapMachineAnswer.MachineTypeId}_{swapMachineAnswer.DecrementReceptId}"].Count--;
                            Machines[$"{swapMachineAnswer.MachineTypeId}_{swapMachineAnswer.IncrementReceptId}"].Count++;
                            break;
                        case StateAnswer stateAnswer:
                        {
                            foreach (var resource in stateAnswer.Resources)
                                Resources[resource.ResourceTypeId].Count = resource.Count;
                            foreach (var deposit in stateAnswer.UsedDeposits)
                            {
                                Deposits[deposit.ResourceTypeId].Count = deposit.Count;
                                Deposits[deposit.ResourceTypeId].Performance = deposit.Performance;
                                Deposits[deposit.ResourceTypeId].UsedSlots = deposit.UsedSlots;
                            }

                            break;
                        }
                    }
                }

                await Task.Delay(50);
            }
        });
    }
}

public class AnswerJsonConverter : JsonConverter<Answer>
{
    public override Answer? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        if (!root.TryGetProperty("type", out var typeProp))
            throw new JsonException("Missing 'type' discriminator");

        var typeDiscriminator = typeProp.GetString();

        return typeDiscriminator switch
        {
            nameof(BuildMachineAnswer) => root.Deserialize<BuildMachineAnswer>(options),
            nameof(DestroyMachineAnswer) => root.Deserialize<DestroyMachineAnswer>(options),
            nameof(SwapMachineAnswer) => root.Deserialize<SwapMachineAnswer>(options),
            nameof(ErrorAnswer) => root.Deserialize<ErrorAnswer>(options),
            nameof(StateAnswer) => root.Deserialize<StateAnswer>(options),
            _ => throw new JsonException($"Unknown type: {typeDiscriminator}")
        };
    }

    public override void Write(Utf8JsonWriter writer, Answer value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }
}