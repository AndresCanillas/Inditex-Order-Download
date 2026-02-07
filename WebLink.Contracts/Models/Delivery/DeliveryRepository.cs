using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Misc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using WebLink.Contracts.Models.Delivery.DTO;
using WebLink.Contracts.Services.Ship24;

namespace WebLink.Contracts.Models.Delivery
{
    public class OrderStatusDTO
    {
        public int CompanyOrderId;
        public int Completed;
    }

    public class DeliveryRepository : IDeliveryRepository
    {
        private IFactory factory;
        private IConnectionManager connManager;
        private readonly IUserData userData;
        private IOrderRepository orderRepo;
        private readonly ILocalizationService g;
        private IShip24ClientService ship24ClientService;

        public DeliveryRepository(IFactory factory, IConnectionManager connManager, IUserData userData,
            IShip24ClientService ship24ClientService, IOrderRepository orderRepo, ILocalizationService g
            )
        {
            this.factory = factory;
            this.connManager = connManager;
            this.userData = userData;
            this.ship24ClientService = ship24ClientService;
            this.orderRepo = orderRepo;
            this.g = g; 
        }

        public void UpdateOrdersDeliveryStatus()
        {
            // look for all orders that have not been delivered or cancelled and have delivery note   

            using(var conn = connManager.OpenDB("MainDB"))
            {
                var ordersList = conn.Select<OrderStatusDTO>(@"
                    select CompanyOrderID, min(Completed) completed
                    from 
                    (select pj.CompanyOrderID, 
                    case when 
                    sum (pd.Quantity) - max(isnull (pjd.Quantity, pj.Quantity)) < 0 then 0
                    else 1
                    end Completed
                    from DeliveryNotes d
                    join Packages pk on pk.DeliveryNoteID = d.ID
                    join PackageDetails pd on pd.PackageID = pk.ID
                    left join PrinterJobDetails pjd  on pjd.ID = pd.PrinterJobDetailID
                    join PrinterJobs pj on pj.id = isnull (pd.PrinterJobID,pjd.PrinterJobID)
                    join CompanyOrders o on o.ID = pj.CompanyOrderID
                    where 
                    o.DeliveryStatusID <> @DeliveryStatusDelivered  
                    group by pj.CompanyOrderID, pjd.ID
                    ) list
                    group by CompanyOrderID
                ", DeliveryStatus.Delivered);

                foreach(var item in ordersList)
                {
                    UpdateOrderDeliveryStatus(item.CompanyOrderId, item.Completed);

                    if(item.Completed == 1)
                    {
                        // cerrar orden => status = completed
                        orderRepo.ChangeStatus(item.CompanyOrderId, OrderStatus.Completed);
                    }
                }
            }
        }

        private void UpdateOrderDeliveryStatus(int companyOrderId, int completed)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var order = ctx.CompanyOrders.Find(companyOrderId);
                if(order != null)
                {
                    order.DeliveryStatusID = completed == 1 ? DeliveryStatus.Delivered : DeliveryStatus.PartiallyShipped;
                    ctx.SaveChanges();
                }
                else
                {
                    throw new Exception(g["Order with ID {0} not found.", companyOrderId]);
                }
            }
        }
        public ImportReturnDTO ImportDeliveryNote(DeliveryNoteDTO deliveryNoteDTO)
        {
            if(!CheckOrderStatusForDeliveryNote(deliveryNoteDTO))
            {
                return new ImportReturnDTO { 
                    DeliveryNoteID = 0, 
                    CarrierID = 0, 
                    Success = false,
                    Message = g["One or more orders associated with the delivery note are not in a valid status for delivery."] };
            }

            if(deliveryNoteDTO.Carrier == null)
            {
                return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Carrier is required"] };
            }

            string trackinCodeRegex = "^[a-zA-Z0-9-_/.]*$";

            if(deliveryNoteDTO.DeliveryNote.TrackingCode.Length > 0)
            {
                if(deliveryNoteDTO.DeliveryNote.TrackingCode.Length < 5 || deliveryNoteDTO.DeliveryNote.TrackingCode.Length > 50)
                {
                    throw new Exception(g["Invalid tracking code format."], null);
                }
                else
                {
                    if(!System.Text.RegularExpressions.Regex.IsMatch(deliveryNoteDTO.DeliveryNote.TrackingCode, trackinCodeRegex))
                    {
                        throw new Exception(g["Invalid tracking code format."], null);
                    }
                }
            }

            // Avoid duplicate notes, import only if the DeliveryID is null 
            if(deliveryNoteDTO.DeliveryNote.DeliveryID == null)
            {
                using(var ctx = factory.GetInstance<PrintDB>())
                {
                    if(deliveryNoteDTO.DeliveryNote.Source == DeliveryNoteSource.Dinamo)
                    {
                        // Check factory
                        var factory = ctx.Locations.Find(deliveryNoteDTO.DeliveryNote.FactoryID);
                        if(factory == null)
                        {
                            return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Factory is not valid"] };
                        }

                        // Check vendor
                        var vendor = ctx.Companies.Find(deliveryNoteDTO.DeliveryNote.SendToCompanyID);
                        if(vendor == null)
                        {
                            return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Vendor is not valid"] };
                        }

                        // Check address
                        var address = ctx.Addresses.Find(deliveryNoteDTO.DeliveryNote.SendToAddressID);
                        if(address == null)
                        {
                            return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Address is not valid"] };
                        }
                    }

                    // Check delivery note number
                    if(deliveryNoteDTO.DeliveryNote.Number.Trim()=="")
                    {
                        return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Delivery note number is required"] };
                    }

                    // Check delivery date
                    if(deliveryNoteDTO.DeliveryNote.ShippingDate==null)
                    {
                        return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Shipping date is required"] };
                    }

                    //Import the carrier if not exists
                    var carrier = ctx.Find<Carrier>(deliveryNoteDTO.Carrier.CarrierID);

                    if(carrier == null)
                    {
                        deliveryNoteDTO.Carrier.ID = 0; // Reset the ID to ensure a new carrier is added    
                        carrier = ctx.Carriers.Add(deliveryNoteDTO.Carrier).Entity; // Add the new carrier if it does not exist  
                        ctx.SaveChanges(); // Save changes to ensure the carrier is added before associating it with the delivery note  
                    }
                
                    deliveryNoteDTO.DeliveryNote.ID = 0; // Ensure the ID is reset for a new delivery note  
                    deliveryNoteDTO.DeliveryNote.CarrierID = carrier.ID;

                    var note = ctx.DeliveryNotes.Add(deliveryNoteDTO.DeliveryNote);
                    ctx.SaveChanges();

                    foreach(var package in deliveryNoteDTO.Packages)
                    {
                        package.Package.ID = 0; // Reset the ID for each package to ensure new entries
                        package.Package.DeliveryNoteID = note.Entity.ID; // Associate the package with the new delivery note

                        ctx.Packages.Add(package.Package);
                        ctx.SaveChanges();

                        foreach(var packageDetail in package.Details)
                        {
                            packageDetail.ID = 0; // Reset the ID for each item to ensure new entries
                            packageDetail.PackageID = package.Package.ID; // Associate the item with the package
                            ctx.PackageDetails.Add(packageDetail);
                            ctx.SaveChanges();
                        }
                    }

                    UpdateOrdersDeliveryStatus();

                    if (note.Entity.TrackingCode != null && note.Entity.TrackingCode.Trim() != "")
                    {
                        CreateShip24Tracker(note.Entity.ID);
                    }

                    return new ImportReturnDTO { DeliveryNoteID = note.Entity.ID, CarrierID = carrier.ID, Success = true, Message= g["Delivery note loaded successfully"] };
                }
            }
            else
            {
                return new ImportReturnDTO { DeliveryNoteID = 0, CarrierID = 0, Success = false, Message = g["Delivery note already exists or could not be imported"] };
            }
        }

