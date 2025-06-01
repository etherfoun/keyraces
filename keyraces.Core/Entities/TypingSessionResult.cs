using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace keyraces.Core.Entities
{
    public class TypingSessionResult
    {
        [Key]
        public int Id { get; set; }

        public int TypingSessionId { get; set; } 
        [ForeignKey("TypingSessionId")]
        public virtual TypingSession TypingSession { get; set; }

        public int UserProfileId { get; set; }
        [ForeignKey("UserProfileId")]
        public virtual UserProfile UserProfile { get; set; } 

        public double WPM { get; set; } 
        public double Accuracy { get; set; } 
        public int ErrorsCount { get; set; }
        public TimeSpan Duration { get; set; } 

        public DateTime CalculatedAtUtc { get; set; } 
    }
}
