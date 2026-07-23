using System;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace PsobbLauncher
{
    /// <summary>
    /// Wraps Windows DPAPI (ProtectedData) for encrypting the captured
    /// REG_BINARY password blob before it's persisted to servers.json.
    /// CurrentUser scope ties the encryption to the logged-in Windows user
    /// on this machine — which matches the blob's own machine-bound nature.
    /// Windows-only.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class CredentialProtector
    {
        public static byte[] Protect(byte[] plain)
        {
            ArgumentNullException.ThrowIfNull(plain);
            return ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
        }

        public static byte[] Unprotect(byte[] proteced)
        {
            ArgumentNullException.ThrowIfNull(proteced);
            return ProtectedData.Unprotect(proteced, null, DataProtectionScope.CurrentUser);
        }
    }
}