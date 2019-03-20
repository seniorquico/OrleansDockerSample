using System.Threading.Tasks;
using Orleans;

namespace OrleansDockerSample
{
    /// <summary>Represents a simple, incrementing counter.</summary>
    public interface ICounterGrain : IGrainWithGuidKey
    {
        /// <summary>Asynchronously gets the value.</summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<long> GetValueAsync();

        /// <summary>Asynchronously increments and gets the (incremented) value.</summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<long> IncrementAndGetValueAsync();
    }
}
