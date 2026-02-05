using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;

namespace Service.Contracts
{
	/// <summary>
	/// Provides information about an encoding scheme registered in the system
	/// </summary>
	public class EncodingTypeInfo
	{
		public EncodingTypeInfo(Type t)
		{
			this.Type = t;
			this.ClassName = t.FullName;
			ITagEncoding enc = (ITagEncoding) t.InvokeMember("", BindingFlags.CreateInstance, null, null, null);
			this.ID = enc.TypeID;
			this.Name = enc.Name;
			this.UrnNameSpace = enc.UrnNameSpace;
		}

		public Type Type { get; }
		public string ClassName { get; }
		public int ID { get; }
		public string Name { get; }
		public string UrnNameSpace { get; }

		public ITagEncoding CreateTagEncoding()
		{
			ITagEncoding tag = Activator.CreateInstance(Type) as ITagEncoding;
			return tag;
		}
	}

	public interface ITagEncodingFactory
	{
		int Count { get; }
		EncodingTypeInfo this[int index] { get; }
		EncodingTypeInfo this[string encodingName] { get; }
		ITagEncoding CreateFromByteArray(byte[] byteCode);
		ITagEncoding CreateFromHexString(string hexCode);
		ITagEncoding CreateFromURN(string urn);
	}

	/// <summary>
	/// Factory class for the different encodings supported by the system.
	/// </summary>
	public class TagEncodingFactory: ITagEncodingFactory
	{
		private static List<EncodingTypeInfo> types;

		static TagEncodingFactory()
		{
			types = new List<EncodingTypeInfo>();
			types.Add(new EncodingTypeInfo(typeof(Sgtin96)));
			types.Add(new EncodingTypeInfo(typeof(Tempe128)));
			types.Add(new EncodingTypeInfo(typeof(Gid96)));
			types.Add(new EncodingTypeInfo(typeof(Sscc96)));
			types.Add(new EncodingTypeInfo(typeof(Sgln96)));
			types.Add(new EncodingTypeInfo(typeof(Grai96)));
			types.Add(new EncodingTypeInfo(typeof(Giai96)));
			types.Add(new EncodingTypeInfo(typeof(DoD96)));
			types.Add(new EncodingTypeInfo(typeof(Sgtin198)));
			types.Add(new EncodingTypeInfo(typeof(Sgln195)));
			types.Add(new EncodingTypeInfo(typeof(Grai170)));
			types.Add(new EncodingTypeInfo(typeof(Giai202)));
			types.Add(new EncodingTypeInfo(typeof(DecimalTagEncoding)));
			if (Directory.Exists("Encodings"))
			{
				string[] dlls = Directory.GetFiles("Encoding", "*.dll");
				foreach (string entry in dlls)
				{
					try
					{
						Assembly asm = Assembly.Load(File.ReadAllBytes(entry));
						Type[] typesInAssembly = asm.GetTypes();
						foreach (Type t in typesInAssembly)
						{
							ConstructorInfo ci = t.GetConstructor(Type.EmptyTypes);
							if (t.IsSubclassOf(typeof(TagEncoding)) && t.IsPublic && ci != null && !t.IsAbstract)
							{
								types.Add(new EncodingTypeInfo(t));
							}
						}
					}
					catch
					{
						// Either not a valid assembly or file cannot be read. Either way, skip and keep going...
					}
				}
			}
		}


		/// <summary>
		/// Gets the number of encodings registered in the system
		/// </summary>
		public int Count { get { return types.Count; } }


		/// <summary>
		/// Gets the information of the encoding in the specified index.
		/// </summary>
		public EncodingTypeInfo this[int index]
		{
			get
			{
				return types[index];
			}
		}

		/// <summary>
		/// Gets the information of the encoding with the given name.
		/// </summary>
		public EncodingTypeInfo this[string encodingName]
		{
			get
			{
				return types.FirstOrDefault(p => p.Name == encodingName);
			}
		}

		/// <summary>
		/// Attempts to decode the specified byte array using each registered encoding scheme, returns the first that matches the code.
		/// </summary>
		public ITagEncoding CreateFromByteArray(byte[] byteCode)
		{
			foreach(var encodingInfo in types)
			{
				try
				{
					var codec = encodingInfo.CreateTagEncoding();
					if (codec.InitializeFromByteArray(byteCode)) return codec;
				}
				catch
				{
					// ignores errors, keeps looking for a match...
				}
			}
			return UnknownEncoding.Instance;
		}

		/// <summary>
		/// Attempts to decode the specified code given as an hexadecimal string using each registered encoding scheme, returns the first that matches the code.
		/// </summary>
		public ITagEncoding CreateFromHexString(string hexCode)
		{
			foreach(var encodingInfo in types)
			{
				try
				{
					var codec = encodingInfo.CreateTagEncoding();
					if (codec.InitializeFromHexString(hexCode)) return codec;
				}
				catch
				{
					// ignores errors, keeps looking for a match...
				}
			}
			return UnknownEncoding.Instance;
		}


		/// <summary>
		/// Attempts to decode the specified code given in URN format using each registered encoding scheme, returns the first that matches the code.
		/// </summary>
		public ITagEncoding CreateFromURN(string urn)
		{
			foreach(var encodingInfo in types)
			{
				try
				{
					var codec = encodingInfo.CreateTagEncoding();
					if (codec.InitializeFromUrn(urn)) return codec;
				}
				catch
				{
					// ignores errors, keeps looking for a match...
				}
			}
			return UnknownEncoding.Instance;
		}
	}
}
