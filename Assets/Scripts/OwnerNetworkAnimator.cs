using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// A specialized <see cref="NetworkAnimator"/> that forces client/owner authority
/// instead of server authority for animation updates.
/// </summary>
/// <remarks>
/// By overriding <see cref="OnIsServerAuthoritative"/> to return <c>false</c>,
/// this component allows the owning client to drive animator parameters
/// (e.g., movement, actions) without requiring the server to be the authority.
/// <para>
/// Useful for client-authoritative movement setups where animations must sync
/// across the network but originate from the player’s local inputs.
/// </para>
/// </remarks>
[DisallowMultipleComponent]
public class OwnerNetworkAnimator : NetworkAnimator
{
    /// <summary>
    /// Determines whether the server is authoritative over animation state.
    /// </summary>
    /// <returns>
    /// Always returns <c>false</c>, meaning animation state is owner/client authoritative.
    /// </returns>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
