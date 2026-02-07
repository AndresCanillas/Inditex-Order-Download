using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Services
{
    public class TendamMapping
    {
        [FixedWidthField(1, 6)]
        [OrderPoolMapping("ProviderCode1")]
        public string SupplierCode { get; set; }

        [FixedWidthField(7, 8)]
        [OrderPoolMapping("ExtraData")]
        public string FICOSCode { get; set; }

        [FixedWidthField(15, 35)]
        [OrderPoolMapping("ProviderName1")]
        public string SupplierName { get; set; }

        [FixedWidthField(50, 1)]
        [OrderPoolMapping("ExtraData")]
        public string InternalCodeBrand { get; set; }

        [FixedWidthField(51, 12)]
        [OrderPoolMapping("ExtraData")]
        public string Brand { get; set; }

        [FixedWidthField(63, 3)]
        [OrderPoolMapping("ExtraData")]
        public string ManWoman { get; set; }

        [FixedWidthField(66, 13)]
        [OrderPoolMapping("CategoryText2")]
        public string EANCode { get; set; }

        [FixedWidthField(79, 7)]
        [OrderPoolMapping("CategoryCode1")]
        public string Style { get; set; }

        [FixedWidthField(86, 2)]
        [OrderPoolMapping("ColorCode")]
        public string Color { get; set; }

        [FixedWidthField(88, 2)]
        [OrderPoolMapping("Size")]
        public string SizeGrament { get; set; }

        [FixedWidthField(90, 25)]
        [OrderPoolMapping("ExtraData")]
        public string SupplierModel { get; set; }

        [FixedWidthField(115, 11)]
        [OrderPoolMapping("ColorName")]
        public string SupplierColor { get; set; }

        [FixedWidthField(126, 3)]
        [OrderPoolMapping("ExtraData")]

        public string SupplierSize { get; set; }

        [FixedWidthField(129, 7)]
        [OrderPoolMapping("OrderNumber")]
        public string OrderInfo { get; set; }

        [FixedWidthField(136, 9)]
        [OrderPoolMapping("Price1")]
        public string PriceSpain { get; set; }

        [FixedWidthField(145, 7)]
        [OrderPoolMapping("ExtraData")]
        public string OrderNum { get; set; }

        [FixedWidthField(152, 6)]
        [OrderPoolMapping("Quantity")]
        [FixedWidthPadding(PaddingDirection.Left, '0')]
        public string LabelsPrint { get; set; }

        [FixedWidthField(158, 3)]
        [OrderPoolMapping("CategoryText3")]
        public string Format { get; set; }

        [FixedWidthField(161, 2)]
        [OrderPoolMapping("ExtraData")]
        public string LabelType { get; set; }

        [FixedWidthField(163, 1)]
        [OrderPoolMapping("ExtraData")]
        public string Franchises { get; set; }

        [FixedWidthField(164, 1)]
        [OrderPoolMapping("ExtraData")]
        public string ExceptionalRecord { get; set; }

        [FixedWidthField(165, 3)]
        [OrderPoolMapping("ExtraData")]

        public string TotalNumberLines { get; set; }

        [FixedWidthField(168, 59)]

        [OrderPoolMapping("ExtraData")]
        public string Sizing { get; set; }

        [FixedWidthField(227, 55)]
        [OrderPoolMapping("ExtraData")]
        public string SizeConversion { get; set; }

        [FixedWidthField(282, 2)]
        [OrderPoolMapping("ExtraData")]
        public string IDCountry { get; set; }

        [FixedWidthField(284, 3)]
        [OrderPoolMapping("ExtraData")]
        public string ISOCountryOrder { get; set; }

        [FixedWidthField(287, 20)]
        [OrderPoolMapping("ExtraData")]
        public string NameCountryOrder { get; set; }

        // ===== PRECIOS (1 a 15) =====

        [FixedWidthField(307, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_1 { get; set; }
        [FixedWidthField(316, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_1 { get; set; }
        [FixedWidthField(319, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_1 { get; set; }
        [FixedWidthField(328, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_1 { get; set; }
        [FixedWidthField(331, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_1 { get; set; }

        [FixedWidthField(334, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_2 { get; set; }
        [FixedWidthField(343, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_2 { get; set; }
        [FixedWidthField(346, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_2 { get; set; }
        [FixedWidthField(355, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_2 { get; set; }
        [FixedWidthField(358, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_2 { get; set; }

        [FixedWidthField(361, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_3 { get; set; }
        [FixedWidthField(370, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_3 { get; set; }
        [FixedWidthField(373, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_3 { get; set; }
        [FixedWidthField(382, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_3 { get; set; }
        [FixedWidthField(385, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_3 { get; set; }

        [FixedWidthField(388, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_4 { get; set; }
        [FixedWidthField(397, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_4 { get; set; }
        [FixedWidthField(400, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_4 { get; set; }
        [FixedWidthField(409, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_4 { get; set; }
        [FixedWidthField(412, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_4 { get; set; }

        [FixedWidthField(415, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_5 { get; set; }
        [FixedWidthField(424, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_5 { get; set; }
        [FixedWidthField(427, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_5 { get; set; }
        [FixedWidthField(436, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_5 { get; set; }
        [FixedWidthField(439, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_5 { get; set; }

        [FixedWidthField(442, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_6 { get; set; }
        [FixedWidthField(451, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_6 { get; set; }
        [FixedWidthField(454, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_6 { get; set; }
        [FixedWidthField(463, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_6 { get; set; }
        [FixedWidthField(466, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_6 { get; set; }

        [FixedWidthField(469, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_7 { get; set; }
        [FixedWidthField(478, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_7 { get; set; }
        [FixedWidthField(481, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_7 { get; set; }
        [FixedWidthField(490, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_7 { get; set; }
        [FixedWidthField(493, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_7 { get; set; }

        [FixedWidthField(496, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_8 { get; set; }
        [FixedWidthField(505, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_8 { get; set; }
        [FixedWidthField(508, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_8 { get; set; }
        [FixedWidthField(517, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_8 { get; set; }
        [FixedWidthField(520, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_8 { get; set; }

        [FixedWidthField(523, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_9 { get; set; }
        [FixedWidthField(532, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_9 { get; set; }
        [FixedWidthField(535, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_9 { get; set; }
        [FixedWidthField(544, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_9 { get; set; }
        [FixedWidthField(547, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_9 { get; set; }

        [FixedWidthField(550, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_10 { get; set; }
        [FixedWidthField(559, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_10 { get; set; }
        [FixedWidthField(562, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_10 { get; set; }
        [FixedWidthField(571, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_10 { get; set; }
        [FixedWidthField(574, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_10 { get; set; }

        [FixedWidthField(577, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_11 { get; set; }
        [FixedWidthField(586, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_11 { get; set; }
        [FixedWidthField(589, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_11 { get; set; }
        [FixedWidthField(598, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_11 { get; set; }
        [FixedWidthField(601, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_11 { get; set; }

        [FixedWidthField(604, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_12 { get; set; }
        [FixedWidthField(613, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_12 { get; set; }
        [FixedWidthField(616, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_12 { get; set; }
        [FixedWidthField(625, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_12 { get; set; }
        [FixedWidthField(628, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_12 { get; set; }

        [FixedWidthField(631, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_13 { get; set; }
        [FixedWidthField(640, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_13 { get; set; }
        [FixedWidthField(643, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_13 { get; set; }
        [FixedWidthField(652, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_13 { get; set; }
        [FixedWidthField(655, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_13 { get; set; }

        [FixedWidthField(658, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_14 { get; set; }
        [FixedWidthField(667, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_14 { get; set; }
        [FixedWidthField(670, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_14 { get; set; }
        [FixedWidthField(679, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_14 { get; set; }
        [FixedWidthField(682, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_14 { get; set; }

        [FixedWidthField(685, 9)]
        [OrderPoolMapping("ExtraData")]
        public string Price_15 { get; set; }
        [FixedWidthField(694, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Currency_15 { get; set; }
        [FixedWidthField(697, 9)]
        [OrderPoolMapping("ExtraData")]
        public string PriceEuros_15 { get; set; }
        [FixedWidthField(706, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag1_15 { get; set; }
        [FixedWidthField(709, 3)]
        [OrderPoolMapping("ExtraData")]
        public string Flag2_15 { get; set; }

        [FixedWidthField(712, 1)]
        [OrderPoolMapping("ExtraData")]
        public string RussianSymbol { get; set; }

        [FixedWidthField(713, 5)]
        [OrderPoolMapping("ExtraData")]
        public string RussianGenericDescription { get; set; }

        [FixedWidthField(718, 10)]
        [OrderPoolMapping("ExtraData")]
        public string DateImportMonth { get; set; }

        [FixedWidthField(728, 4)]
        [OrderPoolMapping("ExtraData")]
        public string DateImportYear { get; set; }

        [FixedWidthField(732, 8)]
        [OrderPoolMapping("ExtraData")]
        public string DeliveryDate { get; set; }

        [FixedWidthField(740, 9)]
        [OrderPoolMapping("ExtraData")]
        public string IdLote { get; set; }

        [FixedWidthField(749, 2)]
        [OrderPoolMapping("ExtraData")]
        public string IdSizeType { get; set; }

        // ===== TALLAS POR PAÍS =====

        [FixedWidthField(751, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_1 { get; set; }
        [FixedWidthField(754, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_1 { get; set; }

        [FixedWidthField(768, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_2 { get; set; }
        [FixedWidthField(771, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_2 { get; set; }

        [FixedWidthField(785, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_3 { get; set; }
        [FixedWidthField(788, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_3 { get; set; }

        [FixedWidthField(802, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_4 { get; set; }
        [FixedWidthField(805, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_4 { get; set; }

        [FixedWidthField(819, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_5 { get; set; }
        [FixedWidthField(822, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_5 { get; set; }

        [FixedWidthField(836, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_6 { get; set; }
        [FixedWidthField(839, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_6 { get; set; }

        [FixedWidthField(853, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_7 { get; set; }
        [FixedWidthField(856, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_7 { get; set; }

        [FixedWidthField(870, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_8 { get; set; }
        [FixedWidthField(873, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_8 { get; set; }

        [FixedWidthField(887, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_9 { get; set; }
        [FixedWidthField(890, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_9 { get; set; }

        [FixedWidthField(904, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_10 { get; set; }
        [FixedWidthField(907, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_10 { get; set; }

        [FixedWidthField(921, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_11 { get; set; }
        [FixedWidthField(924, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_11 { get; set; }

        [FixedWidthField(938, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_12 { get; set; }
        [FixedWidthField(941, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_12 { get; set; }

        [FixedWidthField(955, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_13 { get; set; }
        [FixedWidthField(958, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_13 { get; set; }

        [FixedWidthField(972, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_14 { get; set; }
        [FixedWidthField(975, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_14 { get; set; }

        [FixedWidthField(989, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_15 { get; set; }
        [FixedWidthField(992, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_15 { get; set; }

        [FixedWidthField(1006, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_16 { get; set; }
        [FixedWidthField(1009, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_16 { get; set; }

        [FixedWidthField(1023, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_17 { get; set; }
        [FixedWidthField(1026, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_17 { get; set; }

        [FixedWidthField(1040, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_18 { get; set; }
        [FixedWidthField(1043, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_18 { get; set; }

        [FixedWidthField(1057, 3)]
        [OrderPoolMapping("ExtraData")]
        public string CountryISOCode_19 { get; set; }
        [FixedWidthField(1060, 14)]
        [OrderPoolMapping("ExtraData")]
        public string Size_19 { get; set; }

        [FixedWidthField(1074, 40)]
        [OrderPoolMapping("ExtraData")]
        public string TipologyProduct { get; set; }

        [FixedWidthField(1114, 15)]
        [OrderPoolMapping("ExtraData")]
        public string ArticleGroup { get; set; }

        [FixedWidthField(1129, 19)]
        [OrderPoolMapping("ExtraData")]
        public string LinkToSage { get; set; }

        [FixedWidthField(1148, 5)]
        [OrderPoolMapping("ExtraData")]
        public string logo_TRIMAN { get; set; }

        [FixedWidthField(1153, 25)]
        [OrderPoolMapping("ArticleCode")]
        [FixedWidthPadding(PaddingDirection.Right, ' ')]
        public string ArticleCodeMD { get; set; }

        //[FixedWidthField(1178, 30)]
        //[OrderPoolMapping("ExtraData")]
        //public string WebUrl { get; set; }
    }
}