        public List<Order> GetOrdersByPrinterJobID(int printerjobID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var result = from pj in ctx.PrinterJobs
                             join co in ctx.CompanyOrders on pj.CompanyOrderID equals co.ID
                             where pj.ID == printerjobID
                             select new { co };

                return result.Select(r => r.co).ToList();
            }
        }

        public List<Order> GetOrdersByPrinterJobDetailID(int printerjobDetailID)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var result = from pjd in ctx.PrinterJobDetails
                    join pj in ctx.PrinterJobs on pjd.PrinterJobID equals pj.ID
                    join co in ctx.CompanyOrders on pj.CompanyOrderID equals co.ID
                    where pjd.ID == printerjobDetailID  
                    select new { co };

                return result.Select(r => r.co).ToList();
            }
        }

        public bool CheckOrderStatusForDeliveryNote(DeliveryNoteDTO deliveryNoteDTO)
        {
            bool deliveryAllowed = false;

            List<Order> orders = new List<Order>();  
            foreach(var package in deliveryNoteDTO.Packages)
            {
                foreach(var packageDetail in package.Details)
                {
                    if(packageDetail.PrinterJobID != null)
                    {
                        orders.AddRange(GetOrdersByPrinterJobID(packageDetail.PrinterJobID.Value));
                    }
                    else
                    {
                        if(packageDetail.PrinterJobDetailID != null)
                        {
                            orders.AddRange(GetOrdersByPrinterJobDetailID(packageDetail.PrinterJobDetailID.Value));
                        }
                    }
                }
            }

            foreach (var order in orders)
            {
                if( order.OrderStatus == OrderStatus.Printing || 
                    order.OrderStatus == OrderStatus.Completed)
                {
                    deliveryAllowed = true;
                }
                else
                {
                    deliveryAllowed = false;
                    break;
                }
            }   

            return deliveryAllowed;
        }

        public List<DeliveryNoteDetailsDTO> GetNotesForOrder(int OrderId)
        {
            using(var conn = connManager.OpenDB("MainDB"))
            {
                var deliveryDetailsDTO = conn.Select<DeliveryNoteDetailsDTO>(@"
                    select d.ID DeliveryNoteID, d.Number DeliveryNote, l.FactoryCode, l.name FactoryName,
                    c.name CarrierName, d.ShippingDate, d.TrackingCode, p.PackageNumber, 
                    a.ArticleCode, a.Description, pd.Size, pd.Colour, d.sendtocompanyid,
                    isnull (pjd.Quantity, pj.Quantity) quantity, pd.Quantity QuantitySent,
                    sc.Name SendToName,
                    ct.Name Country
                    from DeliveryNotes d
                    join Packages p on p.DeliveryNoteID = d.ID
                    join PackageDetails pd on pd.PackageID = p.ID
                    left join PrinterJobDetails pjd on pjd.ID = pd.PrinterJobDetailID
	                join PrinterJobs pj on pj.id = isnull (pd.PrinterJobID,pjd.PrinterJobID)
                    join CompanyOrders o on o.ID = pj.CompanyOrderID
                    join Locations l on l.ID = d.FactoryID
                    join Articles a on a.id = pj.ArticleID
                    join Carriers c on c.ID = d.CarrierID
                    join Companies sc on sc.id = o.SendToCompanyID
                    join Addresses ad on ad.id = o.SendToAddressID
                    join Countries ct on ct.id = ad.CountryID
                    where pj.CompanyOrderID = @OrderId
                    order by d.CreatedDate desc,  d.number, p.PackageNumber
                    ", OrderId);

                return deliveryDetailsDTO;
            }
        }

        private int CreateTheCarrierIfNotExists(DeliveryRowDTO row, PrintDB ctx, string user)
        {
            //Create the carrier if not exists
            var carrier = ctx.Carriers.Where(o => o.Name == row.Carrier && o.FactoryID == row.LocationID).FirstOrDefault();
            if(carrier == null)
            {
                Carrier newCarrier = new Carrier()
                {
                    Name = row.Carrier,
                    Description = row.Carrier,
                    FactoryID = row.LocationID,
                    CreatedBy = user,
                    UpdatedBy = user,
                    CreatedDate = DateTime.Now,
                    UpdatedDate = DateTime.Now
                };
                ctx.Carriers.Add(newCarrier);
                ctx.SaveChanges();
                return newCarrier.ID;   
            }
            else
            {
                return carrier.ID;
            }
        }

        public void ImportDeliveryFile(string user, string jsondata)
        {
            try
            {
                var deliveryinfo = JsonConvert.DeserializeObject<DeliveryFile>(jsondata);
                string previousRowNoteNumber =  deliveryinfo.rows.OrderBy(o => o.DeliveryNote).FirstOrDefault().DeliveryNote;
                var packageDetailsList = new List<PackageDetail>(); 
                var previousDeliveryNote = new DeliveryNote() { Number = "" };

                // process all rows of the file ordered by deliverynote
                foreach(var row in deliveryinfo.rows.OrderBy(o=>o.DeliveryNote))
                {
                    // Skip empty rows
                    if(row.OrderID == 0 || row.ArticleID == 0 || row.PrintJobId == 0 || row.Quantity == 0)
                        continue;

                    // Chcek mandatory fields
                    if(row.DeliveryNote == "" || row.Carrier == "" || row.ShippingDate == "")
                    {
                        throw new Exception(g["Field is required in order number : {0}", row.OrderNumber]); ;
                    }

                    // Check the checkcode
                    if(!CheckKeyFieldsOk(row))
                    {
                        throw new Exception(g["Checkcode mismatch"]);
                    }

                    using(var ctx = factory.GetInstance<PrintDB>())
                    {
                        // Search for the carrier and create it if not exists
                        int carrierId = CreateTheCarrierIfNotExists(row, ctx, user);

                        // find send to address from orders information
                        var order = ctx.CompanyOrders.Find(row.OrderID);
                        if(order == null)
                        {
                            throw new Exception(g["Order with ID {0} not found.", row.OrderID]);
                        }
                        if(order.SendToCompanyID != row.SendToCompanyID)
                        {
                            throw new Exception(g["Send to company ID mismatch for order ID {0}.", row.OrderID]);
                        }

                        // if note number changes import the note
                        if(previousRowNoteNumber != row.DeliveryNote.Trim())
                        {
                            SaveTheNote(previousDeliveryNote, packageDetailsList);

                            previousRowNoteNumber = row.DeliveryNote.Trim();
                            packageDetailsList.Clear();
                        }

                        // Add the package detail to the details list
                        packageDetailsList.Add(new PackageDetail()
                        {
                            ID = 0,
                            PackageID = 0,
                            PrinterJobID = row.PrintJobId,
                            PrinterJobDetailID = null,
                            ArticleID = row.ArticleID,
                            Quantity = row.Quantity,
                            Size = "",
                            Colour = "",
                            Description = row.ArticleName,
                            ArticleCode = row.ArticleCode,
                            ArticleUnitsID = null
                        });

                        if(previousDeliveryNote.Number != row.DeliveryNote)
                        {
                            previousDeliveryNote = new DeliveryNote()
                            {
                                ID = 0,
                                DeliveryID = null,
                                Number = row.DeliveryNote,
                                ShippingDate = DateTime.ParseExact(row.ShippingDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                                Carrier= new Carrier()
                                {
                                    ID = carrierId,
                                    CarrierID = carrierId,
                                    Name = row.Carrier,
                                    FactoryID = row.LocationID
                                },
                                CarrierID = carrierId,
                                FactoryID = row.LocationID,
                                SendToCompanyID = row.SendToCompanyID,
                                TrackingCode = row.TrackingNumber,
                                CreatedDate = DateTime.Now,
                                UpdatedDate = DateTime.Now,
                                CreatedBy = user,
                                UpdatedBy = user,
                                Status = DeliveryNoteStatus.Closed,
                                SendToAddressID = order.SendToAddressID,
                                Source = DeliveryNoteSource.ExcelFile
                            };
                        }
                    }
                }
                SaveTheNote(previousDeliveryNote, packageDetailsList);
            }

            catch(Exception ex)
            {
                throw new Exception(g["Error importing delivery file: "] + ex.Message, ex);
            }
        }

        private void SaveTheNote(DeliveryNote deliveryNote, List<PackageDetail> packageDetailsList)
        {
            // Create one package per note (don't have package information in the import file)
            var packages = new List<PackageDTO>()
            {
                new PackageDTO()
                {
                    Package = new Package()
                    {
                        ID = 0,
                        DeliveryNoteID = 0,
                        PackageNumber = 1,
                    },
                    Details = packageDetailsList
                }
            };

            var newNote = new DeliveryNoteDTO()
            {
                DeliveryNote = deliveryNote,
                Packages = packages,
                Carrier = deliveryNote.Carrier
            };

            var importresult = ImportDeliveryNote(newNote);

            if (importresult.Success == false)
            {
                throw new Exception(g["Error importing delivery note {0}: {1}", deliveryNote.Number, importresult.Message]);
            }
        }

        private bool CheckKeyFieldsOk(DeliveryRowDTO row)
        {
            string keys =
            (row.LocationID.ToString() ?? String.Empty) +
            (row.ArticleID.ToString() ?? String.Empty) +
            (row.OrderID.ToString() ?? String.Empty) +
            (row.PrintJobId.ToString() ?? String.Empty) +
            row.SendToCompanyID.ToString() ?? String.Empty;

            using(System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var checkCode = BitConverter.ToString(
                  md5.ComputeHash(Encoding.UTF8.GetBytes(keys))
                ).Replace("-", String.Empty);

                return (checkCode == row.CheckCode);
            }
        }

        public Ship24TrackingInfo GetTrackingInfoParametrers (int deliveryNoteId)
        {
            using(var ctx = factory.GetInstance<PrintDB>())
            {
                var deliveryNote = ctx.DeliveryNotes.Find(deliveryNoteId);

                var packages = ctx.Packages.Where(p => p.DeliveryNoteID == deliveryNoteId);

                var orderData =
                    from p in ctx.Packages
                    join pd in ctx.PackageDetails on p.ID equals pd.PackageID
                    join pjd in ctx.PrinterJobDetails
                        on pd.PrinterJobDetailID equals pjd.ID into pjdGroup
                    from pjd in pjdGroup.DefaultIfEmpty() // LEFT JOIN
                    join pj in ctx.PrinterJobs
                        on (pd.PrinterJobID ?? pjd.PrinterJobID) equals pj.ID
                    join companyorder in ctx.CompanyOrders
                        on pj.CompanyOrderID equals companyorder.ID
                    where p.DeliveryNoteID == deliveryNoteId
                    select new
                    {
                        companyorder.OrderNumber,
                        companyorder.SendToAddressID
                    };

                if(deliveryNote == null)
                    return null;

                var destinationAdress = ctx.Addresses.Find(orderData.Distinct().Select(p => p.SendToAddressID).FirstOrDefault()); 

                if(destinationAdress == null)
                    return null;

                var destinationCountry = ctx.Countries.Find(destinationAdress.CountryID);

                var carrier = ctx.Carriers.Find(deliveryNote.CarrierID);

                var location = ctx.Locations.Find(deliveryNote.FactoryID);

                var originCountry = ctx.Countries.Find(location.CountryID);

                var orders = orderData.Distinct().Select(p=>p.OrderNumber).ToList();

                return new Ship24TrackingInfo()
                {
                    trackingNumber = deliveryNote.TrackingCode,
                    shipmentReference = deliveryNote.Number,
                    clientTrackerId = deliveryNoteId.ToString(),
                    originCountryCode = originCountry !=null ? originCountry.Alpha2 : "",
                    destinationCountryCode = destinationCountry != null ? destinationCountry.Alpha2 : "",
                    destinationPostCode = destinationAdress != null ? destinationAdress.ZipCode : "",
                    shippingDate = deliveryNote.ShippingDate,
                    courierCode = carrier != null ? new string[] { carrier.Name } : new string[] { "" },    
                    courierName = carrier != null ? carrier.Description : "",
                    trackingUrl = carrier.TrackingURL,
                    orderNumber = String.Join( ", ", orders),
                    title = $"{location.Name} / {deliveryNote.Number} / {carrier.Name}",
                    recipient = new Recipient()
                    {
                        email = destinationAdress.Email1 != null ? destinationAdress.Email1 : "",
                        name = destinationAdress.BusinessName1 != null ? destinationAdress.BusinessName1 : ""
                    },
                    settings = new Settings()
                    {
                        restrictTrackingToCourierCode = false
                    }
                };
            }
        }

        public string CreateShip24Tracker(int deliveryNoteId)
        {
            return ship24ClientService.CreateTracker(GetTrackingInfoParametrers(deliveryNoteId));  
        }

        public string GetShip24TrackingInfo(int deliveryNoteId)
        {
            return ship24ClientService.CreateTrackerAndGetTrackingResults(GetTrackingInfoParametrers(deliveryNoteId));
        }

        public string GetDeliveryInfo(string ordernumber)
        {
            using(var conn = connManager.OpenDB("MainDB"))
            {
                var order = conn.SelectOne<Order>(@"
                    select * from CompanyOrders where OrderNumber = @ordernumber
                    and orderstatus in (3,50,6)
                    ", ordernumber);

                if(order == null)
                    throw new Exception(g["Order with number {0} can't be delivered", ordernumber]);

                var packageDetail = conn.Select<PackageDetail>(@"
                    select 
                    0 ID, 0 PackageID, a.id ArticleID, null ArticleUnitsID,
                    j.ID PrinterjobID, null PrinterJobDetailID, a.ArticleCode,
                    a.Description, null Size, null Colour, j.Quantity, 0.0 Price
                    from CompanyOrders o
                    join PrinterJobs j on j.CompanyOrderID = o.ID
                    join Articles a on a.ID = j.ArticleID
                    where o.ordernumber = @ordernumber  
                    and orderstatus in (3,50)
                    order by ArticleCode
                    ", ordernumber);

                DeliveryNoteDTO deliveryNoteDTO = new DeliveryNoteDTO()
                {
                    DeliveryNote = new DeliveryNote()
                    {
                        ID = 0,
                        DeliveryID = null,
                        Number = "",
                        ShippingDate = DateTime.Now,
                        CarrierID = 0,
                        FactoryID = order.LocationID ?? 0,
                        SendToCompanyID = order.SendToCompanyID,
                        TrackingCode = "",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        CreatedBy = userData.Principal.Identity.Name,
                        UpdatedBy = userData.Principal.Identity.Name,
                        Status = DeliveryNoteStatus.Closed,
                        SendToAddressID = order.SendToAddressID,
                        Source = DeliveryNoteSource.Dinamo
                    },

                    Packages = new List<PackageDTO>()
                    {
                        new PackageDTO()
                        {
                            Package = new Package()
                            {
                                ID = 0,
                                DeliveryNoteID = 0,
                                PackageNumber = 1,
                            },
                            Details = packageDetail
                        }
                    },

                    Carrier = new Carrier()
                    {
                        ID = 0,
                        FactoryID = order.LocationID ?? 0,
                        Name = "Carrier_code",
                        Description = "Carrier_Description",
                        CreatedDate = DateTime.Now,
                        UpdatedDate = DateTime.Now,
                        CreatedBy = userData.Principal.Identity.Name,
                        UpdatedBy = userData.Principal.Identity.Name,
                    },
                };

                return JsonConvert.SerializeObject(deliveryNoteDTO);
            }

        }

        private class DeliveryFile
        {
            public DeliveryRowDTO[] rows { get; set; }
        }

        private class DeliveryRowDTO
        {
            public string ShippingDate { get; set; }
            public string DeliveryNote { get; set; }
            public string Carrier { get; set; }
            public string TrackingNumber { get; set; }
            public int Quantity { get; set; }
            public string ArticleCode { get; set; }
            public string ArticleName { get; set; }
            public string CompanyName { get; set; }
            public int OrderID { get; set; }
            public string OrderNumber { get; set; }
            public string MDOrderNumber { get; set; }
            public string OrderDate { get; set; }
            public string OrderStatusText { get; set; }
            public int LocationID { get; set; }
            public string FactoryCode { get; set; }
            public string LocationName { get; set; }
            public int SendToCompanyID { get; set; }
            public string SendTo { get; set; }
            public int ArticleID { get; set; }
            public int PrintJobId { get; set; }
            public string SageReference { get; set; }
            public string CheckCode { get; set; } 
        }

    }

}