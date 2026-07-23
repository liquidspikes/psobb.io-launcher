using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace PsobbLauncher
{
    /// <summary>
    /// Reads and writes the credential values the teth client stores under
    /// HKCU\Software\SonicTeam\PSOBB. ACCOUNT is REG_SZ (plain text);
    /// PASSWORD is REG_BINARY (an opaque, server-validated blob we replay
    /// verbatim, never decode). Windows-only.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class PsoRegistry
    {
        private const string KeyPath = @"Software\SonicTeam\PSOBB";
        private const string AccountValue = "ACCOUNT";
        private const string PasswordValue = "PASSWORD";

        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Reads the current ACCOUNT string and PASSWORD blob from base PSOBB.
        /// Returns false if the key is missing or the password isn't present
        /// (e.g. no successful login has written it yet).
        /// </summary>
        public static bool TryReadCredentials(out string account, out byte[]? password)
        {
            account = "";
            password = null;

            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: false);
            if (key is null)
                return false;

            account = key.GetValue(AccountValue) as string ?? "";
            password = key.GetValue(PasswordValue) as byte[];

            // The blob only exists after a successful login. Treat an empty
            // or missing password as "nothing captured yet".
            return password is { Length: > 0 };
        }

        /// <summary>
        /// Stamps ACCOUNT (REG_SZ) and PASSWORD (REG_BINARY) into base PSOBB,
        /// ready for the client to read on launch. Creates the key if absent.
        /// </summary>
        public static void WriteCredentials(string account, byte[] password)
        {
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(password);

            using var key = Registry.CurrentUser.CreateSubKey(KeyPath, writable: true)
                ?? throw new InvalidOperationException(
                    $@"Could not open or create HKCU\{KeyPath}");

            key.SetValue(AccountValue, account, RegistryValueKind.String);
            key.SetValue(PasswordValue, password, RegistryValueKind.Binary);
        }

        /// <summary>
        /// Blanks the PASSWORD value after a session so the shared base key
        /// isn't left holding a working credential blob. Leaves ACCOUNT alone.
        /// Safe to call when the key or value is already absent.
        /// </summary>
        public static void ClearPassword()
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath, writable: true);
            if (key is null)
                return;

            // Overwrite with an empty blob rather than deleting, matching the
            // client's own empty-state ("PASSWORD"="" / zero-length).
            key.SetValue(PasswordValue, Array.Empty<byte>(), RegistryValueKind.Binary);
        }
    }
}