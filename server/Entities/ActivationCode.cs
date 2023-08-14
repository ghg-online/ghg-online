using LiteDB;

namespace server.Entities
{
    public class ActivationCode
    {
        [BsonId]
        public string Code { get; set; }

        ActivationCode()
        {
            Code = new Guid().ToString();
        }
    }
}
