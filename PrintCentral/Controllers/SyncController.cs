using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.PrintCentral;
using Services.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using WebLink.Contracts;
using WebLink.Contracts.Models;

namespace PrintCentral.Controllers
{
    [Authorize]
    public class SyncController : Controller
    {
        private readonly IFactory factory;
        private readonly ILogSection log;
        private IEventQueue events;
        private IOrderActionsService actionsService;

        public SyncController(IFactory factory, ILogService log, IEventQueue events, IOrderActionsService actionsService)
        {
            this.factory = factory;
            this.log = log.GetSection("DuplicatedEPCNotification");
            this.events = events;
            this.actionsService = actionsService;
        }

        [HttpPost, Route("/api/sync/labelsrfiddata")]
        public async Task<SyncResult> SyncLabelsRFIDData([FromBody] EncodedLabelsSync request)
        {
            return await Insertdata(request, SyncState.Pending);
        }


        [HttpPost, Route("/api/sync/encodedlabels")]
        public async Task<SyncResult> SyncEncodedLabels([FromBody] EncodedLabelsSync request)
        {
            try
            {
                var location = new Location();
                var epcList = new List<DuplicatedEPC>();
                var newEPC = new List<EncodedLabelDTO>();
                using(var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromMinutes(10), TransactionScopeAsyncFlowOption.Enabled))
                {
                    using(var ctx = factory.GetInstance<PrintDB>())
                    {
                        location = ctx.Locations.FirstOrDefault(x => x.ID == request.FactoryID);
                        // log.LogMessage($" Try send SyncEncodedLabels Factory : {request.FactoryID}, Data: {JsonConvert.SerializeObject(request.Data)}");
                        foreach(var l in request.Data)
                        {

                            var existingEncodedLabel = await ctx.EncodedLabels.Where(lbl => lbl.EPC == l.EPC).AsNoTracking().FirstOrDefaultAsync();
                            if(existingEncodedLabel != null)
                            {
                                if(l.TID != null)
                                {
                                    int idx = l.TID.IndexOf(",");
                                    if(idx > 0)
                                        l.TID = l.TID.Substring(0, idx);
                                }
                                //log.LogMessage($"This is  EPC { l.EPC } - TID pc { recordEncodedLabel.TID } TID pl { l.TID } on Factory: { location.Name }");
                                if(existingEncodedLabel.TID is null || !existingEncodedLabel.TID.StartsWith("E280")) 
                                {
                                    //EKOI EPC Sync Customization
                                    if(l.CompanyID == 2999 && existingEncodedLabel.SyncState == SyncState.Completed)
                                    {
                                        //IF The tag was already synced, only update TID, RSSI, Date and DeviceID
                                        existingEncodedLabel.TID = l.TID;
                                        existingEncodedLabel.RSSI = l.RSSI;
                                        existingEncodedLabel.Date = l.Date;
                                        if(existingEncodedLabel.DeviceID != l.DeviceID)
                                        {
                                            existingEncodedLabel.DeviceID = l.DeviceID;
                                        }
                                    }
                                    else
                                    {
                                        existingEncodedLabel.TID = l.TID;
                                        existingEncodedLabel.RSSI = l.RSSI;
                                        existingEncodedLabel.SyncState = SyncState.Completed;
                                        existingEncodedLabel.Date = l.Date;
                                        if(existingEncodedLabel.DeviceID != l.DeviceID)
                                        {
                                            existingEncodedLabel.DeviceID = l.DeviceID;
                                        }
                                    }
                                    ctx.Update<EncodedLabel>(existingEncodedLabel);
                                    await ctx.SaveChangesAsync();
                                }
                                else
                                {
                                    if(existingEncodedLabel.TID == l.TID)
                                    {
                                        log.LogMessage($"This is an EPC sync retry: EPC {l.EPC} - TID {l.TID} on Factory: {location.Name}");
                                    }
                                    else
                                    {
                                        epcList.Add(new DuplicatedEPC()
                                        {
                                            EPC = l.EPC,
                                            OrderID = l.OrderID,
                                            DeviceId = l.DeviceID
                                        });
                                    }
                                }
                            }
                            else
                            {
                                newEPC.Add(l);
                            }

                        }

                    }
                    tx.Complete();



                }
                ValidateEPC(epcList, location);
                if(newEPC.Count() > 0)
                {
                    //log.LogMessage($"INSERT: {newEPC.Count()} EPCs");
                    return await Insertdata(new EncodedLabelsSync { FactoryID = request.FactoryID, Data = newEPC }, SyncState.Completed);
                }
                else
                {
                    return new SyncResult() { Success = true };
                }


            }
            catch(Exception ex)
            {
                log.LogException(ex);
                log.LogMessage($"SyncEncodedLabels Data: {JsonConvert.SerializeObject(request.Data)}");
                return new SyncResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
        private async Task<SyncResult> Insertdata(EncodedLabelsSync request, SyncState state)
        {
            var firstEpc = request.Data.FirstOrDefault();
            var lastEpc = request.Data.LastOrDefault();
            var location = new Location();
            var epcList = new List<DuplicatedEPC>();
            var tdiFormatedFound = false;




            try
            {
                using(var tx = new TransactionScope(TransactionScopeOption.RequiresNew, TimeSpan.FromMinutes(10), TransactionScopeAsyncFlowOption.Enabled))
                {
                    using(var ctx = factory.GetInstance<PrintDB>())
                    {
                        location = await ctx.Locations.FirstOrDefaultAsync(x => x.ID == request.FactoryID);

                        log.LogMessage($@"SyncEncodedLabels Insertdata [{request.Data.Count}] EPCs FactoryID [{request.FactoryID}] OrderID [{firstEpc.OrderID}] 
START CHECK
FirstEpc.Serial [{(firstEpc != null ? firstEpc.Serial : 0)}]
FirstEpc.EPC [{(firstEpc != null ? firstEpc.EPC : string.Empty)}]
LastEpc.Serial [{(lastEpc != null ? lastEpc.Serial : 0)}]
LastEpc.EPC [{(lastEpc != null ? lastEpc.EPC : string.Empty)}]
");



                        foreach(var l in request.Data)
                        {
                            var existing = await ctx.EncodedLabels.Where(lbl => lbl.EPC == l.EPC && lbl.CompanyID == l.CompanyID).AsNoTracking().FirstOrDefaultAsync();
                            if(existing == null)
                            {
                                //log.LogMessage($" not exist EPC: {l.EPC} try insert");
                                if(l.TID != null)
                                {
                                    int idx = l.TID.IndexOf(",");

                                    if(tdiFormatedFound == false && idx > 0)
                                    {
                                        log.LogMessage($"TID formateado encontrado {l.TID}");
                                        tdiFormatedFound = true;
                                    }

                                    if(idx > 0)
                                        l.TID = l.TID.Substring(0, idx);


                                }
                                await ctx.EncodedLabels.AddAsync(new EncodedLabel()
                                {
                                    CompanyID = l.CompanyID,
                                    ProjectID = l.ProjectID,
                                    OrderID = l.OrderID,
                                    DeviceID = l.DeviceID,
                                    ArticleCode = l.ArticleCode,
                                    Barcode = l.Barcode,
                                    Serial = l.Serial,
                                    TID = l.TID,
                                    EPC = l.EPC,
                                    AccessPassword = l.AccessPassword,
                                    KillPassword = l.KillPassword,
                                    RSSI = l.RSSI,
                                    Date = l.Date,
                                    ProductionLocationID = request.FactoryID,
                                    ProductionType = 2,
                                    Success = true,
                                    SyncState = state,
                                    InlayConfigID = l.InlayConfigID,
                                    InlayConfigDescription = l.InlayConfigDescription

                                });
                            }
                            else if(existing.TID == l.TID)
                            {
                                log.LogMessage($"This is an EPC sync retry: EPC {l.EPC} - TID {l.TID} on Factory: {location.Name}");
                            }
                            else
                            {
                                epcList.Add(new DuplicatedEPC()
                                {
                                    EPC = l.EPC,
                                    OrderID = l.OrderID,
                                    DeviceId = l.DeviceID
                                });
                            }
                        }
                        await ctx.SaveChangesAsync();
                        tx.Complete();
                    }
                }

                log.LogMessage($"Terminada Verificacion de [{request.Data.Count}] EPCs para la fábrica  [{request.FactoryID}] OrderID [{firstEpc.OrderID}]");

                ValidateEPC(epcList, location);
                return new SyncResult() { Success = true };

            }

            catch(Exception ex)
            {

                log.LogException($@"SyncEncodedLabels Insertdata FactoryID [{request.FactoryID}] FactoryName [{location.Name}] 
FAIL CHECK
FirstEpc.Serial [{(firstEpc != null ? firstEpc.Serial : 0)}]
FirstEpc.EPC [{(firstEpc != null ? firstEpc.EPC : string.Empty)}]
LastEpc.Serial [{(lastEpc != null ? lastEpc.Serial : 0)}]
LastEpc.EPC [{(lastEpc != null ? lastEpc.EPC : string.Empty)}]
", ex);
                return new SyncResult() { Success = false, ErrorMessage = ex.Message };
            }
        }
        private void ValidateEPC(List<DuplicatedEPC> epcList, Location location)
        {
            if(epcList.Count < 1)
                return;

            var groupedData = epcList.GroupBy(x => x.OrderID).ToList();
            foreach(var d in groupedData)
            {
                log.LogMessage($"Create new SendDuplicatedEPC Event for order {d.Key} on Factory: {location.Name}");
                var duplicatedEPC = d.Select(x => "<tr><td>" + x.EPC + "</td></tr>").ToList();
                var deviceId = d.FirstOrDefault().DeviceId;

                events.Send(new Service.Contracts.PrintLocal.DuplicatedEPCEvent(location.Name, d.Key, duplicatedEPC));
                actionsService.OrderWithDuplicatedEPC(d.Key);
                actionsService.StopOrder(d.Key);
            }
        }
        //[HttpPost, Route("/api/sync/sendduplicatedepcemail")]
        //public bool SendEmail([FromBody] EPCEntity model)
        //{
        //    return clsService.SendDuplicatedEPCEMail(model.epcList);
        //}
    }

    public class DuplicatedEPC
    {
        public string EPC;
        public int OrderID;
        public int DeviceId;
    }
}