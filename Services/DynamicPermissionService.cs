using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using kingdom_Preparatory_School_Management_System.Data;
using kingdom_Preparatory_School_Management_System.Models;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>
    /// Dynamic permission service that loads permissions from the database.
    /// Caches the current user's permissions in-memory for session lifetime.
    /// Falls back gracefully to legacy AuthService.CanAccess / CanWrite when
    /// the dynamic permission tables haven't been seeded yet.
    /// </summary>
    public static class DynamicPermissionService
    {
        private static readonly object _lock = new object();
        private static HashSet<string> _cachedPermissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static string _cachedUsername;
        private static DateTime _cacheExpiry = DateTime.MinValue;
        private static bool _refreshInProgress;
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(15);

        private static readonly IPermissionRepository _repo = new PermissionRepository();

        public static async Task RefreshCurrentUserPermissionsAsync()
        {
            var session = AuthService.CurrentUser;
            if (session == null || !session.IsAuthenticated) return;

            try
            {
                var codes = await _repo.GetUserPermissionCodesAsync(session.Username);
                lock (_lock)
                {
                    _cachedUsername = session.Username;
                    _cachedPermissions = new HashSet<string>(codes, StringComparer.OrdinalIgnoreCase);
                    _cacheExpiry = DateTime.UtcNow.Add(CacheLifetime);
                    _refreshInProgress = false;
                }
            }
            catch (Exception ex)
            {
                lock (_lock)
                {
                    _refreshInProgress = false;
                    _cacheExpiry = DateTime.UtcNow.AddMinutes(1);
                }
                LoggerHelper.LogWarning("DynamicPermissionService refresh failed: " + ex.Message);
            }
        }

        public static void ClearCache()
        {
            lock (_lock)
            {
                _cachedPermissions.Clear();
                _cachedUsername = null;
                _cacheExpiry = DateTime.MinValue;
                _refreshInProgress = false;
            }
        }

        /// <summary>
        /// Check if the current user has a specific permission code.
        /// Falls back to legacy role-based check if no dynamic permissions are configured.
        /// </summary>
        public static bool HasPermission(string permissionCode)
        {
            if (string.IsNullOrWhiteSpace(permissionCode)) return false;
            var session = AuthService.CurrentUser;
            if (session == null || !session.IsAuthenticated) return false;

            lock (_lock)
            {
                if (_cachedUsername == session.Username && DateTime.UtcNow <= _cacheExpiry)
                {
                    if (_cachedPermissions.Count == 0)
                        return LegacyFallback(permissionCode);
                    return _cachedPermissions.Contains(permissionCode);
                }

                QueueRefreshIfNeeded(session.Username);
                if (_cachedPermissions.Count == 0)
                    return LegacyFallback(permissionCode);
                return _cachedPermissions.Contains(permissionCode);
            }
        }

        public static bool HasAnyPermission(params string[] permissionCodes)
        {
            if (permissionCodes == null || permissionCodes.Length == 0) return false;
            return permissionCodes.Any(HasPermission);
        }

        public static bool HasAllPermissions(params string[] permissionCodes)
        {
            if (permissionCodes == null || permissionCodes.Length == 0) return false;
            return permissionCodes.All(HasPermission);
        }

        private static void QueueRefreshIfNeeded(string username)
        {
            if (_refreshInProgress) return;

            _refreshInProgress = true;
            if (!string.Equals(_cachedUsername, username, StringComparison.OrdinalIgnoreCase))
            {
                _cachedPermissions.Clear();
            }
            _cachedUsername = username;
            _cacheExpiry = DateTime.UtcNow.AddMinutes(1);

            Task.Run(async () =>
            {
                try { await RefreshCurrentUserPermissionsAsync().ConfigureAwait(false); }
                catch (Exception ex)
                {
                    lock (_lock) _refreshInProgress = false;
                    LoggerHelper.LogWarning("QueueRefreshIfNeeded: " + ex.Message);
                }
            });
        }

        private static bool LegacyFallback(string permissionCode)
        {
            return AuthService.CanWrite(permissionCode) || AuthService.CanAccess(permissionCode);
        }
    }
}
