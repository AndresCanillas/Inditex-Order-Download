using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Service.Contracts
{
	public interface ISerialSequence
	{
		/// <summary>
		/// Gets the SerialSequenceInfo object containing the configuration for this sequence. This information is required by the ISerialRepository in order to perform operations with the sequence.
		/// </summary>
		SerialSequenceInfo SequenceInfo { get; }

		/// <summary>
		/// Gets the next available serial number in the sequence. It is warranted that no other process will be assigned the same serial.
		/// Note: For performance reasons, this method should cache multiple serials at once to make subsecuent calls faster. 
		/// This method is ideal when we are programing individual tags one at a time and want to be able to reuse serials when a tag is
		/// found defective.
		/// </summary>
		long Acquire();

		/// <summary>
		/// Returns the specified serial to the sequence so it can be reused later. The 'Acquire' method is the only method that can reuse
		/// released serials. Implementations are expected to keep track of all released serials, also 'Acquire' should first attempt to 
		/// reuse those serials before allocating new ones.
		/// </summary>
		void Release(long serial);

		/// <summary>
		/// Reserves 'count' serials from the sequence. Returns the first serial that was reserved. It is warranted that no other process
		/// will be assigned those same serials. This method is ideal to reserve multiple serials at once, for instacne to fill a cache, 
		/// or program multiple tags at once. However since it needs to allocate consecutive serials, this method cannot use released
		/// serials.
		/// </summary>
		List<long> AcquireMultiple(int count);

		/// <summary>
		/// If applicable, ensures that at least 'count' serials are preallocated from the central serial repository.
		/// NOTE: Applicable only if the serial reposiotry is connecting internally to Print Central to request serials,
		/// otherwise this method should be left empty and have no effect. This is meant to be used primarly in the local
		/// print system, to ensure enough serials are preallocated to produce an order even if the conection to Print
		/// Central is down.
		/// </summary>
		void Preallocate(int count);

		/// <summary>
		/// Returns the curren value of the sequence.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: Should never use this method to assign serials. To assign serials in a safe way use Acquire & AcquireMultiple methods.
		/// This method is used only for generating previews and encoding sample labels, in those applications there is no need to ensure that serials
		/// are unique.
		/// </remarks>
		long GetCurrent();

		/// <summary>
		/// Allows to change the current counter of the sequence, the specified serial will be the next one returned by the sequence. The specified
		/// value must fall in the range [Start, Max].
		/// NOTES: 1) Use with care as changing this value carelessly can easily result in duplicated serial numbers.
		///		   2) Also when changing the current serial of a sequence, all serials stored in the returned serials collection are discarded.
		/// </summary>
		void SetCurrent(long value);

		/// <summary>
		/// Specifies what happens when the sequence reaches the max serial number. Possible options are Cycle and Throw.
		/// NOTES: The sequence implementation MUST start sending notifications when the number of serials left in the sequence is less than 10%
		/// of the available serials.
		/// </summary>
		SerialSequenceBehavior Behavior { get; set; }

		/// <summary>
		/// We anticipate that some possible implementations of serial number sequence might require data from the product to determine which 
		/// serial(s) will be returned.
		/// 
		/// Example: MultiSerialSequence can use the Barcode field to keep an independent sequence per Barcode.
		/// 
		/// Even though the most simple implementation (SingleSerialSequence) does not use this at all, the tag encoding algorithms
		/// should still set this to ensure proper operation under all foreseen use cases.
		/// </summary>
		JObject Data { get; set; }
	}

	public enum SerialSequenceBehavior
	{
		Cycle,		// Sequence will cycle back to the start when Max value is reached.
		Throw		// Sequence will throw an error when the sequence reaches the Max value, no more serials will be assigned (hence the system will not work
					// until the sequence is updated by an administrator).
	}
}
