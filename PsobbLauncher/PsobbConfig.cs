using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PsobbLauncher
{
    /// <summary>
    /// Reads and writes psobb.cfg (simple Key=Value lines). Per-server launches
    /// only change LoginHost/LoginPort/PatchHost/PatchPort; all other keys
    /// (Language, DisableIME, SkipIntro, etc.) are preserved as-is.
    /// </summary>
    public static class PsobbConfig
    {
        private const string FileName = "psobb.cfg";

        // Sensible defaults used only when no existing cfg is present.
        private static readonly (string Key, string Value)[] Defaults =
        {
            ("Language", "english"),
            ("DisableIME", "1"),
            ("SkipIntro", "0"),
            ("LoginHost", ""),
            ("LoginPort", "12000"),
            ("PatchHost", ""),
            ("PatchPort", "11000"),
        };

        public static string GetConfigPath(string gameDir) =>
            Path.Combine(gameDir, FileName);

        /// <summary>
        /// Writes the profile's connection values into psobb.cfg in gameDir,
        /// preserving any other existing keys. Creates the file from defaults
        /// if it doesn't exist.
        /// </summary>
        public static void WriteForProfile(string gameDir, ServerProfile profile)
        {
            string path = GetConfigPath(gameDir);

            // Load existing keys (preserving order), or start from defaults.
            var entries = File.Exists(path)
                ? Parse(File.ReadAllLines(path))
                : Defaults.ToList();

            // Overwrite only the four connection values.
            Set(entries, "LoginHost", profile.LoginHost);
            Set(entries, "LoginPort", profile.LoginPort.ToString());
            Set(entries, "PatchHost", profile.PatchHost);
            Set(entries, "PatchPort", profile.PatchPort.ToString());

            File.WriteAllLines(path, entries.Select(e => $"{e.Key}={e.Value}"));
        }

        private static List<(string Key, string Value)> Parse(string[] lines)
        {
            var list = new List<(string, string)>();
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0)
                    continue; // skip malformed lines

                string key = line.Substring(0, eq).Trim();
                string value = line.Substring(eq + 1).Trim();
                list.Add((key, value));
            }
            return list;
        }

        private static void Set(List<(string Key, string Value)> entries, string key, string value)
        {
            int idx = entries.FindIndex(e =>
                string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));

            if (idx >= 0)
                entries[idx] = (entries[idx].Key, value); // keep original key casing
            else
                entries.Add((key, value));
        }
    }
}