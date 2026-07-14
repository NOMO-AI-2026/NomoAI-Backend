using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoAI.API.Domain.Entities
{
    public class SpeechLevel:BaseEntity<int>
    {
        public string LevelName { get; set; }
        public string Description { get; set; }
    }
}
