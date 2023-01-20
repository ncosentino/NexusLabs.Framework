using BenchmarkDotNet.Attributes;

using NexusLabs.Collections.Generic;

[ShortRunJob()]
[MemoryDiagnoser]
public class FrozenCollectionsBenchmark
{
    private static readonly Random _random = new Random(1337);

    [Params(100, 1_000, 10_000, 100_000, 1_000_000)]
    public int Size { get; set; }

    private IFrozenCollection<int>? _arrayFrozenAsCollection;
    private IFrozenSpannableCollection<int>? _arrayFrozenAsSpannableCollection;
    private IFrozenList<int>? _arrayFrozenAsList;
    private IFrozenSpannableList<int>? _arrayFrozenAsSpannableList;
    private IFrozenHashSet<int>? _arrayFrozenAsHashSet;

    private IFrozenCollection<int>? _listFrozenAsCollection;
    private IFrozenSpannableCollection<int>? _listFrozenAsSpannableCollection;
    private IFrozenList<int>? _listFrozenAsList;
    private IFrozenSpannableList<int>? _listFrozenAsSpannableList;
    private IFrozenHashSet<int>? _listFrozenAsHashSet;

    private int[]? _array;
    private List<int>? _list;

    [GlobalSetup]
    public void Setup()
    {
        _array = Enumerable.Range(0, Size).Select(_ => _random.Next()).ToArray();
        _list = _array.ToList();

        _arrayFrozenAsCollection = _array.AsFrozenCollection();
        _arrayFrozenAsSpannableCollection = _array.AsFrozenSpannableCollection();
        _arrayFrozenAsList = _array.AsFrozenList();
        _arrayFrozenAsSpannableList = _array.AsFrozenSpannableList();
        _arrayFrozenAsHashSet = _array.AsFrozenHashSet();
        _listFrozenAsCollection = _list.AsFrozenCollection();
        _listFrozenAsSpannableCollection = _list.AsFrozenSpannableCollection();
        _listFrozenAsList = _list.AsFrozenList();
        _listFrozenAsSpannableList = _list.AsFrozenSpannableList();
        _listFrozenAsHashSet = _list.AsFrozenHashSet();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
    }

    [Benchmark]
    public void ReadOnlySpan_Foreach()
    {
        foreach (var x in new ReadOnlySpan<int>(_array))
        {
        }
    }

    [Benchmark]
    public void Array_Foreach()
    {
        foreach (var x in _array)
        {
        }
    }

    [Benchmark]
    public void List_Foreach()
    {
        foreach (var x in _list)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsCollection_Foreach()
    {
        foreach (var x in _arrayFrozenAsCollection)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsSpannableCollection_Foreach()
    {
        foreach (var x in _arrayFrozenAsSpannableCollection)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsList_Foreach()
    {
        foreach (var x in _arrayFrozenAsList)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsSpannableList_Foreach()
    {
        foreach (var x in _arrayFrozenAsSpannableList)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsHashSet_Foreach()
    {
        foreach (var x in _arrayFrozenAsHashSet)
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsCollection_Foreach()
    {
        foreach (var x in _listFrozenAsCollection)
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsSpannableCollection_Foreach()
    {
        foreach (var x in _listFrozenAsSpannableCollection)
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsList_Foreach()
    {
        foreach (var x in _listFrozenAsList)
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsSpannableList_Foreach()
    {
        foreach (var x in _listFrozenAsSpannableList)
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsHashSet_Foreach()
    {
        foreach (var x in _listFrozenAsHashSet)
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsSpannableCollection_ForeachOverReadOnlySpan()
    {
        foreach (var x in _arrayFrozenAsSpannableCollection.GetReadOnlySpan())
        {
        }
    }

    [Benchmark]
    public void ArrayFrozenAsSpannableList_ForeachOverReadOnlySpan()
    {
        foreach (var x in _arrayFrozenAsSpannableList.GetReadOnlySpan())
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsSpannableCollection_ForeachOverReadOnlySpan()
    {
        foreach (var x in _listFrozenAsSpannableCollection.GetReadOnlySpan())
        {
        }
    }

    [Benchmark]
    public void ListFrozenAsSpannableList_ForeachOverReadOnlySpan()
    {
        foreach (var x in _listFrozenAsSpannableList.GetReadOnlySpan())
        {
        }
    }
}