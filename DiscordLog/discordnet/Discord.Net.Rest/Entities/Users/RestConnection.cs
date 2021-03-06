using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Model = Discord.API.Connection;

namespace Discord.Rest
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public class RestConnection : IConnection
    {
        /// <inheritdoc />
        public string Id { get; }
        /// <inheritdoc />
        public string Type { get; }
        /// <inheritdoc />
        public string Name { get; }
        /// <inheritdoc />
        public bool IsRevoked { get; }
        /// <inheritdoc />
        public IReadOnlyCollection<ulong> IntegrationIds { get; }

        internal RestConnection(string id, string type, string name, bool isRevoked, IReadOnlyCollection<ulong> integrationIds)
        {
            Id = id;
            Type = type;
            Name = name;
            IsRevoked = isRevoked;

            IntegrationIds = integrationIds;
        }
        internal static RestConnection Create(Model model)
        {
           // var Integrations = model.Integrations.IsSpecified ? model.Integrations.Value
           //     .Select(intergration => RestIntegration.Create(Discord, null, intergration)).ToImmutableArray() : null;
            return new RestConnection(model.Id, model.Type, model.Name, model.Revoked, null);
        }

        /// <summary>
        ///     Gets the name of the connection.
        /// </summary>
        /// <returns>
        ///     Name of the connection.
        /// </returns>
        public override string ToString() => Name;
        private string DebuggerDisplay => $"{Name} ({Id}, {Type}{(IsRevoked ? ", Revoked" : "")})";
    }
}
