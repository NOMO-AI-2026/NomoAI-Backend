using NomoAI.API.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace NomoAI.API.Domain.Entities
{
    public class PaymentMethod
    {
        [Key]
        public required string Id { get; set; }
        
        public  string? Name { get; set; }

        public required PaymentMethods PaymentMethodType { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
