using System;

namespace Autofac
{
    /// <summary>
    /// An <see cref="Attribute"/> for decorating types that can be discovered 
    /// for usage with Autofac.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class DiscoverableForRegistrationAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of 
        /// <see cref="DiscoverableForRegistrationAttribute"/>.
        /// </summary>
        /// <param name="discoveryRegistrationMode">
        /// Defines how the registration should work.
        /// </param>
        /// <param name="singleInstance">
        /// <c>true</c> if the registration should be a singleton; Otherwise, 
        /// <c>false</c>.
        /// </param>
        public DiscoverableForRegistrationAttribute(
            DiscoveryRegistrationMode discoveryRegistrationMode = DiscoveryRegistrationMode.Self,
            bool singleInstance = false)
        {
            DiscoveryRegistrationMode = discoveryRegistrationMode;
            SingleInstance = singleInstance;
        }

        /// <summary>
        /// Gets a value indicating the mode of registration.
        /// </summary>
        public DiscoveryRegistrationMode DiscoveryRegistrationMode { get; }

        /// <summary>
        /// Gets a value indicating whether or not the registration should be 
        /// a singleton.
        /// </summary>
        public bool SingleInstance { get; }
    }
}
