using System;
using System.Collections.Generic;
using System.Text;

namespace WebLink.Contracts.Models
{
    public class FieldsToUpdateDTO
    {
        public int ProjectID;
        // public int CatalogID -> for the future
        public int ProductDataID;// row id
        public List<ProductField> ProductFields; //field name and value

        public FieldsToUpdateDTO() {
            ProjectID = 0;
            ProductDataID = 0;
            ProductFields = new List<ProductField>();
        }

    }
}
