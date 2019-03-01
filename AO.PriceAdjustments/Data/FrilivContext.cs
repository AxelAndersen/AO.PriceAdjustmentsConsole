using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Data
{
    public class FrilivContext : DbContext
    {
        private IConfiguration _config;
    }
}
