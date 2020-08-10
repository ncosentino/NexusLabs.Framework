namespace NexusLabs.Framework
{
    public interface IOnlyOnce
    {
        void Run();

        void RunAsync();
    }
}