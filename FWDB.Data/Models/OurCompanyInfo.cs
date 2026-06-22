using System;
using System.Collections.Generic;

namespace StandOrder.Models;

public partial class OurCompanyInfo
{
    public int SetupId { get; set; }

    public string? CompanyName { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? StateOrProvince { get; set; }

    public string? Zipcode { get; set; }

    public string? PhoneNumber { get; set; }

    public string? FaxNumber { get; set; }

    public string? WholesaleLicenseNumber { get; set; }

    public string? Email { get; set; }

    public string? CompanyNameLine2 { get; set; }

    public string? WebSite { get; set; }

    public decimal? SalesTaxRate { get; set; }
}
