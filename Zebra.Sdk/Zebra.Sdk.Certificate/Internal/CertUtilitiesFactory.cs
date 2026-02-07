using System;
using System.Reflection;

namespace Zebra.Sdk.Certificate.Internal
{
	internal class CertUtilitiesFactory
	{
		public CertUtilitiesFactory()
		{
		}

		public static CertificateHelperI GetCertificateHelper()
		{
			CertificateHelperI certificateHelperI = null;
			try
			{
				AssemblyName[] referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
				for (int i = 0; i < (int)referencedAssemblies.Length; i++)
				{
					string name = referencedAssemblies[i].Name;
					if (name.Contains("SdkApi"))
					{
						try
						{
							certificateHelperI = (CertificateHelperI)Activator.CreateInstance(Assembly.Load(new AssemblyName(name)).GetType("Zebra.Sdk.Certificate.Internal.CertificateHelper", true, true));
							break;
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			return certificateHelperI;
		}

		public static CertUtilitiesI GetCertUtilities()
		{
			CertUtilitiesI certUtilitiesI = null;
			try
			{
				AssemblyName[] referencedAssemblies = Assembly.GetEntryAssembly().GetReferencedAssemblies();
				for (int i = 0; i < (int)referencedAssemblies.Length; i++)
				{
					string name = referencedAssemblies[i].Name;
					if (name.Contains("SdkApi"))
					{
						try
						{
							certUtilitiesI = (CertUtilitiesI)Activator.CreateInstance(Assembly.Load(new AssemblyName(name)).GetType("Zebra.Sdk.Certificate.Internal.CertUtilities", true, true));
							break;
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			return certUtilitiesI;
		}
	}
}