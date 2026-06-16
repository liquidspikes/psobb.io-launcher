using System;
using System.Runtime.Versioning;

namespace PsobbLauncher
{
    [SupportedOSPlatform("windows")]
    public class CaptureService
    {
        private readonly ServerStore _store;

        public CaptureService(ServerStore store) => _store = store;

        /// <summary>
        /// Captures whatever credentials currently sit in base PSOBB into the
        /// given profile. Call after the user has launched and logged into that
        /// server at least once (the login is what writes the blob).
        /// Returns false if no validated password blob is present yet.
        /// </summary>
        public bool CaptureCurrentLogin(ServerProfile profile)
        {
            if (!PsoRegistry.TryReadCredentials(out string account, out byte[]? blob)
                || blob is not { Length: > 0 })
            {
                return false;
            }

            profile.Account = account;
            profile.ProtectedPassword = CredentialProtector.Protect(blob);
            profile.CredentialsCapturedUtc = DateTime.UtcNow;
            profile.CredentialsValid = true;
            _store.AddOrUpdate(profile);
            return true;
        }

        /// <summary>
        /// Stamps a profile's saved credentials into base PSOBB so the client
        /// reads them on its next launch. Call right before launching.
        /// </summary>
        public bool ApplyCredentials(ServerProfile profile)
        {
            if (!profile.HasCredentials || profile.ProtectedPassword is null)
                return false;

            byte[] blob = CredentialProtector.Unprotect(profile.ProtectedPassword);
            PsoRegistry.WriteCredentials(profile.Account, blob);
            return true;
        }
    }
}