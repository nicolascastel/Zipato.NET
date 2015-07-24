﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVMS.Zipato.Model
{
    public class AlarmZone
    {
        public string Link { get; set; }
        public string Name { get; set; }
        public Guid Uuid { get; set; }
        public AlarmZoneStatus Status { get; set; }
    }
}
