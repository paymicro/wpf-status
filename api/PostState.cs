namespace WpfStatus.api
{
    public class PostState
    {
        public string Id { get; set; } = string.Empty;

        public string IdInHex
        {
            get
            {
                if (string.IsNullOrEmpty(Id))
                {
                    return string.Empty;
                }
                try
                {
                    var base64 = Convert.FromBase64String(Id);
                    return Convert.ToHexString(base64).ToLower();
                }
                catch
                {
                    return string.Empty;
                }
            }
        }

        public string State { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }
}
