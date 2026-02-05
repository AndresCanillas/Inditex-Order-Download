using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.Contracts.Infrastructure.Encoding.Tempe
{

    public partial class AllocateEpcsTempe
    {
        public List<AllocatedEpc> GetOrAllocate(
            DetailDto detailDto,                // contiene Quantity total
            Func<int, AllocateEpcsRequest> allocateEpcsBuild = null,
            Func<int, PreEncodeRequest> preEncodeBuild = null) // delegate para construir request dinámico
        {

            // 1. Intentamos extraer y borrar los EPCs disponibles
            var freeEpcs = repo.TakeAndDeleteEpcs(detailDto.OrderID, detailDto.DetailID, detailDto.Quantity);
            if(freeEpcs.Count == detailDto.Quantity)   // notamos si siempre pedimos menos, se adapta
            {

                return freeEpcs;
            }

            // 2. Calcular faltantes
            int missingEpcs = detailDto.Quantity - freeEpcs.Count;
            bool isFirst = !IsFirst(preEncodeBuild != null, detailDto.OrderID, detailDto.DetailID);
            int amountRequest = isFirst
                ? detailDto.Quantity                 // la totalidad del detalle (ej. 9000)
                : ((missingEpcs + 99) / 100) * 100;        // múltiplo 100

            // 3. Pedir al API
            var rfidRequestId = 0;
            if(allocateEpcsBuild != null)
            {
                var allocateEpcResps = epcService.AllocateEpcs(allocateEpcsBuild(amountRequest));
                if(allocateEpcResps == null || !allocateEpcResps.Any())
                    throw new Exception($"Error while allocating epcs for order {detailDto.OrderNumber}, " +
                        $"Model/Quality/Color/Size: {detailDto.Model}/{detailDto.Quality}/{detailDto.Color}/{detailDto.Size}");

                rfidRequestId = allocateEpcResps.First().RfidRequestId;
            }
            else if(preEncodeBuild != null)
            {
                var preEncodeResp = epcService.PreEncode(preEncodeBuild(amountRequest));
                if(preEncodeResp == null)
                    throw new Exception($"Error while allocating epcs for order {detailDto.OrderNumber}");

                rfidRequestId = preEncodeResp.RfidRequestId;
            }
            else
                throw new ArgumentException("At least one delegate (allocateEpcsBuild o preEncodeBuild) must be provided.");

            var newEpcs = ReadAllocatedEpcs(   // Usa el método ya existente
                   detailDto.OrderID, detailDto.DetailID,
                   rfidRequestId,
                   amountRequest);

            // 4. Leer de la API, guardar en tabla

            repo.SaveEpcs(newEpcs);


            // 5. Volver a tomar (y borrar) justo los que faltan
            var secondary = repo.TakeAndDeleteEpcs(
                detailDto.OrderID,
                detailDto.DetailID,
                missingEpcs);

            MarkAsUsed(preEncodeBuild != null, detailDto.OrderID, detailDto.DetailID);

            // 6. Combinar y devolver
            return freeEpcs
                   .Concat(secondary)
                   .ToList();

        }


        private void MarkAsUsed(bool isPreencoded, int orderID, int detailID)
        {
            if(isPreencoded)
            {
                repo.MarkAsUsedPreencoding(orderID, detailID);
                return;
            }
            repo.MarkAsUsed(orderID, detailID);
        }
        private bool IsFirst(bool isPreencoded, int orderID, int detailID)
        {
            if(isPreencoded)
            {
                return repo.IsFirstTimePreencoding(orderID, detailID);
            }
            return repo.IsFirstTime(orderID, detailID);
        }

        private List<AllocatedEpc> ReadAllocatedEpcs(int orderID, int detailID, int rfidRequestId, int quantity)
        {
            int read = 0;
            var allocatedEpcs = new List<AllocatedEpc>();

            do
            {
                var allocatedEpcResps = epcService.GetEpcs(new GetEpcsRequest()
                {
                    RfidRequestId = rfidRequestId,
                    Offset = read,
                    Limit = 1000
                });

                if(allocatedEpcs == null)
                    throw new Exception($"Error while reading EPCs from Epc Service");

                allocatedEpcs.AddRange(allocatedEpcResps.Results.Select(row => new AllocatedEpc()
                {
                    Epc = row.EpcHex,
                    OrderID = orderID,
                    DetailID = detailID,
                    UserMemory = row.UserMemoryHex,
                    KillPassword = row.KillPasswordHex,
                    AccessPassword = row.AccessPasswordHex
                }));

                read += allocatedEpcResps.Results.Count;
            } while(read < quantity);

            return allocatedEpcs;
        }
    }
    public class DetailDto : OrderDetail
    {
        public int BrandId { get; set; }
        public int ProductType { get; set; }
        public string OrderNumber { get; set; }
    }
}
