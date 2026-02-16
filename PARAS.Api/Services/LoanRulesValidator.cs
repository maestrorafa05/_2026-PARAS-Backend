using Microsoft.Extensions.Options;
using PARAS.Api.Options;

namespace PARAS.Api.Services;

public class LoanRulesValidator
{
    private readonly BookingRulesOptions _rules;

    public LoanRulesValidator(IOptions<BookingRulesOptions> options)
    {
        _rules = options.Value;
    }

    public List<string> Validate(DateTime start, DateTime end, DateTime nowLocal)
    {
        var errors = new List<string>();

        if (end <= start)
            errors.Add("EndTime harus lebih besar dari StartTime.");

        var duration = (end - start).TotalMinutes;
        if (duration < _rules.MinDurationMinutes)
            errors.Add($"Durasi minimal {_rules.MinDurationMinutes} menit.");
        if (duration > _rules.MaxDurationMinutes)
            errors.Add($"Durasi maksimal {_rules.MaxDurationMinutes} menit.");
            
        // tidak boleh booking masa lalu
        if (start < nowLocal.AddMinutes(_rules.MinLeadMinutes))
            errors.Add($"StartTime minimal {_rules.MinLeadMinutes} menit dari sekarang.");

        // batas maksimal advance
        if (start.Date > nowLocal.Date.AddDays(_rules.MaxAdvanceDays))
            errors.Add($"Booking maksimal H+{_rules.MaxAdvanceDays} hari.");

        // jam operasional
        var startT = TimeOnly.FromDateTime(start);
        var endT = TimeOnly.FromDateTime(end);

        if (startT < _rules.OpenTime || endT > _rules.CloseTime)
            errors.Add($"Booking hanya boleh antara {_rules.OpenTime:HH:mm} - {_rules.CloseTime:HH:mm}.");

        // weekend rule
        if (!_rules.AllowWeekend){
            if (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
                end.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                errors.Add("Booking tidak diperbolehkan di akhir pekan.");
        }

        return errors;
    }
}
