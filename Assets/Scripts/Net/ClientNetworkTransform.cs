using Unity.Netcode.Components;

namespace FirstGame.Net
{
    /// <summary>Owner-authoritative transform: each client drives its own player's movement,
    /// which is then replicated to the others. (Default NetworkTransform is server-authoritative.)</summary>
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
