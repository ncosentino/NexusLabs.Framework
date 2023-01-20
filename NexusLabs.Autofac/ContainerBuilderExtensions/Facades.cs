using System;
using System.Collections.Generic;
using System.Linq;

using Autofac.Builder;
using Autofac.Core;

namespace Autofac
{
    /// <summary>
    /// A class that contains extension methods for working with facade design 
    /// patterns.
    /// </summary>
    public static class Facades
    {
        /// <summary>
        /// Registers a facade implementation based on discoverable sources.
        /// </summary>
        /// <typeparam name="TFacade">
        /// The <see cref="Type"/> of the facade. This must inherit from 
        /// <typeparamref name="TSource"/>.
        /// </typeparam>
        /// <typeparam name="TSource">
        /// The <see cref="Type"/> of the source. Sources must be marked as 
        /// discoverable with the 
        /// <see cref="DiscoverableForRegistrationAttribute"/> and registered 
        /// via the <see cref="TypeRegistration.RegisterDiscoverableTypes(ContainerBuilder, System.Reflection.Assembly)"/> 
        /// extension method.
        /// </typeparam>
        /// <param name="containerBuilder">
        /// The instance of the <see cref="ContainerBuilder"/>.
        /// </param>
        /// <returns>
        /// A <see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/> 
        /// allowing further configuration.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the name of the constructor parameter for the sources 
        /// cannot be inferred.
        /// </exception>
        /// <remarks>
        /// The expected usage for this is to have an interface that multiple 
        /// service implementations use. The facade class is expected to 
        /// implement this same interface and takes the collection of 
        /// registered interfaces as a parameter
        /// </remarks>
        /// <example>
        /// <code>
        /// private interface IService { }
        /// 
        /// [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        /// private sealed class ImplementationA : IService { }
        /// 
        /// [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        /// private sealed class ImplementationB : IService { }
        /// 
        /// private class SomeDependency { }
        /// 
        /// private class TheFacade : IService
        /// {
        ///     private IReadOnlyCollection&lt;IService&gt; _services;
        /// 
        ///     public TheFacade(
        ///         SomeDependency someDependency,
        ///         IReadOnlyCollection&lt;IService&gt; services)
        ///     {
        ///         _services = services;
        ///     }
        /// }
        /// 
        /// public void Main()
        /// {
        ///     var containerBuilder = new ContainerBuilder();
        ///     var containerBuilder = new ContainerBuilder();
        ///     containerBuilder.RegisterDiscoverableTypes(Assembly.GetExecutingAssembly());
        ///     containerBuilder
        ///         .RegisterType&lt;SomeDependency&gt;()
        ///         .SingleInstance();
        ///     containerBuilder
        ///         .RegisterFacadeWithDiscoverableSources&lt;TFacade, IService&gt;()
        ///         .SingleInstance()
        ///         .AsImplementedInterfaces();
        ///     var container = containerBuilder.Build();
        /// 
        ///     // this instance should be guaranteed to be the facade
        ///     var service = container.Resolve&lt;IService&gt;();
        /// }
        /// </code>
        /// </example>
        public static IRegistrationBuilder<TFacade, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterFacadeWithDiscoverableSources<TFacade, TSource>(
            this ContainerBuilder containerBuilder)
            where TFacade : notnull, TSource
        {
            var parameter = typeof(TFacade)
                .GetConstructors()
                .Select(ctor => ctor
                    .GetParameters()
                    .FirstOrDefault(para => typeof(IEnumerable<TSource>).IsAssignableFrom(para.ParameterType)))
                .FirstOrDefault(x => x != null);
            if (parameter == null)
            {
                throw new InvalidOperationException(
                    $"Could automatically determine the constructor parameter " +
                    $"name to use for resolution for facade of type " +
                    $"'{typeof(TFacade)}' with source parameter of type " +
                    $"'{typeof(TSource)}'.");
            }

            return containerBuilder.RegisterFacade<TFacade, TSource>(parameter.Name);
        }

