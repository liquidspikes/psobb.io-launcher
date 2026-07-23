using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PsobbLauncher
{
    /// <summary>
    /// Persists the list of ServerProfiles to %AppData%\PsobbLauncher\servers.json.
    /// Password blobs inside each profile are already DPAPI-protected, so the
    /// file never contains a usable plaintext or raw registry credential.
    /// </summary>
    public class ServerStore
    {
        private static readonly string Dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PsobbLauncher");

        private static readonly string StorePath = Path.Combine(Dir, "servers.json");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true
        };

        public List<ServerProfile> Servers { get; private set; } = new();

        public void Load()
        {
            try
            {
                if (!File.Exists(StorePath))
                {
                    Servers = new List<ServerProfile>();
                    return;
                }

                string json = File.ReadAllText(StorePath);
                Servers = JsonSerializer.Deserialize<List<ServerProfile>>(json, JsonOpts)
                          ?? new List<ServerProfile>();
            }
            catch (Exception ex)
            {
                // Corrupt or unreadable store: start empty rather than crash.
                // (Log ex somewhere once you wire up logging.)
                System.Diagnostics.Debug.WriteLine($"ServerStore load failed: {ex.Message}");
                Servers = new List<ServerProfile>();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            string json = JsonSerializer.Serialize(Servers, JsonOpts);
            File.WriteAllText(StorePath, json);
        }

        // --- convenience helpers ---

        public ServerProfile? GetById(Guid id) =>
            Servers.FirstOrDefault(s => s.Id == id);

        public void AddOrUpdate(ServerProfile profile)
        {
            int idx = Servers.FindIndex(s => s.Id == profile.Id);
            if (idx >= 0)
                Servers[idx] = profile;
            else
                Servers.Add(profile);
            Save();
        }

        public void Remove(Guid id)
        {
            Servers.RemoveAll(s => s.Id == id);
            Save();
        }
        private static readonly string SeededMarkerPath = Path.Combine(Dir, ".seeded");

        /// <summary>
        /// Seeds the default psobb.io profile exactly once (first run). A marker
        /// file records that seeding happened, so a user who later deletes the
        /// default profile doesn't get it re-added on the next launch.
        /// </summary>
        public void SeedDefaultsIfFirstRun()
        {
            if (File.Exists(SeededMarkerPath))
                return;

            // Only seed if the store is also empty, so we never inject into an
            // existing user's profile list (e.g. someone upgrading from a build
            // that predates seeding).
            if (Servers.Count == 0)
            {
                Servers.Add(new ServerProfile
                {
                    Name = "psobb.io",
                    LoginHost = "psobb.io",
                    LoginPort = 12000,
                    PatchHost = "psobb.io",
                    PatchPort = 11000,
                    AuthMode = AuthMode.Standard
                    // No credentials — user captures their own login.
                });
                Save();
            }

            // Mark seeded regardless, so this only ever runs once.
            Directory.CreateDirectory(Dir);
            File.WriteAllText(SeededMarkerPath, DateTime.UtcNow.ToString("o"));
        }
    }
}