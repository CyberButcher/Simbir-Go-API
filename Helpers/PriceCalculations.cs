using static Simbir_GO_Api.MyDBContext;

namespace Simbir_GO_Api.Helpers
{
    public class PriceCalculations
    {
        public static double CalculateFinalRentalPrice(Rental rental) 
        {
            double timeRate = rental.PriceOfUnit;
            double finalPrice = 0.0;

            DateTime startTime = DateTime.Parse(rental.TimeStart);
            DateTime endTime = DateTime.Parse(rental.TimeEnd);

            if (rental.PriceType == "Days")
            {
                int rentalDurationInDays = (int)(endTime - startTime).TotalDays;

                rentalDurationInDays = Math.Max(rentalDurationInDays, 1);

                finalPrice = timeRate * rentalDurationInDays;
            }
            else
            {
                double rentalDurationInMinutes = (endTime - startTime).TotalMinutes;
                finalPrice = timeRate * rentalDurationInMinutes;
            }

            return finalPrice;
        }
    }
}
