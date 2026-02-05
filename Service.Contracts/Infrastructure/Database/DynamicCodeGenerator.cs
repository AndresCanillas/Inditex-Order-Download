using System;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Service.Contracts.Database
{
	class DynamicCodeGenerator
	{
		/// <summary>
		/// Creates a dynamic method that copies all properties from an Entity to another (of the same type)
		/// </summary>
		/// <param name="info">EntityMetadata object containing all metadata that has been extracted from the type T</param>
		/// <remarks>
		/// The objective is to create a Dynamic Method that copies properties and fields from one object to another
		/// in the fastest posible way (without using reflection each time).
		/// 
		/// The resulting method would look like the following code:
		/// 
		/// void CopyEmployee(Employee src, Employee dest)
		/// {
		///		dest.ID = src.ID;
		///		dest.Name = src.Name;
		///		dest.Address = src.Address;
		///		dest.JoinDate = src.JoinDate;
		/// }
		/// 
		/// The returned DynamicMethod will allow you to call this code in the fastest posible way, almost
		/// matching the execution time of an early-bound method call.
		/// </remarks>
		public static DynamicMethod CopyEntity<T>(EntityMetadata info)
		{
			Type type = typeof(T);
			DynamicMethod dm = new DynamicMethod("EntityCopy_" + type.Name, null, new Type[] { type, type }, typeof(DynamicCodeGenerator).Module);
			ILGenerator il = dm.GetILGenerator();
			foreach (ColumnInfo c in info.Columns)
			{
				if (c.IsIgnore) continue;
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_0);
				if (c.MemberType == MemberCType.Field)
					il.Emit(OpCodes.Ldfld, c.fInfo);
				else
					il.EmitCall(OpCodes.Callvirt, c.pInfo.GetGetMethod(), null);
				if (c.MemberType == MemberCType.Field)
					il.Emit(OpCodes.Stfld, c.fInfo);
				else
					il.EmitCall(OpCodes.Callvirt, c.pInfo.GetSetMethod(), null);
			}
			il.Emit(OpCodes.Ret);
			return dm;
		}


		/// <summary>
		/// Creates a DynamicMethod that initializes the properties and fields of the given entity using data from
		/// a data reader.
		/// </summary>
		/// <param name="info">TableMetadata object containing all metadata that has been extracted from the type T</param>
		/// <param name="rd">DataReader used to fetch data (this is required since we need a reference to the get_Item method)</param>
		/// <remarks>
		/// The objective is to create a Dynamic Method that initializes the properties and fields of the object
		/// using data from the IDataReader in the fastest posible way (without using reflection to assign each field).
		/// 
		/// IMPORTANT NOTES:
		/// 
		/// In case you get an exception when calling this dynamic method, double check that the data type 
		/// of the field that causes the exception matches the data type of the corresponding column
		/// in the database.
		/// 
		/// Also a frecuent source for exceptions is to include fields in the entity class that do not exist
		/// in the database, make sure the names of the columns are the same than the name of the fields
		/// in the entity or use the TargetColumn attribute when required.
		/// 
		/// The Nullable attribute is also very important, if a column in the database has a null value
		/// and the corresponding field in the entity is not marked as Nullable the system will not try
		/// to check for null, this will cause an exception.
		/// 
		/// Finally, if your entity requires fields that do not exist in the database decorate them
		/// with the Hidden attribute so that they are ignored by the DBX library.
		/// </remarks>
		public static DynamicMethod FillEntityFromDataReader(Type type, EntityMetadata info, IDataReader rd)
		{
			MethodInfo drGetItem = rd.GetType().GetMethod("get_Item", new Type[] { typeof(string) });
			MethodInfo stringFormat = typeof(string).GetMethod("Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
			DynamicMethod dm = new DynamicMethod("FillEntity_" + type.Name, null, new Type[] { type, typeof(IDataReader) }, typeof(DynamicCodeGenerator).Module);
			ILGenerator il = dm.GetILGenerator();

			ConstructorInfo exConstructor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });
			MethodInfo exceptionMessage = typeof(Exception).GetMethod("get_Message", BindingFlags.Public | BindingFlags.Instance);
			MethodInfo containsFields = typeof(DBConfiguration).GetMethod("ReaderContainsFields", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(IDataReader), typeof(string[]) }, null);

			LocalBuilder loc_obj = il.DeclareLocal(typeof(object));
			LocalBuilder loc_fieldName = il.DeclareLocal(typeof(string));
			LocalBuilder loc_exception = il.DeclareLocal(typeof(Exception));
			LocalBuilder loc_fieldNames = il.DeclareLocal(typeof(string[]));
			LocalBuilder loc_fieldFlags = il.DeclareLocal(typeof(bool[]));

			il.Emit(OpCodes.Ldstr, "NULL");
			il.Emit(OpCodes.Stloc, loc_fieldName);

			// Create an array containing all the data contract field names
			il.Emit(OpCodes.Ldc_I4, info.Columns.Count);
			il.Emit(OpCodes.Newarr, typeof(string));
			il.Emit(OpCodes.Stloc, loc_fieldNames);
			int idx = 0;
			foreach (ColumnInfo c in info.Columns)
			{
				//Ignore fields and properties that are marked as LazyLoad or Ignore.
				if (c.IsLazyLoad || c.IsIgnore) continue;

				il.Emit(OpCodes.Ldloc, loc_fieldNames);
				il.Emit(OpCodes.Ldc_I4, idx++);
				il.Emit(OpCodes.Ldstr, c.CName);
				il.Emit(OpCodes.Stelem_Ref);
			}

			// Initialize the fieldFlags local
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldloc, loc_fieldNames);
			il.Emit(OpCodes.Call, containsFields);
			il.Emit(OpCodes.Stloc, loc_fieldFlags);

			// try
			il.BeginExceptionBlock();
			idx = 0;
			foreach (ColumnInfo c in info.Columns)
			{
				//Ignore fields and properties that are marked as LazyLoad or Ignore.
				if (c.IsLazyLoad || c.IsIgnore) continue;

				// Check if the data reader contains the column, if not skip this field.
				Label LFlagJump = il.DefineLabel();
				il.Emit(OpCodes.Ldloc, loc_fieldFlags);
				il.Emit(OpCodes.Ldc_I4, idx);
				il.Emit(OpCodes.Ldelem_I1);
				il.Emit(OpCodes.Brfalse, LFlagJump);

				//Save field name for reference in case of an exception...
				il.Emit(OpCodes.Ldstr, c.CName);
				il.Emit(OpCodes.Stloc, loc_fieldName);

				//Copy field
				if (c.IsNullable)
				{
					Label L1 = il.DefineLabel();
					Label L2 = il.DefineLabel();

					//Get value from DataReader using the indexer (get_Item) and store it in local variable 0
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldstr, c.CName);
					il.EmitCall(OpCodes.Callvirt, drGetItem, null);
					il.Emit(OpCodes.Stloc, loc_obj);

					//Verify if value in local variable 0 is of type DBNull
					il.Emit(OpCodes.Ldloc, loc_obj);
					il.Emit(OpCodes.Isinst, typeof(DBNull));
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Beq, L1);  //jump to L1 if not

					//===================>
					//Local variable 0 is of type DBNull, so we will asign null or the value zero to the field
					//===================>

					il.Emit(OpCodes.Ldarg_0);
					if (c.DataType.IsPrimitive)
					{
						//Initializes the field using diferent "zero" numeric constants
						if (c.DataType == typeof(Byte))
							il.Emit(OpCodes.Ldc_I4_0);
						else if (c.DataType == typeof(Int16))
							il.Emit(OpCodes.Ldc_I4_0);
						else if (c.DataType == typeof(Int32))
							il.Emit(OpCodes.Ldc_I4_0);
						else if (c.DataType == typeof(Int64))
						{
							il.Emit(OpCodes.Ldc_I4_0);
							il.Emit(OpCodes.Conv_I8);
						}
						else if (c.DataType == typeof(Single))
							il.Emit(OpCodes.Ldc_R4, 0.0f);
						else if (c.DataType == typeof(Double))
							il.Emit(OpCodes.Ldc_R8, 0.0d);
						else if (c.DataType == typeof(Char))
							il.Emit(OpCodes.Ldc_I4_0);
						else if (c.DataType == typeof(Boolean))
							il.Emit(OpCodes.Ldc_I4_0);
						else throw new Exception("Invalid data type");  //Unhandled primitive data type, throw an exception
					}
					else if (c.DataType.IsValueType)
					{
						//Initializes the field using OpCode:Initobj (usable on any struct, usually a DateTime field)
						LocalBuilder tmp = il.DeclareLocal(c.DataType);
						il.Emit(OpCodes.Ldloca, tmp.LocalIndex);
						il.Emit(OpCodes.Initobj, c.DataType);
						il.Emit(OpCodes.Ldloc, tmp);
					}
					else il.Emit(OpCodes.Ldnull);		//Initializes the field with null since its a reference type

					if (c.MemberType == MemberCType.Field)
						il.Emit(OpCodes.Stfld, c.fInfo);
					else
						il.EmitCall(OpCodes.Callvirt, c.pInfo.GetSetMethod(), null);
					il.Emit(OpCodes.Br, L2);


					il.MarkLabel(L1);
					//===================>
					//LABEL L1: Value in local variable 0 is not DBNull
					//===================>

					//Assign local variable to the corresponding field, unboxing/casting as necesary
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldloc, loc_obj);
					if (c.DataType.IsValueType)
						il.Emit(OpCodes.Unbox_Any, c.DataType);
					else
						il.Emit(OpCodes.Castclass, c.DataType);
					if (c.MemberType == MemberCType.Field)
						il.Emit(OpCodes.Stfld, c.fInfo);
					else
						il.EmitCall(OpCodes.Callvirt, c.pInfo.GetSetMethod(), null);


					il.MarkLabel(L2);
					//===================>
					//LABEL L2: End of conditional statement
					//===================>
				}
				else
				{
					//Get value from DataReader using the indexer (get_Item) and leave it in stack
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg_1);
					il.Emit(OpCodes.Ldstr, c.CName);
					il.EmitCall(OpCodes.Callvirt, drGetItem, null);

					//Assign value to the corresponding field, unboxing/casting as necesary
					if (c.DataType.IsValueType)
						il.Emit(OpCodes.Unbox_Any, c.DataType);
					else
						il.Emit(OpCodes.Castclass, c.DataType);
					if (c.MemberType == MemberCType.Field)
						il.Emit(OpCodes.Stfld, c.fInfo);
					else
						il.EmitCall(OpCodes.Call, c.pInfo.GetSetMethod(), null);
				}
				il.MarkLabel(LFlagJump);
				idx++;
			}

			// catch
			il.BeginCatchBlock(typeof(Exception));
			// throw an exception with the name of the field that caused the problem...
			il.Emit(OpCodes.Stloc, loc_exception);
			il.Emit(OpCodes.Ldstr, "There is a problem in the data contract " + type.FullName + ". Field name: {0} Exception: {1}");
			il.Emit(OpCodes.Ldloc, loc_fieldName);
			il.Emit(OpCodes.Ldloc, loc_exception);
			il.Emit(OpCodes.Callvirt, exceptionMessage);
			il.Emit(OpCodes.Call, stringFormat);
			il.Emit(OpCodes.Newobj, exConstructor);
			il.Emit(OpCodes.Throw);
			il.EndExceptionBlock();

			il.Emit(OpCodes.Ret);
			return dm;
		}


		private static ConcurrentDictionary<string, Func<object, object[]>> GetColumnsMethods = new ConcurrentDictionary<string, Func<object, object[]>>();


		internal static Func<object, object[]> GetColumnValues(object o, EntityMetadata info)
		{
			Func<object, object[]> func;
			Type type = o.GetType();
			if (!GetColumnsMethods.TryGetValue(type.FullName, out func))
			{
				DynamicMethod dm = new DynamicMethod("GetCols_" + type.Name, typeof(object[]), new Type[] { typeof(object) }, true);
				ILGenerator il = dm.GetILGenerator();
				LocalBuilder locResult = il.DeclareLocal(typeof(object[]));
				LocalBuilder locEntity = il.DeclareLocal(type);
				il.Emit(OpCodes.Ldc_I4, info.Columns.Count);
				il.Emit(OpCodes.Newarr, typeof(object));
				il.Emit(OpCodes.Stloc, locResult);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Isinst, type);
				il.Emit(OpCodes.Stloc, locEntity);
				int idx = 0;
				foreach (ColumnInfo c in info.Columns)
				{
					if (c.IsLazyLoad || c.IsIgnore)
						continue;
					il.Emit(OpCodes.Ldloc, locResult);
					il.Emit(OpCodes.Ldc_I4, idx++);
					il.Emit(OpCodes.Ldloc, locEntity);
					if (c.MemberType == MemberCType.Field)
						il.Emit(OpCodes.Ldfld, c.fInfo);
					else
						il.EmitCall(OpCodes.Callvirt, c.pInfo.GetGetMethod(), null);
					if (c.DataType.IsValueType)
						il.Emit(OpCodes.Box, c.DataType);
					il.Emit(OpCodes.Stelem_Ref);
				}
				il.Emit(OpCodes.Ldloc, locResult);
				il.Emit(OpCodes.Ret);
				func = dm.CreateDelegate(typeof(Func<object, object[]>)) as Func<object, object[]>;
				GetColumnsMethods[type.FullName] = func;
			}
			return func;
		}


		




		/// <summary>
		/// Creates a DynamicMethod that initializes a list of entities from a data reader.
		/// </summary>
		/// <param name="info">TableMetadata object containing all metadata that has been extracted from the entity</param>
		/// <param name="rd">DataReader used to fetch data (this is required since we need a reference to the get_Item method)</param>
		/// <remarks>
		/// The objective is to create a Dynamic Method which initializes the properties and fields of an entity
		/// for each row fetched from the data reader, these entities are then placed in a List that will be returned
		/// at the end of the method execution.
		/// 
		/// Using the same Employee entity seen in the FillEntityFromDataReader example, a call to
		/// MakeListFromDataReader with the Employee type would generate code similar to the code found in the template section.
		/// 
		/// The returned DynamicMethod will allow you to call this code in the fastest posible way, almost
		/// matching the execution time of an early-bound method call.
		/// 
		/// IMPORTANT NOTES:
		/// 
		/// In case you get an exception when calling this dynamic method, double check that the data type 
		/// of the field that causes the exception matches the data type of the corresponding column
		/// in the database. Search "SQL Server Data Types and their .NET Framework Equivalents" in google
		/// for a guide on data type compatibilities between SQL Server and C#.
		/// 
		/// Also a frecuent source for exceptions is to include fields in the entity class that do not exist
		/// in the database, make sure the names of the columns are the same than the name of the fields
		/// in the entity or use the TargetColumn attribute when required.
		/// 
		/// The Nullable attribute is also very important, if a column in the database has a null value
		/// and the corresponding field in the entity is not marked as Nullable, the system will not try
		/// to check for null, this will cause an exception.
		/// 
		/// Finally, if your entity requires fields that do not exist in the database decorate them
		/// with the Hidden attribute so that they are ignored by the DBX library.
		/// </remarks>
		public static DynamicMethod MakeListFromDataReader(Type entityType, Type specificListType, EntityMetadata info, IDataReader rd)
		{
			DynamicMethod dm = new DynamicMethod("MakeList_" + entityType.Name, specificListType, new Type[] { typeof(IDataReader) }, typeof(DynamicCodeGenerator).Module);
			ILGenerator il = dm.GetILGenerator();

			MethodInfo rdGetItem = rd.GetType().GetMethod("get_Item", new Type[] { typeof(string) });
			MethodInfo rdReadMethod = rd.GetType().GetMethod("Read", Type.EmptyTypes);
			MethodInfo rdCloseMethod = rd.GetType().GetMethod("Close", Type.EmptyTypes);
			MethodInfo stringFormat = typeof(string).GetMethod("Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string), typeof(object), typeof(object) }, null);
			MethodInfo listAdd = specificListType.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { entityType }, null);
			MethodInfo containsFields = typeof(DBConfiguration).GetMethod("ReaderContainsFields", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(IDataReader), typeof(string[]) }, null);

			ConstructorInfo exConstructor = typeof(Exception).GetConstructor(new Type[] { typeof(string) });
			MethodInfo exceptionMessage = typeof(Exception).GetMethod("get_Message", BindingFlags.Public | BindingFlags.Instance);
			ConstructorInfo lstConstructor = specificListType.GetConstructor(Type.EmptyTypes);
			ConstructorInfo entityConstructor = entityType.GetConstructor(Type.EmptyTypes);

			LocalBuilder loc_obj = il.DeclareLocal(typeof(object));
			LocalBuilder loc_fieldName = il.DeclareLocal(typeof(string));
			LocalBuilder loc_list = il.DeclareLocal(specificListType);
			LocalBuilder loc_entity = il.DeclareLocal(entityType);
			LocalBuilder loc_exception = il.DeclareLocal(typeof(Exception));
			LocalBuilder loc_exceptionMsg = il.DeclareLocal(typeof(string));
			LocalBuilder loc_fieldNames = il.DeclareLocal(typeof(string[]));
			LocalBuilder loc_fieldFlags = il.DeclareLocal(typeof(bool[]));

			Label LoopStart = il.DefineLabel();
			Label LoopCondition = il.DefineLabel();
			Label EndPoint = il.DefineLabel();

			il.Emit(OpCodes.Ldstr, "NULL");
			il.Emit(OpCodes.Stloc, loc_fieldName);

			// try
			il.BeginExceptionBlock();

			// Create an array containing all the data contract field names
			il.Emit(OpCodes.Ldc_I4, info.Columns.Count);
			il.Emit(OpCodes.Newarr, typeof(string));
			il.Emit(OpCodes.Stloc, loc_fieldNames);
			int idx = 0;
			foreach (ColumnInfo c in info.Columns)
			{
				il.Emit(OpCodes.Ldloc, loc_fieldNames);
				il.Emit(OpCodes.Ldc_I4, idx++);
				il.Emit(OpCodes.Ldstr, c.CName);
				il.Emit(OpCodes.Stelem_Ref);
			}

			// Initialize the fieldFlags local
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, loc_fieldNames);
			il.Emit(OpCodes.Call, containsFields);
			il.Emit(OpCodes.Stloc, loc_fieldFlags);

			//Create the list that will hold the initialized entities and store the new instance in the local variable loc_list.
			il.Emit(OpCodes.Newobj, lstConstructor);
			il.Emit(OpCodes.Stloc, loc_list);

			//Jump to the "LoopCondition" label
			il.Emit(OpCodes.Br, LoopCondition);

			//Mark the location of the "LoopStart" label
			il.MarkLabel(LoopStart);

			//Initialize fieldName to "(Entity Contructor)" to reflect that if an exception is raised here,
			//the source is the constructor of the entity and not a field in the data reader.
			il.Emit(OpCodes.Ldstr, "(Entity Contructor)");
			il.Emit(OpCodes.Stloc, loc_fieldName);

			//Create new entity instance and store it in the local variable "loc_entity"
			il.Emit(OpCodes.Newobj, entityConstructor);
			il.Emit(OpCodes.Stloc, loc_entity);

			// The following foreach will generate the required code to Copy Data from the data reader to the entity fields
			idx = 0;
			foreach (ColumnInfo c in info.Columns)
			{
				//Ignore fields and properties that are marked as LazyLoad.
				if (c.IsLazyLoad || c.IsIgnore)
				{
					idx++;
					continue;
				}

				// Check if the data reader contains the column, if not skip this field.
				Label LFlagJump = il.DefineLabel();
				il.Emit(OpCodes.Ldloc, loc_fieldFlags);
				il.Emit(OpCodes.Ldc_I4, idx);
				il.Emit(OpCodes.Ldelem_I1);
				il.Emit(OpCodes.Brfalse, LFlagJump);

			    //Stores the field name in the local variable loc_fieldName for reference in case of an exception...
			    il.Emit(OpCodes.Ldstr, c.CName);
			    il.Emit(OpCodes.Stloc, loc_fieldName);

			    if (c.IsNullable) //Copy field checking for DBNull 
			    {
			        Label L1 = il.DefineLabel();
			        Label L2 = il.DefineLabel();

			        //Get value from DataReader using the indexer (get_Item) and store it in local variable "loc_obj"
			        il.Emit(OpCodes.Ldarg_0);
			        il.Emit(OpCodes.Ldstr, c.CName);
			        il.EmitCall(OpCodes.Callvirt, rdGetItem, null);
			        il.Emit(OpCodes.Stloc, loc_obj);

			        //Verify if value in local variable "loc_obj" is of type DBNull
			        il.Emit(OpCodes.Ldloc, loc_obj);
			        il.Emit(OpCodes.Isinst, typeof(DBNull));
			        il.Emit(OpCodes.Ldnull);
			        il.Emit(OpCodes.Beq, L1);  //jump to L1 if not

			        //===================>
			        //Local variable "loc_obj" is DBNull, so we will asign null or the value zero to the corresponding entity field
			        //===================>

			        il.Emit(OpCodes.Ldloc, loc_entity);
			        if (c.DataType.IsPrimitive)
			        {
			            //Initializes the field using diferent "zero" numeric constants
			            if (c.DataType == typeof(Byte))
			                il.Emit(OpCodes.Ldc_I4_0);
			            else if (c.DataType == typeof(Int16))
			                il.Emit(OpCodes.Ldc_I4_0);
			            else if (c.DataType == typeof(Int32))
			                il.Emit(OpCodes.Ldc_I4_0);
			            else if (c.DataType == typeof(Int64))
			            {
			                il.Emit(OpCodes.Ldc_I4_0);
			                il.Emit(OpCodes.Conv_I8);
			            }
			            else if (c.DataType == typeof(Single))
			                il.Emit(OpCodes.Ldc_R4, 0.0f);
			            else if (c.DataType == typeof(Double))
			                il.Emit(OpCodes.Ldc_R8, 0.0d);
			            else if (c.DataType == typeof(Char))
			                il.Emit(OpCodes.Ldc_I4_0);
			            else if (c.DataType == typeof(Boolean))
			                il.Emit(OpCodes.Ldc_I4_0);
			            else throw new Exception("Invalid data type");  //Unhandled primitive data type, throw an exception
			        }
			        else if (c.DataType.IsValueType)
			        {
			            //Initializes the field using OpCode:Initobj (usable on any struct, usually a DateTime field)
			            LocalBuilder tmp = il.DeclareLocal(c.DataType);
			            il.Emit(OpCodes.Ldloca, tmp.LocalIndex);
			            il.Emit(OpCodes.Initobj, c.DataType);
			            il.Emit(OpCodes.Ldloc, tmp);
			        }
			        else il.Emit(OpCodes.Ldnull);		//Initializes the field with null since its a reference type

			        if (c.MemberType == MemberCType.Field)
			            il.Emit(OpCodes.Stfld, c.fInfo);
			        else
			            il.EmitCall(OpCodes.Callvirt, c.pInfo.GetSetMethod(), null);
			        il.Emit(OpCodes.Br, L2);


			        il.MarkLabel(L1);
			        //===================>
			        //LABEL L1: Value in local variable "loc_obj" is not DBNull
			        //===================>

			        //Assign value stored in "loc_obj" to the corresponding entity field, unboxing/casting as necesary
			        il.Emit(OpCodes.Ldloc, loc_entity);
			        il.Emit(OpCodes.Ldloc, loc_obj);
			        if (c.DataType.IsValueType)
			            il.Emit(OpCodes.Unbox_Any, c.DataType);
			        else
			            il.Emit(OpCodes.Castclass, c.DataType);
			        if (c.MemberType == MemberCType.Field)
			            il.Emit(OpCodes.Stfld, c.fInfo);
			        else
			            il.EmitCall(OpCodes.Callvirt, c.pInfo.GetSetMethod(), null);


			        il.MarkLabel(L2);
			        //===================>
			        //LABEL L2: End of conditional statement
			        //===================>
			    }
			    else
			    {
			        //Copy without DBNull check (since field is not nullable)

			        //Get value from DataReader using the indexer (get_Item) and leave it in stack
			        il.Emit(OpCodes.Ldloc, loc_entity);
			        il.Emit(OpCodes.Ldarg_0);
			        il.Emit(OpCodes.Ldstr, c.CName);
			        il.EmitCall(OpCodes.Callvirt, rdGetItem, null);

			        //Assign extracted value to the corresponding field, unboxing/casting as necesary
			        if (c.DataType.IsValueType)
			            il.Emit(OpCodes.Unbox_Any, c.DataType);
			        else
			            il.Emit(OpCodes.Castclass, c.DataType);
			        if (c.MemberType == MemberCType.Field)
			            il.Emit(OpCodes.Stfld, c.fInfo);
			        else
			            il.EmitCall(OpCodes.Call, c.pInfo.GetSetMethod(), null);
			    }
				il.MarkLabel(LFlagJump);
				idx++;
			}

			//Add initialized entity to the list
			il.Emit(OpCodes.Ldloc, loc_list);
			il.Emit(OpCodes.Ldloc, loc_entity);
			il.Emit(OpCodes.Callvirt, listAdd);

			//Loop condition check: if ( reader.Read() ) jump to LoopStart
			il.MarkLabel(LoopCondition);
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Callvirt, rdReadMethod);
			il.Emit(OpCodes.Brtrue, LoopStart);

			il.Emit(OpCodes.Leave, EndPoint);

			// catch
			il.BeginCatchBlock(typeof(Exception));
			// throw new exception with the name of the field that caused the problem...
			il.Emit(OpCodes.Stloc, loc_exception);
			il.Emit(OpCodes.Ldstr, "There is a problem in the data contract " + entityType.FullName + ". Field name: {0}; Exception: {1}");
			il.Emit(OpCodes.Ldloc, loc_fieldName);
			il.Emit(OpCodes.Ldloc, loc_exception);
			il.Emit(OpCodes.Call, stringFormat);
			il.Emit(OpCodes.Newobj, exConstructor);
			il.Emit(OpCodes.Throw);
			il.EndExceptionBlock();

			// Loop broke, so return the populated list and we are done...
			il.MarkLabel(EndPoint);
			//close the reader
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Callvirt, rdCloseMethod);
			//return the list
			il.Emit(OpCodes.Ldloc, loc_list);
			il.Emit(OpCodes.Ret);

			return dm;
		}


		#region templatecode
		class Employee
		{
			public int ID;
			public string Name;
			public string Address;
			public DateTime JoinDate;
		}

		static void InitializeEmployee(Employee e, IDataReader rd)
		{
			object o;
			string fieldName = "NULL";
			string[] fieldNames = new string[] { "ID", "Name", "Address", "JoinDate" };
			bool[] fieldFlags = DBConfiguration.ReaderContainsFields(rd, fieldNames);

			for (int i = 0; i < 50; i++)
			{
				fieldNames[i] = "Hello";
			}


			try
			{
				if (fieldFlags[0])
				{
					fieldName = "ID";
					e.ID = (int)rd["ID"];
				}

				if (fieldFlags[1])
				{
					fieldName = "Name";
					e.Name = (string)rd["Name"];
				}

				if (fieldFlags[2])
				{
					fieldName = "Address";
					o = rd["Address"];  //Check if value is DBNull cause this field is nullable...
					if (o is DBNull)
						e.Address = null;
					else
						e.Address = (string)o;
				}

				if (fieldFlags[3])
				{
					fieldName = "JoinDate";
					o = rd["JoinDate"]; //Check if value is DBNull cause this field is nullable...
					if (o is DBNull)
						e.JoinDate = default(DateTime);
					else
						e.JoinDate = (DateTime)o;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("There is a problem with a field in the data contract. Field name: {0} Exception: {1}", fieldName, ex.Message), ex);
			}
		}


		static List<Employee> MakeList_Employee(IDataReader rd)
		{
			List<Employee> lst;
			Employee e;
			object o;
			string fieldName = "NULL";
			try
			{
				string[] fieldNames = new string[]{"ID", "Name", "Address", "JoinDate"};
				bool[] fieldFlags = DBConfiguration.ReaderContainsFields(rd, fieldNames);
				lst = new List<Employee>();
				while (rd.Read())
				{
					// Set fieldName to (Entity Contructor) so that in case of exception we can know that the
					// source is the constructor and not a field.
					fieldName = "(Entity Constructor)";

					// IMPORTANT: entity must have an empty constructor
					e = new Employee();

					//Copy field ID without checking for DBNull becuase the field is not marked as nullable
					if (fieldFlags[0])
					{
						fieldName = "ID";
						e.ID = (int)rd["ID"];
					}

					//Copy field Name without checking for DBNull becuase the field is not marked as nullable
					if (fieldFlags[1])
					{
						fieldName = "Name";
						e.Name = (string)rd["Name"];
					}

					//Copy field Address checking for DBNull cause this field is marked as nullable...
					if (fieldFlags[2])
					{
						fieldName = "Address";
						o = rd["Address"];
						if (o is DBNull)
							e.Address = null;
						else
							e.Address = (string)o;
					}

					//Copy field Address checking for DBNull cause this field is marked as nullable...
					if (fieldFlags[3])
					{
						fieldName = "JoinDate";
						o = rd["JoinDate"];
						if (o is DBNull)
							e.JoinDate = default(DateTime);
						else
							e.JoinDate = (DateTime)o;
					}

					if (fieldFlags[2493])
					{
						Console.Write("7");
					}

					int idx = 100;
					if (fieldFlags[idx])
					{
						Console.Write("x");
					}

					//Add the newly created & initialized entity to the list
					lst.Add(e);
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("There is a problem with a field in the data contract [TYPE]. Field name: {0} Exception: {1}", fieldName, ex.Message), ex);
			}
			return lst;
		}


		static object[] GetCols_Employee(object obj)
		{
			object[] result = new object[4];
			Employee e = obj as Employee;
			result[0] = e.ID;
			result[1] = e.Name;
			result[2] = e.Address;
			result[3] = e.JoinDate;
			return result;
		}
		#endregion
	}
}
