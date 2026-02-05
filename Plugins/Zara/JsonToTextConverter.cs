using Service.Contracts.Database;
using Services.Core;
using StructureInditexOrderFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JsonColor
{
    public static class JsonToTextConverter
    {
        private static int ProjectID = 0;
        private static IConnectionManager ConnMng;
        public static string LoadData(InditexOrderData orderData, ILogService log = null, IConnectionManager connMng = null, int projectID = 0)
        {
            string filedata = "";
            bool hasHead = false;
            ProjectID = projectID;
            ConnMng = connMng;
            foreach (var color in orderData.POInformation.colors)
            {
                foreach (var size in color.sizes)
                {


                    string dataLabel = "";
                    string line = "";
                    string head = "";

                    filedata += CreateLineByLabel(color.color, size, orderData, ref hasHead, ref dataLabel, ref line, ref head, log, connMng);
                }
            }
            return filedata;
        }

        private static string CreateLineByLabel(int color, Size size, InditexOrderData
            orderData, ref bool hasHead, ref string dataLabel,
            ref string line, ref string head,ILogService log, IConnectionManager connMng)
        {
            //Codex: debemos usar como contexto el proyecto Mango.JsonColor.csproj
            //para generar archivos planos csv con los Json ubicados
            //en la carpeta OrderFiles ejemplo 15536_05987_I25_NNO_ZARANORTE.json
            //Utilizar como reglas de negocio el pdf documentacionparaconvertirJsonatextoplano.pdf para conocer el formato de los archivos planos a generar


            return dataLabel;
        }



    }
}
