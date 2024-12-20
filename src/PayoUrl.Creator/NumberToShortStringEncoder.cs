using Sqids;

namespace PayoUrl.Creator;
public class NumberToShortStringEncoder
{
    private const long MaxValue = 9007199254740990;
    private const string Alphabet = "x42M8NUpQKudYo3cB7n5hvRmOb9VzwqjIHGAWtT1XeFl6rCPsyf0EZaSJLgkDi";
    private readonly SqidsEncoder<long> _encoder = new(new SqidsOptions
    {
        Alphabet = Alphabet,
        MinLength = 6
    });
    public string Encode(long id)
    {
        if (id > MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "The ID is too large to be encoded.");
        }
        return _encoder.Encode(id);
    }
}