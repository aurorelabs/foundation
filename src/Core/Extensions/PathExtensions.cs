using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Aurore.Foundation.Core.Extensions;

/// <summary>File path helpers.</summary>
public static class PathExtensions
{
    private const string WslPrefix = "/mnt/";

    extension(Path)
    {
        /// <summary>
        /// Combines path segments into a full path, translating Windows drive paths when running under WSL.
        /// </summary>
        /// <param name="paths">The path segments. Each is converted to the path style of the running system, then they are combined.</param>
        /// <returns>The full path.</returns>
        public static string NormalizeRuntimePath(params string[] paths)
        {
            var normalized = paths
                .Select(NormalizeSegment)
                .ToArray();

            return Path.GetFullPath(Path.Combine(normalized));
        }

        [SuppressMessage("Style", "IDE0051", Justification = "IDE0051 does not follow the method-group use of a private member of an extension block.")]
        private static string NormalizeSegment(string path)
        {
            if (OperatingSystem.IsWindows())
                return FromWslPath(path);

            return OperatingSystem.IsWsl()
                ? ToWslPath(path)
                : path;
        }

        private static string FromWslPath(string path)
        {
            var isDrivePath = path.StartsWith(WslPrefix, StringComparison.OrdinalIgnoreCase)
                && path.Length > WslPrefix.Length
                && char.IsAsciiLetter(path[WslPrefix.Length])
                && (path.Length == WslPrefix.Length + 1 || path[WslPrefix.Length + 1] == '/');

            if (!isDrivePath)
                return path;

            var drive = char.ToUpperInvariant(path[5]);

            var remainder = path[6..]
                .Replace('/', '\\');

            return $"{drive}:\\{remainder}";
        }

        private static string ToWslPath(string path)
        {
            if (path.Length < 2 || path[1] != ':' || !char.IsAsciiLetter(path[0]))
                return path;

            var drive = char.ToLowerInvariant(path[0]);

            var remainder = path[2..]
                .Replace('\\', '/')
                .TrimStart('/');

            return $"/mnt/{drive}/{remainder}";
        }
    }
}
