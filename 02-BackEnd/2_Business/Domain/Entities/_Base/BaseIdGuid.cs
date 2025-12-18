namespace Domain.Entities._Base
{
    public class BaseIdGuid
    {
        public BaseIdGuid()
        {

        }

        public BaseIdGuid(string userLog)
        {
            UserLog = userLog;
        }

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserLog { get; set; }
        public DateTime DateChange { get; set; } = DateTime.Now;

        public void AtualizarBase(string usuarioLog)
        {
            UserLog = usuarioLog.Trim();
            DateChange = DateTime.Now;
        }
    }
}
