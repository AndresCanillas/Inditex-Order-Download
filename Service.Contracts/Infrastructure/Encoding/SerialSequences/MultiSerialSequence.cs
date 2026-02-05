using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Service.Contracts;

namespace Service.Contracts
{
	public class MultiSerialSequence : ISerialSequence, IConfigurable<MultiSequenceConfig>
	{
		private JObject data;
		private ISerialRepository repo;
		private MultiSequenceConfig config;
		private SerialSequenceInfo sq;

		public MultiSerialSequence(ISerialRepository repo)
		{
			this.repo = repo;
			config = new MultiSequenceConfig() { ID = Guid.NewGuid().ToString(), SequenceStart = 1, SequenceMax = 9999999, Behavior = SerialSequenceBehavior.Throw, SelectorField = "Barcode", SelectorType = SelectorType.EAN13 };
			sq = new SerialSequenceInfo(config.ID, "", config.SequenceStart, config.SequenceMax, config.Behavior, config.SelectorType);
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
			var serials = repo.AcquireMultiple(sq, count);
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

		public JObject Data
		{
			get { return data; }
			set
			{
				data = value;
				if (data != null)
					sq.Filter = GetFilterValue();
				else
					sq.Filter = "";
			}
		}

		public MultiSequenceConfig GetConfiguration()
		{
			return config;
		}

		public void SetConfiguration(MultiSequenceConfig config)
		{
			if (String.IsNullOrWhiteSpace(config.ID))
				throw new Exception("Sequence ID cannot be null or empty");
			if (config.SequenceMax <= config.SequenceStart)
				throw new InvalidOperationException("SequenceStart cannot be greater than SequenceMax");
			if (string.IsNullOrWhiteSpace(config.SelectorField))
				throw new InvalidOperationException("Selector Field cannot be empty, it must reference a field from ProductData.");
			this.config = config;
			Behavior = config.Behavior;
			sq.ID = config.ID;
			sq.SequenceStart = config.SequenceStart;
			sq.MaxValue = config.SequenceMax;
			sq.Behavior = config.Behavior;
			sq.SelectorType = config.SelectorType;
		}

		private string GetFilterValue()
		{
			if (data == null)
				throw new InvalidOperationException("Cannot call Acquire/AcquireMultiple/Release/SetCurrent if the product data has not been set.");
			var fieldValue = data.GetValue<string>(config.SelectorField);
			if (String.IsNullOrWhiteSpace(fieldValue))
				throw new InvalidOperationException("Invalid Product Data, field " + config.SelectorField + " is null, empty or does not exist.");
			return fieldValue;
		}
	}


	public class MultiSequenceConfig
	{
		[MaxLen(40)]
		public string ID;
		public long SequenceStart;
		public long SequenceMax;
		public SerialSequenceBehavior Behavior;
		public string SelectorField;      // This is the name of the field (within IProductData) that will be used
										  // to segregate multiple sequences. The most obvious would be to segregate
										  // the sequence by Barcode, however by letting this field be configurable,
										  // we leave the door open for other criteria.

		public SelectorType SelectorType; // SelectorType indicates how to validate and normalize the value of the selector field.
	}

	public enum SelectorType
	{
		EAN13 = 0,     // EAN13 type is normalized by decoding the barcode and removing the checkdigit, so only 12 digits are used as value for the selector field.
		EAN_13 = 1,    // Due to the issue in configuration causing Repeated Serial Numbers, we will now take both 0 and 1 as EAN13, and String will now have a value of 1000. This works because, at this point in time, we know that all multiserial sequences work with Barcode as selector field and therefore, all should be normalized as EAN13.
		String = 1000  // String type is normalized by converting all letters to lowercase and removing blanks at the beggining and end of the string.
	}
}
