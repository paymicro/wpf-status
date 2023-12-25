namespace WpfStatus.api
{
    public class Layer: IComparable<Layer>
    {
        public int Number { get; set; }

        public int CompareTo(Layer? other) => Number.CompareTo(other?.Number ?? 0);

        public override string ToString() => Number.ToString();
    }
}
