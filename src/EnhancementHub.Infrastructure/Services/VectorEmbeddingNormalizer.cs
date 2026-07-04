namespace EnhancementHub.Infrastructure.Services;

internal static class VectorEmbeddingNormalizer
{
    public static float[] Normalize(float[] embedding, int dimensions)
    {
        if (embedding.Length == dimensions)
        {
            return embedding;
        }

        var normalized = new float[dimensions];
        for (var i = 0; i < dimensions; i++)
        {
            normalized[i] = embedding[i % embedding.Length];
        }

        return normalized;
    }
}
