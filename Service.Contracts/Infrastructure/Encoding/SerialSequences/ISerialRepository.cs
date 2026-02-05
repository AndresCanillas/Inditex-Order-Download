using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	// =====================================================================================================
	// Provides methods to get serials. This service is used primarily by the different implementations of
	// ISerialSequence, which are used in turn by the implementations of the RFID encoding algorithms such
	// as StandardEncodingAlgorithm and InditexEncodingAlgorithm.
	//
	// This repository must be implemented (and registered) by the system that will be using the 
	// RFIDConfigurationSystem. To date, PrintCentral provides the master implementation for the serial 
	// repository. In the other hand, PrintLocal also implements this contract, but in a way that it
	// depends on calls to PrintCentral to get serials. In the end all serials come from a single system:
	// PrintCentral.
	// =====================================================================================================
	public interface ISerialRepository
	{
		// Acquires a single serial number from the specified sequence (next available)
		long AcquireSerial(SerialSequenceInfo sq);

		// Acquires multiple serials from the specified sequence. For this method, serials are not warranted to be sequential, that is why the return type is a list. The only warranties are: That count serials will be allocated (or an exception thrown) and that serials in the returned list will be in ascending order.
		List<long> AcquireMultiple(SerialSequenceInfo sq, int count);

		// Acquires multiple serials as a sequential block starting at the returned "Serial" up to "Serial" + Count - 1.
		long AcquireSequential(SerialSequenceInfo sq, int count);

		// Marks a serial that was previusly extracted from a sequence as reusable. This means that the serial can be reallocated again, later when more serials fro that sequence are requested.
		void ReuseSerial(SerialSequenceInfo sq, long serial);

		// Gets a sample serial number from the specified sequence.
		// IMPORTANT: Do not use GetSample to try to allocate serials. GetSample will not ensure serials are unique. To "consume" serials in a way that ensures uniqueness, call Acquire/AcquireMultiple.
		long GetSample(SerialSequenceInfo sq);

		// Changes the current value of the specified sequence
		void SetCurrent(SerialSequenceInfo sq, long value);

		// If the serial repository implementation is getting serials from PrintCentral, then this method should cache 'count' serials for future use.
		// Otherwise, if this is the SerialRepository of PrintCentral, this method can be left empty and produce no effects.
		void Preallocate(SerialSequenceInfo sq, int count);
	}


	// Contract used to represent a sequence of serials.
	public class SerialSequenceInfo
	{
		public SerialSequenceInfo(string id, string filter, long sequenceStart, long maxValue, SerialSequenceBehavior behavior, SelectorType selectorType)
		{
			ID = id;
			SelectorType = selectorType;
			Filter = filter;
			SequenceStart = sequenceStart;
			MaxValue = maxValue;
			Behavior = behavior;
		}

		// SequenceID
		public string ID { get; set; }

		// Filter value used for multi serial sequence
		public string Filter { get; set; }

		// Sequence start, used as initial value when the sequence does not exist in the database.
		public long SequenceStart { get; set; }

		// The maximum value that can be extracted from this sequence (including MaxValue), used to validate if the maximum range of the sequence has been reached.
		public long MaxValue { get; set; }

		// Behavior of the sequence once the MaxValue is reached
		public SerialSequenceBehavior Behavior { get; set; }

		// Indicates how the value of the filter field should be normalized
		public SelectorType SelectorType { get; set; }

		public string GetNormalizedFilter()
		{
			if (Filter == null)
				return null;

			switch (SelectorType)
			{
				case SelectorType.String:
					return Filter.Trim().ToLower();
				case SelectorType.EAN13:
				case SelectorType.EAN_13:
					if (!Filter.IsNumeric() || Filter.Length < 12 || Filter.Length > 13)
						throw new InvalidOperationException($"Invalid Product Data, Filter value ({Filter}) is not a valid EAN13.");
					if (Filter.Length == 12)
						return Filter;
					else
						return Filter.Substring(0, 12);
				default:
					throw new NotImplementedException($"SelectorType {SelectorType} is not implemented");
			}
		}
	}
}
