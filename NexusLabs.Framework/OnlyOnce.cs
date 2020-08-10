using System;
using System.Threading.Tasks;

namespace NexusLabs.Framework
{
    public sealed class OnlyOnce : IOnlyOnce
    {
        private Lazy<object> _lazyOnce;

        public OnlyOnce(Action once, params Action[] followupActions)
        {
            _lazyOnce = new Lazy<object>(() =>
            {
                once.Invoke();

                foreach (var action in followupActions ?? new Action[0])
                {
                    action.Invoke();
                }

                return new object();
            });
        }

        public void Run()
        {
            var _ = _lazyOnce.Value;
        }

        public void RunAsync() => Task
            .Run(Run)
            .ConfigureAwait(false);
    }

    public sealed class OnlyOnce<T> : IOnlyOnce
    {
        private OnlyOnce _delayed;

        public OnlyOnce(Lazy<T> lazy)
        {
            _delayed = new OnlyOnce(() =>
            {
                var _ = lazy.Value;
            });
        }

        public void Run() => _delayed.Run();

        public void RunAsync() => _delayed.RunAsync();
    }
}
