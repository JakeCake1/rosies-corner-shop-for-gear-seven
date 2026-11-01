using System;
using Newtonsoft.Json.Linq;

namespace Game.Services.Save
{
    /// <summary>
    /// Upgrades raw save JSON step by step to <see cref="PlayerProfileData.CurrentVersion"/>.
    /// </summary>
    /// <remarks>
    /// Steps operate on <see cref="JObject"/> so fields removed from the DTO can still be read. Add a case per
    /// version bump and a sample save of the old version to the tests.
    /// </remarks>
    public static class SaveMigrations
    {
        /// <summary>
        /// Migrates <paramref name="root"/> in place.
        /// </summary>
        /// <param name="root">Parsed save; a missing <c>version</c> field means version 1.</param>
        /// <returns>The resulting version.</returns>
        /// <exception cref="NotSupportedException">The save comes from a newer build; it must not be overwritten.</exception>
        public static int Migrate(JObject root)
        {
            var version = root.Value<int?>("version") ?? 1;
            if (version > PlayerProfileData.CurrentVersion)
            {
                throw new NotSupportedException($"Save version {version} is newer than {PlayerProfileData.CurrentVersion}.");
            }

            // No structural changes yet: version 1 is the first format. Future steps go here, e.g.
            // if (version == 1) { ...; version = 2; }
            root["version"] = version;
            return version;
        }
    }
}
