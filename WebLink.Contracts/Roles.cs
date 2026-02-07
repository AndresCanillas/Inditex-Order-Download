//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc.Filters;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace WebLink.Contracts
//{
//	public static class Roles
//	{
//		// IDT level roles - These roles grant access to different operations accross all existing companies

//		public const string SysAdmin = "SysAdmin";				 // Has unrestricted access to all records and operations and all the Companies
//		public const string IDTCostumerService = "IDTCommercial";// Has access to some records and some operations (including records for other companies)
//		public const string IDTProdManager = "IDTProdManager";   // Can see and manage production orders from all companies as long as they are to be produced by IDT
//		public const string IDTLabelDesign = "IDTLabelDesign";   // Can see and manage Labels from all companies
//																 // NOTE: Role PrinterOperator can be used for IDT users to allow them to print labels, they dont need
//																 // to see or operate printers that do not belong to IDT, and IDTPrinterOperator role is unnecesary.
//		public const string IDTDeliveryManager = "IDTDeliveryManager";  // User in charge of completing orders by sending them to the client once they are produced.

//		// Company level roles - These roles will grant access to records and operation associated to the company the user belongs to, while preventing access to records from other companies.

//		public const string CompanyAdmin = "CompanyAdmin";		// Can update any company record that is allowed to be modified by external users, it also grants all the permissions of the roles below
//		public const string ProdManager = "ProdManager";		// Can see and manage local production orders placed for the company
//		public const string DataUpload = "DataUpload";			// Can update company product data and upload orders 
//		public const string PrinterOperator = "PrinterOperator";// Can see and operate company printers, this also allows use of the single product and batch print options

//		public static bool IsIDTRole(string role)
//		{
//			if (role == SysAdmin || role == IDTCostumerService || role == IDTProdManager || role == IDTLabelDesign || role == IDTDeliveryManager)
//				return true;
//			else
//				return false;
//		}

//		public static List<RoleInfo> GetRoles()
//		{
//			var result = new List<RoleInfo>() {
//				new RoleInfo() { Name = Roles.SysAdmin, IsSysAdminRole = true, IsIDTRole = false, IsCompanyRole = false },
//				new RoleInfo() { Name = Roles.IDTCostumerService, IsSysAdminRole = false, IsIDTRole = true, IsCompanyRole = false },
//				new RoleInfo() { Name = Roles.IDTLabelDesign, IsSysAdminRole = false, IsIDTRole = true, IsCompanyRole = false },
//				new RoleInfo() { Name = Roles.IDTProdManager, IsSysAdminRole = false, IsIDTRole = true, IsCompanyRole = false },
//				new RoleInfo() { Name = Roles.IDTDeliveryManager, IsSysAdminRole = false, IsIDTRole = true, IsCompanyRole = false },
//				new RoleInfo() { Name = Roles.CompanyAdmin, IsSysAdminRole = false, IsIDTRole = false, IsCompanyRole = true },
//				new RoleInfo() { Name = Roles.ProdManager, IsSysAdminRole = false, IsIDTRole = false, IsCompanyRole = true },
//				new RoleInfo() { Name = Roles.DataUpload, IsSysAdminRole = false, IsIDTRole = false, IsCompanyRole = true },
//				new RoleInfo() { Name = Roles.PrinterOperator, IsSysAdminRole = false, IsIDTRole = false, IsCompanyRole = true },
//			};
//			return result;
//		}
//	}


//	public class RoleInfo
//	{
//		public string Name;
//		public bool IsSysAdminRole;
//		public bool IsIDTRole;
//		public bool IsCompanyRole;
//	}


//	public class AuthorizeRolesAttribute : AuthorizeAttribute
//	{
//		public AuthorizeRolesAttribute(params string[] roles) : base()
//		{
//			Roles = string.Join(",", roles);
//		}
//	}
//}
