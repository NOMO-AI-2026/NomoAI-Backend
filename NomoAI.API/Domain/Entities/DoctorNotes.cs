using NomoAI.API.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NomoDoc.Domain.Entities
{
    public class DoctorNotes:BaseEntity<int>
    {
        public int DoctorId   { get; set; }

        public int ChildId { get; set; }

        public string NoteTitle { get; set; }

        public string NoteContent { get; set; }

        public Doctor Doctor { get; set; } = null!;

        public Children Child { get; set; } = null!;


    }
}
