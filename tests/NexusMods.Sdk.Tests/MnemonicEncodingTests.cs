using System.Diagnostics;
using System.Text;
using NexusMods.Sdk.IO;

namespace NexusMods.Sdk.Tests;

public class MnemonicEncodingTests
{
    [Test]
    [Arguments("jom18z", 27421098889UL, "cyst-hashtag-goalie")]
    [Arguments("tckf0m", 13322461331UL, "amnesia-baptize-conduct")]
    [Arguments("g14kxi", 8978556614UL, "hardwood-champion-brine")]
    public async Task Test(string slug, ulong expectedEncodedValue, string expectedEncodedMnemonic)
    {
        var encoded = SlugEncoding.Encode(slug);
        await Assert.That(encoded).IsEqualTo(expectedEncodedValue);

        var decoded = SlugEncoding.Decode(encoded);
        await Assert.That(decoded).IsEqualTo(slug);

        var options = await GetOptions();
        var encodedString = MnemonicEncoding.Encode(encoded, options);
        await Assert.That(encodedString).IsEqualTo(expectedEncodedMnemonic);

        var decodedValue = MnemonicEncoding.Decode(encodedString, options);
        await Assert.That(decodedValue).IsEqualTo(encoded);
    }

    private static async ValueTask<MnemonicEncodingOptions> GetOptions()
    {
        // slugs: 6 characters -> 36 bits
        // 256 words -> 8 bits per word
        // 1024 words -> 10 bits per word
        // 2048 words -> 11 bits per word
        // 4096 words -> 12 bits per word
        // 36 bits / 3 words = 12 bits per word

        var streamFactory = new EmbeddedResourceStreamFactory<MnemonicEncodingTests>("NexusMods.Sdk.Tests.wordlist4096.txt");
        await using var stream = await streamFactory.GetStreamAsync();
        using var reader = new StreamReader(stream);

        var words = new string[4096];
        var i = 0;

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            words[i++] = line;
        }

        return new MnemonicEncodingOptions(Words: words, NumWords: 3, BitsPerWord: 12);
    }
}

public record MnemonicEncodingOptions(string[] Words, int NumWords, int BitsPerWord);

public static class MnemonicEncoding
{
    public static string Encode(ulong input, MnemonicEncodingOptions options)
    {
        var sb = new StringBuilder();

        var mask = (1 << options.BitsPerWord) - 1;
        Debug.Assert(mask <= options.Words.Length);

        for (var i = 0; i < options.NumWords; i++)
        {
            var lower = input >> options.BitsPerWord * i;
            var index = (int)lower & mask;

            var word = options.Words[index];

            if (i is not 0) sb.Append('-');
            sb.Append(word);
        }

        return sb.ToString();
    }

    public static ulong Decode(ReadOnlySpan<char> input, MnemonicEncodingOptions options)
    {
        ulong output = 0;
        var numWords = 0;

        var spanSplitEnumerator = input.Split('-');
        foreach (var range in spanSplitEnumerator)
        {
            var word = input[range];
            var index = GetIndex(options.Words, word);
            if (index == -1) return 0;

            var value = ((ulong)index) << (options.BitsPerWord * numWords);
            output |= value;
            numWords += 1;
        }

        return output;
    }

    private static int GetIndex(ReadOnlySpan<string> words, ReadOnlySpan<char> word)
    {
        for (var i = 0; i < words.Length; i++)
        {
            var current = words[i];
            if (current.AsSpan().Equals(word, StringComparison.Ordinal)) return i;
        }

        return -1;
    }
}

public static class SlugEncoding
{
    private const byte SlugLength = 6;
    private const byte BitsPerValue = 6;
    private const string AllowedCharacters = "abcdefghijklmnopqrstuvwxyz0123456789";

    public static ulong Encode(ReadOnlySpan<char> input)
    {
        if (input.Length != SlugLength) throw new ArgumentException("Input must have a length of 6", nameof(input));

        ulong output = 0;
        var currentBitIndex = 0;

        for (var i = 0; i < SlugLength; i++)
        {
            var c = input[i];

            var index = AllowedCharacters.IndexOf(c, StringComparison.Ordinal);
            Debug.Assert(index >= 0 && index < AllowedCharacters.Length);

            var value = (ulong)index << currentBitIndex;
            output |= value;

            currentBitIndex += BitsPerValue;
        }

        return output;
    }

    public static string Decode(ulong input)
    {
        const int numEmptyBits = (sizeof(ulong) * 8) - (SlugLength * BitsPerValue);

        Span<char> span = stackalloc char[SlugLength];

        for (var i = 0; i < SlugLength; i++)
        {
            var bitsToMove = ((SlugLength - i - 1) * BitsPerValue) + numEmptyBits;

            var upper = input << bitsToMove;
            var index = (int)(upper >> ((sizeof(ulong) * 8) - BitsPerValue));

            Debug.Assert(index < AllowedCharacters.Length);
            var c = AllowedCharacters[index];

            span[i] = c;
        }

        return span.ToString();
    }
}
