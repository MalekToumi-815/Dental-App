using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dental_App.Services
{
    /// <summary>
    /// Defines a generic service for debouncing and executing live searches.
    /// </summary>
    /// <typeparam name="T">The type of the search result entities.</typeparam>
    public interface ILiveSearchService<T>
    {
        /// <summary>
        /// Executes a debounced search using the provided search source.
        /// </summary>
        /// <param name="searchTerm">The search term to evaluate.</param>
        /// <param name="searchSource">A function that retrieves the search results based on the search term.</param>
        /// <returns>A limited enumerable of results, or null if the search was cancelled by a subsequent call.</returns>
        Task<IEnumerable<T>?> SearchAsync(string searchTerm, Func<string, Task<IEnumerable<T>>> searchSource);
    }

    /// <summary>
    /// Implementation of the live search service with debouncing and result limiting.
    /// </summary>
    /// <typeparam name="T">The type of the search result entities.</typeparam>
    public class LiveSearchService<T> : ILiveSearchService<T>
    {
        private CancellationTokenSource? _cts;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(300);
        private readonly int _maxResults = 10;

        public async Task<IEnumerable<T>?> SearchAsync(string searchTerm, Func<string, Task<IEnumerable<T>>> searchSource)
        {
            // Cancel any ongoing search before starting a new one
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            var token = _cts.Token;

            try
            {
                // Wait for the debounce interval
                await Task.Delay(_debounceDelay, token);

                // Fetch data from the injected source func
                var results = await searchSource(searchTerm);

                // Apply optimization: take only the first matches
                return results?.Take(_maxResults);
            }
            catch (TaskCanceledException)
            {
                // A newer search request was initiated, so we gracefully exit
                return null;
            }
            catch (OperationCanceledException)
            {
                // Same handling for general cancellation exceptions
                return null;
            }
        }
    }
}
