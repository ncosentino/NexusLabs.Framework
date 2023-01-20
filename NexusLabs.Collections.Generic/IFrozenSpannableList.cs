namespace NexusLabs.Collections.Generic
{
#if NET6_0_OR_GREATER
    public interface IFrozenSpannableList<T> :
        IFrozenList<T>,
        IFrozenSpannableCollection<T>
    {
    }
#endif
}
