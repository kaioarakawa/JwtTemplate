using System;
using System.ComponentModel.DataAnnotations;

namespace JwtTemlate.Models.DTO
{
	public class RegistrationModel
	{
        [Required]
        public string? Name { get; set; }
        [Required]
		public string? Username { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}

