using Newtonsoft.Json;
using Service.Contracts;
using Service.Contracts.Database;
using Services.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using WebLink.Contracts.Models;

namespace WebLink.Contracts.Services
{
    public class TempeOrderXmlHandler : ITempeOrderXmlHandler
    {

        private const string INDITEX_URL = "https://api.inditex.com/tcorelab-provider/api/v1/product/order/validation";
        private const string INDITEX_APIKEY = "AZK4YS601yIXu6n5WPovWUdwk2xNmB9L";

        private static readonly HttpClient client = new HttpClient();
        private readonly IConnectionManager _conManager;
        private readonly ILogService _log;
        private readonly IFileStoreManager _storeManager;
        private readonly IRemoteFileStore _tempStore;
        private readonly IFactory _factory;
        private readonly string _connStr;
        private string _ColorsCatalogName = "";
        private ITempFileService temp;
        private IAppConfig _config;

        public TempeOrderXmlHandler(ILogService log,
                                       IFileStoreManager storeManager,
                                       IConnectionManager conManager,
                                       IConnectionManager connectionManager,
                                       IFactory factory,
                                       IAppConfig appConfig,
                                       ITempFileService temp)
        {
            _log = log;
            _storeManager = storeManager;
            _tempStore = _storeManager.OpenStore("TempStore");
            _conManager = conManager;
            _factory = factory;
            _config = appConfig;
            _connStr = appConfig.GetValue<string>("Databases.CatalogDB.ConnStr");
            this.temp = temp;
        }
        public async Task<TempeOrderData> ProcessingFile(Stream stream, ManualEntryOrderFileDTO manualEntryOrderFileDTO)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DocumentXML));
            DocumentXML fileData = null;
            _ColorsCatalogName = GetColorsCatalog(manualEntryOrderFileDTO.ProjectID);
            using(XmlReader reader = XmlReader.Create(stream))
            {
                fileData = (DocumentXML)serializer.Deserialize(reader);
            }

            if(fileData == null)
            {
                throw new InvalidOperationException("Failed to deserialize the XML data.");
            }

            var tempFileName = await SaveStreamToFileAsync(stream);
            TempeOrderData tempeOrderData = GenerateOrderData(fileData, manualEntryOrderFileDTO.CompanyID, manualEntryOrderFileDTO.BrandID, manualEntryOrderFileDTO.ProjectID, tempFileName);
            return tempeOrderData;
        }

        public async Task<string> SaveStreamToFileAsync(Stream stream)
        {
            var path = temp.GetTempDirectory();
            var fileName = $"Order-{Guid.NewGuid()}.xml";
            var tempFileName = Path.Combine(path, fileName);


            if(stream == null)
                throw new ArgumentNullException(nameof(stream));

            if(string.IsNullOrWhiteSpace(tempFileName))
                throw new ArgumentException("Invalid file path", nameof(tempFileName));

            // Asegurarse de que el stream esté en la posición inicial
            if(stream.CanSeek)
            {
                stream.Seek(0, SeekOrigin.Begin);
            }

            string FileName = temp.GetTempFileName(fileName, true);
            using(FileStream fileStream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fileStream);
            }

            temp.RegisterForDelete(FileName, DateTime.Now.AddMonths(1));

            return FileName;

        }

        private string GetColorsCatalog(int projectID)
        {
            var query = $@"SELECT ct.Name,ct.CatalogID
                          FROM Catalogs ct
                          LEFT JOIN Projects p ON ct.ProjectID = p.ID
                          WHERE p.ID = @projectID
                          AND ct.Name LIKE '%Colors%'
                          ORDER BY ct.Name, p.Name";

            using(var conn = _conManager.OpenDB("MainDB"))
            {
                var catalog = conn.SelectOne<CatalogPluginDTO>(query, projectID);
                return string.Format("{0}_{1}", catalog.Name, catalog.CatalogID);
            }
        }


        private TempeOrderData GenerateOrderData(DocumentXML orderData, int CompanyID, int BrandID, int ProjectID, string fileName)
        {
            var output = new TempeOrderData();
            output.ArticlesCount = CalculateArticles(orderData);
            if(output.ArticlesCount > 0)
            {

                output.OrderLines = new List<TempeOrderLine>();
                output.CompanyID = CompanyID;
                output.BrandID = BrandID;
                output.ProjectID = ProjectID;
                for(int i = 0; i < output.ArticlesCount; i++)
                {
                    var allLangs = NeedAllLangs(orderData?.InstruccionesConservacion?.InstruccionConservacion);
                    var instructionsList = new List<string>();
                    var instructionsLine = string.Empty;

                    if(allLangs)
                    {
                        foreach(var careinstruction in orderData?.InstruccionesConservacion?.InstruccionConservacion)
                        {
                            var careInstructionAllLangs = GetAllLLangsCareInstructions(careinstruction);
                            instructionsList.Add(careInstructionAllLangs);
                        }
                        instructionsLine = string.Join(";", instructionsList);
                    }

                    output.OrderLines.Add(new TempeOrderLine()
                    {
                        ColorCode = CalculateColorNumber(orderData?.CodigosBarras?.CodigoBarras[i]?.Color),
                        SizeCode = CalculateSizeNumber(orderData?.CodigosBarras?.CodigoBarras[i]?.Talla),
                        FullFamily = CalculateFamily(orderData?.Familia?.DescripcionesInternacional?.DescripcionInternacional),
                        Color = GenerateColorsNames(CalculateColorNumber(orderData?.CodigosBarras?.CodigoBarras[i]?.Color)),
                        FullMadeIn = CalculateMadeIn(orderData?.DescripcionesIntPaisOrigen?.DescIntPaisOrigen),
                        FullCareInstructionsAllLangs = (instructionsLine.Length == 0)? ";;;;" : instructionsLine,
                        FullCareInstructions = CalculateFullCareInstructions(orderData?.InstruccionesConservacion?.InstruccionConservacion),
                        FullCareInstructionsString = CalculateFullCareInstructionsImages(orderData?.InstruccionesConservacionImagen?.InstruccionConservacionImagen),
                        FullExceptions = CalculateFullExceptions(orderData?.ExcepcionesArticulo?.ExcepcionArticulo),
                        Modelo = orderData?.Modelo,
                        Calidad = orderData?.Calidad,
                        DescCampana = orderData?.DescCampana,
                        DescCadena = orderData?.DescCadena,
                        DescPaisOrigen = orderData?.DescripcionesIntPaisOrigen?.DescIntPaisOrigen[2].Descripcion,
                        Medidas = orderData?.CodigosBarras?.CodigoBarras[i]?.Medidas,
                        CodigoBarras = new List<string>() { orderData?.CodigosBarras?.CodigoBarras[i]?.CodBarras, orderData?.CodigosBarras?.CodigoBarras[i]?.CodBarras },
                        Importadores = orderData?.DatosImportadores?.Importadores,
                        Equivalencia = new List<string>() { orderData?.CodigosBarras?.CodigoBarras[i]?.Equivalencia },
                        XmlPath = fileName,
                    });
                }

            }


            return output;
        }

        private bool NeedAllLangs(List<InstruccionConservacion> conservationsIntruction) =>
             conservationsIntruction?.Any(ci => ci?.DescripcionesInternacional?.DescripcionInternacional?.Any() == true) == true;

        private string GetAllLLangsCareInstructions(InstruccionConservacion careinstruction)
        {
            if(careinstruction?.DescripcionesInternacional == null || careinstruction?.DescripcionesInternacional?.DescripcionInternacional == null || careinstruction?.DescripcionesInternacional?.DescripcionInternacional?.Count() == 0)
                _log.LogWarning("There are no translations of the care instructions.");

            var description = careinstruction.DescripcionesInternacional?.DescripcionInternacional
                            ?.Select(d => d.Descripcion)
                            .Where(d => !string.IsNullOrWhiteSpace(d)) // opcional
                            .ToList();

            string allLangsCareInstructions = description != null
                ? string.Join(" - ", description)
                : string.Empty;

            return allLangsCareInstructions;
        }

        private string CalculateFullExceptions(List<ExcepcionArticulo> excepcionArticulo)
        {
            var exceptionsList = new List<string>();
            foreach(var exception in excepcionArticulo)
            {
                foreach(var description in exception.DescripcionesInternacional.DescripcionInternacional)
                    exceptionsList.Add(description.Descripcion);
            }
            return string.Join(" / ", exceptionsList);
        }

        private string CalculateFullCareInstructionsImages(List<InstruccionConservacionImagen> instruccionsConservacionImagen)
        {
            var fullCareInstructionsStrings = string.Empty;

            foreach(var instruccionImagen in instruccionsConservacionImagen)
                fullCareInstructionsStrings = fullCareInstructionsStrings + GetImagesStringsCareInstructions(instruccionImagen);

            return fullCareInstructionsStrings;
        }
        private string GetImagesStringsCareInstructions(InstruccionConservacionImagen instruccionsImagen)
        {
            var concatImagesString = instruccionsImagen?.DescripcionTipoEstandar + ";";

            foreach(var image in instruccionsImagen.ImagenesInstrucciones)
                concatImagesString = concatImagesString + image?.ToString() + ";";

            return concatImagesString;
        }

        private string CalculateFullCareInstructions(List<InstruccionConservacion> instruccionConservacion)
        {
            var careInstructionsStringWriter = new StringWriter();
            foreach(var careInstruction in instruccionConservacion)
                careInstructionsStringWriter.Write($"{careInstruction.Descripcion};");
            return careInstructionsStringWriter.ToString();
        }

        private string CalculateMadeIn(List<DescIntPaisOrigen> descIntPaisOrigen)
        {
            var madeInList = new List<string>();
            foreach(var madeIn in descIntPaisOrigen)
                madeInList.Add(madeIn.Descripcion);
            return string.Join(" / ", madeInList);
        }

        private string GenerateColorsNames(string color)
        {
            var query = $@"SELECT DESC_ESP,DESC_FRA,DESC_ING FROM {_ColorsCatalogName} WHERE CODIGO = @color";

            using(var conn = _conManager.OpenDB("CatalogDB"))
            {
                var colors = conn.SelectOne<ColorsCatalogDTO>(query, color);
                return string.Format("{0}/{1}/{2}", colors.DESC_ESP, colors.DESC_ING, colors.DESC_FRA);
            }
        }

        private string CalculateFamily(List<DescripcionInternacional> descripcionInternacional)
        {
            var familyList = new List<string>();
            foreach(var family in descripcionInternacional)
                familyList.Add(family.Descripcion);
            return string.Join(" / ", familyList);
        }

        private string CalculateSizeNumber(string size)
        {
            if(size.Length < 2)
                return string.Format("{0:D2}", int.Parse(size));
            return size;
        }

        private string CalculateColorNumber(string color)
        {
            if(color.Length < 3)
                return string.Format("{0:D3}", int.Parse(color));
            return color;
        }

        private int CalculateArticles(DocumentXML fileData)
        {
            if(fileData.CodigosBarras.CodigoBarras.Count() == 0)
            {
                // _log.LogMessage($"There is no barcode, therefore no items were found in the order.");
                throw new InvalidOperationException($"There is no barcode, therefore no items were found in the order.");
            }
            else
                return fileData.CodigosBarras.CodigoBarras.Count();
        }

        public async Task<string> GetInditexAPIData(APIInditexDataRq rq)
        {

            //string url = "https://api.inditex.com/tcorelab-provider/api/v1/product/order/validation";
            //string apiKey = "AZK4YS601yIXu6n5WPovWUdwk2xNmB9L";


            string url = _config.GetValue<string>("InditexAPI.URL");
            string apiKey = _config.GetValue<string>("InditexAPI.APIKey");

            if(string.IsNullOrEmpty(url))
            {
                url = INDITEX_URL;
            }

            if(string.IsNullOrEmpty(apiKey))
            {
                apiKey = INDITEX_APIKEY;
            }

            var json = JsonConvert.SerializeObject(rq);
            var content = new StringContent(json, Encoding.UTF8, "application/json");


            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("itx-apiKey", apiKey);


            HttpResponseMessage response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            return result;


        }
    }

    public class CatalogPluginDTO
    {
        public string Name { get; set; }
        public int CatalogID { get; set; }

    }
    public class ColorsCatalogDTO
    {
        public string DESC_ESP { get; set; }
        public string DESC_FRA { get; set; }
        public string DESC_ING { get; set; }
    }

    public class TempeOrderData
    {
        public int ArticlesCount { get; internal set; }
        public int CompanyID { get; internal set; }
        public int BrandID { get; internal set; }
        public List<TempeOrderLine> OrderLines { get; internal set; }
        public int ProjectID { get; internal set; }

        public OrderPoolDTO MapToOrderPoolDto()
        {
            var output = new OrderPoolDTO();
            output.CompanyID = CompanyID;
            output.BrandID = BrandID;
            output.SeasonID = ProjectID;
            output.ArticleQuality1 = OrderLines.FirstOrDefault()?.Modelo;
            output.ArticleQuality2 = OrderLines.FirstOrDefault()?.Calidad;
            output.RowJsonData = JsonConvert.SerializeObject(OrderLines);
            return output;
        }
    }

    public class APIInditexDataRq
    {
        public string purchaseOrder { get; set; }
        public int model { get; set; }
        public int quality { get; set; }

    }

    public class TempeOrderLine
    {
        public string OrderNumber { get; set; }
        public string Modelo { get; set; }
        public string Calidad { get; set; }

        public List<string> Equivalencia { get; set; }

        public string DescCampana { get; set; }
        [JsonProperty("DescCadena")]
        public string DescCadena { get; set; }
        public string DescPaisOrigen { get; set; }
        public string Medidas { get; set; }
        public List<string> CodigoBarras { get; set; }

        public string Importadores { get; set; }

        public string ColorCode { get; set; }
        public string SizeCode { get; set; }
        public string FullFamily { get; set; }
        public string Color { get; set; }
        public string FullCareInstructions { get; set; }
        public string FullCareInstructionsAllLangs { get; set; }
        public string FullCareInstructionsString { get; set; }
        public string FullExceptions { get; set; }
        public string FullMadeIn { get; set; }

        public string XmlPath { get; set; }

        public int Quantity { get; set; }

        public string FileGUID { get; set; }

        private string Currency1 { get; set; }
        private string Currency2 { get; set; }
        private string Currency3 { get; set; }
        private string Currency4 { get; set; }
    }

    [Serializable()]
    [System.Xml.Serialization.XmlRoot("articuloEtiquetado")]
    public class DocumentXML
    {
        [System.Xml.Serialization.XmlElementAttribute("idArticulo")]
        public string IdArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("modelo")]
        public string Modelo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("calidad")]
        public string Calidad { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codProducto")]
        public string CodProducto { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idCampana")]
        public string IdCampana { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descCampana")]
        public string DescCampana { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idCentroCompra")]
        public string IdCentroCompra { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idCadena")]
        public string IdCadena { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descCadena")]
        public string DescCadena { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idSistemaTallaje")]
        public string IdSistemaTallaje { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idPaisOrigen")]
        public string IdPaisOrigen { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("numeroVoluntario")]
        public string NumeroVoluntario { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("numeroVoluntarioFormateado")]
        public string NumeroVoluntarioFormateado { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("familia")]
        public Familia Familia { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesIntPaisOrigen")]
        public DescripcionesIntPaisOrigen DescripcionesIntPaisOrigen { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codigosBarras")]
        public CodigosBarras CodigosBarras { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("refuerzosComposArticulo")]
        public RefuerzosComposArticulo RefuerzosComposArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("excepcionesArticulo")]
        public ExcepcionesArticulo ExcepcionesArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("ponderadas")]
        public Ponderadas Ponderadas { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("despieces")]
        public Despieces Despieces { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("instruccionesConservacion")]
        public InstruccionesConservacion InstruccionesConservacion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("instruccionesConservacionImagen")]
        public InstruccionesConservacionImagen InstruccionesConservacionImagen { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("pesosRelleno")]
        public string PesosRelleno { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("degloses")]
        public Desgloses Desgloses { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("datosImportadores")]
        public DatosImportadores DatosImportadores { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("datosEtiquetaPlumon")]
        public DatosEtiquetaPlumon DatosEtiquetaPlumon { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("denimDestenido")]
        public DenimDestenido DenimDestenido { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("warningsEtiqueta")]
        public WarningsEtiqueta WarningsEtiqueta { get; set; }

    }

    [Serializable()]
    public class Familia
    {
        [System.Xml.Serialization.XmlElementAttribute("idFamilia")]
        public string IdFamilia { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codFamilia")]
        public string CodFamilia { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcion")]
        public string Descripcion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesInternacional")]
        public DescripcionesInternacional DescripcionesInternacional { get; set; }

    }

    [Serializable()]
    public class DescripcionInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("idIdioma")]
        public string IdIdioma { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcion")]
        public string Descripcion { get; set; }

    }

    [Serializable()]
    public class DescripcionesIntPaisOrigen
    {
        [System.Xml.Serialization.XmlElementAttribute("descIntPaisOrigen")]
        public List<DescIntPaisOrigen> DescIntPaisOrigen { get; set; }
    }

    [Serializable()]
    public class DescIntPaisOrigen
    {
        [System.Xml.Serialization.XmlElementAttribute("idIdioma")]
        public string IdIdioma { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcion")]
        public string Descripcion { get; set; }
    }

    [Serializable()]
    public class CodigosBarras
    {
        [System.Xml.Serialization.XmlElementAttribute("codigoBarras")]
        public List<CodigoBarras> CodigoBarras { get; set; }
    }

    [Serializable()]
    public class CodigoBarras
    {
        [System.Xml.Serialization.XmlElementAttribute("idColor")]
        public string IdColor { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idTalla")]
        public string IdTalla { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idDesglose")]
        public string IdDesglose { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("color")]
        public string Color { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("talla")]
        public string Talla { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descCodBarras")]
        public string DescCodBarras { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codBarras")]
        public string CodBarras { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codBarrasForFont")]
        public string CodBarrasForFont { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("equivalencia")]
        public string Equivalencia { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descTalla")]
        public string DescTalla { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("medidas")]
        public string Medidas { get; set; }

    }

    [Serializable()]
    public class RefuerzosComposArticulo
    {
        [System.Xml.Serialization.XmlElementAttribute("composicionesRefuerzo")]
        public string ComposicionesRefuerzo { get; set; }
    }

    [Serializable()]
    public class ExcepcionesArticulo
    {
        [System.Xml.Serialization.XmlElementAttribute("excepcionArticulo")]
        public List<ExcepcionArticulo> ExcepcionArticulo { get; set; }
    }

    [Serializable()]
    public class ExcepcionArticulo
    {
        [System.Xml.Serialization.XmlElementAttribute("idExcepcionComposicion")]
        public string IdExcepcionComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesInternacional")]
        public DescripcionesInternacional DescripcionesInternacional { get; set; }
    }

    [Serializable()]
    public class DescripcionesInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descripcionInternacional")]
        public List<DescripcionInternacional> DescripcionInternacional { get; set; }
    }


    [Serializable()]
    public class Ponderadas
    {
        [System.Xml.Serialization.XmlElementAttribute("ponderada")]
        public List<Ponderada> Ponderada { get; set; }
    }

    [Serializable()]
    public class Ponderada
    {
        [System.Xml.Serialization.XmlElementAttribute("idArticulo")]
        public string IdArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idTipoComposicion")]
        public string IdTipoComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionTipoComposicion")]
        public string DescripcionTipoComposicion { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("descripcionesTipoComposicionInternacional")]
        public DescripcionesTipoComposicionInternacional DescripcionesTipoComposicionInternacional { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("composiciones")]
        public Composiciones Composiciones { get; set; }

    }

    [Serializable()]
    public class DescripcionesTipoComposicionInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descripcionTipoComposicionInternacional")]
        public List<DescripcionTipoComposicionInternacional> DescripcionTipoComposicionInternacional { get; set; }

    }

    [Serializable()]
    public class Composiciones
    {
        [System.Xml.Serialization.XmlElementAttribute("composicion")]
        public List<Composicion> Composicion { get; set; }

    }

    [Serializable()]
    public class Composicion
    {
        [System.Xml.Serialization.XmlElementAttribute("idComposicion")]
        public string IdComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idTipoComposicion")]
        public string IdTipoComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionComposicion")]
        public string DescripcionComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codComposicionAs400")]
        public string CodComposicionAs400 { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("porcentaje")]
        public string Porcentaje { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("zonaPrenda")]
        public ZonaPrenda ZonaPrenda { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("esMicrocontenido")]
        public bool EsMicrocontenido { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("esCuero")]
        public bool EsCuero { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesComposicionInternacional")]
        public DescripcionesComposicionInternacional DescripcionesComposicionInternacional { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesTipoCompInternacional")]
        public DescripcionesTipoCompInternacional DescripcionesTipoCompInternacional { get; set; }
    }

    [Serializable()]
    public class DescripcionesComposicionInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descripcionComposicionInternacional")]
        public List<Descripciones> DescripcionComposicionInternacional { get; set; }

    }

    [Serializable()]
    public class DescripcionesTipoCompInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descTipoCompInternacional")]
        public List<Descripciones> DescTipoCompInternacional { get; set; }

    }

    [Serializable()]
    public class DescripcionTipoComposicionInternacional : Descripciones { }

    [Serializable()]
    public class ZonaPrenda
    {
        [System.Xml.Serialization.XmlElementAttribute("idZonaPrenda")]
        public string IdZonaPrenda { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("orden")]
        public string Orden { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descZonaPrenda")]
        public string DescZonaPrenda { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesZonaPrendaInternacional")]
        public DescripcionesZonaPrendaInternacional DescripcionesZonaPrendaInternacional { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("descripcionesDespieceInternacional")]
        public DescripcionesDespieceInternacional DescripcionesDespieceInternacional { get; set; }

    }

    [Serializable()]
    public class DescripcionesZonaPrendaInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descZonaPrendaInternacional")]
        public List<Descripciones> DescZonaPrendaInternacional { get; set; }

    }

    [Serializable()]
    public class DescripcionesDespieceInternacional
    {
        [System.Xml.Serialization.XmlElementAttribute("descDespieceInternacional")]
        public List<Descripciones> DescDespieceInternacional { get; set; }

    }

    [Serializable()]
    public class Despieces
    {
        [System.Xml.Serialization.XmlElementAttribute("despiece")]
        public List<Despiece> Despiece { get; set; }
    }

    [Serializable()]
    public class Despiece
    {
        [System.Xml.Serialization.XmlElementAttribute("idArticulo")]
        public string IdArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idTipoComposicion")]
        public string IdTipoComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionTipoComposicion")]
        public string DescripcionTipoComposicion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesTipoComposicionInternacional")]
        public DescripcionesTipoComposicionInternacional DescripcionesTipoComposicionInternacional { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("composiciones")]
        public Composiciones Composiciones { get; set; }
    }
    [Serializable()]
    public class InstruccionesConservacion
    {

        [System.Xml.Serialization.XmlElementAttribute("instruccionConservacion")]
        public List<InstruccionConservacion> InstruccionConservacion { get; set; }
    }

    [Serializable()]
    public class InstruccionConservacion
    {

        [System.Xml.Serialization.XmlElementAttribute("idInstruccionConservacion")]
        public string IdInstruccionConservacion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("codInstruccionConservacion")]
        public string CodInstruccionConservacion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idTipoInstruccionConservacion")]
        public string IdTipoInstruccionConservacion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("idArticulo")]
        public string IdArticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcion")]
        public string Descripcion { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionesInternacional")]
        public DescripcionesInternacional DescripcionesInternacional { get; set; }
    }

    public class InstruccionesConservacionImagen
    {
        [System.Xml.Serialization.XmlElementAttribute("instruccionConservacionImagen")]
        public List<InstruccionConservacionImagen> InstruccionConservacionImagen { get; set; }
    }

    [Serializable()]
    public class InstruccionConservacionImagen
    {
        [System.Xml.Serialization.XmlElementAttribute("idTipoEstandar")]
        public string IdTipoEstandar { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("descripcionTipoEstandar")]
        public string DescripcionTipoEstandar { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("imagenesInstrucciones")]
        public List<string> ImagenesInstrucciones { get; set; } = new List<string>();
        /*[System.Xml.Serialization.XmlElementAttribute("imagenesInstrucciones")]
        public string ImagenesInstrucciones2 { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("imagenesInstrucciones")]
        public string ImagenesInstrucciones3 { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("imagenesInstrucciones")]
        public string ImagenesInstrucciones4 { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("imagenesInstrucciones")]
        public string ImagenesInstrucciones5 { get; set; }*/
    }

    [Serializable()]
    public class Desgloses
    {
        [System.Xml.Serialization.XmlElementAttribute("desglose")]
        public List<Desglose> Desglose { get; set; }
    }
    [Serializable()]
    public class Desglose
    {
        [System.Xml.Serialization.XmlElementAttribute("idSubarticulo")]
        public string IdSubarticulo { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("ponderadas")]
        public Ponderadas IdTipoComposicion { get; set; }
    }

    [Serializable()]
    public class DatosImportadores
    {
        [System.Xml.Serialization.XmlElementAttribute("importadores")]
        public string Importadores { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("rn")]
        public string Rn { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("ruc")]
        public string Ruc { get; set; }
    }

    [Serializable()]
    public class DatosEtiquetaPlumon
    {
        [System.Xml.Serialization.XmlElementAttribute("mostrarEtiquetaPlumon")]
        public string MostrarEtiquetaPlumon { get; set; }
    }

    [Serializable()]
    public class DenimDestenido
    {
        [System.Xml.Serialization.XmlElementAttribute("mostrarEtiquetaDesteñido")]
        public string MostrarEtiquetaDesteñido { get; set; }
    }
    [Serializable()]
    public class WarningsEtiqueta
    {
        [System.Xml.Serialization.XmlElementAttribute("mostrarKAFF")]
        public string MostrarKAFF { get; set; }
    }

    public class Descripciones
    {
        [System.Xml.Serialization.XmlElementAttribute("idIdioma")]
        public string IdIdioma { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("descripcion")]
        public string Descripcion { get; set; }
    }
}
