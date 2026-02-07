using System;

namespace Zebra.Sdk.Settings.Internal
{
	internal interface SettingRange
	{
		bool IsInRange(string value);
	}
}