using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bmg.ConsigBoilerplate.Database.Entities.v1
{
    [Table("tbl_wth", Schema = "teste")]
    public record Weather
    {
        /// <summary>
        /// wth_id
        /// </summary>
        [Key]
        [Column("wth_id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; init; }

        /// <summary>
        /// wth_date
        /// </summary>
        [Column("wth_date")]
        public DateTime Date { get; init; }

        /// <summary>
        /// wth_temp
        /// </summary>
        [Column("wth_temp")]
        public int TemperatureC { get; init; }

        /// <summary>
        /// wth_summ
        /// </summary>
        [Column("wth_summ")]
        public string? Summary { get; init; }
    }
}
