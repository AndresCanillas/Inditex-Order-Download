using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Service.Contracts;

namespace Service.Contracts
{
	public class SingleSerialSequence : ISerialSequence, IConfigurable<SingleSequenceConfig>
	{
		private ISerialRepository repo;
		private SingleSequenceConfig config;
		private SerialSequenceInfo sq;

		public SingleSerialSequence(ISerialRepository repo)
		{
			this.repo = repo;
			config = new SingleSequenceConfig() { ID = Guid.NewGuid().ToString(), SequenceStart = 1, SequenceMax = 9999999, Behavior = SerialSequenceBehavior.Throw };
			sq = new SerialSequenceInfo(config.ID, "", config.SequenceStart, config.SequenceMax, config.Behavior, SelectorType.String);
		}


		public SerialSequenceInfo SequenceInfo { get => sq; }


		public long Acquire()
		{
			long value = repo.AcquireSerial(sq);
			if (value > config.SequenceMax)
			{
				if (config.Behavior == SerialSequenceBehavior.Throw)
					throw new Exception("No more serials can be assigned from sequence.");
				value = config.SequenceStart;
				repo.SetCurrent(sq, value + 1);
			}
			return value;
		}

		public List<long> AcquireMultiple(int count)
		{
			List<long> serials = repo.AcquireMultiple(sq, count);
			var value = serials[serials.Count - 1];
			if (value > config.SequenceMax)
			{
				if (config.Behavior == SerialSequenceBehavior.Throw)
                    throw new NoMoreSerialAvailableMSQException("No more serials can be assigned from sequence.");
                if (config.SequenceStart + count > config.SequenceMax)
                    throw new CannotAllocateSerialException("Cannot allocate that many serials from sequence.");
                repo.SetCurrent(sq, config.SequenceStart);
				serials = repo.AcquireMultiple(sq, count);
			}
			return serials;
		}

		public void Release(long serial)
		{
			repo.ReuseSerial(sq, serial);
		}

		public void Preallocate(int count)
		{
			repo.Preallocate(sq, count);
		}

		public long GetCurrent()
		{
			return repo.GetSample(sq);
		}

		public void SetCurrent(long value)
		{
			if (value < config.SequenceStart) value = config.SequenceStart;
			if (value > config.SequenceMax) value = config.SequenceMax;
			repo.SetCurrent(sq, value);
		}

		public SerialSequenceBehavior Behavior { get; set; }

		public JObject Data { get; set; }

		public SingleSequenceConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(SingleSequenceConfig config)
		{
			if (String.IsNullOrWhiteSpace(config.ID))
				throw new Exception("Sequence ID cannot be null or empty");
			if (config.SequenceMax <= config.SequenceStart)
				throw new InvalidOperationException("SequenceStart cannot be greater than SequenceMax");
			this.config = config;
			Behavior = config.Behavior;
			sq.ID = config.ID;
			sq.Filter = "";
			sq.SequenceStart = config.SequenceStart;
			sq.MaxValue = config.SequenceMax;
			sq.Behavior = config.Behavior;
			sq.SelectorType = SelectorType.String;
		}
	}


	public class SingleSequenceConfig
	{
		[MaxLen(40)]
		public string ID;
		public long SequenceStart;
		public long SequenceMax;
		public SerialSequenceBehavior Behavior;
	}
}
