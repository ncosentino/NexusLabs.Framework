using System;
using System.Reflection;

namespace NexusLabs.Reflection
{
    public static class ObjectExtensions
    {
        public static void SetProperty<T>(
            this object obj,
            string propertyName,
            T value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(
                    nameof(obj),
                    "The object cannot be null.");
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(
                    nameof(propertyName),
                    "The property name cannot be null.");
            }

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
            if (obj == null)
            {
                throw new ArgumentNullException(
                    nameof(obj),
                    "The object cannot be null.");
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(
                    nameof(propertyName),
                    "The property name cannot be null.");
            }

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
            if (obj == null)
            {
                throw new ArgumentNullException(
                    nameof(obj),
                    "The object cannot be null.");
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(
                    nameof(fieldName),
                    "The field name cannot be null.");
            }

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
            if (obj == null)
            {
                throw new ArgumentNullException(
                    nameof(obj),
                    "The object cannot be null.");
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(
                    nameof(fieldName),
                    "The field name cannot be null.");
            }

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
            if (obj == null)
            {
                throw new ArgumentNullException(
                    nameof(obj),
                    "The object cannot be null.");
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(
                    nameof(methodName),
                    "The method name cannot be null.");
            }

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
    }
}