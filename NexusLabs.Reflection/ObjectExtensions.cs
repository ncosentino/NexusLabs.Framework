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
                throw new ArgumentNullException(nameof(obj));
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
                throw new ArgumentNullException(nameof(obj));
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

        public static void SetField<T>(
           this object obj,
           string fieldName,
           T value)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
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
                throw new ArgumentNullException(nameof(obj));
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
    }
}