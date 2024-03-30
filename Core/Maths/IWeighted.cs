public interface IWeighted
{
    float Weight { get; set; }
}

public struct WeightedType<T> : IWeighted
{
    public float Weight { get => weight; set => weight = value; }

    public float weight;
    public T value;

    public WeightedType(T value, float weight)
    {
        this.value = value;
        this.weight = weight;
    }
}