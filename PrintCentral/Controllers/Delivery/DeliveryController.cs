using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Service.Contracts;
using System;
using WebLink.Contracts.Models.Delivery;
using WebLink.Contracts.Models.Delivery.DTO;


namespace PrintCentral.Controllers.Delivery
{
    [Authorize]
    public class DeliveryController : Controller
    {
        private readonly IDeliveryRepository repo;
        private readonly ILocalizationService g;

        public DeliveryController(IDeliveryRepository repo, ILocalizationService g)
        {
            this.repo = repo;
            this.g = g;
        }

        [HttpPost, Route("/delivery/importdeliverynote")]
        public OperationResult ImportDeliveryNote([FromBody] string deliveryNoteJson)
        {
            if(deliveryNoteJson == null)
            {
                return new OperationResult(false, g["Parameter deliveryNoteJson cannot be null"]);
            }

            try
            {
                var deliveryNote = JsonConvert.DeserializeObject<DeliveryNoteDTO>(deliveryNoteJson);
                deliveryNote.DeliveryNote.Source = DeliveryNoteSource.PrintLocal;
                ImportReturnDTO importReturnDTO = repo.ImportDeliveryNote(deliveryNote);

                if(importReturnDTO == null)
                {
                    return new OperationResult(false, g["Delivery note already exists or could not be imported"]);
                }
                else
                {
                    return new OperationResult(true, g["Delivery note loaded successfully"], JsonConvert.SerializeObject(importReturnDTO));
                }
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        [HttpPost, Route("/delivery/importdeliverynotes")]
        public OperationResult ImportDeliveryNote([FromBody] DeliveryNoteDTO deliveryNote)
        {
            if(deliveryNote == null)
            {
                return new OperationResult(false, g["Parameter deliveryNoteJson cannot be null"]);
            }

            try
            {
                deliveryNote.DeliveryNote.Source = DeliveryNoteSource.Dinamo;
                ImportReturnDTO importReturnDTO = repo.ImportDeliveryNote(deliveryNote);

                if(!importReturnDTO.Success)
                {
                    return new OperationResult(false, importReturnDTO.Message);
                }
                else
                {
                    return new OperationResult(true, importReturnDTO.Message, JsonConvert.SerializeObject(importReturnDTO));
                }
            }
            catch(Exception ex)
            {
                return new OperationResult(false, ex.Message);
            }
        }

        [HttpPost, Route("/delivery/getnotesfororder")]
        public OperationResult GetNotesForOrder([FromBody] int OrderId)
        {
            return new OperationResult(true, null, repo.GetNotesForOrder(OrderId));  
        }

        [HttpPost, Route("/delivery/gettrackinginfo")]
        public OperationResult GetTrackingInfo([FromBody] int DeliveryNoteId)
        {
            var trackingInfo = repo.GetShip24TrackingInfo(DeliveryNoteId);

            if (trackingInfo == null)
            {
                return new OperationResult(false, g["Tracking info not found"]);
            }
            else
            {
                return new OperationResult(true, null, trackingInfo);
            }
        }


        [HttpPost, Route("/delivery/getdeliveryinfo")]
        public OperationResult GetDeliveryInfo([FromBody] string ordernumber)
        {
            try
            {
                return new OperationResult(true, null, repo.GetDeliveryInfo(ordernumber));
            }
            catch (Exception ex)
            {
                return new OperationResult(false, ex.Message, null);
            }
            
        }
    }
}
