using Service.Contracts.Database;
using Service.Contracts.Infrastructure.Encoding.SerialSequences;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Contracts
{
    public class JomaSerialSequence : IJomaSerialSequence
    {
        private readonly IConnectionManager conManager;
               
        private readonly object syncObj = new object();

        public JomaSerialSequence(IConnectionManager conManager)
        {
            this.conManager = conManager;
        }

        public List<long> AcquireMultiple(int count)
        {
            List<long> result = new List<long>();

            lock(syncObj)
            {
                using(var conn = conManager.OpenDB("MainDB"))
                {
                    // Use UTC date to avoid daylight savings issues
                    DateTime a = DateTime.UtcNow;

                    // Cut seconds, only interested in minutes
                    var currentDate = new DateTime(a.Year, a.Month, a.Day, a.Hour, a.Minute, 0, a.Kind);

                    // Get current sequence
                    var sequence = new JomaSerialSequences();
                    sequence = conn.Select<JomaSerialSequences>("select * from jomaserialsequences").FirstOrDefault();

                    // Create sequence if don't exists in DB
                    if(sequence == null)
                    {
                        sequence = new JomaSerialSequences();
                        sequence.Date = currentDate;
                        sequence.NextValue = 0;
                        conn.Insert(sequence);
                    }

                    // Update the date if it's before now
                    if(sequence.Date < currentDate)
                    {
                        sequence.Date = currentDate;
                        sequence.NextValue = 0;
                    }

                    // generate serials
                    for(var i = 0; i < count; i++)
                    {
                        var seq = sequence.NextValue.ToString("D2");
                        string datepart = sequence.Date.ToString("yyyyMMddHHmm");
                        string seqnumber = $"{datepart}{seq}";

                        result.Add(Convert.ToInt64(seqnumber));

                        // Increment sequence
                        if(sequence.NextValue == 99)
                        {
                            sequence.NextValue = 0;
                            sequence.Date = sequence.Date.AddMinutes(1);
                        }
                        else
                        {
                            sequence.NextValue++;
                        }
                    }
                    // Save sequence to DB
                    conn.Update(sequence);
                }
            }
            return result;
        }
    }

    [TargetTable("JomaSerialSequences")]
    public class JomaSerialSequences
    {
        [PK, Identity]
        public int ID { get; set; }
        public DateTime Date { get; set; }
        public int NextValue { get; set; }
    }
}
