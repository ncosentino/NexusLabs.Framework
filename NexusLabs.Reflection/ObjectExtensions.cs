using System;
using System.Linq;
using System.Reflection;

using NexusLabs.Contracts;

namespace NexusLabs.Reflection
{
    public static class ObjectExtensions
    {
        public static void SetProperty<T>(
            this object obj,
            string propertyName,
            T value)
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(propertyName, nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.SetProperty);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Could not find property '{propertyName}' on type '{type}'.");
            }

            property.SetValue(obj, value);
        }

        public static object GetProperty(
            this object obj,
            string propertyName)
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(propertyName, nameof(propertyName));

            var type = obj.GetType();
            var property = type.GetProperty(
                propertyName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.GetProperty);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Could not find property '{propertyName}' on type '{type}'.");
            }

            var value = property.GetValue(obj);
            return value;
        }

        public static T GetProperty<T>(
            this object obj,
            string propertyName)
        {
            var value = GetProperty(
                obj,
                propertyName);
            return (T)value;
        }

        public static void SetField<T>(
           this object obj,
           string fieldName,
           T value)
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(fieldName, nameof(fieldName));

            var type = obj.GetType();
            var field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.SetField);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Could not find field '{fieldName}' on type '{type}'.");
            }

            field.SetValue(obj, value);
        }

        public static object GetField(
            this object obj,
            string fieldName)
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(fieldName, nameof(fieldName));

            var type = obj.GetType();
            var field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.GetField);
            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Could not find field '{fieldName}' on type '{type}'.");
            }

            var value = field.GetValue(obj);
            return value;
        }

        public static T GetField<T>(
            this object obj,
            string fieldName)
        {
            var value = GetField(
                obj,
                fieldName);
            return (T)value;
        }

        public static object InvokeMethod(
            this object obj,
            string methodName,
            object[] parameters = null)
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(methodName, nameof(methodName));

            var type = obj.GetType();
            var method = type.GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);
            if (method == null)
            {
                throw new InvalidOperationException(
                    $"Could not find method '{methodName}' on type '{type}'.");
            }

            var value = method.Invoke(
                obj,
                parameters ?? new object[0]);
            return value;
        }

        public static T InvokeMethod<T>(
            this object obj,
            string methodName,
            object[] parameters = null)
        {
            var value = InvokeMethod(
                obj,
                methodName,
                parameters);
            return (T)value;
        }

        public static void RaiseEvent<TEventArgs>(
            this object obj,
            string eventName,
            TEventArgs eventArgs)
            where TEventArgs : EventArgs =>
            RaiseEvent<TEventArgs>(obj, eventName, obj, eventArgs);

        public static void RaiseEvent<TEventArgs>(
            this object obj,
            string eventName,
            object sender,
            TEventArgs eventArgs)
            where TEventArgs : EventArgs
        {
            ArgumentContract.RequiresNotNull(obj, nameof(obj));
            ArgumentContract.RequiresNotNull(eventName, nameof(eventName));

            var eventDelegate = obj.GetField<MulticastDelegate>(eventName);
            var parameters = new object[] { sender, eventArgs };
            var handlers = eventDelegate
                ?.GetInvocationList()
                ?.Where(x => x!= null)
                ?? new Delegate[0];

            foreach (var handler in handlers)
            {
                handler.Method.Invoke(handler.Target, parameters);
            }
        }
    }
}