//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CrawlerHolidaysLK
{
    using System;
    using System.Collections.Generic;
    
    public partial class HotelFacility
    {
        public long HotelFacilityId { get; set; }
        public long HotelId { get; set; }
        public long FacilityId { get; set; }
        public Nullable<System.DateTime> CreatedOn { get; set; }
        public Nullable<System.DateTime> UpdatedOn { get; set; }
        public Nullable<bool> IsActive { get; set; }
    
        public virtual Facility Facility { get; set; }
        public virtual Hotel Hotel { get; set; }
    }
}
