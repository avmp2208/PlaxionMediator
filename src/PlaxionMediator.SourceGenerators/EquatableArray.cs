using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace PlaxionMediator.SourceGenerators;

/// <summary>
/// Immutable array wrapper with value equality for incremental generator caching.
/// Equality and hash code are stable across instances wrapping equal element sequences,
/// which is required for Roslyn's incremental pipeline to skip unchanged outputs.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    private readonly ImmutableArray<T> _array;

    /// <summary>
    /// Cached hash code. Empty/default arrays use a non-zero sentinel so they do not collide
    /// with an uninitialized "0" hash on a default struct in every case.
    /// </summary>
    private readonly int _hashCode;

    public EquatableArray(ImmutableArray<T> array)
    {
        _array = array.IsDefault ? ImmutableArray<T>.Empty : array;
        _hashCode = ComputeHashCode(_array);
    }

    private ImmutableArray<T> Items => _array.IsDefault ? ImmutableArray<T>.Empty : _array;

    public int Length => Items.Length;

    public T this[int index] => Items[index];

    public bool Equals(EquatableArray<T> other)
    {
        ImmutableArray<T> left = Items;
        ImmutableArray<T> right = other.Items;

        // Fast path: identical backing storage.
        if (left.Equals(right))
        {
            return true;
        }

        if (_hashCode != other._hashCode || left.Length != right.Length)
        {
            // When both are default structs, _hashCode is 0 on both — still compare lengths (both 0).
            if (left.Length != right.Length)
            {
                return false;
            }

            if (_hashCode != other._hashCode && left.Length != 0)
            {
                return false;
            }
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        // Default struct has _hashCode == 0; treat as empty-array hash.
        return _hashCode == 0 && Items.Length == 0 ? 1 : _hashCode;
    }

    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

    public ImmutableArray<T>.Enumerator GetEnumerator() => Items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)Items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Items).GetEnumerator();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHashCode(ImmutableArray<T> array)
    {
        unchecked
        {
            // Non-zero seed so an empty array does not collide with the "default struct" zero hash.
            int hash = 17;
            for (int i = 0; i < array.Length; i++)
            {
                hash = (hash * 31) + (array[i]?.GetHashCode() ?? 0);
            }

            return hash == 0 ? 1 : hash;
        }
    }
}
