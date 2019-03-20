using System;
using System.Threading.Tasks;
using Orleans;

namespace OrleansDockerSample.Grains
{
    /// <summary>A simple, incrementing counter.</summary>
    public sealed class CounterGrain : Grain, ICounterGrain
    {
        /// <summary>The current value.</summary>
        private long value;

        /// <summary>Asynchronously gets the value.</summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<long> GetValueAsync() =>
            Task.FromResult(this.value);

        /// <summary>Asynchronously increments and gets the (incremented) value.</summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public Task<long> IncrementAndGetValueAsync()
        {
            if (this.value == long.MaxValue)
            {
                throw new InvalidOperationException("The value has reached the maximum value.");
            }

            return Task.FromResult(++this.value);
        }
    }
}
