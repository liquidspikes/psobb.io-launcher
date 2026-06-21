using System;

namespace PsobbLauncher
{
    public enum AuthMode { Standard, Hangame }

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

        // Which auth path this profile uses at launch.
        public AuthMode AuthMode { get; set; } = AuthMode.Standard;

        // --- Hangame path (memory-hook auth via native loader) ---
        // Username must end in @HG, <= 11 chars. Plain text like Account.
        public string HangameUsername { get; set; } = "";
        // Numeric 1-8 digits. DPAPI-protected at rest, same as the captured blob.
        public byte[]? HangameProtectedPassword { get; set; }

        public bool HasHangameCredentials =>
                !string.IsNullOrEmpty(HangameUsername)
                && HangameProtectedPassword is { Length: > 0 };

        // --- Optional per-profile install isolation ---
        // Absolute path to this server's install directory (the folder
        // containing psobb.exe). If null/empty, the launcher falls back to
        // its own directory (then parent), preserving the original
        // single-install behavior. Set this to point a server at a separate
        // install so its data/ folder doesn't collide with other servers.
        public string? InstallPath { get; set; }

        // True if this profile points at its own isolated install.
        public bool HasCustomInstallPath =>
            !string.IsNullOrWhiteSpace(InstallPath);
    }
}