﻿using keyraces.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace keyraces.Server.Dtos
{
    public class CreateAchievementDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public AchievementKey Key { get; set; }
        public string? IconCssClass { get; set; }
    }
}
