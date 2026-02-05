using System;

// References:  http://www.eternallyconfuzzled.com/tuts/algorithms/jsw_tut_hashing.aspx
//				http://www.burtleburtle.net/bob/hash/doobs.html

namespace Service.Contracts
{
	class JenkinsHash
	{
		public static unsafe int Compute(string s)
		{
			uint initval = 985468;
			uint len = (uint)s.Length;
			uint a, b, c = initval;
			a = b = 0x9e3779b9;
			fixed (char* pointer = s)
			{
				char* k = pointer;
				while (len >= 12)
				{
					a += ((uint)k[0] + ((uint)k[1] << 8) + ((uint)k[2] << 16) + ((uint)k[3] << 24));
					b += ((uint)k[4] + ((uint)k[5] << 8) + ((uint)k[6] << 16) + ((uint)k[7] << 24));
					c += (k[8] + ((uint)k[9] << 8) + ((uint)k[10] << 16) + ((uint)k[11] << 24));

					a -= b; a -= c; a ^= (c >> 13);
					b -= c; b -= a; b ^= (a << 8);
					c -= a; c -= b; c ^= (b >> 13);
					a -= b; a -= c; a ^= (c >> 12);
					b -= c; b -= a; b ^= (a << 16);
					c -= a; c -= b; c ^= (b >> 5);
					a -= b; a -= c; a ^= (c >> 3);
					b -= c; b -= a; b ^= (a << 10);
					c -= a; c -= b; c ^= (b >> 15);

					k += 12;
					len -= 12;
				}
				c += len;
				switch (len)
				{
					case 11: c += ((uint)k[10] << 24); goto case 10;
					case 10: c += ((uint)k[9] << 16); goto case 9;
					case 9: c += ((uint)k[8] << 8); goto case 8;
					case 8: b += ((uint)k[7] << 24); goto case 7;
					case 7: b += ((uint)k[6] << 16); goto case 6;
					case 6: b += ((uint)k[5] << 8); goto case 5;
					case 5: b += k[4]; goto case 4;
					case 4: a += ((uint)k[3] << 24); goto case 3;
					case 3: a += ((uint)k[2] << 16); goto case 2;
					case 2: a += ((uint)k[1] << 8); goto case 1;
					case 1:
						a += k[0];
						break;
				}

				a -= b; a -= c; a ^= (c >> 13);
				b -= c; b -= a; b ^= (a << 8);
				c -= a; c -= b; c ^= (b >> 13);
				a -= b; a -= c; a ^= (c >> 12);
				b -= c; b -= a; b ^= (a << 16);
				c -= a; c -= b; c ^= (b >> 5);
				a -= b; a -= c; a ^= (c >> 3);
				b -= c; b -= a; b ^= (a << 10);
				c -= a; c -= b; c ^= (b >> 15);
			}
			return (int)c;
		}
	}
}
