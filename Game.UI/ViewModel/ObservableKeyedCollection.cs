using System.Collections.ObjectModel;

namespace Game.UI.ViewModel;

public class ObservableKeyedCollection<TValue> : ObservableCollection<TValue> 
{
    private readonly Func<TValue, string> _keySelector;
    private readonly Dictionary<string, TValue> _dict = new();

    public ObservableKeyedCollection(Func<TValue, string> keySelector)
    {
        _keySelector = keySelector;
    }
    
    public ObservableKeyedCollection(IList<TValue> collection, Func<TValue, string> keySelector) : base(collection)
    {
        _keySelector = keySelector;
        foreach (var item in collection)
        {
            var key = _keySelector(item);
            _dict.Add(key, item);
        }
    }

    
    
    protected override void InsertItem(int index, TValue item)
    {
        var key = _keySelector(item);
        _dict.Add(key, item);
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        var key = _keySelector(this[index]);
        _dict.Remove(key);
        base.RemoveItem(index);
    }

    public TValue this[string key] => _dict[key];

    public bool TryGetValue(string key, out TValue value) => _dict.TryGetValue(key, out value);
}