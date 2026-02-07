using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebLink.Contracts.Models
{
    public class ProjectImage : IProjectImage
    {

        #region IEntity
        public int ID { get; set; }
        #endregion IEntity

        public string Name { get; set; }

        public string Description { get; set; }

        public int? ProjectID { get; set; }

        public string Extension { get; set; }

        [NotMapped]
        public ImageMetadata UserMetaData { get; set; }


        #region IBasicTracking
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        #endregion IBasicTracking

    }
}

