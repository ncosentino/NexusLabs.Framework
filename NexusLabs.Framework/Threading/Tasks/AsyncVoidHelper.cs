namespace System.Threading.Tasks
{
    public static class AsyncVoidHelper
    {
        public static Task InvokeAsync(
            Action action) =>
            action.InvokeAsync();

        public static Task InvokeAsync<T>(
            Action<T> action,
            T arg1) => 
            action.InvokeAsync(arg1);

        public static Task InvokeAsync<T1, T2>(
            Action<T1, T2> action,
            T1 arg1,
            T2 arg2) =>
            action.InvokeAsync(arg1, arg2);

        public static Task InvokeAsync<T1, T2, T3>(
            Action<T1, T2, T3> action,
            T1 arg1,
            T2 arg2,
            T3 arg3) =>
            action.InvokeAsync(arg1, arg2, arg3);
    }
}
