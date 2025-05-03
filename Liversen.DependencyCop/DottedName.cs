using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Liversen.DependencyCop
{
    public sealed class DottedName
    {
        public DottedName(string value)
        {
            Value = value;
            Parts = value.Split('.').ToImmutableArray();
            if (string.IsNullOrEmpty(value) || Parts.IsEmpty)
            {
                throw new ArgumentException("Invalid value");
            }

            if (Parts.Any(string.IsNullOrWhiteSpace))
            {
                throw new ArgumentException("Invalid value");
            }
        }

        public string Value { get; }

        public ImmutableArray<string> Parts { get; }

        public static DottedName Create(IEnumerable<string> parts) =>
            new DottedName(string.Join('.', parts));

        public static (DottedName Value1, DottedName Value2)? TakeIncludingFirstDifferingPart(DottedName value1, DottedName value2)
        {
            for (var i = 0; i < Math.Min(value1.Parts.Length, value2.Parts.Length); ++i)
            {
                if (value1.Parts[i] != value2.Parts[i])
                {
                    return (value1.Take(i + 1), value2.Take(i + 1));
                }
            }

            return null;
        }

        public DottedName? SkipCommonPrefix(DottedName other)
        {
            for (var i = 0; i < Parts.Length; ++i)
            {
                if (other.Parts.Length <= i || other.Parts[i] != Parts[i])
                {
                    return Skip(i);
                }
            }

            return null;
        }

        public DottedName Skip(int count) =>
            Create(Parts.Skip(count));

        public DottedName Take(int count) =>
            Create(Parts.Take(count));

        public override string ToString() =>
            Value;

        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((DottedName)obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        bool Equals(DottedName other)
        {
            return Value == other.Value;
        }
    }
}
