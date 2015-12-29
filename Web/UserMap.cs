using CreativeColon.ChatterClub.Web.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CreativeColon.ChatterClub.Web
{
    class UserMap
    {
        readonly ConcurrentDictionary<string, User> ActiveUsers =
            new ConcurrentDictionary<string, User>(StringComparer.InvariantCultureIgnoreCase);

        public void AddUser(string email, string username)
        {
            ActiveUsers.TryAdd(email, new User { Email = email, Username = username });
        }

        public void SetAuthenticationCode(string email, string authenticationCode)
        {
            User User;
            ActiveUsers.TryGetValue(email, out User);
            User.AuthenticationCode = authenticationCode;
        }

        public void RemoveUser(string email)
        {
            User User;
            ActiveUsers.TryRemove(email, out User);
        }

        //public User GetUser(string email)
        //{
        //    User User = null;

        //    lock (ActiveUsers)
        //    {
        //        ActiveUsers.TryGetValue(email, out User);
        //    }

        //    return User;
        //}

        public UserDetail GetUserDetail(string connectionId)
        {
            return ActiveUsers.Where(u => u.Value.Connections.Any(c => c.Key.Equals(connectionId)))
                              .Select(u => new UserDetail { Email = u.Value.Email, Username = u.Value.Username })
                              .SingleOrDefault();
        }

        public IEnumerable<UserDetail> GetOtherUsers(string email)
        {
            return ActiveUsers.Where(u => !u.Key.Equals(email) && u.Value.Connections.Any(c => c.Value.IsOnline))
                              .Select(u => new UserDetail { Email = u.Value.Email, Username = u.Value.Username });
        }

        public bool IsUserActive(string email, string agentIdentifier)
        {
            return ActiveUsers.ContainsKey(email) && ActiveUsers.Any(u => u.Value.Connections.Any(c => c.Value.AgentIdentifier.Equals(agentIdentifier)));
        }

        public bool IsUserOnline(string email)
        {
            return ActiveUsers.Any(u => u.Key.Equals(email) && u.Value.Connections.Any(c => c.Value.IsOnline));
        }

        public void AddConnection(string email, Connection connection)
        {
            User User;

            if (ActiveUsers.TryGetValue(email, out User))
            {
                //if (User.Connections.ContainsKey(connection.ConnectionId))
                //    User.Connections[connection.ConnectionId] = connection;

                //else
                User.Connections.Add(connection.ConnectionId, connection);
            }
        }

        //public Connection GetConnection(string email, string connectionId)
        //{
        //    return ActiveUsers.Where(u => u.Value.Connections.Any(c => c.Key.Equals(connectionId)))
        //                      .Select(u => u.Value.Connections[connectionId])
        //                      .SingleOrDefault();
        //}

        //public IEnumerable<Connection> GetAllConnections()
        //{
        //    return ActiveUsers.SelectMany(u => u.Value.Connections.Select(c => c.Value));
        //}

        public IList<string> GetUserConnections(string email)
        {
            return GetActiveConnections(email);
        }

        public IList<string> GetOtherConnections(string email)
        {
            return GetActiveConnections(email, false);
        }

        public bool Authenticate(string code, string connectionId)
        {
            var IsSuccess = false;

            if (ActiveUsers.Any(u => u.Value.AuthenticationCode.Equals(code) && u.Value.Connections.Any(c => c.Key.Equals(connectionId))))
            {
                var UserDetail = GetUserDetail(connectionId);

                User User;

                if (ActiveUsers.TryGetValue(UserDetail.Email, out User))
                {
                    var Connection = User.Connections[connectionId];
                    Connection.IsOnline = true;
                    User.Connections[connectionId] = Connection;
                    IsSuccess = true;
                }
            }

            return IsSuccess;
        }

        public bool IsAuthenticated(string connectionId)
        {
            return ActiveUsers.Any(u => u.Value.Connections.Any(c => c.Key.Equals(connectionId) && c.Value.IsOnline));
        }

        public bool IsAuthenticated(string email, Connection connection)
        {
            CheckAgentAuthentication(email, connection);
            return IsAuthenticated(connection.ConnectionId);
        }

        void CheckAgentAuthentication(string email, Connection connection)
        {
            var IsAgentAuthenticated = ActiveUsers.Any(u => u.Key.Equals(email) && u.Value.Connections.Any(c => c.Value.AgentIdentifier.Equals(connection.AgentIdentifier) && c.Value.IsOnline));
            var IsUserComingBack = ActiveUsers.Any(u => u.Key.Equals(email)
                                                   && !string.IsNullOrWhiteSpace(u.Value.AuthenticationCode)
                                                   && u.Value.Connections.Count > 0
                                                   && u.Value.Connections.Any(c => !c.Value.IsOnline && c.Value.AgentIdentifier.Equals(connection.AgentIdentifier)));

            if (IsAgentAuthenticated || IsUserComingBack)
                connection.IsOnline = true;

            AddConnection(email, connection);
            Cleanup(connection.ConnectionId);
        }

        public void MarkOffline(string connectionId)
        {
            if (ActiveUsers.Any(u => u.Value.Connections.Any(c => c.Key.Equals(connectionId))))
            {
                var UserDetail = GetUserDetail(connectionId);
                User User;

                if (ActiveUsers.TryGetValue(UserDetail.Email, out User))
                {
                    var Connection = User.Connections[connectionId];
                    Connection.IsOnline = false;
                    User.Connections[connectionId] = Connection;
                }
            }
        }

        public void Cleanup(string currentConnectionId)
        {
            var SkipCheck = ActiveUsers.Where(u => u.Value.Connections.Any(c => c.Key.Equals(currentConnectionId)))
                                       .Select(u => u.Value.Connections.Where(c => !c.Key.Equals(currentConnectionId) && !c.Value.IsOnline).Count())
                                       .SingleOrDefault() == 0;

            if (!SkipCheck)
            {
                var UserDetail = GetUserDetail(currentConnectionId);

                if (UserDetail != null)
                {
                    User User;
                    if (ActiveUsers.TryGetValue(UserDetail.Email, out User))
                    {
                        var OfflineConnectionIds = User.Connections.Where(c => !c.Key.Equals(currentConnectionId)
                                                                          && !c.Value.IsOnline).Select(c => c.Key)
                                                                   .ToList();

                        foreach (var ConnectionId in OfflineConnectionIds)
                            Remove(ConnectionId);
                    }
                }
            }
        }

        void Remove(string connectionId)
        {
            if (ActiveUsers.Any(u => u.Value.Connections.Any(c => c.Key.Equals(connectionId))))
            {
                var UserDetail = GetUserDetail(connectionId);
                User User;

                if (ActiveUsers.TryGetValue(UserDetail.Email, out User))
                {
                    User.Connections.Remove(connectionId);
                    if (User.Connections.Count == 0)
                        ActiveUsers.TryRemove(UserDetail.Email, out User);
                }
            }
        }

        IList<string> GetActiveConnections(string email, bool forThisUser = true)
        {
            return ActiveUsers.Where(u => (forThisUser ? u.Key.Equals(email) : !u.Key.Equals(email)))
                              .SelectMany(u => u.Value.Connections.Where(c => c.Value.IsOnline).Select(c => c.Key))
                              .ToList();
        }

        // TODO: Remove this method before final version
        internal void LogActiveUsers()
        {
            Utility.Logger.Information(JsonConvert.SerializeObject(ActiveUsers.Select(u => u.Key + " - " + u.Value.Connections.Count + " : " + string.Join(", ", u.Value.Connections.Select(c => c.Key.Substring(0, 8))))));
        }
    }
}