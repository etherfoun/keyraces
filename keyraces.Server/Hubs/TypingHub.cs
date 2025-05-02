using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace keyraces.Server.Hubs
{
    public class TypingHub : Hub
    {
        public Task JoinRace(int competitionId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"race-{competitionId}");

        public Task ReportProgress(int competitionId, int userId, double wpm) =>
            Clients.Group($"race-{competitionId}")
                   .SendAsync("ReceiveProgress", userId, wpm);
    }
}
