using Sqids;

namespace PayoUrl.Creator;
public class ShortUrlEncoder
{
    private const string Alphabet = "x42M8NUpQKudYo3cB7n5hvRmOb9VzwqjIHGAWtT1XeFl6rCPsyf0EZaSJLgkDi";
    private readonly SqidsEncoder<long> _encoder = new(new SqidsOptions
    {
        Alphabet = Alphabet,
        MinLength = 6
    });
    public string Encode(long id) => _encoder.Encode(id);
}