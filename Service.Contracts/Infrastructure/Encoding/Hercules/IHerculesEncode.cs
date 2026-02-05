using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Service.Contracts.Database;

namespace Service.Contracts
{
    //======================================================================================
    //Provide methods to insert data into Hercules DB. This service is used by Print Local
    //after Print Web creates jobs packages, Also it is implemented by the UploadAccess tools
    //to load access data, proccess it then fill Hercules DB
    //======================================================================================
	public interface IHerculesEncode
	{
        //Add main data on Hercules DB with a JobId
        int AddPrint(IDBX conn, Print print);
        //Add barcode data by the specified print and jbId
        int AddRFIDHeader(IDBX conn, PrintHeader header);
        //Add detail data by a header and printId
        int AddRFIDDetail(IDBX conn, PrintHeaderDetail detail);
        //Delets job data on Hercules DB
        void DeletePrintData(IDBX conn, int printId);
        //get print id for an existent order
        int GetPrintId(IDBX conn, string order);
        //get barcode header list
        Dictionary<int, string> GetBarcodeHeader(IDBX conn, int printId);
    }
}
