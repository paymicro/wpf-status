namespace WpfStatus.api
{
    public class Layer: IComparable, IComparable<Layer>
    {
        public int Number { get; set; }

        public int CompareTo(Layer? other) => Number.CompareTo(other?.Number ?? 0);

        public int CompareTo(object? obj)
        {
            if (obj is Layer layer)
            {
                return CompareTo(layer);
            }
            return 0;
        }

        public override string ToString() => Number.ToString();
    }
}
