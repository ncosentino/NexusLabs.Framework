namespace System.Threading.Tasks
{
    public static class EventExtensions
    {
        public static Task InvokeUnorderedAsync(
            this EventHandler @this,
            object sender,
            EventArgs eventArgs) => InvokeUnorderedAsync(
                @this,
                sender,
                eventArgs,
                true);

        public static Task InvokeUnorderedAsync(
            this EventHandler @this,
            object sender,
            EventArgs eventArgs,
            bool stopOnFirstError) => InvokeAsync(
                @this,
                sender,
                eventArgs,
                false,
                stopOnFirstError);

        public static Task InvokeOrderedAsync(
            this EventHandler @this,
            object sender,
            EventArgs eventArgs) => InvokeOrderedAsync(
                 @this,
                 sender,
                 eventArgs,
                 true);

        public static Task InvokeOrderedAsync(
            this EventHandler @this,
            object sender,
            EventArgs eventArgs,
            bool stopOnFirstError) => InvokeAsync(
                @this,
                sender,
                eventArgs,
                true,
                stopOnFirstError);

        public static Task InvokeAsync(
            this EventHandler @this,
            object sender,
            EventArgs eventArgs,
            bool forceOrdering,
            bool stopOnFirstError) => MulticastDelegateExtensions.InvokeAsync(
                @this,
                sender,
                eventArgs,
                forceOrdering,
                stopOnFirstError);
    }
}
