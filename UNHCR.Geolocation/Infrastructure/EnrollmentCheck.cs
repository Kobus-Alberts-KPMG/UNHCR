using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UNHCR.Geolocation.Infrastructure
{

    public class EnrollmentCheck    {
        public string odatacontext { get; set; }
        public List<CountryTerritories> value { get; set; }
    }

    public class CountryTerritories
    {
        public string odataetag { get; set; }
        public int statuscode { get; set; }
        public bool progres_isenrollmentopen { get; set; }
        public string _modifiedby_value { get; set; }
        public object _modifiedonbehalfby_value { get; set; }
        public int versionnumber { get; set; }
        public string _createdby_value { get; set; }
        public string _owningbusinessunit_value { get; set; }
        public object _createdonbehalfby_value { get; set; }
        public string progres_buenrollmentid { get; set; }
        public DateTime modifiedon { get; set; }
        public int progres_currentregistrationcount { get; set; }
        public int progres_registrationlimit { get; set; }
        public string _ownerid_value { get; set; }
        public int statecode { get; set; }
        public int progres_blockregistration { get; set; }
        public string _owninguser_value { get; set; }
        public object overriddencreatedon { get; set; }
        public object importsequencenumber { get; set; }
        public string progres_name { get; set; }
        public object _owningteam_value { get; set; }
        public string _progres_portalcountry_value { get; set; }
        public DateTime createdon { get; set; }
        public object utcconversiontimezonecode { get; set; }
        public object timezoneruleversionnumber { get; set; }
        public string _progres_businessunit_value { get; set; }
        public List<Progres_Progres_Buenrollment_Progres_Countryterri> progres_progres_buenrollment_progres_countryterri { get; set; }
        public string progres_progres_buenrollment_progres_countryterriodatanextLink { get; set; }
    }

    public class Progres_Progres_Buenrollment_Progres_Countryterri
    {
        public string odataetag { get; set; }
        public object progres_timezone { get; set; }
        public object progres_timezoneformat { get; set; }
        public DateTime createdon { get; set; }
        public string progres_name { get; set; }
        public string _ownerid_value { get; set; }
        public string _createdby_value { get; set; }
        public object timezoneruleversionnumber { get; set; }
        public int versionnumber { get; set; }
        public object _createdonbehalfby_value { get; set; }
        public string progres_isocode2 { get; set; }
        public string progres_countryterritoryid { get; set; }
        public int statuscode { get; set; }
        public int importsequencenumber { get; set; }
        public string progres_progresguid { get; set; }
        public string _owningbusinessunit_value { get; set; }
        public string _modifiedby_value { get; set; }
        public object _modifiedonbehalfby_value { get; set; }
        public object _progres_defaultcontactowningteam_value { get; set; }
        public string progres_isocode3 { get; set; }
        public int statecode { get; set; }
        public DateTime modifiedon { get; set; }
        public string _owninguser_value { get; set; }
        public object overriddencreatedon { get; set; }
        public object _owningteam_value { get; set; }
        public object utcconversiontimezonecode { get; set; }
    }

}
