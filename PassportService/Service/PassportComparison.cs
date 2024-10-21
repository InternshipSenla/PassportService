using PassportService.Core;

namespace PassportService.Service
{
    public class PassportComparison
    {
        public string Series { get; set; }
        public string Number { get; set; }

        public override bool Equals(object obj)
        {
            if(obj is PassportComparison other)
            {
                return Series == other.Series && Number == other.Number;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Series, Number);
        }

        public static Passport ConvertPassportComparisonToPassport(PassportComparison passportComparison)
        {
            return new Passport
            {
                Series = passportComparison.Series,
                Number = passportComparison.Number,
            };
        }
    }
}
