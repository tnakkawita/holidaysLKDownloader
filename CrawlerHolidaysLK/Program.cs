using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Supremes;
using Supremes.Nodes;

namespace CrawlerHolidaysLK
{
    class Program
    {
        static void Main(string[] args)
        {
            DownLoadHotels();
            CrawleHotelInfo();
        }

        private static void DownLoadHotels()
        {
            Console.WriteLine("New hotel data download is started");
            using (var db = new holidayslkEntities())
            {
                var crawlingURL =
                    "http://www.booking.com/searchresults.html?label=gen173nr-1FCAEoggJCAlhYSDNiBW5vcmVmaIUBiAEBmAExuAEGyAEM2AEB6AEB-AECqAID&sid=3666bc022d8e0188b8689070c947b0aa&checkin_year_month_monthday=2016-10-10&checkout_year_month_monthday=2016-10-11&city=-2219694&class_interval=1&dtdisc=0&hlrd=0&hyb_red=0&inac=0&label_click=undef&nha_red=0&postcard=0&redirected_from_city=0&redirected_from_landmark=0&redirected_from_region=0&review_score_group=empty&room1=A%2CA&sb_price_type=total&score_min=0&ss_all=0&ssb=empty&sshis=0&order=class";
                //var crawlingURL = ConfigurationManager.AppSettings["CrawlingURL"];
                var doc = Dcsoup.Parse(new Uri(crawlingURL), 5000);

                var hotelsList = doc.Select("div[id=hotellist_inner]").First().Children.ToList();
                foreach (var hotelRec in hotelsList)
                {
                    var head = hotelRec.Select("h3");
                    var name = head.Select("span[class=sr-hotel__name]").Text.Trim();

                    if (!String.IsNullOrEmpty(name))
                    {
                        var hotel = (from h in db.Hotels where h.HotelName == name select h).FirstOrDefault();
                        if (hotel == null)
                        {
                            var link = head.Select("a").Attr("href");
                            var istars = head.First.Children;
                            var star = "";
                            if (istars != null && istars.Count > 1 && istars[1].Text.Trim().ToLower().Contains("star"))
                            {
                                star = istars[1].Text.Trim().ToLower().Replace("-star hotel", "").Replace("stars", "").Replace("star", "");
                            }
                            
                            if (!String.IsNullOrEmpty(link))
                            {
                                var fullUrl = "http://www.booking.com" + link;

                                Uri url = new Uri(fullUrl);
                                string path = String.Format("{0}{1}{2}{3}", url.Scheme, Uri.SchemeDelimiter, url.Authority, url.AbsolutePath);

                                hotel = new Hotel();
                                hotel.HotelName = name;
                                hotel.BookingURL = path;
                                hotel.Stars = star != "" ? Int32.Parse(star): (int?) null;
                                hotel.CreatedOn = DateTime.Now;
                                hotel.PropertyTypeId = 1;
                                hotel.IsActive = true;
                                db.Hotels.Add(hotel);
                                db.SaveChanges();

                                Console.WriteLine("New hotel added: " + name);
                            }
                        }
                    }
                    
                }
            }
            Console.WriteLine("New hotel data download is completed");
        }

