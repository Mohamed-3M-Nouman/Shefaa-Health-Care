using System.ComponentModel.DataAnnotations;

namespace ShefaaHealthCare.Models.Validation
{
    public class PastDateAttribute : ValidationAttribute
    {
        public PastDateAttribute()
        {
            ErrorMessage = "تاريخ الميلاد يجب أن يكون في الماضي";
        }

        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
                return date.Date < DateTime.Today;

            return false;
        }
    }
}
