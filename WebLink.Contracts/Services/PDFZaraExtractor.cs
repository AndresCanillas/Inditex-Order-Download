namespace WebLink.Contracts.Services
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class PDFZaraExtractor
    {
        [JsonProperty("Main")]
        public Main Main { get; set; }

        [JsonProperty("Pedido")]
        public Pedido Pedido { get; set; }

        [JsonProperty("Datos_adicionales")]
        public DatosAdicionales DatosAdicionales { get; set; }

        [JsonProperty("Leyenda")]
        public string Leyenda { get; set; }
    }

    public class Main
    {
        [JsonProperty("numeroPedido")]
        public int NumeroPedido { get; set; }

        [JsonProperty("empresa")]
        public string Empresa { get; set; }

        [JsonProperty("proveedor")]
        public string Proveedor { get; set; }

        [JsonProperty("fecha")]
        public string Fecha { get; set; }
    }

    public class Pedido
    {
        [JsonProperty("articulos")]
        public List<Articulo> Articulos { get; set; }

        [JsonProperty("observaciones_generales")]
        public string ObservacionesGenerales { get; set; }

        [JsonProperty("Total_importe")]
        public string TotalImporte { get; set; }

        [JsonProperty("business_information")]
        public BusinessInformation BusinessInformation { get; set; }
        [JsonProperty("SizeRange")]
        public string SizeRange { get; set; }
    }

    public class Articulo
    {
        [JsonProperty("Ref. Fornitura")]
        public string RefFornitura { get; set; }

        [JsonProperty("Fornitura")]
        public string Fornitura { get; set; }

        [JsonProperty("Cantidad")]
        public string Cantidad { get; set; }

        [JsonProperty("Unidades")]
        public string Unidades { get; set; }

        [JsonProperty("F.Entrega")]
        public string FEntrega { get; set; }

        [JsonProperty("Precio")]
        public string Precio { get; set; }

        [JsonProperty("MCC")]
        public string MCC { get; set; }

        [JsonProperty("Observaciones")]
        public string Observaciones { get; set; }

        [JsonProperty("tallas")]
        public List<Talla> Tallas { get; set; }

        [JsonProperty("ArticleCode")]
        public string ArticleCode { get; set; }

        [JsonProperty("NormalizeMCC")]
        public NormalizeMCC NormalizeMCC { get; set; }

        [JsonProperty("TipoArticulo")]
        public string TipoArticulo { get; set; }
    }

    public class Talla
    {
        [JsonProperty("Talla")]
        public string TallaNombre { get; set; }

        [JsonProperty("id_talla")]
        public string TallaID { get; set; }

        [JsonProperty("Cantidad")]
        public string Cantidad { get; set; }

        [JsonProperty("F.Entrega")]
        public string FEntrega { get; set; }

        [JsonProperty("Precio")]
        public string Precio { get; set; }

        [JsonProperty("Observaciones")]
        public string Observaciones { get; set; }
    }

    public class NormalizeMCC
    {
        [JsonProperty("Temporada")]
        public string Temporada { get; set; }

        [JsonProperty("Modelo")]
        public string Modelo { get; set; }

        [JsonProperty("Calidad")]
        public string Calidad { get; set; }

        [JsonProperty("Color")]
        public string Color { get; set; }
    }

    public class BusinessInformation
    {
        [JsonProperty("PedidoZara")]
        public string PedidoZara { get; set; }

        [JsonProperty("esSur")]
        public bool EsSur { get; set; }

        [JsonProperty("Alarma")]
        public bool Alarma { get; set; }

        [JsonProperty("codigoTaller")]
        public string CodigoTaller { get; set; }

        [JsonProperty("seccion")]
        public string Seccion { get; set; }


        [JsonProperty("id_seccion")]
        public string SeccionID { get; set; }

        [JsonProperty("nombre_proveedor")]
        public string NombreProveedor { get; set; }

        [JsonProperty("id_proveedor")]
        public string IdProveedor { get; set; }

        [JsonProperty("nombre_sizeset")]
        public string NombreSizeset { get; set; }

        [JsonProperty("id_sizeset")]
        public string IdSizeset { get; set; }

        [JsonProperty("id_proyecto")]
        public string ProjectID { get; set; }

        [JsonProperty("id_marca")]
        public string BrandID { get; set; }

        [JsonProperty("id_company")]
        public string CompanyID { get; set; }

        [JsonProperty("nombre_subseccion")]
        public string SubSeccion { get; set; }


        [JsonProperty("id_subseccion")]
        public string SubSeccionID { get; set; }

        [JsonProperty("nombre_madeIn")]
        public string MadeIn { get; set; }


        [JsonProperty("id_madeIn")]
        public string MadeInID { get; set; }
    }

    public class DatosAdicionales
    {
        [JsonProperty("etiqueta_composicion")]
        public EtiquetaComposicion EtiquetaComposicion { get; set; }

        [JsonProperty("etiqueta_precio")]
        public EtiquetaPrecio EtiquetaPrecio { get; set; }
    }

    public class EtiquetaComposicion
    {
        [JsonProperty("main_adicionales")]
        public MainAdicionales MainAdicionales { get; set; }

        [JsonProperty("info_composicion")]
        public InfoComposicion InfoComposicion { get; set; }
    }

    public class EtiquetaPrecio
    {
        [JsonProperty("main_adicionales")]
        public MainAdicionales MainAdicionales { get; set; }

        [JsonProperty("info_precio")]
        public InfoPrecio InfoPrecio { get; set; }
    }

    public class MainAdicionales
    {
        [JsonProperty("ref")]
        public string Ref { get; set; }

        [JsonProperty("MCC")]
        public string MCC { get; set; }

        [JsonProperty("familia")]
        public string Familia { get; set; }

        [JsonProperty("parte")]
        public string Parte { get; set; }

        [JsonProperty("genero")]
        public string Genero { get; set; }
    }

    public class InfoComposicion
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("ref_tallas")]
        public Dictionary<string, string> RefTallas { get; set; }

        [JsonProperty("mercado_origen")]
        public MercadoOrigen MercadoOrigen { get; set; }

        [JsonProperty("composicion_prenda")]
        public ComposicionPrenda ComposicionPrenda { get; set; }

        [JsonProperty("instrucciones_conservacion")]
        public List<InstruccionConservacionPDF> InstruccionesConservacion { get; set; }

        [JsonProperty("otras_instrucciones_conservacion")]
        public List<InstruccionConservacionPDF> OtrasInstruccionesConservacion { get; set; }

        [JsonProperty("observaciones_composicion")]
        public string ObservacionesComposicion { get; set; }
    }

    public class MercadoOrigen
    {
        [JsonProperty("Nombre")]
        public string Nombre { get; set; }

        [JsonProperty("ID")]
        public string ID { get; set; }
    }

    public class ComposicionPrenda
    {
        [JsonProperty("Secciones")]
        public List<Seccion> Secciones { get; set; }

        [JsonProperty("Excepciones")]
        public List<Excepcion> Excepciones { get; set; }
    }

    public class Seccion
    {
        [JsonProperty("Nombre")]
        public string Nombre { get; set; }

        [JsonProperty("Fibras")]
        public List<Fibra> Fibras { get; set; }

        [JsonProperty("ID")]
        public string ID { get; set; }
    }

    public class Fibra
    {
        [JsonProperty("Porcentaje")]
        public string Porcentaje { get; set; }

        [JsonProperty("Nombre")]
        public string Nombre { get; set; }

        [JsonProperty("ID")]
        public string ID { get; set; }
    }

    public class Excepcion
    {
        [JsonProperty("Nombre")]
        public string Nombre { get; set; }

        [JsonProperty("ID")]
        public string ID { get; set; }
    }

    public class InstruccionConservacionPDF
    {
        [JsonProperty("Nombre")]
        public string Nombre { get; set; }

        [JsonProperty("ID")]
        public string ID { get; set; }
    }

    public class InfoPrecio
    {
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("ref_tallas")]
        public Dictionary<string, string> RefTallas { get; set; }

        [JsonProperty("precios")]
        public List<PrecioEtiqueta> Precios { get; set; }

        [JsonProperty("tallas")]
        public List<string> Tallas { get; set; }
    }

    public class PrecioEtiqueta
    {
        [JsonProperty("tipo_divisa")]
        public string TipoDivisa { get; set; }

        [JsonProperty("precio_divisa")]
        public string PrecioDivisa { get; set; }

        [JsonProperty("etiqueta_color")]
        public string EtiquetaColor { get; set; }
    }

}
