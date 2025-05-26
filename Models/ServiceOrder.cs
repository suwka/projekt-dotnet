using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WorkshopManager.Models
{
    public enum ServiceOrderStatus
    {
        Nowe,
        WTrakcie,
        Zakonczone,
        Anulowane
    }

    public class ServiceOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public ServiceOrderStatus Status { get; set; }

        // Mechanik przypisany do zlecenia
        public string AssignedMechanicId { get; set; }
        public IdentityUser AssignedMechanic { get; set; }

        // Powiązanie z pojazdem
        [Required]
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }

        // Lista czynności serwisowych
        public ICollection<ServiceTask> ServiceTasks { get; set; }

        // Lista komentarzy
        public ICollection<Comment> Comments { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }
}

