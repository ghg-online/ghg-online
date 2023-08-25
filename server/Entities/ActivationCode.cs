namespace server.Entities
{
    public class ActivationCode
    {
        public int Id { get; set; }
        public string Code { get; set; }

        public ActivationCode()
        {
            Code = Guid.NewGuid().ToString();
        }
    }
}
