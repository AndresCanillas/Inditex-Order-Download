using Newtonsoft.Json.Linq;
using Service.Contracts;
using Service.Contracts.Database;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebLink.Contracts.Models
{
    public partial class OrderRepository : GenericRepository<IOrder, Order>, IOrderRepository
    {

        public IEnumerable<OrderDetailWithCompoDTO> GetOrderCustomReportPage(OrderReportFilter filter)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                return GetOrderCustomReportPage(ctx, filter).ToList();
            }
        }

        public IEnumerable<OrderDetailWithCompoDTO> GetOrderCustomReportPage(PrintDB ctx, OrderReportFilter filter)
        {
            var providerRepo = factory.GetInstance<IProviderRepository>();
            var userData = factory.GetInstance<IUserData>();

            var details = GetOrderCustomReportDetail(ctx, userData, filter).ToList();
            filter.TotalRecords = GetOrderCustomReportCount(ctx, userData, filter);   // TODO: calculate from data retrieved in the previous line to avoid double query??

            List<OrderDetailWithCompoDTO> page = new List<OrderDetailWithCompoDTO>();
            page.AddRange(details);

            return page;
        }


        private IEnumerable<OrderDetailWithCompoDTO> GetOrderCustomReportDetail(PrintDB ctx, IUserData userData, OrderReportFilter filter)
        {
            ProductionType prodType = (ProductionType)filter.ProductionType;
            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };

            var vendorID = 0;
            // for current company and suppliers ?
            if(userData.SelectedCompanyID != 1)
            {
                vendorID = userData.SelectedCompanyID;
            }

            IQueryable<OrderDetailWithCompoDTO> qry = Enumerable.Empty<OrderDetailWithCompoDTO>().AsQueryable();

            if(filter.ReportType == ReportType.Detailed) {

                qry = from j in ctx.PrinterJobs
                      join pjd in ctx.PrinterJobDetails on j.ID equals pjd.PrinterJobID
                      join a in ctx.Articles on j.ArticleID equals a.ID
                      join o in ctx.CompanyOrders on j.CompanyOrderID equals o.ID
                      join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                      //join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                      join project in ctx.Projects on o.ProjectID equals project.ID
                      join brand in ctx.Brands on project.BrandID equals brand.ID
                      join st in ctx.Companies on o.SendToCompanyID equals st.ID
                      join pv in ctx.CompanyProviders on o.ProviderRecordID equals pv.ID


                      // left join providers with multiple keys
                      //join prvmap in ctx.CompanyProviders on new { k1 = brand.CompanyID, k2 = o.SendToCompanyID } equals new { k1 = prvmap.CompanyID, k2 = prvmap.ProviderCompanyID } into Providers
                      //join prvmap in ctx.ProviderTrewView on new { k1 = o.CompanyID } equals new { k1 = prvmap.TopParentID } into Providers
                      join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                      from provider in Providers.DefaultIfEmpty()

                          // left join with Wizard
                          //join wzdmap in ctx.Wizards on o.ID equals wzdmap.OrderID into Wizards
                          //from wzd in Wizards.DefaultIfEmpty()

                          // left join with fabrics
                      join locmap in ctx.Locations on o.LocationID equals locmap.ID into Locations
                      from loc in Locations.DefaultIfEmpty()

                      join lblmap in ctx.Labels on a.LabelID equals lblmap.ID into labelsMap
                      from l in labelsMap.DefaultIfEmpty()

                      where

                      (
                          filter.OrderStatus == OrderStatus.Cancelled
                          || (
                              props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                          //&& grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                          )
                      )
                      && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                      && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                      && (vendorID.Equals(0) || o.SendToCompanyID == vendorID || brand.CompanyID == vendorID)
                      //&& (vendorID.Equals(0) || provider.CompanyID.Equals(vendorID) || provider.Parents.Contains($".{vendorID}."))
                      && (filter.CompanyID.Equals(0) || brand.CompanyID.Equals(filter.CompanyID))
                      && project.ID.Equals(filter.ProjectID)
                      && ((filter.OrderStatus.Equals(OrderStatus.None) && !excludeFromAllStatusOption.Contains(o.OrderStatus))
                      || o.OrderStatus.Equals(filter.OrderStatus))
                      && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                      && (
                          filter.InConflict.Equals(ConflictFilter.Ignore)
                          || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                          || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                      )
                      //&& (
                      //    filter.IsBilled.Equals(BilledFilter.Ignore)
                      //    || (filter.IsBilled.Equals(BilledFilter.Yes) && o.IsBilled.Equals(true) && o.SyncWithSage == true)
                      //    || (filter.IsBilled.Equals(BilledFilter.No) && o.IsBilled.Equals(false) && o.SyncWithSage == false)
                      //)
                      //&& (
                      //    filter.IsStopped.Equals(StopFilter.Ignore)
                      //    || filter.IsStopped.Equals(StopFilter.Stoped) && o.IsStopped.Equals(true)
                      //    || filter.IsStopped.Equals(StopFilter.NoStoped) && o.IsStopped.Equals(false)
                      //)
                      && o.CreatedDate >= filter.OrderDate
                      && o.CreatedDate <= filter.OrderDateTo
                      &&
                      (
                          prodType == ProductionType.All
                          || o.ProductionType == prodType
                      )
                      && (filter.FactoryID == 0 || o.LocationID == filter.FactoryID)
                      &&
                      (
                          string.IsNullOrEmpty(filter.ProviderClientReference)
                          || provider.ClientReference.Contains(filter.ProviderClientReference)
                      )
                      && (
                        string.IsNullOrEmpty(filter.OrderCategoryClient)
                      //|| grp.OrderCategoryClient.Contains(filter.OrderCategoryClient)
                      )
                      orderby o.CreatedDate descending, o.OrderGroupID
                      select new OrderDetailWithCompoDTO
                      {
                          ArticleID = a.ID,
                          Article = a.Name,
                          ArticleCode = a.ArticleCode,
                          Description = a.Description,
                          Quantity = pjd.Quantity,
                          QuantityRequested = pjd.QuantityRequested,
                          ArticleBillingCode = a.BillingCode,
                          IsItem = a.LabelID == null || a.LabelID < 1 ? true : false,
                          UpdatedDate = j.UpdatedDate.ToCSVDateFormat(),
                          LabelID = a.LabelID,

                          ProductDataID = pjd.ProductDataID,
                          PrinterJobDetailID = pjd.ID,
                          PrinterJobID = j.ID,
                          PackCode = pjd.PackCode,
                          OrderID = j.CompanyOrderID,
                          ProjectID = o.ProjectID,
                          OrderGroupID = o.OrderGroupID,
                          OrderNumber = o.OrderNumber,
                          OrderStatus = o.OrderStatus,
                          IsBilled = o.IsBilled,
                          SyncWithSage = a.SyncWithSage,
                          SageReference = a.SageRef,
                          DisplayField = GetGroupingColumn(l.GroupingFields),
                          IncludeComposition = l != null ? l.IncludeComposition : false,
                          SendTo = st.Name,
                          ClientReference = pv.ClientReference,
                          Location = loc.Name,
                          CreatedDate = o.CreatedDate.ToCSVDateFormat(),
                          OrderDueDate = o.DueDate.ToCSVDateFormat(),
                          ValidatedDate = o.ValidationDate.ToCSVDateFormat()
                      };
            }
            else
                qry = GetReportGrouped(qry,filter,ctx, excludeFromAllStatusOption,vendorID, prodType);


            if(filter.CSV == false)
            {
                qry = qry
                 .Skip((filter.CurrentPage - 1) * filter.PageSize)
                 .Take(filter.PageSize);
            }

            var details = qry.ToList();


            if(details.Count > 0)
            {

                var catalogs = GetCompositionCatalogsForProject(ctx, filter.ProjectID);
                var detailCatalog = catalogs.First(f => f.Name.Equals(Catalog.ORDERDETAILS_CATALOG));
                var productCatalog = catalogs.First(f => f.Name.Equals(Catalog.VARIABLEDATA_CATALOG));
                var compositionCatalog = catalogs.FirstOrDefault(f => f.Name.Equals(Catalog.COMPOSITIONLABEL_CATALOG));

                var productColumns = string.Empty;
                var compositionColumns = string.Empty;
                var compositionJoin = string.Empty;

                if(filter.ProductFields != null && filter.ProductFields.Count > 0)
                {
                    productColumns = string.Join(',', filter.ProductFields.Select(s => "Product." + s).ToArray());
                    productColumns = ", " + productColumns;
                }

                if(filter.CompositionFields != null && filter.CompositionFields.Count > 0)
                {
                    compositionColumns = string.Join(',', filter.CompositionFields.Select(s => "Compo." + s).ToArray());
                    compositionColumns = ", " + compositionColumns;

                    if(compositionCatalog != null)
                        compositionJoin = $"LEFT JOIN {compositionCatalog.TableName} Compo ON Product.HasComposition = Compo.ID";

                }

                var productDataList = details.Select(s => s.ProductDataID);
                JArray productDetails = null;


                using(var dynamicDB = connManager.CreateDynamicDB())
                {
                    productDetails = dynamicDB.Select(detailCatalog.CatalogID, $@"
                    SELECT Details.ID AS ProductDataID   {productColumns} {compositionColumns}
                    FROM #TABLE Details
                    INNER JOIN {productCatalog.TableName} Product ON Details.Product = Product.ID
                    {compositionJoin}
                    WHERE Details.ID in ({string.Join(",", productDataList.ToArray())})
                    ");

                }


                details.ForEach(d =>
                {
                    var product = productDetails.Where(w => ((JObject)w).GetValue<int>("ProductDataID").Equals(d.ProductDataID)).FirstOrDefault();

                    if(product != null)
                    {
                        d.Size = ((JObject)product).GetValue<string>("Size");
                        d.UnitDetails = ((JObject)product).GetValue<string>("TXT1");
                        d.Color = ((JObject)product).GetValue<string>("Color");
                        d.GroupingField = ((JObject)product).GetValue<string>(d.DisplayField);
                    }
                    else
                    {// corrupted data was lossed by database problem, file logs was fullup at 202407
                        product = new JObject();
                        product["ProductDataID"] = string.Empty;

                        if(filter.ProductFields != null && filter.ProductFields.Count > 0)
                        {
                            filter.ProductFields.ForEach(col =>
                            {
                                product[col] = string.Empty;
                            });
                        }

                        if(filter.CompositionFields != null && filter.CompositionFields.Count > 0)
                        {
                            filter.CompositionFields.ForEach(col =>
                            {
                                product[col] = string.Empty;
                            });
                        }


                    }
                    d.ProductData = (JObject)product; // composition is included here
                });
            }

            return details;
        }

        private IQueryable<OrderDetailWithCompoDTO> GetReportGrouped(IQueryable<OrderDetailWithCompoDTO> qry, OrderReportFilter filter, PrintDB ctx, List<OrderStatus> excludeFromAllStatusOption, int vendorID, ProductionType prodType)
        {
            qry = from j in ctx.PrinterJobs
                  join pjd in ctx.PrinterJobDetails on j.ID equals pjd.PrinterJobID
                  join a in ctx.Articles on j.ArticleID equals a.ID
                  join o in ctx.CompanyOrders on j.CompanyOrderID equals o.ID
                  join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                  //join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                  join project in ctx.Projects on o.ProjectID equals project.ID
                  join brand in ctx.Brands on project.BrandID equals brand.ID
                  join st in ctx.Companies on o.SendToCompanyID equals st.ID
                  join pv in ctx.CompanyProviders on o.ProviderRecordID equals pv.ID

                  join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                  from provider in Providers.DefaultIfEmpty()

                  join locmap in ctx.Locations on o.LocationID equals locmap.ID into Locations
                  from loc in Locations.DefaultIfEmpty()

                  join lblmap in ctx.Labels on a.LabelID equals lblmap.ID into labelsMap
                  from l in labelsMap.DefaultIfEmpty()
                  where
                  (
                            filter.OrderStatus == OrderStatus.Cancelled
                            || (
                                props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                            )
                  )
                  && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                  && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                        && (vendorID.Equals(0) || o.SendToCompanyID == vendorID || brand.CompanyID == vendorID)
                        && (filter.CompanyID.Equals(0) || brand.CompanyID.Equals(filter.CompanyID))
                  && project.ID.Equals(filter.ProjectID)
                  && ((filter.OrderStatus.Equals(OrderStatus.None) && !excludeFromAllStatusOption.Contains(o.OrderStatus))
                        || o.OrderStatus.Equals(filter.OrderStatus))
                        && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                  && (
                  filter.InConflict.Equals(ConflictFilter.Ignore)
                            || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                            || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                  )
                        && o.CreatedDate >= filter.OrderDate
                        && o.CreatedDate <= filter.OrderDateTo
                  &&
                        (
                  prodType == ProductionType.All
                            || o.ProductionType == prodType
                        )
                        && (filter.FactoryID == 0 || o.LocationID == filter.FactoryID)
                        &&
                  (
                  string.IsNullOrEmpty(filter.ProviderClientReference)
                            || provider.ClientReference.Contains(filter.ProviderClientReference)
                  )
                        && (
                  string.IsNullOrEmpty(filter.OrderCategoryClient)
                        )
                  //orderby o.CreatedDate descending, o.OrderGroupID
                  group new { j, pjd, a, o, st, pv, loc, l, brand, project, props } by new { a.ArticleCode, o.OrderNumber } into grouped
                  select new OrderDetailWithCompoDTO
                  {
                      ArticleID = grouped.Where(x => x.a.ID != null).Select(x => x.a.ID).DefaultIfEmpty(0).Max(),
                      Article = grouped.Where(x => !string.IsNullOrEmpty(x.a.Name)).Select(x => x.a.Name).DefaultIfEmpty("").Max(),
                      ArticleCode = grouped.Key.ArticleCode,
                      Description = grouped.Where(x => !string.IsNullOrEmpty(x.a.Description)).Select(x => x.a.Description).DefaultIfEmpty("").Max(),
                      Quantity = grouped.Select(x => x.pjd.Quantity).DefaultIfEmpty(0).Sum(),
                      QuantityRequested = grouped.Select(x => x.pjd.QuantityRequested).DefaultIfEmpty(0).Sum(),
                      ArticleBillingCode = grouped.Where(x => !string.IsNullOrEmpty(x.a.BillingCode)).Select(x => x.a.BillingCode).DefaultIfEmpty("").Max(),
                      IsItem = grouped.Where(x => x.a.LabelID != null).Select(x => x.a.LabelID == null || x.a.LabelID < 1).DefaultIfEmpty(false).Max(),
                      UpdatedDate = grouped.Where(x => x.j.UpdatedDate != null).Select(x => x.j.UpdatedDate.ToCSVDateFormat()).DefaultIfEmpty("").Max(),
                      LabelID = grouped.Where(x => x.a.LabelID != null).Select(x => x.a.LabelID).DefaultIfEmpty(0).Max(),
                      ProductDataID = grouped.Where(x => x.pjd.ProductDataID != null).Select(x => x.pjd.ProductDataID).DefaultIfEmpty(0).Max(),
                      PrinterJobDetailID = grouped.Where(x => x.pjd.ID != null).Select(x => x.pjd.ID).DefaultIfEmpty(0).Max(),
                      PrinterJobID = grouped.Where(x => x.j.ID != null).Select(x => x.j.ID).DefaultIfEmpty(0).Max(),
                      PackCode = grouped.Where(x => !string.IsNullOrEmpty(x.pjd.PackCode)).Select(x => x.pjd.PackCode).DefaultIfEmpty("").Max(),
                      OrderID = grouped.Where(x => x.j.CompanyOrderID != null).Select(x => x.j.CompanyOrderID).DefaultIfEmpty(0).Max(),
                      ProjectID = grouped.Where(x => x.o.ProjectID != null).Select(x => x.o.ProjectID).DefaultIfEmpty(0).Max(),
                      OrderGroupID = grouped.Where(x => x.o.OrderGroupID != null).Select(x => x.o.OrderGroupID).DefaultIfEmpty(0).Max(),
                      OrderNumber = grouped.Where(x => !string.IsNullOrEmpty(x.o.OrderNumber)).Select(x => x.o.OrderNumber).DefaultIfEmpty("").Max(),
                      OrderStatus = grouped.Where(x => x.o.OrderStatus != null).Select(x => x.o.OrderStatus).DefaultIfEmpty(OrderStatus.None).Max(),
                      IsBilled = grouped.Where(x => x.o.IsBilled != null).Select(x => x.o.IsBilled).DefaultIfEmpty(false).Max(),
                      SyncWithSage = grouped.Where(x => x.a.SyncWithSage != null).Select(x => x.a.SyncWithSage).DefaultIfEmpty(false).Max(),
                      SageReference = grouped.Where(x => !string.IsNullOrEmpty(x.a.SageRef)).Select(x => x.a.SageRef).DefaultIfEmpty("").Max(),
                      DisplayField = grouped.Any(x => x.a.LabelID != null)
                                ? GetGroupingColumn(grouped.Where(x => x.l.GroupingFields != null).Select(x => x.l.GroupingFields).DefaultIfEmpty("").Max())
                                : "Barcode",
                      IncludeComposition = grouped.Where(x => x.l != null).Select(x => x.l.IncludeComposition).DefaultIfEmpty(false).Max(),
                      SendTo = grouped.Where(x => !string.IsNullOrEmpty(x.st.Name)).Select(x => x.st.Name).DefaultIfEmpty("").Max(),
                      ClientReference = grouped.Where(x => !string.IsNullOrEmpty(x.pv.ClientReference)).Select(x => x.pv.ClientReference).DefaultIfEmpty("").Max(),
                      Location = grouped.Where(x => !string.IsNullOrEmpty(x.loc.Name)).Select(x => x.loc.Name).DefaultIfEmpty("").Max(),
                      CreatedDate = grouped.Where(x => x.o.CreatedDate != null).Select(x => x.o.CreatedDate.ToCSVDateFormat()).DefaultIfEmpty("").Max(),
                      OrderDueDate = grouped.Where(x => x.o.DueDate != null).Select(x => x.o.DueDate.ToCSVDateFormat()).DefaultIfEmpty("").Max(),
                      ValidatedDate = grouped.Where(x => x.o.ValidationDate != null).Select(x => x.o.ValidationDate.ToCSVDateFormat()).DefaultIfEmpty("").Max()
                  };

            return qry;

        }

        private int GetOrderCustomReportCount(PrintDB ctx, IUserData userData, OrderReportFilter filter)
        {
            ProductionType prodType = (ProductionType)filter.ProductionType;
            var excludeFromAllStatusOption = new List<OrderStatus>()
            {
                OrderStatus.Cancelled
            };

            var vendorID = 0;
            // for current company and suppliers ?
            if(userData.SelectedCompanyID != 1)
            {
                vendorID = userData.SelectedCompanyID;
            }

            int count = 0;

            if(filter.ReportType == ReportType.Detailed)
            {
                count = (from j in ctx.PrinterJobs
                         join pjd in ctx.PrinterJobDetails on j.ID equals pjd.PrinterJobID
                         join a in ctx.Articles on j.ArticleID equals a.ID
                         join o in ctx.CompanyOrders on j.CompanyOrderID equals o.ID
                         join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                         join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                         join project in ctx.Projects on o.ProjectID equals project.ID
                         join brand in ctx.Brands on project.BrandID equals brand.ID


                         // left join providers with multiple keys
                         //join prvmap in ctx.CompanyProviders on new { k1 = brand.CompanyID, k2 = o.SendToCompanyID } equals new { k1 = prvmap.CompanyID, k2 = prvmap.ProviderCompanyID } into Providers
                         //join prvmap in ctx.ProviderTrewView on new { k1 = o.CompanyID } equals new { k1 = prvmap.TopParentID } into Providers
                         join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                         from provider in Providers.DefaultIfEmpty()

                             // left join with Wizard
                         join wzdmap in ctx.Wizards on o.ID equals wzdmap.OrderID into Wizards
                         from wzd in Wizards.DefaultIfEmpty()

                             // left join with fabrics
                         join locmap in ctx.Locations on o.LocationID equals locmap.ID into Locations
                         from loc in Locations.DefaultIfEmpty()

                         join lblmap in ctx.Labels on a.LabelID equals lblmap.ID into labelsMap
                         from l in labelsMap.DefaultIfEmpty()

                         where

                         (
                             filter.OrderStatus == OrderStatus.Cancelled
                             || (
                                 props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                                 && grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                             )
                         )
                         && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                         && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                         && (vendorID.Equals(0) || grp.SendToCompanyID == vendorID || brand.CompanyID == vendorID)
                         //&& (vendorID.Equals(0) || provider.CompanyID.Equals(vendorID) || provider.Parents.Contains($".{vendorID}."))
                         && (filter.CompanyID.Equals(0) || brand.CompanyID.Equals(filter.CompanyID))
                         && project.ID.Equals(filter.ProjectID)
                         && ((filter.OrderStatus.Equals(OrderStatus.None) && !excludeFromAllStatusOption.Contains(o.OrderStatus))
                         || o.OrderStatus.Equals(filter.OrderStatus))
                         && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                         && (
                             filter.InConflict.Equals(ConflictFilter.Ignore)
                             || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                             || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                         )
                         //&& (
                         //    filter.IsBilled.Equals(BilledFilter.Ignore)
                         //    || (filter.IsBilled.Equals(BilledFilter.Yes) && o.IsBilled.Equals(true) && o.SyncWithSage == true)
                         //    || (filter.IsBilled.Equals(BilledFilter.No) && o.IsBilled.Equals(false) && o.SyncWithSage == false)
                         //)
                         //&& (
                         //    filter.IsStopped.Equals(StopFilter.Ignore)
                         //    || filter.IsStopped.Equals(StopFilter.Stoped) && o.IsStopped.Equals(true)
                         //    || filter.IsStopped.Equals(StopFilter.NoStoped) && o.IsStopped.Equals(false)
                         //)
                         && o.CreatedDate >= filter.OrderDate
                         && o.CreatedDate <= filter.OrderDateTo
                         &&
                         (
                             prodType == ProductionType.All
                             || o.ProductionType == prodType
                         )
                         && (filter.FactoryID == 0 || o.LocationID == filter.FactoryID)
                         &&
                         (
                             string.IsNullOrEmpty(filter.ProviderClientReference)
                             || provider.ClientReference.Contains(filter.ProviderClientReference)
                         )
                         && (
                           string.IsNullOrEmpty(filter.OrderCategoryClient)
                           || grp.OrderCategoryClient.Contains(filter.OrderCategoryClient)
                         )
                         select o.ID).Count();
            }
            else
                count = GetOrderCustomReportCountDetailed(count, filter, ctx, excludeFromAllStatusOption, vendorID, prodType);

                

            return count;
        }

        private int GetOrderCustomReportCountDetailed(int count, OrderReportFilter filter, PrintDB ctx, List<OrderStatus> excludeFromAllStatusOption, int vendorID, ProductionType prodType)
        {
            count = (from j in ctx.PrinterJobs
                     join pjd in ctx.PrinterJobDetails on j.ID equals pjd.PrinterJobID
                     join a in ctx.Articles on j.ArticleID equals a.ID
                     join o in ctx.CompanyOrders on j.CompanyOrderID equals o.ID
                     join props in ctx.OrderUpdateProperties on o.ID equals props.OrderID
                     join grp in ctx.OrderGroups on o.OrderGroupID equals grp.ID
                     join project in ctx.Projects on o.ProjectID equals project.ID
                     join brand in ctx.Brands on project.BrandID equals brand.ID
                     join prvmap in ctx.CompanyProviders on o.ProviderRecordID equals prvmap.ID into Providers
                     from provider in Providers.DefaultIfEmpty()
                     join wzdmap in ctx.Wizards on o.ID equals wzdmap.OrderID into Wizards
                     from wzd in Wizards.DefaultIfEmpty()
                     join locmap in ctx.Locations on o.LocationID equals locmap.ID into Locations
                     from loc in Locations.DefaultIfEmpty()
                     join lblmap in ctx.Labels on a.LabelID equals lblmap.ID into labelsMap
                     from l in labelsMap.DefaultIfEmpty()
                     where

                     (
                         filter.OrderStatus == OrderStatus.Cancelled
                         || (
                             props.IsActive.Equals(true) && props.IsRejected.Equals(false)
                             && grp.IsActive.Equals(true) && grp.IsRejected.Equals(false)
                         )
                     )
                     && (String.IsNullOrWhiteSpace(filter.OrderNumber) || o.OrderNumber.Contains(filter.OrderNumber))
                     && (filter.OrderID.Equals(0) || o.ID.Equals(filter.OrderID))
                     && (vendorID.Equals(0) || grp.SendToCompanyID == vendorID || brand.CompanyID == vendorID)

                     && (filter.CompanyID.Equals(0) || brand.CompanyID.Equals(filter.CompanyID))
                     && project.ID.Equals(filter.ProjectID)
                     && ((filter.OrderStatus.Equals(OrderStatus.None) && !excludeFromAllStatusOption.Contains(o.OrderStatus))
                     || o.OrderStatus.Equals(filter.OrderStatus))
                     && (string.IsNullOrEmpty(filter.ArticleCode) || a.ArticleCode.Contains(filter.ArticleCode))
                     && (
                         filter.InConflict.Equals(ConflictFilter.Ignore)
                         || (filter.InConflict.Equals(ConflictFilter.InConflict) && o.IsInConflict.Equals(true))
                         || (filter.InConflict.Equals(ConflictFilter.NoConflict) && o.IsInConflict.Equals(false))
                     )
                     && o.CreatedDate >= filter.OrderDate
                     && o.CreatedDate <= filter.OrderDateTo
                     &&
                     (
                         prodType == ProductionType.All
                         || o.ProductionType == prodType
                     )
                     && (filter.FactoryID == 0 || o.LocationID == filter.FactoryID)
                     &&
                     (
                         string.IsNullOrEmpty(filter.ProviderClientReference)
                         || provider.ClientReference.Contains(filter.ProviderClientReference)
                     )
                     && (
                       string.IsNullOrEmpty(filter.OrderCategoryClient)
                       || grp.OrderCategoryClient.Contains(filter.OrderCategoryClient)
                     )
                     group a by a.ArticleCode into g
                     select g.Key).Count();

            return count;
        }

        public MemoryStream GetOrderFileCustomReport(OrderReportFilter filter)
        {

            var customFields = filter.ProductFields.Concat(filter.CompositionFields);
            var dynReport = new List<ExpandoObject>();
            var projectRepo = factory.GetInstance<IProjectRepository>();
            var project = projectRepo.GetByID(filter.ProjectID, true);
            var reportFields = Newtonsoft.Json.JsonConvert.DeserializeObject<CustomReportConfig>(project.CustomOrderDataReport);

            //var queryProperties = typeof(OrderDetailWithCompoDTO).GetProperties();
            var minDate = filter.OrderDate;
            var maxDate = filter.OrderDateTo;


            using(var strm = new MemoryStream())
            {
                while(minDate <= maxDate)
                {
                    filter.OrderDate = minDate;

                    if(minDate.AddDays(30) < maxDate)
                        filter.OrderDateTo = minDate.AddDays(30);
                    else
                        filter.OrderDateTo = maxDate;


                    minDate = filter.OrderDateTo.AddTicks(1);

                    using(var ctx = factory.GetInstance<PrintDB>())
                    {
                        var rpt = GetOrderCustomReportPage(ctx, filter).ToList();
                        // convert to csv report columns interface
                        //var newRpt = rpt.Select(s => new CSVCompanyOrderDTO(s));

                        rpt.ForEach(o =>
                        {
                            var row = new ExpandoObject();

                            var likeDictionary = row as IDictionary<string, Object>;



                            reportFields.Config
                            .Where(w => w.Include)
                            .OrderBy(sr => sr.Position)
                            .ToList()
                            .ForEach(col =>
                            {
                                if(col.CatalogName == "Default")
                                {
                                    var v = o.GetType().GetProperty(col.Column);
                                    likeDictionary.Add(col.Column, v.GetValue(o) ?? String.Empty);
                                }
                                else
                                {
                                    if(o.ProductData != null)
                                    {
                                        likeDictionary.Add(col.Column, o.ProductData.GetValue<string>(col.Column, string.Empty));
                                    }
                                }
                            });



                            dynReport.Add(row);

                        });
                    }

                }
                var dtt = dynReport.ToDataTable();

                using(var writer = new StreamWriter(strm))
                {
                    Rfc4180Writer.CreateStream(dtt, writer, true);
                    strm.Position = 0;
                    return strm;
                }
            }
        }

        public MemoryStream GetDeliveryReport(OrderReportFilter filter)
        {
            var dynReport = new List<ExpandoObject>();

            string[] reportfields =
            {
                "Quantity",
                "ArticleName",
                "ArticleCode",
                "Size",
                "Colour",
                "CompanyName",
                "OrderNumber",
                "MDOrderNumber",
                "OrderDate",
                "OrderStatusText",
                "FactoryCode",
                "LocationName",
                "SendTo",
                "SageReference"
            };

            string[] keyFields =
            {
                "LocationID",
                "ArticleID",
                "OrderID",
                "PrintJobId",
                "SendToCompanyID"
            };  

            using(var strm = new MemoryStream())
            {
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    var userData = factory.GetInstance<IUserData>();
                    var rpt = GetDeliveryReportDetail(ctx, userData, filter);

                    foreach(var row in rpt.Result)
                    {
                        var customrow = new ExpandoObject();
                        var likeDictionary = customrow as IDictionary<string, Object>;

                        likeDictionary.Add("ShippingDate", String.Empty);
                        likeDictionary.Add("DeliveryNote", String.Empty);
                        likeDictionary.Add("Carrier", String.Empty);
                        likeDictionary.Add("TrackingNumber", String.Empty);

                        string keys = String.Empty; 

                        foreach(var field in reportfields.Concat(keyFields))
                        {
                            var v = row.GetType().GetProperty(field);
                            if(v != null)
                            {
                                likeDictionary.Add(field, v.GetValue(row) ?? String.Empty);

                                if (keyFields.Contains(field))
                                {
                                    keys += v.GetValue(row)?.ToString() ?? String.Empty;
                                }
                            }
                        }

                        using(System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                        {
                            var checkCode = BitConverter.ToString(
                              md5.ComputeHash(Encoding.UTF8.GetBytes(keys))
                            ).Replace("-", String.Empty);

                            likeDictionary.Add("CheckCode", checkCode);
                        }
                        dynReport.Add(customrow);
                    }

                    var dtt = dynReport.ToDataTable();

                    using(var writer = new StreamWriter(strm))
                    {
                        Rfc4180Writer.CreateStream(dtt, writer, true);
                        strm.Position = 0;
                        return strm;
                    }
                }
            }
        }

    }

    internal class CustomReportConfig
    {
        public List<ReportColumn> Config { get; set; }
    }

    internal class ReportColumn
    {
        public string CatalogName { get; set; }
        public string Column { get; set; }
        public ColumnType Type { get; set; }
        public bool Include { get; set; }
        public int Position { get; set; }
    }
}
