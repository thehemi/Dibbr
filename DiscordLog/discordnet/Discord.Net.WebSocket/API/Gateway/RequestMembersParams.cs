using Newtonsoft.Json;
using System.Collections.Generic;

namespace Discord.API.Gateway
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class RequestMembersParams
    {
        [JsonProperty("query")]
        public string Query { get; set; }
        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("guild_id")]
        public IEnumerable<ulong> GuildIds { get; set; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    internal class LazyGuilds
    {
        [JsonProperty("activities")]
        public bool Activities { get; set; } = true;
        [JsonProperty("typing")]
        public bool Typing { get; set; } = true;
        [JsonProperty("threads")]
        public bool Threads { get; set; } = false;
        [JsonProperty("members")]
        public Optional<Dictionary<string, GuildMember>> Members { get; set; }

        [JsonProperty("channels")]
        public Dictionary<ulong, List<int>> Channels { get; set; } = new Dictionary<ulong, List<int>>();
        [JsonProperty("guild_id")]
        public ulong GuildId { get; set; }
    }
}
