using System;
using System.Diagnostics.CodeAnalysis;

namespace Liversen.DependencyCop
{
    [SuppressMessage("", "S4035:Seal class 'Equatable' or implement IEqualityComparer<T>1 instead.", Justification = "False positive.")]
    public abstract class Equatable<TValue> : IEquatable<TValue>
        where TValue : Equatable<TValue>
    {
        public static bool operator ==(Equatable<TValue> left, Equatable<TValue> right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(Equatable<TValue> left, Equatable<TValue> right) =>
            !(left == right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((TValue)obj);
        }

        public bool Equals(TValue other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return EqualsInner(other);
        }

        public override int GetHashCode() =>
            GetHashCodeInner();

        protected abstract int GetHashCodeInner();

        protected abstract bool EqualsInner(TValue other);
    }
}
