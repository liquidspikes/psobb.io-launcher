using System;

namespace PsobbLauncher
{
    /// <summary>
    /// One server the launcher can connect to. Holds the psobb.cfg
    /// connection values and the captured (DPAPI-protected) credentials.
    /// </summary>
    public class ServerProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New Server";

        // Values templated into psobb.cfg for this server
        public string LoginHost { get; set; } = "";
        public int LoginPort { get; set; } = 12000;
        public string PatchHost { get; set; } = "";
        public int PatchPort { get; set; } = 11000;

        // Captured credentials.
        // Account is plain text; password is the REG_BINARY blob,
        // DPAPI-protected before it ever touches disk.
        public string Account { get; set; } = "";
        public byte[]? ProtectedPassword { get; set; }

        public DateTime? CredentialsCapturedUtc { get; set; }
        public bool CredentialsValid { get; set; }

        public bool HasCredentials =>
            CredentialsValid && ProtectedPassword is { Length: > 0 };
    }
}