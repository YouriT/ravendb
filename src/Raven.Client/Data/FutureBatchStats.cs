using System;

namespace Raven.Client.Data
{
    public class FutureBatchStats
    {
        /// <summary>
        /// Time when future batch was created.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Indicates how much time it took to prepare future batch.
        /// </summary>
        public TimeSpan? Duration { get; set; }

        /// <summary>
        /// Number of documents in batch.
        /// </summary>
        public int? Size { get; set; }

        /// <summary>
        /// Number of retries till the future batch calculation succeeded.
        /// </summary>
        public int Retries { get; set; }

        /// <summary>
        /// Indicates what prefetching user (indexer, replicator, sql replicator) calculated the future batch.
        /// </summary>
        public PrefetchingUser PrefetchingUser { get; set; }
    }
}
