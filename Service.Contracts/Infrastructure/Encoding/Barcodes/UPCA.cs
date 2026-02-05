using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Contracts
{
/*
 * Is a 12 digit code composed of the following 4 components:
 *	> First digit: Number System,
 *	> 5 digits: Company Prefix
 *	> 5 digits: Product Number
 *	> Last digit: Check Code
 * 
 * Only the following number systems are defined:
 *	> 0		Regular UPC code
 *	> 2		Weight items marked at store
 *	> 3		Drug/Health related code
 *	> 4		No format restrictions
 *	> 5		Coupons
 *	> 7		Regular UPC code
 *	
 *	The check digit is calculated using the standard MOD algorithm. UPC-A is compatible with EAN-13 (GTIN-13)
 */
	class UPCA
	{
	}
}
