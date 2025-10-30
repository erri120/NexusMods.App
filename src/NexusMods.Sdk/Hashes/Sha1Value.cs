using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Hashes;

/// <summary>
/// Represents a SHA1 value.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(Sha1JsonConverter))]
public readonly struct Sha1Value : IEquatable<Sha1Value>
{
    internal const int Size = 20;
    internal const int HexStringSize = 40;
    private readonly InlineArray _array;

    /// <summary>
    /// Gets the value as a span.
    /// </summary>
    public ReadOnlySpan<byte> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineArray, byte>(ref Unsafe.AsRef(in _array)), length: Size);

    private Sha1Value(InlineArray array)
    {
        _array = array;
    }

    /// <summary>
    /// Creates a new value from a span.
    /// </summary>
    public static Sha1Value From(ReadOnlySpan<byte> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, Size, nameof(input));
        var slice = input[..Size];

        var array = new InlineArray();
        var span = array.AsSpan();
        slice.CopyTo(span);

        return new Sha1Value(array);
    }

    /// <summary>
    /// Creates a new value from a hex string.
    /// </summary>
    public static Sha1Value FromHex(ReadOnlySpan<char> input)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(input.Length, HexStringSize, nameof(input));
        var slice = input[..(Size * 2)];

        var array = new InlineArray();
        var span = array.AsSpan();

        var status = Convert.FromHexString(slice, span, out _, out _);
        if (status != OperationStatus.Done) throw new ArgumentException($"Failed to convert from hex: status={status},input=`{input.ToString()}`", nameof(input));

        return new Sha1Value(array);
    }

    /// <summary>
    /// Writes the value as a hex string to <paramref name="destination"/>.
    /// </summary>
    public int ToHex(Span<char> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, HexStringSize, nameof(destination));

        var success = Convert.TryToHexString(AsSpan(), destination, out var charsWritten);
        Debug.Assert(success, "should always be true after destination length check");

        return charsWritten;
    }

    /// <inheritdoc/>
    public bool Equals(Sha1Value other) => other.AsSpan().SequenceEqual(AsSpan());

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Sha1Value other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => MemoryMarshal.Read<int>(AsSpan());

    /// <inheritdoc/>
    public override string ToString() => Convert.ToHexString(AsSpan());

    [InlineArray(length: Size)]
    private struct InlineArray
    {
        private byte _value;

        public Span<byte> AsSpan() => MemoryMarshal.CreateSpan(ref Unsafe.As<InlineArray, byte>(ref this), length: Size);
    }

    /// <summary/>
    public static bool operator ==(Sha1Value left, Sha1Value right) => left.Equals(right);
    /// <summary/>
    public static bool operator !=(Sha1Value left, Sha1Value right) => !(left == right);
}

[PublicAPI]
public class Sha1Hasher : IStreamingHasher<Sha1Value, SHA1, Sha1Hasher>
{
    public static Sha1Value Hash(ReadOnlySpan<byte> input) => Sha1Value.From(SHA1.HashData(input));

    public static SHA1 Initialize() => SHA1.Create();

    public static SHA1 Update(SHA1 state, ReadOnlySpan<byte> input) => throw new NotSupportedException("SHA1 hasher doesn't support updates with spans");

    public static SHA1 Update(SHA1 state, byte[] input)
    {
        state.TransformBlock(input, inputOffset: 0, inputCount: input.Length, input, outputOffset: 0);
        return state;
    }

    public static Sha1Value Finish(SHA1 state)
    {
        var bytes = state.TransformFinalBlock([], inputCount: 0, inputOffset: 0);
        return Sha1Value.From(bytes);
    }
}
