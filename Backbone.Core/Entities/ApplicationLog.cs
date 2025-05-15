// Backbone.Infrastructure/Entities/ApplicationLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backbone.Infrastructure.Entities
{
    [Table("application_logs")]
    public class ApplicationLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_name")]
        public string UserName { get; set; }
        [Column("user_roles")]
        public string UserRoles { get; set; }  // Comma-separated roles
        [Column("jwt_token_id")]
        public string JwtTokenId { get; set; } // Stores the jti claim
        [Column("jwt_token_expiry")]
        public DateTime? TokenExpiry { get; set; }

        [Column("project_name")]
        public string ProjectName { get; set; }

        [Column("source_file")]
        public string SourceFile { get; set; }

        [Column("method_name")]
        public string MethodName { get; set; }

        [Column("line_number")]
        public int? LineNumber { get; set; }

        [Column("message")]
        public string Message { get; set; }

        [Column("message_template")]
        public string MessageTemplate { get; set; }

        [Column("level")]
        public string Level { get; set; }

        [Column("timestamp")]
        public DateTime Timestamp { get; set; }

        [Column("exception")]
        public string Exception { get; set; }

        [Column("properties", TypeName = "jsonb")]
        public string Properties { get; set; }

        [Column("log_event")]
        public string LogEvent { get; set; }

    }
}