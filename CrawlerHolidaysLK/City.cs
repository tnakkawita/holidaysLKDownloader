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
    
    public partial class City
    {
        public long CityId { get; set; }
        public string CityName { get; set; }
        public string CityImage { get; set; }
        public long CountryId { get; set; }
        public bool IsActive { get; set; }
    
        public virtual Country Country { get; set; }
    }
}
