using System.Data;

namespace PassportService.Core
{
    public class Passport
    {
        public int Id { get; set; }           
        public string Series { get; set; }    
        public string Number { get; set; }     
        public List<DateTime> CreatedAt { get; set; } 
        public List<DateTime?>? RemovedAt { get; set; }
        public DateTime DateLastRequest { get; set; }
    }
}
