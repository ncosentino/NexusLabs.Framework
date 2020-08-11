using System;

using Autofac;

namespace NexusLabs.Autofac
{
    public abstract class SingleRegistrationModule : Module
    {
        private static readonly string PREFIX = $"{Guid.NewGuid()}_RegistrationCount_";

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            var propertyKey = $"{PREFIX}{GetType().FullName}";
            if (builder.Properties.ContainsKey(propertyKey))
            {
                return;
            }

            builder.Properties[propertyKey] = new object();

            SafeLoad(builder);
        }

        protected abstract void SafeLoad(ContainerBuilder builder);
    }
}
