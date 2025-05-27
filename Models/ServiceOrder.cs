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
        [StringLength(450)]
        public string? AssignedMechanicId { get; set; }
        public IdentityUser? AssignedMechanic { get; set; }

        // Powiązanie z pojazdem
        [Required]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        // Lista czynności serwisowych
        public ICollection<ServiceTask> ServiceTasks { get; set; } = new List<ServiceTask>();

        // Lista komentarzy
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();

        [Required]
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        [Required]
        [MaxLength(1000)]
        public string ProblemDescription { get; set; } = string.Empty;
    }
}

