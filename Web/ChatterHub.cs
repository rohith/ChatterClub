using CreativeColon.ChatterClub.Web.ViewModels;
using Microsoft.AspNet.SignalR;
using System;
using System.Threading.Tasks;

namespace CreativeColon.ChatterClub.Web
{
    public class ChatterHub : Hub
    {
        readonly static UserMap UserMap = new UserMap();

        public override Task OnConnected()
        {
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            DisconnectUser();
            return base.OnDisconnected(stopCalled);
        }

        public override Task OnReconnected()
        {
            return base.OnReconnected();
        }

        public void GetStarted(string email, string username, bool isRequestAutomated)
        {
            var AgentIdentifier = Utility.GetAgentIdentifier(Context.Request);
            var Connection = new Connection { ConnectionId = Context.ConnectionId, AgentIdentifier = AgentIdentifier };

            var IsUserActive = UserMap.IsUserActive(email, AgentIdentifier);

            if (IsUserActive)
            {
                var IsAuthenticated = UserMap.IsAuthenticated(email, Connection);

                if (IsAuthenticated)
                {
                    UserAuthenticated();
                    Clients.Caller.AuthenticationCallback(IsAuthenticated);
                }

                else
                {
                    Clients.Caller.SendUserToLoginScreen();
                }
            }

            else
            {
                if (isRequestAutomated)
                    Clients.Caller.SendUserToLoginScreen();

                else
                {
                    var AuthenticationCode = Randomizer.GenerateString(5);
                    Utility.MailCode(username, email, AuthenticationCode);

                    // TODO: Save to DB
                    UserMap.AddUser(email, username);
                    UserMap.SetAuthenticationCode(email, AuthenticationCode);
                    UserMap.AddConnection(email, Connection);

                    Clients.Caller.ProceedToVerifyScreen();
                }
            }
        }

        public void Verify(string code)
        {
            var IsAuthenticated = UserMap.Authenticate(code, Context.ConnectionId);
            Clients.Caller.AuthenticationCallback(IsAuthenticated);

            if (IsAuthenticated)
                UserAuthenticated();
        }

        // TODO: After implementing DB
        //public void GetUserInfo()
        //{
        //    AuthenticationStatus(() =>
        //    {
        //        using (var ClubDB = new ClubContext())
        //        {
        //            var UserDetail = UserMap.GetUserDetail(Context.ConnectionId);
        //            var UserId = ClubDB.People.Where(u => u.Email.Equals(UserDetail.Email)).Select(u => u.Id).Single();
        //            var RoomIds = ClubDB.Rooms.Where(r => r.People.Any(u => u.Id.Equals(UserId))).Select(r => r.Id).ToList();
        //            var History = ClubDB.Conversations
        //                                .Where(c => c.FromPersonId.Equals(UserId)
        //                                            || c.ToPersonId.Equals(UserId)
        //                                            || (c.ToRoomId.HasValue && RoomIds.Contains(c.ToRoomId.Value)));

        //            var UserHistory = History.GroupBy(c => new { SourceId = c.FromPersonId, TargetId = c.ToPersonId.Value, IsGroup = false });
        //            var GroupHistory = History.GroupBy(c => new { SourceId = c.FromPersonId, TargetId = c.ToRoomId.Value, IsGroup = true });

        //            Clients.Caller.ShowUserHistory(UserHistory);
        //        }
        //    });
        //}

        public void Send(string targetUserEmail, string message)
        {
            if (!string.IsNullOrWhiteSpace(targetUserEmail) && !string.IsNullOrWhiteSpace(message))
            {
                var FromUserDetail = UserMap.GetUserDetail(Context.ConnectionId);
                var SourceActiveConnections = UserMap.GetUserConnections(FromUserDetail.Email);
                var TargetActiveConnections = UserMap.GetUserConnections(targetUserEmail);
                Clients.Clients(SourceActiveConnections).SentMessage(targetUserEmail, message);
                Clients.Clients(TargetActiveConnections).ReceiveMessage(FromUserDetail.Email, message);
            }
        }

        void UserAuthenticated()
        {
            var UserDetail = UserMap.GetUserDetail(Context.ConnectionId);
            var OtherUsers = UserMap.GetOtherUsers(UserDetail.Email);
            var ActiveConnections = UserMap.GetOtherConnections(UserDetail.Email);
            Clients.Caller.ActiveUsers(OtherUsers);
            Clients.Clients(ActiveConnections).UserOnline(UserDetail);
        }

        void AuthenticationStatus(Action callback)
        {
            if (UserMap.IsAuthenticated(Context.ConnectionId))
                callback();
            else
                Clients.Caller.SendUserToLoginScreen();
        }

        void DisconnectUser()
        {
            var UserDetail = UserMap.GetUserDetail(Context.ConnectionId);

            if (UserDetail != null)
            {
                UserMap.MarkOffline(Context.ConnectionId);
                UserMap.Cleanup(Context.ConnectionId);

                if (!UserMap.IsUserOnline(UserDetail.Email))
                {
                    Clients.Others.UserOffline(UserDetail);
                    Clients.Caller.SendUserToLoginScreen();
                }
            }
        }
    }
}
