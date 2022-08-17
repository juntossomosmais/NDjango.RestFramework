﻿using System;
using AspNetCore.RestFramework.Core.Base;

namespace AspNetRestFramework.Sample.DTO
{
    public class SellerDto : BaseDto<Guid>
    {
        public string Name { get; set; }
    }
}