        /// <summary>
        /// Registers a facade implementation based on discoverable sources.
        /// </summary>
        /// <typeparam name="TFacade">
        /// The <see cref="Type"/> of the facade. This must inherit from 
        /// <typeparamref name="TSource"/>.
        /// </typeparam>
        /// <typeparam name="TSource">
        /// The <see cref="Type"/> of the source. Sources must be marked as 
        /// discoverable with the 
        /// <see cref="DiscoverableForRegistrationAttribute"/> and registered 
        /// via the <see cref="TypeRegistration.RegisterDiscoverableTypes(ContainerBuilder, System.Reflection.Assembly)"/> 
        /// extension method.
        /// </typeparam>
        /// <param name="containerBuilder">
        /// The instance of the <see cref="ContainerBuilder"/>.
        /// </param>
        /// <param name="constructorParameterNameForSource">
        /// The name of the parameter in the constructor to provide source 
        /// instances for.
        /// </param>
        /// <returns>
        /// A <see cref="IRegistrationBuilder{TLimit, TActivatorData, TRegistrationStyle}"/> 
        /// allowing further configuration.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the name of the constructor parameter for the sources 
        /// cannot be inferred.
        /// </exception>
        /// <remarks>
        /// The expected usage for this is to have an interface that multiple 
        /// service implementations use. The facade class is expected to 
        /// implement this same interface and takes the collection of 
        /// registered interfaces as a parameter
        /// </remarks>
        /// <example>
        /// <code>
        /// private interface IService { }
        /// 
        /// [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        /// private sealed class ImplementationA : IService { }
        /// 
        /// [DiscoverableForRegistration(DiscoveryRegistrationMode.ImplementedInterfaces, true)]
        /// private sealed class ImplementationB : IService { }
        /// 
        /// private class SomeDependency { }
        /// 
        /// private class TheFacade : IService
        /// {
        ///     private IReadOnlyCollection&lt;IService&gt; _services;
        /// 
        ///     public TheFacade(
        ///         SomeDependency someDependency,
        ///         IReadOnlyCollection&lt;IService&gt; services)
        ///     {
        ///         _services = services;
        ///     }
        /// }
        /// 
        /// public void Main()
        /// {
        ///     var containerBuilder = new ContainerBuilder();
        ///     var containerBuilder = new ContainerBuilder();
        ///     containerBuilder.RegisterDiscoverableTypes(Assembly.GetExecutingAssembly());
        ///     containerBuilder
        ///         .RegisterType&lt;SomeDependency&gt;()
        ///         .SingleInstance();
        ///     containerBuilder
        ///         .RegisterFacadeWithDiscoverableSources&lt;TFacade, IService&gt;()
        ///         .SingleInstance()
        ///         .AsImplementedInterfaces();
        ///     var container = containerBuilder.Build();
        /// 
        ///     // this instance should be guaranteed to be the facade
        ///     var service = container.Resolve&lt;IService&gt;();
        /// }
        /// </code>
        /// </example>
        public static IRegistrationBuilder<TFacade, ConcreteReflectionActivatorData, SingleRegistrationStyle> RegisterFacade<TFacade, TSource>(
            this ContainerBuilder containerBuilder,
            string constructorParameterNameForSource)
            where TFacade : notnull, TSource
        {
            return containerBuilder
                .RegisterType<TFacade>()
                .WithParameter(
                    (p, c) => p.Name == constructorParameterNameForSource,
                    (p, c) => c.ResolveKeyed<IEnumerable<TSource>>(typeof(DiscoverableForRegistrationAttribute)))
                .OnActivated(x =>
                {
                    var services = x.Context.Resolve<IEnumerable<TSource>>().ToArray();
                    if (services.Length != 1)
                    {
                        throw new DependencyResolutionException(
                            $"There should only be one resolution for type " +
                            $"'{typeof(TSource)}' but there were " +
                            $"{services.Length}.\r\n" +
                            $"{string.Join("\r\n", services.Select(x => $"\t{x.GetType()}"))}");
                    }
                });
        }
    }
}
