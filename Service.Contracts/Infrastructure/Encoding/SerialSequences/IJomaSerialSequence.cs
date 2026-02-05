using System.Collections.Generic;

namespace Service.Contracts.Infrastructure.Encoding.SerialSequences
{
    public interface IJomaSerialSequence 
    {
        /// <summary>
		/// Returns 'count' serials from the sequence. It is warranted that no other process will be assigned those same serials. 
		/// </summary>
		List<long> AcquireMultiple(int count);
    }
}
