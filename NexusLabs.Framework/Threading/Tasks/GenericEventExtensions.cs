namespace System.Threading.Tasks
{
    public static class GenericEventExtensions
    {
        public static Task InvokeUnorderedAsync<T>(
            this EventHandler<T> @this,
            object sender,
            T eventArgs)
            where T : EventArgs => InvokeUnorderedAsync<T>(
                @this,
                sender,
                eventArgs,
                true);

        public static Task InvokeUnorderedAsync<T>(
            this EventHandler<T> @this,
            object sender,
            T eventArgs,
            bool stopOnFirstError)
            where T : EventArgs => InvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                false,
                stopOnFirstError);

        public static Task InvokeOrderedAsync<T>(
            this EventHandler<T> @this,
            object sender,
            T eventArgs)
            where T : EventArgs => InvokeOrderedAsync<T>(
                 @this,
                 sender,
                 eventArgs,
                 true);

        public static Task InvokeOrderedAsync<T>(
            this EventHandler<T> @this,
            object sender,
            T eventArgs,
            bool stopOnFirstError)
            where T : EventArgs => InvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                true,
                stopOnFirstError);

        public static Task InvokeAsync<T>(
            this EventHandler<T> @this,
            object sender,
            T eventArgs,
            bool forceOrdering,
            bool stopOnFirstError)
            where T : EventArgs => MulticastDelegateExtensions.MulticastInvokeAsync<T>(
                @this,
                sender,
                eventArgs,
                forceOrdering,
                stopOnFirstError);
    }
}
