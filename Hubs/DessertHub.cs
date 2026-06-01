using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AspireApp1.Server.Hubs
{
    [Authorize]
    public class DessertHub : Hub
    {
    }
}
