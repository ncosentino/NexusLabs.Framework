namespace System.Threading.Tasks
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
            // do nothing with this guy, but tells callers that we explicitly 
            // don't care about what happens
        }
    }
}
