using Service.Contracts;
using Service.Contracts.Database;
using Service.Contracts.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Platform.PoolFiles.BFC
{
    public class BFCPoolFileHandler : IPoolFileHandler
    {

        private readonly IConnectionManager manager;
        private readonly IFileStoreManager storeManager;
        private readonly ITempFileService tempFileService;
        private readonly IMappingRepository mappingRepo;
        private readonly IDataImportService dataImportService;
        private readonly IOrderPoolRepository poolRepo;
        private IRemoteFileStore tempStore;
        private DocumentImportConfiguration mapping;

        private static string DATEFORMAT = "yyyy-MM-dd";

        //public string OriginalFileName { get; set; }

        public BFCPoolFileHandler(
            IConnectionManager manager
            , IFileStoreManager storeManager
            , ITempFileService tempFileService
            , IMappingRepository mappingRepo
            , IDataImportService dataImportService
            , IOrderEmailService orderEmailService
            , IOrderPoolRepository poolRepo

            )
        {

            this.manager = manager;
            this.storeManager = storeManager;
            this.tempFileService = tempFileService;
            this.mappingRepo = mappingRepo;
            this.dataImportService = dataImportService;
            this.poolRepo = poolRepo;
        }

        public async Task UploadAsync(IProject project, Stream stream, Action<int, IList<IOrderPool>> result = null)
        {

            // Parse Data with Document Service
            var data = await ParseData(project, stream);
            var year = DateTime.Now.Year;

            var culture = CultureInfo.InvariantCulture;// new CultureInfo(mapping.Input.SourceCulture);
            DateTime date;


            var receivedOrders = new List<IOrderPool>();

            using(var conn = manager.OpenDB("MainDB"))
            {

                // slowly, insert one by one
                data.ForEach(row =>
                {
                    var targetRow = new Dictionary<string, object>
                    {
                        { "ProjectID", project.ID },
                        { "Year", year}
                    };

                    // create dictionary using target col like keys
                    for(var i = 0; i < data.Cols.Count; i++)
                    {
                        var colValue = row.Data.GetValueOrDefault(data.Cols[i].InputColumn, string.Empty).ToString();

                        if(data.Cols[i].TargetColumn == "CreationDate" || data.Cols[i].TargetColumn == "ExpectedProductionDate")
                        {
                            DateTime dateValue;

                            if(DateTime.TryParseExact(colValue.Substring(0, 10), mapping.Output.Mappings[i].DateFormat, culture, DateTimeStyles.None, out date))
                            {
                                dateValue = date;
                            }
                            else
                            {
                                dateValue = DateTime.Now;
                            }

                            targetRow.Add(data.Cols[i].TargetColumn, dateValue);

                        }
                        else
                        {

                            targetRow.Add(data.Cols[i].TargetColumn, colValue);
                        }


                    }

                    // TODO: check if already exist to avoid duplicates

                    IOrderPool receivedOrder = MappingObject(targetRow);

                    var found = poolRepo.CheckIfExist(receivedOrder);

                    if(found != null)
                    {
                        receivedOrder.ID = found.ID;
                        poolRepo.Update(receivedOrder);
                    }
                    else
                    {

                        receivedOrder = poolRepo.Insert(receivedOrder);
                    }

                    receivedOrders.Add(receivedOrder);


                });
            }



            result(project.ID, receivedOrders);

        }

        public async Task<ImportedData> ParseData(IProject project, Stream stream)
        {
            tempStore = storeManager.OpenStore("TempStore");// looking the available store names inner appsettings files
            string fileName = tempFileService.GetTempFileName("orderpool_bfc.xlsx", false); // only parse excel files, use a mapping with fixed mask

            var dstfile = tempStore.CreateFile(fileName);
            dstfile.SetContent(stream);

            mapping = mappingRepo.GetDocumentImportConfiguration(fileName, project.ID, dstfile);
            var job = await dataImportService.RegisterUserJob(fileName, project.ID, DocumentSource.Validation, true);
            await dataImportService.StartUserJob(fileName, mapping);

            DocumentImportProgress process = new DocumentImportProgress();

            while(process.ReadProgress < 100)
            {
                process = dataImportService.GetJobProgress(fileName);
                await Task.Delay(250);
            }

            var jobResult = await dataImportService.GetJobResult(fileName);

            if(!jobResult.Success)
                throw new Exception($"Cannot parse the upload file inner BFCPoolFileHandler");

            return await dataImportService.GetImportedDataAsync(fileName);

        }

        private OrderPool MappingObject(Dictionary<string, object> data)
        {

            //return _ObjectFromDictionary<OrderPool>(data);
            return ToObject<OrderPool>(data);
            //return new OrderPool() { 
            //    ProjectID = int.Parse(data["ProjectID"].ToString()),
            //    OrderNumber = data["OrderNumber"].ToString(),
            //    ProviderCode2 = data["ProviderCode2"].ToString(),
            //    ProviderName2 = data["ProviderName2"].ToString(),
            //    CreationDate = (DateTime)data["CreationDate"],
            //    ExpectedProductionDate = (DateTime)data["ExpectedProductionDate"],
            //    ArticleCode = data["ArticleCode"].ToString(),
            //};

        }

        private T _ObjectFromDictionary<T>(IDictionary<string, object> dict)
    where T : class
        {
            Type type = typeof(T);
            T instance = (T)Activator.CreateInstance(type);
            foreach(var item in dict)
            {
                type.GetProperty(item.Key).SetValue(instance, item.Value, null);
            }
            return instance;
        }

        public T ToObject<T>(IDictionary<string, object> source)
            where T : class, new()
        {
            var someObject = new T();
            var someObjectType = someObject.GetType();
            List<Type> numericTypes = new List<Type>() { typeof(int), typeof(short), typeof(long), typeof(Int16), typeof(Int32), typeof(Int64), typeof(float), typeof(decimal) };
            List<Type> dateTypes = new List<Type>() { typeof(DateTime), typeof(DateTime?) };


            foreach(var item in source)
            {
                var key = char.ToUpper(item.Key[0]) + item.Key.Substring(1);
                var targetProperty = someObjectType.GetProperty(key);

                //edited this line
                if(targetProperty.PropertyType == item.Value.GetType())
                {
                    targetProperty.SetValue(someObject, item.Value);
                }
               
                else if (numericTypes.Contains(targetProperty.PropertyType) && item.Value.GetType() == typeof(string))
                {
                    MethodInfo tryParseMethod = targetProperty.PropertyType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), targetProperty.PropertyType.MakeByRefType() }, null);

                    if(tryParseMethod != null)
                    {
                        var parameters = new[] { item.Value, null };
                        var success = (bool)tryParseMethod.Invoke(null, parameters);
                        if(success)
                        {
                            targetProperty.SetValue(someObject, parameters[1]);
                        }
                    }
                }
                else if(dateTypes.Contains(targetProperty.PropertyType) && item.Value != null)
                {


                    // both are "date type" but different types
                    if(dateTypes.Contains(item.Value.GetType()))
                    {
                        targetProperty.SetValue(someObject, item.Value);
                    }

                    // parse strings to date with the host format
                    if (item.Value.GetType() == typeof(string))
                    {
                        // this case is only for datetypes tryparsers is a wrong option to search TryParseMethod
                        var parseMethod = targetProperty.PropertyType.GetMethod("TryParse",
                            BindingFlags.Public | BindingFlags.Static, null,
                            new[] { typeof(string), targetProperty.PropertyType.MakeByRefType(), typeof(CultureInfo), typeof(DateTimeStyles).MakeByRefType() }, null);

                        if(parseMethod != null)
                        {
                            var parameters = new[] { item.Value, null };
                            var success = (bool)parseMethod.Invoke(null, parameters);
                            if(success)
                            {
                                targetProperty.SetValue(someObject, parameters[1]);
                            }

                        }
                    }

                }
                else
                {
                    
                    throw new Exception($"Unknow types- Target: [{targetProperty.PropertyType}], Source: [{item.Value.GetType()}]");
                    
                }
            }

            return someObject;
        }

        public Task InsertListAsync(IProject project, List<OrderPool> orderPools, Action<int, IList<IOrderPool>> result = null)
        {
            throw new NotImplementedException();
        }
    }
}
