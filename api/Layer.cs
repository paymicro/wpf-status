using System.Text.Json.Serialization;

namespace WpfStatus.api
{
    public class Layer
    {
        public int Number { get; set; }

        public override string ToString()
        {
            return Number.ToString();
        }
    }
}
