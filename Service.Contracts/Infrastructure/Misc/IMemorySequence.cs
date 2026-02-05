using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Service.Contracts
{
	public interface IMemorySequence
	{
		int NextID();
	}

	public class MemorySequence : IMemorySequence
	{
		private int currentID = 0;
		public int NextID() => Interlocked.Increment(ref currentID);
	}
}
