using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Services
{
	public class SerialRange
	{
		public string SequenceID { get; set; }
		public string Filter { get; set; }
		public long RangeStart { get; set; }            // First serial of the range (IMPORTANT: This should never be modified)
		public long MaxValue { get; set; }              // Last serial that can be assigned from this range (IMPORTANT: This should never be modified)
		public long CurrentValue { get; set; }          // CurrentValue (its the next serial that can be taken from this range, used internally by IMemorySerialRepository)
	}

	public interface IMemorySerialRepository
	{
		// Initializes the in memory serials with a preallocated collections of serial ranges
		void Initialize(List<SerialRange> ranges);

		// Acquires count serials from the specified sequence. Allocated serials are not warranted to be sequential.
		void Preallocate(SerialSequenceInfo sq, int count);

		// Merges consecutive ranges into single one, and then returns the compacted list of preallocated serials.
		List<SerialRange> Save();
	}


	public class MemorySerialRepository : IMemorySerialRepository
	{
		private ISerialRepository repo;
		private List<SerialRange> ranges;

		public MemorySerialRepository(ISerialRepository repo)
		{
			this.repo = repo;
			ranges = new List<SerialRange>();
		}

		public void Initialize(List<SerialRange> ranges)
		{
			foreach(var r in ranges)
				r.CurrentValue = r.RangeStart;

			if (ranges != null)
				this.ranges = ranges;
		}

		public void Preallocate(SerialSequenceInfo sq, int count)
		{
			var range = ranges.Where(r => r.SequenceID == sq.ID && r.Filter == sq.GetNormalizedFilter() && r.CurrentValue < r.MaxValue).FirstOrDefault();
			if(range == null)
			{
				var rangeStart = repo.AcquireSequential(sq, count);
				ranges.Add(new SerialRange()
				{
					SequenceID = sq.ID,
					Filter = sq.GetNormalizedFilter(),
					RangeStart = rangeStart,
					MaxValue = rangeStart + count,
					CurrentValue = rangeStart + count
				});
			}
			else
			{
				ConsumeSerialsFromRange(sq, range, count);
			}
		}

		private void ConsumeSerialsFromRange(SerialSequenceInfo sq, SerialRange range, int count)
		{
			if(range.CurrentValue + count > range.MaxValue)
			{
				// preallocated range can no longer satisfy the requested number of serials, assign more from another range
				var missingSerials = range.CurrentValue + count - range.MaxValue;
				range.CurrentValue = range.MaxValue;
				Preallocate(sq, (int)missingSerials);
			}
			else
			{
				range.CurrentValue += count;
			}
		}

		public List<SerialRange> Save()
		{
			int i = 0;
			while(i < ranges.Count)
			{
				var range = ranges[i];
				var adjacent = ranges.FindIndex(r => r.SequenceID == range.SequenceID && r.Filter == range.Filter && r.RangeStart == range.MaxValue);
				if (adjacent >= 0)
				{
					range.MaxValue = ranges[adjacent].MaxValue;
					ranges.RemoveAt(adjacent);
				}
				else i++;
			}
			return ranges;
		}
	}
}
