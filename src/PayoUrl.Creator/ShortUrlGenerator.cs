namespace PayoUrl.Creator;

public class ShortUrlGenerator(RedisIncrementalIdGenerator redisIncrementalIdGenerator)
{
    public class GenerationException() : Exception();
    public async Task<string> Create(string longUrl)
    {
        long newId = -1;
        try
        {
            newId = await redisIncrementalIdGenerator.GetId();
        }
        catch (RedisIncrementalIdGenerator.RetrievalException)
        {
            throw new GenerationException();
        }
        return string.Empty;
    }
}