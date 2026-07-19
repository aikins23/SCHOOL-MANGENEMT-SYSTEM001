using System;
using System.Collections.Generic;
using kingdom_Preparatory_School_Management_System.Data;
using KingdomPrep.Shared.Models;
using kingdom_Preparatory_School_Management_System.Services;

namespace kingdom_Preparatory_School_Management_System.Common
{
    /// <summary>
    /// Single source of truth for the grading scheme, read synchronously by the report-card
    /// generator. Loads once from the database and caches; any DB error / empty table falls
    /// back to the legacy 5-band scheme so report cards never break. Call Refresh() after a save.
    /// </summary>
    public static class GradingScheme
    {
        private static readonly object _lock = new object();
        private static List<GradeBand> _bands;

        private static void EnsureLoaded()
        {
            if (_bands != null) return;
            lock (_lock)
            {
                if (_bands != null) return;
                try
                {
                    var repo = new GradingSchemeRepository(AppConfig.ConnectionString);
                    repo.EnsureTablesAsyncSafe();
                    var bands = repo.GetBandsAsync().GetAwaiter().GetResult();
                    _bands = (bands != null && bands.Count > 0)
                        ? bands
                        : GradingSchemeRepository.LegacyBands();
                }
                catch (Exception ex)
                {
                    LoggerHelper.LogError("GradingScheme load failed; using legacy bands", ex);
                    _bands = GradingSchemeRepository.LegacyBands();
                }
            }
        }

        public static void Refresh()
        {
            lock (_lock) { _bands = null; }
        }

        /// <summary>Bands ordered high-&gt;low by MinScore.</summary>
        public static IReadOnlyList<GradeBand> Bands
        {
            get { EnsureLoaded(); return _bands; }
        }

        public static string CodeForScore(decimal score) => BandForScore(score)?.Code ?? "";
        public static string LabelForScore(decimal score) => BandForScore(score)?.Label ?? "";

        private static GradeBand BandForScore(decimal score)
        {
            EnsureLoaded();
            GradeBand match = null;
            foreach (var b in _bands)            // high -> low
            {
                if (score >= b.MinScore) { match = b; break; }
            }
            return match ?? (_bands.Count > 0 ? _bands[_bands.Count - 1] : null);
        }
    }
}
