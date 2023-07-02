using System;
using System.Collections.Generic;

namespace Autofac
{
    public static class WireTapping
    {
        private static readonly Dictionary<ContainerBuilder, WireTapper> _wireTappersByContainerBuilders = new();

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                WireTapper.DefaultSingleInstance,
                WireTapper.DefaultThrowOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                WireTapper.DefaultSingleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                singleInstance,
                WireTapper.DefaultThrowOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<T, object> wireTapCallback,
            WireTapSingleInstance singleInstance,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                (c, x) => wireTapCallback.Invoke(x), 
                singleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                WireTapper.DefaultSingleInstance,
                WireTapper.DefaultThrowOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback,
            WireTapSingleInstance singleInstance)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                singleInstance,
                WireTapper.DefaultThrowOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap<T>(
                wireTapCallback,
                WireTapper.DefaultSingleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <typeparamref name="T"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <typeparam name="T">
        /// The type to wire tap.
        /// </typeparam>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <typeparamref name="T"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/> or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap<T>(
            this ContainerBuilder containerBuilder,
            Func<IComponentContext, T, object> wireTapCallback,
            WireTapSingleInstance singleInstance,
            bool throwOnNoRegisteredTypeToWireTap)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            containerBuilder.WireTap(
                typeof(T),
                (c, x) => wireTapCallback(c, (T)x),
                singleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        /// <summary>
        /// Provides wire tapping functionality that allows the specified type 
        /// <paramref name="typeToWireTap"/> to be intercepted and the result of
        /// <paramref name="wireTapCallback"/> is returned in its place.
        /// </summary>
        /// <param name="typeToWireTap">
        /// The type to wire tap.
        /// </param>
        /// <param name="containerBuilder">The <see cref="ContainerBuilder"/> instance.</param>
        /// <param name="wireTapCallback">
        /// The callback that will be used to provide an instance of type 
        /// <paramref name="typeToWireTap"/>.
        /// </param>
        /// <param name="singleInstance">
        /// Whether or not to use single instance behavior when wire tapping 
        /// types.
        /// </param>
        /// <param name="throwOnNoRegisteredTypeToWireTap">
        /// Whether or not to throw an exception when a wire tap has been 
        /// registered for a type that is not registered at the time a 
        /// <see cref="IContainer"/> is built from the 
        /// <see cref="ContainerBuilder"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="containerBuilder"/>, 
        /// <paramref name="typeToWireTap"/>, or 
        /// <paramref name="wireTapCallback"/> are null.
        /// </exception>
        public static void WireTap(
            this ContainerBuilder containerBuilder,
            Type typeToWireTap,
            Func<IComponentContext, object, object> wireTapCallback,
            WireTapSingleInstance singleInstance = WireTapper.DefaultSingleInstance,
            bool throwOnNoRegisteredTypeToWireTap = WireTapper.DefaultThrowOnNoRegisteredTypeToWireTap)
        {
            if (containerBuilder == null)
            {
                throw new ArgumentNullException(nameof(containerBuilder));
            }

            if (typeToWireTap == null)
            {
                throw new ArgumentNullException(nameof(typeToWireTap));
            }

            if (wireTapCallback == null)
            {
                throw new ArgumentNullException(nameof(wireTapCallback));
            }

            var wireTapper = GetWireTapperForContainerBuilder(containerBuilder);
            wireTapper.WireTap(
                typeToWireTap,
                wireTapCallback,
                singleInstance,
                throwOnNoRegisteredTypeToWireTap);
        }

        private static WireTapper GetWireTapperForContainerBuilder(ContainerBuilder containerBuilder)
        {
            lock (_wireTappersByContainerBuilders)
            {
                if (!_wireTappersByContainerBuilders.TryGetValue(containerBuilder, out var wireTapper))
                {
                    wireTapper = new WireTapper(containerBuilder);
                    _wireTappersByContainerBuilders[containerBuilder] = wireTapper;
                }

                return wireTapper;
            }
        }
    }
}
