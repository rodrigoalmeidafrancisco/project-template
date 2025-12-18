using Flunt.Notifications;
using Shared.Usefuls;
using System.Text.Json.Serialization;

namespace Domain.Commands._Base
{
    public class CommandResult<T>
    {
        public CommandResult()
        {

        }

        [JsonIgnore]
        public int StatusCod { get; set; }

        public Guid? Id { get; set; } = null;
        public long? Total { get; set; } = null;
        public T Data { get; set; }

        public Guid? ErrorId { get; set; } = null;
        public string Message { get; set; } = null;
        public List<string> Errors { get; set; } = null;

        public void ReturnStatus200(T dados, long? total = null)
        {
            StatusCod = 200;
            Data = dados;
            Total = total;
        }

        public void ReturnStatus201(Guid id)
        {
            StatusCod = 201;
            Id = id;
        }

        public void ReturnStatus400(string message, List<string> errors)
        {
            StatusCod = 400;
            Message = message;
            Errors = errors;
        }

        public void ReturnStatus400Flunt(string message, IReadOnlyCollection<Notification> notificacoes)
        {
            StatusCod = 400;
            Message = message;
            Errors = notificacoes.SelectFluntNotifications();
        }

        public void ReturnStatus500(Guid erroId, string message)
        {
            StatusCod = 500;
            ErrorId = erroId;
            Message = message;
        }

    }
}