        private static void CrawleHotelInfo()
        {
            Console.WriteLine("Hotel data synchronization is stared");
            using (var db = new holidayslkEntities())
            {
                var hotels = (from h in db.Hotels where h.IsActive == true orderby !h.LastSyncOn.HasValue ? 1 : h.HotelId select h).ToList();
                hotels = hotels.Where(c => !c.LastSyncOn.HasValue).ToList();
                foreach (var hotel in hotels)
                {
                    var hotelUrl = hotel.BookingURL;

                    var doc = Dcsoup.Parse(new Uri(hotelUrl), 5000);

                    var checkInText = doc.Select("div[id=checkin_policy]").Text.Replace("Check-in", "").Trim();
                    var checkOutText = doc.Select("div[id=checkout_policy]").Text.Replace("Check-out", "").Trim();
                    
                    if (!String.IsNullOrEmpty(checkInText))
                    {
                        if (checkInText.Contains("-"))
                        {
                            var arrCheckIn = checkInText.Split('-').Select(c => c.Trim()).ToList();
                            var checkInFrom = arrCheckIn[0].Trim();
                            var checkInTo = arrCheckIn[1].Trim();

                            DateTime dateWithTime = DateTime.MinValue;
                            DateTime.TryParse(checkInFrom, out dateWithTime);
                            hotel.CheckInTimeFrom = dateWithTime.TimeOfDay;

                            dateWithTime = DateTime.MinValue;
                            DateTime.TryParse(checkInTo, out dateWithTime);
                            hotel.CheckInTimeTo = dateWithTime.TimeOfDay;
                        }
                        else
                        {
                            DateTime dateWithTime = DateTime.MinValue;
                            var formattedText = checkInText.Replace("From", "").Replace("Until", "").Trim();
                            DateTime.TryParse(formattedText, out dateWithTime);
                            hotel.CheckInTimeFrom = dateWithTime.TimeOfDay;

                            hotel.CheckInTimeTo = null;
                        }

                        hotel.CheckInText = checkInText;
                    }
                    else
                    {
                        hotel.CheckInTimeFrom = null;
                        hotel.CheckInTimeTo = null;
                        hotel.CheckInText = "";
                    }

                    if (!String.IsNullOrEmpty(checkOutText))
                    {
                        if (checkOutText.Contains("-"))
                        {
                            var arrCheckOut = checkOutText.Split('-');
                            var checkOutFrom = arrCheckOut[0].Trim();
                            var checkOutTo = arrCheckOut[1].Trim();

                            DateTime dateWithTime = DateTime.MinValue;
                            DateTime.TryParse(checkOutFrom, out dateWithTime);
                            hotel.CheckOutTimeFrom = dateWithTime.TimeOfDay;

                            dateWithTime = DateTime.MinValue;
                            DateTime.TryParse(checkOutTo, out dateWithTime);
                            hotel.CheckOutTimeTo = dateWithTime.TimeOfDay;
                        }
                        else
                        {
                            DateTime dateWithTime = DateTime.MinValue;
                            var formattedText = checkOutText.Replace("From", "").Replace("Until", "").Trim();
                            DateTime.TryParse(formattedText, out dateWithTime);
                            hotel.CheckOutTimeFrom = dateWithTime.TimeOfDay;

                            hotel.CheckOutTimeTo = null;
                        }

                        hotel.CheckOutText = checkOutText;
                    }
                    else
                    {
                        hotel.CheckOutTimeFrom = null;
                        hotel.CheckOutTimeTo = null;
                        hotel.CheckOutText = "";
                    }

                    var childrenpolicy =
                        doc.Select("div[id=children_policy]").Text.Replace("Children and Extra Beds", "").Trim();
                    hotel.ChildrenAndExtraBed = childrenpolicy.Trim();
                    db.SaveChanges();

                    var facilities =
                        doc.Select("div[id=hp_facilities_box]").Select("div[class=facilitiesChecklistSection]").ToList();
                    foreach (var facility in facilities)
                    {
                        var childrens = facility.Children.ToList();
                        var header = childrens[0].Text.Trim();
                        if (header == "Outdoors" || header == "General" || header == "Food & Drink")
                        {
                            for (int i = 1; i < childrens.Count; i++)
                            {
                                var child = childrens[i];
                                var facilityItems = child.Children.Select("li").ToList();

                                //var li = childrens[i].Text.Trim();
                                foreach (var item in facilityItems)
                                {
                                    var li = item.Text.Trim();
                                    var curFac = (from f in db.Facilities where f.FacilityName == li select f).FirstOrDefault();
                                    if (curFac == null)
                                    {
                                        var newFac = db.Facilities.Add(new Facility()
                                        {
                                            FacilityName = li
                                        });
                                        db.SaveChanges();

                                        curFac = newFac;
                                    }

                                    if (!(from hf in db.HotelFacilities
                                          where hf.HotelId == hotel.HotelId && hf.FacilityId == curFac.FacilityId
                                          select hf).Any())
                                    {
                                        db.HotelFacilities.Add(new HotelFacility()
                                        {
                                            FacilityId = curFac.FacilityId,
                                            HotelId = hotel.HotelId,
                                            IsActive = true,
                                            CreatedOn = DateTime.Now,
                                            UpdatedOn = DateTime.Now
                                        });
                                        db.SaveChanges();
                                    }
                                }

                                
                            }
                        }

                        if (header == "Activities")
                        {
                            for (int i = 1; i < childrens.Count; i++)
                            {
                              //  var li = childrens[i].Text.Trim();
                                var child = childrens[i];
                                var items = child.Children.Select("li").ToList();
                                foreach (var item in items)
                                {
                                    var li = item.Text.Trim();
                                    var curAct = (from f in db.Activities where f.ActivityName == li select f).FirstOrDefault();
                                    if (curAct == null)
                                    {
                                        var newAct = db.Activities.Add(new Activity()
                                        {
                                            ActivityName = li,
                                        });
                                        db.SaveChanges();

                                        curAct = newAct;
                                    }

                                    if (!(from hf in db.HotelActivities
                                          where hf.HotelId == hotel.HotelId && hf.ActivityId == curAct.ActivityId
                                          select hf).Any())
                                    {
                                        db.HotelActivities.Add(new HotelActivity()
                                        {
                                            ActivityId = curAct.ActivityId,
                                            HotelId = hotel.HotelId,
                                            IsActive = true,
                                            CreatedOn = DateTime.Now,
                                            UpdatedOn = DateTime.Now
                                        });
                                        db.SaveChanges();
                                    }
                                }

                                
                            }
                        }

                        if (header == "Internet")
                        {
                            for (int i = 1; i < childrens.Count; i++)
                            {
                                var li = childrens[i].Text.Trim();
                                hotel.Internet = li;
                            }
                        }

                        if (header == "Parking")
                        {
                            for (int i = 1; i < childrens.Count; i++)
                            {
                                var li = childrens[i].Text.Trim();
                                hotel.Parking = li;
                            }
                        }

                        if (header == "Pets")
                        {
                            for (int i = 1; i < childrens.Count; i++)
                            {
                                var li = childrens[i].Text.Trim();
                                hotel.Pets = li;
                            }
                        }
                    }


                    var address = doc.Select("span[class=show_map_icon]").First.SiblingElements.First.Text.Trim();
                    hotel.AddressLine = address;

                    var addressArr = address.Split(',').ToList();
                    var country = addressArr[addressArr.Count - 1].Trim();
                    hotel.Country = country.Trim();

                    var exCountry = (from c in db.Countries where c.CountryName==country.Trim() select c).FirstOrDefault();
                    if (exCountry == null)
                    {
                        exCountry = db.Countries.Add(new Country()
                        {
                            IsActive = true,
                            CountryName = country.Trim(),
                            CountryImage = ""
                        });
                        db.SaveChanges();
                    }

                    hotel.CountryId = exCountry.CountryId;

                    var cityWithCode = addressArr[addressArr.Count - 2].Trim();
                    var city = Regex.Replace(cityWithCode, @"[\d-]", string.Empty);
                    hotel.City = city.Trim();

                    var exCity = (from c in db.Cities where c.CityName == city.Trim() select c).FirstOrDefault();
                    if (exCity == null)
                    {
                        exCity = db.Cities.Add(new City()
                        {
                            IsActive = true,
                            CityName = city.Trim(),
                            CityImage = "",
                            CountryId = hotel.CountryId.Value
                        });
                        db.SaveChanges();
                    }

                    hotel.CityId = exCity.CityId;

                    var postalCode = Regex.Match(cityWithCode, @"\d+").Value;
                    hotel.PostalCode = postalCode.Trim();

                    hotel.Town = "";

                    var summary = doc.Select("div[id=summary]").Text.Replace("One of our top picks in Colombo.", "").Trim();
                    hotel.HotelDesc = summary;

                    hotel.HotelShortDesc = summary.Length > 130 ? summary.Substring(0, 130) : summary;


                    var photoWrapper = doc.Select("div[id=photo_wrapper]");
                    List<Element> photos = new List<Element>();
                    if (photoWrapper.First != null)
                    {
                        photos = photoWrapper.First.Children.Select("div[class=hp-gallery-slides]")
                            .First.Children.Take(10)
                            .ToList();
                    }
                    else
                    {
                        var mainContent = doc.Select("div[id=hotel_main_content]");
                        if (mainContent.First != null && mainContent.First.Children.Count > 0 && mainContent.First.Children.First != null)
                        {
                            var photoGrid = mainContent.First.Children.First.Children.Select("a");
                            photos = photoGrid.Select("img").ToList();
                        }
                       
                    }

                    foreach (var photo in photos)
                    {
                        var img = photo.Select("img");
                        var imgUrl = img.Attr("src").Trim();
                        if (String.IsNullOrEmpty(imgUrl))
                        {
                            imgUrl = img.Attr("data-lazy").Trim();
                        }
                       
                        if (!String.IsNullOrEmpty(imgUrl))
                        {
                            if (!(from i in db.HotelImages where i.HotelId == hotel.HotelId && i.ImageUrl == imgUrl select i).Any())
                            {
                                db.HotelImages.Add(new HotelImage()
                                {
                                    IsActive = true,
                                    HotelId = hotel.HotelId,
                                    ImageUrl = imgUrl,
                                    IsMain = false
                                });
                                db.SaveChanges();
                            }
                        }
                    }

                    hotel.LastSyncOn = DateTime.Now;
                    db.SaveChanges();


                    Console.WriteLine("Sync hotel: " + hotel.HotelName);
                }
            }

            Console.WriteLine("Hotel data synchronization is completed");
            Console.WriteLine("Enter any key to exit");
            Console.ReadKey();
        }

        private static string GetTownByPostalCode(string code)
        {
            var town = "";
            switch (code)
            {
                case "00100":
                    town = "Fort";
                    break;
                case "00200":
                    town = "Slave Island";
                    break;
                case "00300":
                    town = "Colpetty";
                    break;
                case "00400":
                    town = "Bambalapitiya";
                    break;
                case "00500":
                    town = "Narahenpita";
                    break;
                case "00600":
                    town = "Wellawatte";
                    break;
                case "00700":
                    town = "Cinnamon Gardens";
                    break;
                case "00800":
                    town = "Borella";
                    break;
                case "00900":
                    town = "Dematagoda";
                    break;
                case "01000":
                    town = "Maradana";
                    break;
                case "01100":
                    town = "Pettah";
                    break;
                case "01200":
                    town = "Hultsdorf";
                    break;
                case "01300":
                    town = "Kotahena";
                    break;
                case "01400":
                    town = "Modera";
                    break;
                case "01500":
                    town = "Mutwal";
                    break;
                default:
                    town = "";
                    break;
            }

            return town;
        }
    }
}
