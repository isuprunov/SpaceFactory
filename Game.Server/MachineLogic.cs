namespace Game.Server;

public class MachineLogic(Machine machine, Dictionary<string, ResourceContainer> resources)
{
    public const int Step = 100;
    private bool ResourceMax() => machine.CurrentRecept == null || machine.CurrentRecept.OutResources.Select(m => resources[m.ResourceType.Id])
        .Any(m => m.Count >= m.MaxCount);
    private static double Power(double count, double consume) => count > consume ? 1 : count / consume;

    private static double PowerInPercent(IResourceCount resourceCount, ReceptPart receptPart, double consumeFactor) => Power(resourceCount.Count, receptPart.Count * consumeFactor);

    private double PowerInPercent(Recept recept, double consumeFactor) => recept.InResources.Count == 0 ? 1 : 
        recept.InResources.Min(receptPart => PowerInPercent(resources[receptPart.ResourceType.Id], receptPart, consumeFactor));

    private double PowerOutPercent(ReceptPart receptPart, double consumeFactor)
    {
        var resourceContainer = resources[receptPart.ResourceType.Id];
        var count = resourceContainer.MaxCount - resourceContainer.Count;
        return Power(count, receptPart.Count * consumeFactor);
    }

    private double PowerOutPercent(Recept recept, double consumeFactor) => recept.OutResources.Count == 0 ? 1 :
        recept.OutResources.Min(receptPart => PowerOutPercent(receptPart, consumeFactor));

    private static void Consume(IResourceCount resourceCount, ReceptPart receptPart, double consumeFactor, double power) =>
        resourceCount.Count -= receptPart.Count * consumeFactor * power;

    private void Consume(Recept recept, double consumeFactor, double power) =>
        recept.InResources.ForEach(receptPart => Consume(resources[receptPart.ResourceType.Id], receptPart, consumeFactor, power));

    private void Produce(ReceptPart receptPart, double consumeFactor, double power) =>
        resources[receptPart.ResourceType.Id].Count += receptPart.Count * consumeFactor * power;

    private void Produce(Recept recept, double consumeFactor, double power) =>
        recept.OutResources.ForEach(receptPart => Produce(receptPart, consumeFactor, power));

    public void WorkProductionMachine()
    {
        if (machine.CurrentRecept == null)
            return;
        if(ResourceMax())
            return;
        if(machine.Count == 0)
            return;
        var recept = machine.CurrentRecept;
        var consumeFactor = machine.Count  / (double)Step;
        var power = Math.Min(PowerInPercent(machine.CurrentRecept, consumeFactor), PowerOutPercent(machine.CurrentRecept, consumeFactor));
        Consume(recept, consumeFactor, power);
        Produce(recept, consumeFactor, power);
    }

    public void WorkMinerMachine(Zone? zone)
    {
        if (zone == null)
            return;
        if (machine.MinerRecept == null)
            return;
        if(ResourceMax())
            return;

        var receptPart = machine.MinerRecept;

        if (zone.Deposits.TryGetValue(receptPart.ResourceType.Id, out var deposit) == false)
            return;
        var count = Math.Min(machine.Count, deposit.Slots);
        if(count == 0)
            return;
        deposit.UsedSlots = count;
        var consumeFactor = count * deposit.Performance / Step;
        var power = Math.Min(PowerInPercent(deposit, receptPart, consumeFactor), PowerOutPercent(receptPart, consumeFactor));
        Consume(deposit, receptPart, consumeFactor, power);
        Produce(receptPart, consumeFactor, power);
        
        
    }
}