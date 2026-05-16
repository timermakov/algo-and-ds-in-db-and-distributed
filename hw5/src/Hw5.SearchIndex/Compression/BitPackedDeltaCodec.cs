using System.Buffers.Binary;

namespace Hw5.SearchIndex.Compression;

public static class BitPackedDeltaCodec
{
    public static byte[] Encode(IReadOnlyList<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var count = values.Count;
        if (count == 0)
        {
            return BitConverter.GetBytes(0);
        }

        var max = 0;
        for (var i = 0; i < count; i++)
        {
            if (values[i] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(values), "Only non-negative values are supported.");
            }

            if (values[i] > max)
            {
                max = values[i];
            }
        }

        var bitWidth = Math.Max(1, 32 - int.LeadingZeroCount(max));
        var packedLength = (count * bitWidth + 7) / 8;
        var output = new byte[8 + packedLength];
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(0, 4), count);
        BinaryPrimitives.WriteInt32LittleEndian(output.AsSpan(4, 4), bitWidth);

        var bitOffset = 0;
        for (var i = 0; i < count; i++)
        {
            WriteBits(output.AsSpan(8), bitOffset, bitWidth, values[i]);
            bitOffset += bitWidth;
        }

        return output;
    }

    public static int[] Decode(ReadOnlySpan<byte> encoded)
    {
        if (encoded.Length < 4)
        {
            throw new InvalidDataException("Corrupt encoded stream: too short.");
        }

        var count = BinaryPrimitives.ReadInt32LittleEndian(encoded[..4]);
        if (count == 0)
        {
            return [];
        }

        if (encoded.Length < 8)
        {
            throw new InvalidDataException("Corrupt encoded stream: missing bit-width.");
        }

        var bitWidth = BinaryPrimitives.ReadInt32LittleEndian(encoded.Slice(4, 4));
        var values = new int[count];
        var bits = encoded[8..];
        var bitOffset = 0;
        for (var i = 0; i < count; i++)
        {
            values[i] = ReadBits(bits, bitOffset, bitWidth);
            bitOffset += bitWidth;
        }

        return values;
    }

    public static int[] DeltaEncode(IReadOnlyList<int> sortedValues)
    {
        ArgumentNullException.ThrowIfNull(sortedValues);
        var deltas = new int[sortedValues.Count];
        var previous = 0;
        for (var i = 0; i < sortedValues.Count; i++)
        {
            var value = sortedValues[i];
            if (value < previous)
            {
                throw new ArgumentException("Values must be sorted ascending.", nameof(sortedValues));
            }

            deltas[i] = value - previous;
            previous = value;
        }

        return deltas;
    }

    public static int[] DeltaDecode(IReadOnlyList<int> deltas)
    {
        ArgumentNullException.ThrowIfNull(deltas);
        var values = new int[deltas.Count];
        var current = 0;
        for (var i = 0; i < deltas.Count; i++)
        {
            current += deltas[i];
            values[i] = current;
        }

        return values;
    }

    private static void WriteBits(Span<byte> buffer, int bitOffset, int bitWidth, int value)
    {
        for (var bit = 0; bit < bitWidth; bit++)
        {
            if (((value >> bit) & 1) == 0)
            {
                continue;
            }

            var offset = bitOffset + bit;
            var byteIndex = offset / 8;
            var bitIndex = offset % 8;
            buffer[byteIndex] |= (byte)(1 << bitIndex);
        }
    }

    private static int ReadBits(ReadOnlySpan<byte> buffer, int bitOffset, int bitWidth)
    {
        var value = 0;
        for (var bit = 0; bit < bitWidth; bit++)
        {
            var offset = bitOffset + bit;
            var byteIndex = offset / 8;
            var bitIndex = offset % 8;
            if ((buffer[byteIndex] & (1 << bitIndex)) != 0)
            {
                value |= 1 << bit;
            }
        }

        return value;
    }
}
