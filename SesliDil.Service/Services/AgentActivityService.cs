using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SesliDil.Core.Entities;

namespace SesliDil.Service.Services
{
    public class AgentActivityService
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>> _activityData = new();

        public ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>> ActivityData => _activityData;

        // ChatHub'dan çağrılacak metodlar:
        public AgentActivity GetAgentActivity(string conversationId, string userId, string agentId)
        {
            if (_activityData.TryGetValue(conversationId, out var usersDict) &&
                usersDict.TryGetValue(userId, out var agentsDict) &&
                agentsDict.TryGetValue(agentId, out var agentActivity))
            {
                return agentActivity;
            }
            return null;
        }

        public IDictionary<string, ConcurrentDictionary<string, AgentActivity>> GetConversationActivities(string conversationId)
        {
            if (_activityData.TryGetValue(conversationId, out var usersDict))
            {
                return usersDict;
            }
            return null;
        }

        public ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, AgentActivity>>> GetAllActivities()
        {
            return _activityData;
        }
    }
}
