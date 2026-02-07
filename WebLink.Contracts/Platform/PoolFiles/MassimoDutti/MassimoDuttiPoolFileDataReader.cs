using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Service.Contracts;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles.MassimoDutti
{
	public class MassimoDuttiPoolFileDataReader : PoolFileDataReader<MassimoDuttiPoolFileRow>
	{
		public MassimoDuttiPoolFileDataReader(int projectid, Stream stream)
			: base(projectid, stream)
		{
			Parser.Delimiter = '\t';
			ValueMapping = new Dictionary<int, Func<object>>()
			{
				{  0, () => 0 },						// ID
				{  1, () => projectid },				// ProjectID
				{  2, () => Values.OrderNum },		// OrderNumber
				{  3, () => Values.Seasson },		// Seasson
				{  4, () => Values.Year },			// Year
				{  5, () => Values.CodProv1 },		// ProviderCode1
				{  6, () => Values.Provider },		// ProviderName1
				{  7, () => Values.CodProv2 },		// ProviderCode2
				{  8, () => null },						// ProviderName2
				{  9, () => Values.Size },			// Size
				{ 10, () => Values.CodColor },		// ColorCode
				{ 11, () => Values.Color },		// ColorName
				{ 12, () =>	Values.PVP },			// Price1
				{ 13, () => Values.PVPEuro },		// Price2
				{ 14, () => Values.Quantity },		// Quantity
				{ 15, () => Values.LabelType },	// ArticleCode
				{ 16, () => Values.Model },		// CategoryCode1
				{ 17, () => Values.Section },		// CategoryText1
				{ 18, () => Values.Quality },		// CategoryCode2
				{ 19, () => Values.SizeSystem },	// CategoryText2
				{ 20, () => Values.CodFamily },	// CategoryCode3
				{ 21, () => Values.Family },		// CategoryText3
				{ 22, () => Values.CodSubFamily },	// CategoryCode4
				{ 23, () => Values.SubFamily },	// CategoryText4
				{ 24, () => null },						// CategoryCode5
				{ 25, () => Values.Origin },		// CategoryText5
				{ 26, () => null },						// CategoryCode6
				{ 27, () => Values.SubFamily },	// CategoryText6
				{ 28, () => DateTime.ParseExact(Values.CreateDate, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) },	// CreationDate
				{ 29, () => Values.UpdateDate == null? null : (object)DateTime.ParseExact(Values.UpdateDate, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) },	// LastUpdatedDate
				{ 30, () => Values.ServiceDate == null? null : (object)DateTime.ParseExact(Values.ServiceDate, "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) },// ExpectedProductionDate
                { 31, () => string.Empty}
			};
		}
	}
}
