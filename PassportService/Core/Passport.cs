namespace PassportService.Core
{
    public class Passport
    {
        public int Id { get; set; }           
        public string Series { get; set; }    
        public string Number { get; set; }     
        public DateTime CreatedAt { get; set; } 
        public DateTime? RemovedAt { get; set; }
    }
}
