using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
	/* 
	 * Password derive methods
	 * ===========================================================================================
	 * These methods reflect formulas and procedures used to derive the passwords used to lock
	 * the RFID tags for write and kill operations.
	 * 
	 * All of these methods revolve around the idea of having a hashing algorithm that is secret,
	 * as well as a predefined "seed" value that is also secret.
	 * 
	 * It is assumed that as long both the algorithm and the seed values remain secret the
	 * tags will be secure, i.e., malicious users will not be able to overwrite the information
	 * in the tags or kill them.
	 * 
	 * In general terms, the seed (or key), which is a fixed value and is known by us and
	 * the client, will be mixed with some other data that is available from the tag. Then
	 * some operations will be performed to generate a password that is "unique" for each
	 * tag.
	 * 
	 * We have 3 main methods to derive password:
	 * 
	 *	> Fixed password. In this case there is no derivation process, we simply agree to use a fixed
	 *	  access and kill password with the client. All tags will have the same access/kill passwords.
	 *	  This is by far the easiest method, and since it does not require any computations, many
	 *	  clients request it.
	 *	  
	 *	> InditexMD5. This is a method defined by Inditex, all brands pertaining to inditex/tempe
	 *	  use it. Changes to this algorithm are not advised because the client will loose its ability
	 *	  to write information on the tag if this algorithm is changed unilateraly. This method uses
	 *	  a seed (Int32) and mixes it with the serial number of the tag (also an int32) using an XOR,
	 *	  then an MD5 hash is calculated, and from the resulting hash, 4 bytes are selected to be used
	 *	  as the password. 
	 *	  
	 *	> HMACSHA256. This is a method similar in spirit to the previous one, but uses the SHA256
	 *	  hash function wich is more robust. In the end we select only 4 bytes to use as password,
	 *	  which negates much of what is gained from using a cryptographic hash anyway.
	 *	  
	 * All these methods have been agreed upon with different clients, and therefore, they should never be 
	 * changed. In case a client requests a different method for deriving their passwords, then a new
	 * class inheriting from PasswordDeriveMethod should be created, this is to ensure that other clients
	 * using the previously defined methods are not impacted by any change.
	 */

	public interface IPasswordDeriveMethod
	{
		int ID { get; }
		string Name { get; }
		bool WritePassword { get; }
		string Seed { get; set; }
		string DerivePassword(ITagEncoding encoding);
	}

	// NOTE: All password derive methods defined to date use a simple seed value to generate passwords.
	// If a new derive method is requested by a client, that new method can have its own configuration
	// class if needed, without impacting existing clients.
	public class PasswordDeriveMethodConfig
	{
		public bool WritePassword;      // Specifies if the command to update the password should be emmited or not
		public string Seed;             // Seed used to generate passwords
		public InputFormat SeedFormat;  // Used to determine how to handle the seed value
		public InputFormat EPCFormat;   // Used to determine how to handle the epc value (when calculating hash)
	}

	public enum InputFormat
	{
		Hexadecimal,
		Text
	}
}
