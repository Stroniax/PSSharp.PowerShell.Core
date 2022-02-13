using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PSSharp
{
    /// <summary>
    /// Helper class for primitive copy-and-update methods that allows copying a property value to a cloned
    /// instance, or updating with a null or non-null value. This is a non-generic alternative in case
    /// use is desired by PowerShell, since the PowerShell system does not provide sufficient support for
    /// generic types.
    /// </summary>
    public struct DefaultOrUpdate
        : IEquatable<DefaultOrUpdate>
    {
        public DefaultOrUpdate(object? value)
        {
            _isNotDefault = true;
            Value = value;
        }
        private readonly bool _isNotDefault;
        /// <summary>
        /// Indicates that the value should be left as its default value.
        /// </summary>
        public bool IsDefault => !_isNotDefault;
        /// <summary>
        /// The value to set, if any.
        /// </summary>
        public object? Value { get; }
        public object? GetValueOrDefault(object? def)
        {
            if (IsDefault)
            {
                return def;
            }
            else
            {
                return Value;
            }
        }
        /// <summary>
        /// Gets a generic version of the <see cref="DefaultOrUpdate"/> value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="InvalidCastException">Thrown if the value is not an instance of <typeparamref name="T"/>.</exception>
        public DefaultOrUpdate<T> MakeGeneric<T>()
        {
            if (IsDefault)
            {
                return new DefaultOrUpdate<T>();
            }
            else
            {
                return new DefaultOrUpdate<T>((T)Value!);
            }
        }
        /// <summary>
        /// Gets a generic version of the <see cref="DefaultOrUpdate"/> value.
        /// </summary>
        /// <param name="genericType">The generic type of the <see cref="DefaultOrUpdate{T}"/> to convert to.</param>
        /// <returns></returns>
        /// <exception cref="InvalidCastException">Thrown if the value is not an instance of <paramref name="genericType"/>.</exception>
        public object MakeGeneric(Type genericType)
        {
            return typeof(DefaultOrUpdate)
                .GetMethod(nameof(MakeGeneric), Type.EmptyTypes)!
                .MakeGenericMethod(genericType)
                .Invoke(this, null)!;
        }

        public bool Equals(DefaultOrUpdate other)
             => _isNotDefault == other._isNotDefault
            && EqualityComparer<object?>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj)
            => obj is DefaultOrUpdate other && Equals(other);

        public override int GetHashCode()
        {
            int hashCode = 1979116530;
            hashCode = hashCode * -1521134295 + _isNotDefault.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<object?>.Default.GetHashCode(Value);
            return hashCode;
        }
    }
    /// <summary>
    /// Helper class for primitive copy-and-update methods that allows copying a property value to a cloned
    /// instance, or updating with a null or non-null value.
    /// </summary>
    /// <typeparam name="T">The type of the property.</typeparam>
    public struct DefaultOrUpdate<T> : IEquatable<DefaultOrUpdate<T>>
    {
        public DefaultOrUpdate(T value)
        {
            _isNotDefault = true;
            Value = value;
        }
        private readonly bool _isNotDefault;
        /// <summary>
        /// Indicates that the value should be left as its default value.
        /// </summary>
        public bool IsDefault => !_isNotDefault;
        /// <summary>
        /// The value to set, if any.
        /// </summary>
        public T Value { get; }
        public T GetValueOrDefault(T def)
        {
            if (IsDefault)
            {
                return def;
            }
            else
            {
                return Value;
            }
        }
        /// <summary>
        /// Creates a non-generic type of the current instance.
        /// </summary>
        /// <returns>A non-generic type of the current instance.</returns>
        public DefaultOrUpdate MakeNonGeneric()
        {
            if (IsDefault)
            {
                return new DefaultOrUpdate();
            }
            else
            {
                return new DefaultOrUpdate(Value);
            }
        }

        public override bool Equals(object? obj)
            => obj is DefaultOrUpdate<T> other && Equals(other);

        public bool Equals(DefaultOrUpdate<T> other)
            => _isNotDefault == other._isNotDefault
                && EqualityComparer<T>.Default.Equals(Value, other.Value);

        public override int GetHashCode()
        {
            int hashCode = 1979116530;
            hashCode = hashCode * -1521134295 + _isNotDefault.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<T>.Default.GetHashCode(Value);
            return hashCode;
        }

        /// <summary>
        /// Cast operator to create an update value.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator DefaultOrUpdate<T>(T value) => new DefaultOrUpdate<T>(value);
        /// <summary>
        /// Cast operator for implicit PowerShell conversion support.
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator DefaultOrUpdate(DefaultOrUpdate<T> value) => value.MakeNonGeneric();
        /// <summary>
        /// Cast operator for implicit PowerShell conversion support.
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator DefaultOrUpdate<T>(DefaultOrUpdate value) => value.MakeGeneric<T>();
    }
}