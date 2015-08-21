using System;
using System.Collections.Generic;

namespace BeYourMarket.Model.Models
{
    public partial class ListingComment : Repository.Pattern.Ef6.Entity
    {
        public int ID { get; set; }
        public int ListingID { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public string AuthorName { get; set; }
        public string AuthorEmail { get; set; }
        public bool Enabled { get; set; }
        public bool Active { get; set; }
        public bool Spam { get; set; }
        public bool Private { get; set; }
        public string UserID { get; set; }
        public virtual Listing Listing { get; set; }
    }
}
