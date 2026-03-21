using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DhCodetaskExtension.Core.Interfaces;
using DhCodetaskExtension.Core.Models;

namespace DhCodetaskExtension.Providers.ReportProviders
{
    /// <summary>
    /// Delegates to all registered generators. Open/Closed: add generators without modifying this class.
    /// </summary>
    public sealed class CompositeReportGenerator : IReportGenerator
    {
        private readonly List<IReportGenerator> _generators = new List<IReportGenerator>();

        public void Register(IReportGenerator generator)
        {
            if (generator == null) throw new ArgumentNullException(nameof(generator));
            _generators.Add(generator);
        }

        public async Task<string> GenerateAsync(CompletionReport report, string outputDirectory)
        {
            string lastPath = null;
            foreach (var g in _generators)
            {
                try
                {
                    lastPath = await g.GenerateAsync(report, outputDirectory);
                }
                catch { /* each generator is independent; log in callers */ }
            }
            return lastPath;
        }
    }
}
